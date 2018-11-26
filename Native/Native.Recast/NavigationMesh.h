#pragma once

using namespace Fusion::Core::Mathematics;

namespace Native {
	namespace NRecast {

		public ref class NavigationMesh
		{
		private:
			unsigned char* m_triareas;
			rcContext *m_ctx;
			rcHeightfield* m_solid;
			rcCompactHeightfield* m_chf;
			rcContourSet* m_cset;
			rcPolyMesh* m_pmesh;
			rcConfig *m_cfg;
			rcPolyMeshDetail* m_dmesh;

			dtNavMesh *m_navMesh;
			dtNavMeshQuery* m_navQuery;
			dtQueryFilter* m_queryFilter;

			void Cleanup();

			bool m_keepInterResults;

		public:
			NavigationMesh(BuildConfig ^config, array<Vector3>^ vertices, array<int>^ indices);

			~NavigationMesh();

			array<Vector3>^ GetPolyMeshVertices();

			int GetNumVertexPerPoly () {
				return m_pmesh->nvp;
			}

			int GetNumPolys() {
				return m_pmesh->npolys;
			}

			int GetNumVertices() {
				return m_pmesh->nverts;
			}

			int GetPolygonVertexIndices(int polyIndex, array<int> ^indices);

			void GetPolygonAdjacencyIndices(int polyIndex, array<int> ^indices);

			array<Vector3>^ FindRoute ( Vector3 startPoint, Vector3 endPoint );

		};
	}
}