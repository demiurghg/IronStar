#include "Local.h"
#include "PolyMesh.h"


Native::NRecast::PolyMesh::PolyMesh(array<System::Byte> ^polyData)
{
	m_pmesh	=	Utils::DeserializePolyMesh( polyData );
}


Native::NRecast::PolyMesh::~PolyMesh()
{
	rcFreePolyMesh( m_pmesh );
	m_pmesh = 0;
}

cli::array<Vector3>^ Native::NRecast::PolyMesh::GetPolyMeshVertices()
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


int Native::NRecast::PolyMesh::GetPolygonVertexIndices(int polyIndex, array<int> ^indices)
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


void Native::NRecast::PolyMesh::GetPolygonAdjacencyIndices(int polyIndex, array<int> ^indices)
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

