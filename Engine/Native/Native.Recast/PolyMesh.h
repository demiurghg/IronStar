#pragma once

namespace Native 
{
	namespace NRecast 
	{
		public ref class PolyMesh
		{
			rcPolyMesh* m_pmesh;

		public:
			PolyMesh(array<System::Byte> ^polyData);

			~PolyMesh();

			array<Vector3>^ GetPolyMeshVertices();

			int GetNumVertexPerPoly () 
			{
				return m_pmesh->nvp;
			}

			int GetNumPolys() 
			{
				return m_pmesh->npolys;
			}

			int GetNumVertices() 
			{
				return m_pmesh->nverts;
			}

			int GetPolygonVertexIndices(int polyIndex, array<int> ^indices);

			void GetPolygonAdjacencyIndices(int polyIndex, array<int> ^indices);
		};
	}
}

