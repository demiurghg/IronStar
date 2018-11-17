
#include "Local.h"


Native::Recast::RCMesh::RCMesh( RCBuildConfig ^config, array<Vector3>^ vertices, array<int>^ indices )
{
	m_ctx		=	0;
	m_triareas	=	0;
	m_solid		=	0;
	m_chf		=	0;
	m_cset		=	0;
	m_pmesh		=	0;
	m_dmesh		=	0;

	m_keepInterResults	=	false;

	if (config	 == nullptr) throw gcnew System::ArgumentNullException("config");
	if (vertices == nullptr) throw gcnew System::ArgumentNullException("vertices");
	if (indices  == nullptr) throw gcnew System::ArgumentNullException("indices");

	Cleanup();

	m_ctx	=	new BuildContext();

	//------------------------------------------------------------

	auto bbox = config->BBox;

	const float bmin[] = { bbox.Minimum.X, bbox.Minimum.Y, bbox.Minimum.Z };
	const float bmax[] = { bbox.Maximum.X, bbox.Maximum.Y, bbox.Maximum.Z };

	pin_ptr<Vector3> managedVerts = &vertices[0];
	pin_ptr<int>	 managedInds  = &indices[0];

	const float* verts	= reinterpret_cast<float*>(managedVerts);
	const int*   tris	= reinterpret_cast<int*>  (managedInds);
	const int nverts	= vertices->Length;
	const int ntris		= indices->Length / 3;

	//------------------------------------------------------------

	#pragma region Step 1. Initialize build config.

	// Init build configuration from GUI
	m_cfg = new rcConfig();
	memset(m_cfg, 0, sizeof(m_cfg));
	m_cfg->cs						= config->CellSize;
	m_cfg->ch						= config->CellHeight;
	m_cfg->walkableSlopeAngle		= config->WalkableSlopeAngle;
	m_cfg->walkableHeight			= (int)ceilf(config->WalkableHeight / m_cfg->ch);
	m_cfg->walkableClimb			= (int)floorf(config->WalkableClimb / m_cfg->ch);
	m_cfg->walkableRadius			= (int)ceilf(config->WalkableRadius / m_cfg->cs);
	m_cfg->maxEdgeLen				= (int)(config->MaxEdgeLen / config->CellSize);
	m_cfg->maxSimplificationError	= config->MaxSimplificationError;
	m_cfg->minRegionArea			= (int)rcSqr(config->MinRegionSize);		// Note: area = size*size
	m_cfg->mergeRegionArea			= (int)rcSqr(config->MergeRegionSize);	// Note: area = size*size
	m_cfg->maxVertsPerPoly			= (int)config->MaxVertsPerPoly;
	m_cfg->detailSampleDist			= config->DetailSampleDist < 0.9f ? 0 : config->CellSize * config->DetailSampleDist;
	m_cfg->detailSampleMaxError		= config->CellHeight * config->DetailSampleMaxError;

	// Set the area where the navigation will be build.
	// Here the bounds of the input mesh are used, but the
	// area could be specified by an user defined box, etc.
	rcVcopy(m_cfg->bmin, bmin);
	rcVcopy(m_cfg->bmax, bmax);
	rcCalcGridSize(m_cfg->bmin, m_cfg->bmax, m_cfg->cs, &m_cfg->width, &m_cfg->height);

	// Reset build times gathering.
	m_ctx->resetTimers();

	// Start the build process.	
	m_ctx->startTimer(RC_TIMER_TOTAL);

	m_ctx->log(RC_LOG_PROGRESS, "Building navigation:");
	m_ctx->log(RC_LOG_PROGRESS, " - %d x %d cells", m_cfg->width, m_cfg->height);
	m_ctx->log(RC_LOG_PROGRESS, " - %.1fK verts, %.1fK tris", nverts / 1000.0f, ntris / 1000.0f);

	#pragma endregion 

	//------------------------------------------------------------

	#pragma region Rasterize input polygon soup.

	// Allocate voxel heightfield where we rasterize our input data to.
	m_solid = rcAllocHeightfield();
	if (!m_solid)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'solid'.");
		throw gcnew System::Exception();
	}
	if (!rcCreateHeightfield(m_ctx, *m_solid, m_cfg->width, m_cfg->height, m_cfg->bmin, m_cfg->bmax, m_cfg->cs, m_cfg->ch))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not create solid heightfield.");
		throw gcnew System::Exception();
	}

	// Allocate array that can hold triangle area types.
	// If you have multiple meshes you need to process, allocate
	// and array which can hold the max number of triangles you need to process.
	m_triareas = new unsigned char[ntris];
	if (!m_triareas)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'm_triareas' (%d).", ntris);
		throw gcnew System::Exception();
	}

	// Find triangles which are walkable based on their slope and rasterize them.
	// If your input data is multiple meshes, you can transform them here, calculate
	// the are type for each of the meshes and rasterize them.
	memset(m_triareas, 0, ntris * sizeof(unsigned char));
	rcMarkWalkableTriangles(m_ctx, m_cfg->walkableSlopeAngle, verts, nverts, tris, ntris, m_triareas);
	if (!rcRasterizeTriangles(m_ctx, verts, nverts, tris, m_triareas, ntris, *m_solid, m_cfg->walkableClimb))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not rasterize triangles.");
		throw gcnew System::Exception();
	}

	if (!m_keepInterResults)
	{
		delete[] m_triareas;
		m_triareas = 0;
	}

	#pragma endregion 

	//------------------------------------------------------------

	#pragma region Step 3. Filter walkables surfaces.

	// Once all geoemtry is rasterized, we do initial pass of filtering to
	// remove unwanted overhangs caused by the conservative rasterization
	// as well as filter spans where the character cannot possibly stand.
	if (config->FilterLowHangingWalkableObjects) {
		rcFilterLowHangingWalkableObstacles(m_ctx, m_cfg->walkableClimb, *m_solid);
	}
	if (config->FilterLedgeSpans) {
		rcFilterLedgeSpans(m_ctx, m_cfg->walkableHeight, m_cfg->walkableClimb, *m_solid);
	}
	if (config->FilterWalkableLowHeightSpans) {
		rcFilterWalkableLowHeightSpans(m_ctx, m_cfg->walkableHeight, *m_solid);
	}

	#pragma endregion 

	//------------------------------------------------------------

	#pragma region Step 4. Partition walkable surface to simple regions.

	// Compact the heightfield so that it is faster to handle from now on.
	// This will result more cache coherent data as well as the neighbours
	// between walkable cells will be calculated.
	m_chf = rcAllocCompactHeightfield();
	if (!m_chf)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'chf'.");
		throw gcnew System::Exception();
	}
	if (!rcBuildCompactHeightfield(m_ctx, m_cfg->walkableHeight, m_cfg->walkableClimb, *m_solid, *m_chf))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build compact data.");
		throw gcnew System::Exception();
	}

	if (!m_keepInterResults)
	{
		rcFreeHeightField(m_solid);
		m_solid = 0;
	}

	// Erode the walkable area by agent radius.
	if (!rcErodeWalkableArea(m_ctx, m_cfg->walkableRadius, *m_chf))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not erode.");
		throw gcnew System::Exception();
	}

	#pragma message ( "ENABLE CONVEX VOLUMES!!!" )
	#if 0
	// (Optional) Mark areas.
	const ConvexVolume* vols = m_geom->getConvexVolumes();
	for (int i = 0; i < m_geom->getConvexVolumeCount(); ++i)
		rcMarkConvexPolyArea(m_ctx, vols[i].verts, vols[i].nverts, vols[i].hmin, vols[i].hmax, (unsigned char)vols[i].area, *m_chf);
	#endif


	// Partition the heightfield so that we can use simple algorithm later to triangulate the walkable areas.
	// There are 3 martitioning methods, each with some pros and cons:
	// 1) Watershed partitioning
	//   - the classic Recast partitioning
	//   - creates the nicest tessellation
	//   - usually slowest
	//   - partitions the heightfield into nice regions without holes or overlaps
	//   - the are some corner cases where this method creates produces holes and overlaps
	//      - holes may appear when a small obstacles is close to large open area (triangulation can handle this)
	//      - overlaps may occur if you have narrow spiral corridors (i.e stairs), this make triangulation to fail
	//   * generally the best choice if you precompute the nacmesh, use this if you have large open areas
	// 2) Monotone partioning
	//   - fastest
	//   - partitions the heightfield into regions without holes and overlaps (guaranteed)
	//   - creates long thin polygons, which sometimes causes paths with detours
	//   * use this if you want fast navmesh generation
	// 3) Layer partitoining
	//   - quite fast
	//   - partitions the heighfield into non-overlapping regions
	//   - relies on the triangulation code to cope with holes (thus slower than monotone partitioning)
	//   - produces better triangles than monotone partitioning
	//   - does not have the corner cases of watershed partitioning
	//   - can be slow and create a bit ugly tessellation (still better than monotone)
	//     if you have large open areas with small obstacles (not a problem if you use tiles)
	//   * good choice to use for tiled navmesh with medium and small sized tiles

	if (config->Partition == RCPartition::Watershed)
	{
		// Prepare for region partitioning, by calculating distance field along the walkable surface.
		if (!rcBuildDistanceField(m_ctx, *m_chf))
		{
			m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build distance field.");
			throw gcnew System::Exception();
		}

		// Partition the walkable surface into simple regions without holes.
		if (!rcBuildRegions(m_ctx, *m_chf, 0, m_cfg->minRegionArea, m_cfg->mergeRegionArea))
		{
			m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build watershed regions.");
			throw gcnew System::Exception();
		}
	}
	else if (config->Partition == RCPartition::Monotone)
	{
		// Partition the walkable surface into simple regions without holes.
		// Monotone partitioning does not need distancefield.
		if (!rcBuildRegionsMonotone(m_ctx, *m_chf, 0, m_cfg->minRegionArea, m_cfg->mergeRegionArea))
		{
			m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build monotone regions.");
			throw gcnew System::Exception();
		}
	}
	else // SAMPLE_PARTITION_LAYERS
	{
		// Partition the walkable surface into simple regions without holes.
		if (!rcBuildLayerRegions(m_ctx, *m_chf, 0, m_cfg->minRegionArea))
		{
			m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build layer regions.");
			throw gcnew System::Exception();
		}
	}

	#pragma endregion 

	//------------------------------------------------------------

	#pragma region Step 5. Trace and simplify region contours.

	// Create contours.
	m_cset = rcAllocContourSet();
	if (!m_cset)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'cset'.");
		throw gcnew System::Exception();
	}
	if (!rcBuildContours(m_ctx, *m_chf, m_cfg->maxSimplificationError, m_cfg->maxEdgeLen, *m_cset))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not create contours.");
		throw gcnew System::Exception();
	}

	#pragma endregion 

	//------------------------------------------------------------

	#pragma region Step 6. Build polygons mesh from contours.

	// Build polygon navmesh from the contours.
	m_pmesh = rcAllocPolyMesh();
	if (!m_pmesh)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'pmesh'.");
		throw gcnew System::Exception();
	}
	if (!rcBuildPolyMesh(m_ctx, *m_cset, m_cfg->maxVertsPerPoly, *m_pmesh))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not triangulate contours.");
		throw gcnew System::Exception();
	}

	#pragma endregion 

	//------------------------------------------------------------

	#pragma region  Step 7. Create detail mesh which allows to access approximate height on each polygon.

	m_dmesh = rcAllocPolyMeshDetail();
	if (!m_dmesh)
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Out of memory 'pmdtl'.");
		throw gcnew System::Exception();
	}

	if (!rcBuildPolyMeshDetail(m_ctx, *m_pmesh, *m_chf, m_cfg->detailSampleDist, m_cfg->detailSampleMaxError, *m_dmesh))
	{
		m_ctx->log(RC_LOG_ERROR, "buildNavigation: Could not build detail mesh.");
		throw gcnew System::Exception();
	}

	if (!m_keepInterResults)
	{
		rcFreeCompactHeightfield(m_chf);
		m_chf = 0;
		rcFreeContourSet(m_cset);
		m_cset = 0;
	}

	// At this point the navigation mesh data is ready, you can access it from m_pmesh.
	// See duDebugDrawPolyMesh or dtCreateNavMeshData as examples how to access the data.

	#pragma endregion


	m_ctx->stopTimer(RC_TIMER_TOTAL);

	// Show performance stats.
	//duLogBuildTimes(*m_ctx, m_ctx->getAccumulatedTime(RC_TIMER_TOTAL));
	m_ctx->log(RC_LOG_PROGRESS, ">> Polymesh: %d vertices  %d polygons", m_pmesh->nverts, m_pmesh->npolys);

	//m_totalBuildTimeMs = m_ctx->getAccumulatedTime(RC_TIMER_TOTAL) / 1000.0f;*/
}



