// FbxLoader2.h

#pragma once

using namespace System;
using namespace Fusion;
using namespace msclr::interop;
using namespace Fusion::Engine::Graphics;
using namespace Fusion::Core::Mathematics;

namespace Native {
	namespace Fbx {

		public ref class FbxLoader {
			public:
						FbxLoader	();
				Scene	^LoadScene	( string ^filename, Options ^options );

			private:

				FbxNode**				fbxNodes;
				int						fbxNodeCount;

				Options					^options;
				FbxManager				*fbxManager	;	
				FbxImporter				*fbxImporter;		
				FbxScene				*fbxScene	;	
				FbxGeometryConverter	*fbxGConv	;	
				FbxTime::EMode			timeMode;

				int GetFbxNodeIndex ( FbxNode* fbxNode ) {
					for ( int i = 0; i < fbxNodeCount; i++) {
						if (fbxNodes[i]==fbxNode) {
							return i;
						}
					}
					return -1;
				}

				TimeMode ConvertTimeMode ( FbxTime::EMode mode )
				{
					switch (mode) {
						case FbxTime::EMode::eFrames1000	: return TimeMode::Frames1000	; break;
						case FbxTime::EMode::eFrames120		: return TimeMode::Frames120	; break;
						case FbxTime::EMode::eFrames100		: return TimeMode::Frames100	; break;
						case FbxTime::EMode::eFrames96		: return TimeMode::Frames96		; break;
						case FbxTime::EMode::eFrames72		: return TimeMode::Frames72		; break;
						case FbxTime::EMode::eFrames60		: return TimeMode::Frames60		; break;
						case FbxTime::EMode::eFrames59dot94	: return TimeMode::Frames59dot94; break;
						case FbxTime::EMode::eFrames50		: return TimeMode::Frames50		; break;
						case FbxTime::EMode::eFrames48		: return TimeMode::Frames48		; break;
						case FbxTime::EMode::eFrames30		: return TimeMode::Frames30		; break;
						case FbxTime::EMode::eFrames24		: return TimeMode::Frames24		; break;
						default : return TimeMode::Unknown;
					}
				}

				Node ^CreateSceneNode		( FbxNode *fbxNode, FbxScene *fbxScene, Fusion::Engine::Graphics::Scene ^scene );
				void HandleMesh				( Scene ^scene, Node ^node, FbxNode *fbxNode );
				void HandleSkinning			( Mesh ^nodeMesh, Scene ^scene, Node ^node, FbxNode *fbxNode, Matrix^ meshTransform, array<Int4> ^skinIndices, array<Vector4>	^skinWeights );
				void HandleCamera			( Scene ^scene, Node ^node, FbxNode *fbxNode );
				void HandleLight			( Scene ^scene, Node ^node, FbxNode *fbxNode );
				void HandleMaterial			( MeshSubset ^sg, FbxSurfaceMaterial *material );
				void GetNormalForVertex		( MeshVertex *vertex, FbxMesh *fbxMesh, int vertexIdCount, int ctrlPointId  );
				void GetTextureForVertex	( MeshVertex *vertex, FbxMesh *fbxMesh, int vertexIdCount, int vertexId );
				void GetColorForVertex		( MeshVertex *vertex, FbxMesh *fbxMesh, int vertexIdCount, int vertexId );

				void GetCustomProperties	( Fusion::Engine::Graphics::Node ^node, FbxNode *fbxNode );
			
				// Animation stuff
		};
	}
}
