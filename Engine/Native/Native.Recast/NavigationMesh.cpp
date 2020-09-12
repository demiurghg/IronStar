
#include "Local.h"

using namespace Fusion;
using namespace Fusion::Core::Mathematics;


Native::NRecast::NavigationMesh::NavigationMesh( BuildConfig ^config, array<Vector3>^ vertices, array<int>^ indices, array<bool>^ walkables )
{
	m_ctx			=	0;
	m_triareas		=	0;
	m_solid			=	0;
	m_chf			=	0;
	m_cset			=	0;
	m_pmesh			=	0;
	m_dmesh			=	0;
	m_navMesh		=	0;
	m_queryFilter	=	0;

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

	for (int i=0; i<ntris; i++)
	{
		m_triareas[i] = walkables[i] ? m_triareas[i] : (unsigned char)0;
	}

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

	if (config->Partition == PartitionMethod::Watershed)
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
	else if (config->Partition == PartitionMethod::Monotone)
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

	//------------------------------------------------------------

	#pragma region  Step 8. Create Detour data from Recast poly mesh.

	unsigned char* navData = 0;
	int navDataSize = 0;

	// Update poly flags from areas.
	for (int i = 0; i < m_pmesh->npolys; ++i)
	{
		if (m_pmesh->areas[i] == RC_WALKABLE_AREA)
			m_pmesh->areas[i] = SAMPLE_POLYAREA_GROUND;

		if (m_pmesh->areas[i] == SAMPLE_POLYAREA_GROUND ||
			m_pmesh->areas[i] == SAMPLE_POLYAREA_GRASS ||
			m_pmesh->areas[i] == SAMPLE_POLYAREA_ROAD)
		{
			m_pmesh->flags[i] = SAMPLE_POLYFLAGS_WALK;
		}
		else if (m_pmesh->areas[i] == SAMPLE_POLYAREA_WATER)
		{
			m_pmesh->flags[i] = SAMPLE_POLYFLAGS_SWIM;
		}
		else if (m_pmesh->areas[i] == SAMPLE_POLYAREA_DOOR)
		{
			m_pmesh->flags[i] = SAMPLE_POLYFLAGS_WALK | SAMPLE_POLYFLAGS_DOOR;
		}
	}


	dtNavMeshCreateParams params;
	memset(&params, 0, sizeof(params));
	params.verts				= m_pmesh->verts;
	params.vertCount			= m_pmesh->nverts;
	params.polys				= m_pmesh->polys;
	params.polyAreas			= m_pmesh->areas;
	params.polyFlags			= m_pmesh->flags;
	params.polyCount			= m_pmesh->npolys;
	params.nvp					= m_pmesh->nvp;
	params.detailMeshes			= m_dmesh->meshes;
	params.detailVerts			= m_dmesh->verts;
	params.detailVertsCount		= m_dmesh->nverts;
	params.detailTris			= m_dmesh->tris;
	params.detailTriCount		= m_dmesh->ntris;
	#pragma message ("Off-mesh connections")
	#if 0
	params.offMeshConVerts		= m_geom->getOffMeshConnectionVerts();
	params.offMeshConRad		= m_geom->getOffMeshConnectionRads();
	params.offMeshConDir		= m_geom->getOffMeshConnectionDirs();
	params.offMeshConAreas		= m_geom->getOffMeshConnectionAreas();
	params.offMeshConFlags		= m_geom->getOffMeshConnectionFlags();
	params.offMeshConUserID		= m_geom->getOffMeshConnectionId();
	params.offMeshConCount		= m_geom->getOffMeshConnectionCount();
	#endif
	params.walkableHeight		= config->WalkableHeight;
	params.walkableRadius		= config->WalkableRadius;
	params.walkableClimb		= config->WalkableClimb;
	rcVcopy(params.bmin, m_pmesh->bmin);
	rcVcopy(params.bmax, m_pmesh->bmax);
	params.cs					= m_cfg->cs;
	params.ch					= m_cfg->ch;
	params.buildBvTree = true;

	if (!dtCreateNavMeshData(&params, &navData, &navDataSize)) {
		m_ctx->log(RC_LOG_ERROR, "Could not build Detour navmesh.");
		throw gcnew System::Exception();
	}

	m_navMesh = dtAllocNavMesh();
	if (!m_navMesh) {
		dtFree(navData);
		m_ctx->log(RC_LOG_ERROR, "Could not create Detour navmesh");
		throw gcnew System::Exception();
	}

	dtStatus status;

	status = m_navMesh->init(navData, navDataSize, DT_TILE_FREE_DATA);
	if (dtStatusFailed(status)) {
		dtFree(navData);
		m_ctx->log(RC_LOG_ERROR, "Could not init Detour navmesh");
		throw gcnew System::Exception();
	}

	m_navQuery = dtAllocNavMeshQuery();
	status = m_navQuery->init(m_navMesh, 2048);
	if (dtStatusFailed(status))	{
		m_ctx->log(RC_LOG_ERROR, "Could not init Detour navmesh query");
		throw gcnew System::Exception();
	}

	#pragma endregion

	//------------------------------------------------------------

	m_queryFilter	=	new dtQueryFilter();

	//------------------------------------------------------------

	m_ctx->stopTimer(RC_TIMER_TOTAL);

	// Show performance stats.
	//duLogBuildTimes(*m_ctx, m_ctx->getAccumulatedTime(RC_TIMER_TOTAL));
	m_ctx->log(RC_LOG_PROGRESS, ">> Polymesh: %d vertices  %d polygons", m_pmesh->nverts, m_pmesh->npolys);

	//m_totalBuildTimeMs = m_ctx->getAccumulatedTime(RC_TIMER_TOTAL) / 1000.0f;*/
}


