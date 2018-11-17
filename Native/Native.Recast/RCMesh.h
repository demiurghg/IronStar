#pragma once

using namespace Fusion::Core::Mathematics;

namespace Native {
	namespace Recast {

		public ref class RCMesh
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

			void Cleanup();

			bool m_keepInterResults;

		public:
			RCMesh(RCBuildConfig ^config, array<Vector3>^ vertices, array<int>^ indices);

			~RCMesh ();

			array<Vector3>^ GetPolyMeshVertices();
		};
	}
}