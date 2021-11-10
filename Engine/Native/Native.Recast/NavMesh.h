#pragma once

using namespace Fusion::Core::Mathematics;

namespace Native 
{
	namespace NRecast 
	{
		public ref class NavMesh
		{
		private:
			//unsigned char* m_triareas;
			//rcHeightfield* m_solid;
			//rcCompactHeightfield* m_chf;
			//rcContourSet* m_cset;
			rcPolyMesh* m_pmesh;
			//rcConfig *m_cfg;
			//rcPolyMeshDetail* m_dmesh;

			dtNavMesh *m_navMesh;
			dtNavMeshQuery* m_navQuery;
			dtQueryFilter* m_queryFilter;

			void Cleanup();

			bool m_keepInterResults;

		public:
			static array<System::Byte> ^Build(Config ^config, array<Vector3>^ vertices, array<int>^ indices, array<bool>^ walkables);

			NavMesh(array<System::Byte> ^navData);

			~NavMesh();

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

			bool GetRandomReachablePoint(Vector3 originVector, float radius, Vector3 %resultVector);
			array<Vector3>^ FindRoute(Vector3 startPoint, Vector3 endPoint);

		};
	}
}