void Native::NRecast::NavigationMesh::Cleanup()
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

	dtFreeNavMesh(m_navMesh);
	m_navMesh = 0;

	dtFreeNavMeshQuery(m_navQuery);
	m_navQuery = 0;

	delete m_queryFilter;
	m_queryFilter = 0;
}


Native::NRecast::NavigationMesh::~NavigationMesh()
{
	Cleanup();
}


cli::array<Vector3>^ Native::NRecast::NavigationMesh::GetPolyMeshVertices()
{
	auto mesh	= m_pmesh;
	auto orig	= mesh->bmin;
	auto ch		= mesh->ch;
	auto cs		= mesh->cs;

	array<Vector3> ^verts = gcnew array<Vector3>( mesh->nverts );

	for (int i = 0; i < mesh->nverts; ++i) {

		const unsigned short* v = &mesh->verts[i * 3];
		const float x = orig[0] + v[0] * cs;
		const float y = orig[1] + v[1] * ch;
		const float z = orig[2] + v[2] * cs;

		verts[i] = Vector3( x, y, z );
	}

	return verts;
}


int Native::NRecast::NavigationMesh::GetPolygonVertexIndices(int polyIndex, array<int> ^indices)
{
	auto npolys	= m_pmesh->npolys;
	auto nvp = m_pmesh->nvp;

	if (polyIndex<0 || polyIndex>=npolys) {
		throw gcnew System::ArgumentOutOfRangeException("polyIndex");
	}
	if (indices==nullptr) {
		throw gcnew System::ArgumentNullException("indices");
	}
	if (indices->Length<nvp) {
		throw gcnew System::ArgumentOutOfRangeException("indices");
	}
	
	auto poly	= &m_pmesh->polys[polyIndex*nvp * 2];
	
	for (int i=0; i<m_pmesh->nvp; i++) {
		indices[i] = poly[i];
		if (poly[i]==RC_MESH_NULL_IDX) {
			return i;
		}
	}

	return nvp;
}