Native::Recast::RCMesh::~RCMesh()
{
	Cleanup();
}


cli::array<Vector3>^ Native::Recast::RCMesh::GetPolyMeshVertices()
{
	auto mesh	= m_pmesh;
	auto orig	= mesh->bmin;
	auto ch		= mesh->ch;
	auto cs		= mesh->cs;

	array<Vector3> ^verts = gcnew array<Vector3>( mesh->nverts );

	for (int i = 0; i < mesh->nverts; ++i) {

		const unsigned short* v = &mesh->verts[i * 3];
		const float x = orig[0] + v[0] * cs;
		const float y = orig[1] + (v[1] + 1)*ch + 0.1f;
		const float z = orig[2] + v[2] * cs;

		verts[i] = Vector3( x, y, z );
	}

	return verts;
}


void Native::Recast::RCMesh::Cleanup()
{
	delete[] m_triareas;
	m_triareas = 0;
	rcFreeHeightField(m_solid);
	m_solid = 0;
	rcFreeCompactHeightfield(m_chf);
	m_chf = 0;
	rcFreeContourSet(m_cset);
	m_cset = 0;
	rcFreePolyMesh(m_pmesh);
	m_pmesh = 0;
	rcFreePolyMeshDetail(m_dmesh);
	m_dmesh = 0;
}

