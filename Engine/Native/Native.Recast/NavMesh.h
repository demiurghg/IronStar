#pragma once

using namespace Fusion::Core::Mathematics;

namespace Native 
{
	namespace NRecast 
	{
		public ref class NavMesh
		{
		private:
			dtNavMesh *m_navMesh;
			dtNavMeshQuery* m_navQuery;
			dtQueryFilter* m_queryFilter;

		public:
			static array<System::Byte> ^Build(Config ^config, array<Vector3>^ vertices, array<int>^ indices, array<bool>^ walkables, array<System::Byte> ^%polyData);

			NavMesh(array<System::Byte> ^navData);

			~NavMesh();

			bool GetRandomReachablePoint(Vector3 originVector, float radius, Vector3 %resultVector);

			array<Vector3>^ FindRoute(Vector3 startPoint, Vector3 endPoint);
		};
	}
}