void Native::NRecast::NavigationMesh::GetPolygonAdjacencyIndices(int polyIndex, array<int> ^indices)
{
	auto npolys = m_pmesh->npolys;
	auto nvp = m_pmesh->nvp;

	if (polyIndex<0 || polyIndex >= npolys) {
		throw gcnew System::ArgumentOutOfRangeException("polyIndex");
	}
	if (indices == nullptr) {
		throw gcnew System::ArgumentNullException("indices");
	}
	if (indices->Length<nvp) {
		throw gcnew System::ArgumentOutOfRangeException("indices");
	}

	auto poly = &m_pmesh->polys[polyIndex*nvp * 2 + nvp];

	for (int i = 0; i<indices->Length; i++) {
		indices[i] = -1;
	}

	for (int i = 0; i<m_pmesh->nvp; i++) {
		
		if (poly[i] & 0x8000) {
			indices[i] = poly[i];
		} else {
			indices[i] = -1;
		}
	}
}


float frand()
{
	return rand() / (float)RAND_MAX;
}



bool Native::NRecast::NavigationMesh::GetRandomReachablePoint(Vector3 centerPos, float radius, Vector3 %resultVector)
{
	float extentsArray[]	= { 2, 4, 2 };
	float centerPosArray[]	= { centerPos.X, centerPos.Y, centerPos.Z };
	dtPolyRef startRef;
	float randomPoint[] = {0,0,0};

	resultVector	=	Vector3::Zero;

	dtPolyRef	randomRef;
	dtStatus	status;

	//	find closest polygon and copy projected point to centerPosArray :
	status = m_navQuery->findNearestPoly( centerPosArray, extentsArray, m_queryFilter, &startRef, centerPosArray );

	if (dtStatusFailed(status)) return false;

	//	find random point around circle :
	status = m_navQuery->findRandomPointAroundCircle( startRef, centerPosArray, radius, m_queryFilter, frand, &randomRef, randomPoint );

	if (dtStatusFailed(status)) return false;

	resultVector.X	=	randomPoint[0];
	resultVector.Y	=	randomPoint[1];
	resultVector.Z	=	randomPoint[2];

	return true;
}


array<Vector3>^ Native::NRecast::NavigationMesh::FindRoute(Vector3 startPoint, Vector3 endPoint)
{
	dtPolyRef startRef;
	dtPolyRef endRef;

	float extents[]	= { 2, 4, 2 };
	float spos[]	= { startPoint.X, startPoint.Y, startPoint.Z };
	float epos[]	= { endPoint.X,	  endPoint.Y,   endPoint.Z };

	float startPointResult[3];
	float endPointResult[3];

	m_navQuery->findNearestPoly( spos, extents, m_queryFilter, &startRef, startPointResult );
	m_navQuery->findNearestPoly( epos, extents, m_queryFilter, &endRef,   endPointResult   );

	// refs are zero - no route:
	if (startRef==0) return nullptr;
	if (endRef==0) return nullptr;

	Log::Message("{0} {1}", startRef, endRef );

	//---------------------------------------------------------

	const int MAX_POLYS = 256;
	dtPolyRef polys[MAX_POLYS];
	int numPolys;

	m_navQuery->findPath( startRef, endRef, spos, epos, m_queryFilter, polys, &numPolys, MAX_POLYS);

	if (numPolys==0) {
		return nullptr;
	}

	unsigned char	straightPathFlags[MAX_POLYS];
	dtPolyRef		straightPathPolys[MAX_POLYS];
	float			straightPathPoints[MAX_POLYS * 3];
	int				numStraightPath = 0;

	dtStraightPathOptions options = DT_STRAIGHTPATH_AREA_CROSSINGS;

	// In case of partial path, make sure the end point is clamped to the last polygon.
	if (polys[numPolys - 1] != endRef) {
		m_navQuery->closestPointOnPoly(polys[numPolys - 1], epos, epos, 0);
	}

	m_navQuery->findStraightPath(spos, epos, polys, numPolys,
		straightPathPoints, 
		straightPathFlags,
		straightPathPolys, 
		&numStraightPath,
		MAX_POLYS, 
		options);

	//------------------------------------------------------------

	auto route = gcnew array<Vector3>( numStraightPath );

	for ( int i=0; i<numStraightPath; i++ ) {
		route[i] = Vector3( 
			straightPathPoints[i * 3 + 0],
			straightPathPoints[i * 3 + 1],
			straightPathPoints[i * 3 + 2]
		);
	}

	return route;
}

