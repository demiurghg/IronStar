﻿// This is the main DLL file.

#include "stdafx.h"

#include <fbxsdk.h>
#include "FbxLoader.h"

using namespace Fusion;
using namespace Fusion::Core::Mathematics;
using namespace Fusion::Core::Utils;
using namespace Native::Fbx;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Collections::Generic;
using namespace System::Diagnostics;


/*-----------------------------------------------------------------------------
	Some helpers :
-----------------------------------------------------------------------------*/

Matrix FbxAMatrix2Matrix ( FbxAMatrix& matrix )
{
	double *m = matrix;
	return Matrix(	(float)m[ 0], (float)m[ 1], (float)m[ 2], (float)m[ 3], 
					(float)m[ 4], (float)m[ 5], (float)m[ 6], (float)m[ 7], 
					(float)m[ 8], (float)m[ 9], (float)m[10], (float)m[11], 
					(float)m[12], (float)m[13], (float)m[14], (float)m[15]	);
}


Matrix FbxMatrix2Matrix ( FbxMatrix& matrix )
{
	double *m = matrix;
	return Matrix(	(float)m[ 0], (float)m[ 1], (float)m[ 2], (float)m[ 3], 
					(float)m[ 4], (float)m[ 5], (float)m[ 6], (float)m[ 7], 
					(float)m[ 8], (float)m[ 9], (float)m[10], (float)m[11], 
					(float)m[12], (float)m[13], (float)m[14], (float)m[15]	);
}


Vector3	FbxVector4ToPoint ( FbxVector4 v )
{
	return Vector3( (float)(v.mData[0] / v.mData[3]),
					(float)(v.mData[1] / v.mData[3]),						
					(float)(v.mData[2] / v.mData[3]) );
}


Vector3	FbxVector4ToVector ( FbxVector4 v )
{
	return Vector3( (float)v.mData[0],
					(float)v.mData[1],						
					(float)v.mData[2] );
}


Color	FbxColorToColor ( FbxColor c )
{
	return Color( (float)c.mRed, (float)c.mGreen, (float)c.mBlue, (float)c.mAlpha );
}


Vector2	FbxVector2ToVector ( FbxVector2 v )
{
	return Vector2( (float)v.mData[0],
		(float)v.mData[1]);
}


/*
**	Fusion::Fbx::FbxLoader::FbxLoader
*/
Native::Fbx::FbxLoader::FbxLoader()
{
	Console::WriteLine( "FBX SDK {0}", gcnew string(FbxManager::GetVersion()) );
	fbxManager		=	FbxManager::Create();
	fbxImporter		=	FbxImporter::Create( fbxManager, "Importer" );
	fbxScene		=	FbxScene::Create( fbxManager, "Scene" );
	fbxGConv		=	new FbxGeometryConverter( fbxManager );
}


/*-----------------------------------------------------------------------------
	FBX loader :
-----------------------------------------------------------------------------*/

/*
**	Fusion::Fbx::FbxLoader::LoadScene
*/
Fusion::Engine::Graphics::Scenes::Scene ^ FbxLoader::LoadScene( string ^filename, bool importGeometry, bool importAnimation )
{
	if ( !fbxImporter->Initialize( StringHelper::ToNative(filename) ) ) {
		throw gcnew Exception( string::Format( "Failed to initialise the FBX importer") );
	}


	if( !fbxImporter->Import( fbxScene ) ) {
		throw gcnew Exception( string::Format( "Failed to import the scene" ) );
	}


	FbxTimeSpan timeSpan;
	FbxTime		start;
	FbxTime		end;

	fbxScene->GetGlobalSettings().GetTimelineDefaultTimeSpan( timeSpan );
	timeMode = fbxScene->GetGlobalSettings().GetTimeMode();
	start = timeSpan.GetStart();
	end   = timeSpan.GetStop();

	Fusion::Engine::Graphics::Scenes::Scene ^scene =	gcnew Fusion::Engine::Graphics::Scenes::Scene();

	scene->TimeMode  = ConvertTimeMode( timeMode );

	//---------------------------------------------
	Console::WriteLine("Triangulating...");

	if (!fbxGConv->Triangulate( fbxScene, true, false )) {
		Log::Warning("Failed to triangulate scene!");
	}

	Console::WriteLine("Traversing hierarchy...");

	fbxNodeCount	=	fbxScene->GetNodeCount();
	fbxNodes		=	new FbxNode*[ fbxNodeCount ];

	for (int i=0; i<fbxNodeCount; i++) {
		fbxNodes[i] = fbxScene->GetNode(i);
	}
	
	for (int i=0; i<fbxNodeCount; i++) {
		auto node = CreateSceneNode( fbxNodes[i], fbxScene, scene );
		scene->Nodes->Add( node );

		if (scene->Nodes->Count-1 != i) {
			throw gcnew InvalidOperationException("Scene hierarchy is broken. Check node creation code.");
		}
	}

	//---------------------------------------------

	Console::WriteLine("Import Geometry...");

	if (importGeometry) {

		for (int i=0; i<fbxNodeCount; i++) {
			//Console::WriteLine( "  {0}",node->Name);
			Node	^node		=	scene->Nodes[i];
			FbxNode *fbxNode	=	fbxNodes[i];
			HandleMesh( scene, node, fbxNode );
		}
	}

	scene->Nodes[0]->Name = "";

	//---------------------------------------------

	if (importAnimation) {

		Console::WriteLine(gcnew String(fbxScene->ActiveAnimStackName.Get().Buffer()));
		Console::WriteLine("Getting take info...");

		FbxString takeName;

		auto takeCount	=	fbxImporter->GetAnimStackCount();
		auto evaluator	=	fbxScene->GetEvaluator();

		for (int takeIndex = 0; takeIndex < takeCount; takeIndex++) {

			FbxTakeInfo* lTakeInfo = fbxImporter->GetTakeInfo(takeIndex);

			FbxTimeSpan takeSpan = lTakeInfo->mLocalTimeSpan;

			auto firstTakeFrame =	(int)takeSpan.GetStart().GetFrameCount(timeMode);
			auto lastTakeFrame	=	(int)takeSpan.GetStop().GetFrameCount(timeMode);
			auto takeName		=	gcnew String(lTakeInfo->mName.Buffer());

			auto animTake		=	gcnew AnimationTake(takeName, fbxNodeCount, firstTakeFrame, lastTakeFrame );

			auto animStack		=	(FbxAnimStack*)fbxScene->GetSrcObject<FbxAnimStack>(takeIndex);
			auto stackName		=	animStack->GetName();

			Console::WriteLine("  {0,-24} : [{1}..{2}]", gcnew String(stackName), firstTakeFrame, lastTakeFrame );

			evaluator->SetContext( fbxScene->GetSrcObject<FbxAnimStack>(takeIndex) );

			//	Animate :
			for (int nodeId = 0; nodeId<fbxNodeCount; nodeId++ ) {
				for (int frame = firstTakeFrame; frame <= lastTakeFrame; frame++) {
				
					FbxTime time;
					time.SetFrame(frame, this->timeMode);

					auto fbxNode = fbxNodes[nodeId];
					auto animKey = FbxAMatrix2Matrix(evaluator->GetNodeLocalTransform(fbxNode, time, FbxNode::eSourcePivot, false, true));

					animTake->SetKey( frame, nodeId, animKey );
					//fbxScene->GetNode(
					//evaluator->SetContext(
					//Console::WriteLine("{0} {2} : {1}", frame, animKey.ToString(), time.GetSecondDouble() );
					//scene->SetAnimKey(frame, trackId, animKey);
				}
			}

			scene->Takes->Add( animTake );
		}


		//scene->StartTime	=	TimeSpan::FromMilliseconds( (long)start.GetMilliSeconds() );
		//scene->EndTime		=	TimeSpan::FromMilliseconds( (long)end.GetMilliSeconds() );

		Console::WriteLine("Scene range : {0} - {1}", scene->FirstFrame, scene->LastFrame);
		Console::WriteLine("Total nodes : {0}", fbxScene->GetNodeCount());
	}


	//	do not destroy...
	// 	stack overflow happens...
	fbxImporter->Destroy(true);
	delete[] fbxNodes;

	return scene;
}



FbxString GetNodeProperty( FbxNode *fbxNode, FbxString propertyName )
{
	auto prop = fbxNode->GetFirstProperty();
	while (prop != NULL) {

		if (prop.GetName()==propertyName) {
			return prop.Get<FbxString>();
		}

		prop = fbxNode->GetNextProperty(prop);
	}

	return "";
}



/*
**	Fusion::Fbx::FbxLoader::IterateChildren
*/
Node ^Native::Fbx::FbxLoader::CreateSceneNode( FbxNode *fbxNode, FbxScene *fbxScene, Scene ^scene )
{
	auto node			=	gcnew Node();
	node->Name			= 	gcnew string( fbxNode->GetName() );
	node->Comment		=	gcnew string( GetNodeProperty( fbxNode, "notes" ) );

	node->ParentIndex	=	GetFbxNodeIndex( fbxNode->GetParent() );

	auto index	=	scene->Nodes->Count-1;

	//	Get transform
	FbxAMatrix	transform	=	fbxNode->GetScene()->GetEvaluator()->GetNodeLocalTransform( fbxNode );
	node->Transform			=	FbxAMatrix2Matrix( transform );
	node->BindPose			=	Matrix::Identity;

	GetCustomProperties( node, fbxNode );

	//	Get bind pose :
	int poseCount = fbxScene->GetPoseCount();
	for (int i = 0; i < poseCount; i++) {
		FbxPose* lPose = fbxScene->GetPose(i);
		if (lPose ) {
			//pNode should be the FBX node of bones.
			int lNodeIndex = lPose->Find(fbxNode);
			if (lNodeIndex > -1) {
				// The bind pose is always a global matrix.
				if (lPose->IsBindPose() || !lPose->IsLocalMatrix(lNodeIndex)) {
					FbxMatrix lPoseMatrix	= lPose ->GetMatrix( lNodeIndex );
					node->BindPose			= FbxMatrix2Matrix(lPoseMatrix);
				}
			}
		}
	}

	return node;
}


/*-----------------------------------------------------------------------------
	Mesh stuff :
-----------------------------------------------------------------------------*/

string^ GetTextureFileName ( FbxTexture* pTexture );
void	AddTextureToDictionaryByProperty( Dictionary<string^, string^> ^dict, FbxProperty pProperty, int pMaterialIndex );
void	TryGetDiffuseTexture ( string ^%textureName, FbxProperty pProperty, int pMaterialIndex );

#pragma warning (disable: 4996)

/*
**	Fusion::Fbx::FbxLoader::HandleMesh
*/
void Native::Fbx::FbxLoader::HandleMesh( Fusion::Engine::Graphics::Scenes::Scene ^scene, Fusion::Engine::Graphics::Scenes::Node ^node, FbxNode *fbxNode )
{
	FbxMesh		*fbxMesh	=	fbxNode->GetMesh();

	//return;

	if (!fbxMesh) {
		return;
	}

	Fusion::Engine::Graphics::Scenes::Mesh	^nodeMesh	=	gcnew Fusion::Engine::Graphics::Scenes::Mesh();

	scene->Meshes->Add( nodeMesh );
	node->MeshIndex = scene->Meshes->Count-1;


	Matrix^ meshTransform = Matrix::Identity;
	array<Int4>		^skinIndices = gcnew array<Int4>	(fbxMesh->GetControlPointsCount());
	array<Vector4>	^skinWeights = gcnew array<Vector4>	(fbxMesh->GetControlPointsCount());

	HandleSkinning(nodeMesh, scene, node, fbxNode, meshTransform, skinIndices, skinWeights);

	int polyCount = fbxMesh->GetPolygonCount();

	int vertexIdCount = 0;

	//
	//	vertices :
	//	
	for (int i=0; i<polyCount; i++) {
		
		int n = fbxMesh->GetPolygonSize(i);
		if (n!=3) {
			Console::WriteLine( "Bad triangle, ignored" );
			continue;
		}

		nodeMesh->Vertices->Capacity = polyCount * 3;

		Fusion::Engine::Graphics::Scenes::MeshTriangle tri;

		for (int j=0; j<3; j++) {
			int			id	=	fbxMesh->GetPolygonVertex( i, j );
			FbxVector4	p	=	fbxMesh->GetControlPointAt( id );

			Fusion::Engine::Graphics::Scenes::MeshVertex v;

			Vector4 transfPos	= Vector3::Transform(FbxVector4ToVector( p ), *meshTransform);

			v.Color0			=	Color::White;
			v.Position			=	Vector3(transfPos[0], transfPos[1], transfPos[2]);

			GetNormalForVertex	( &v, fbxMesh, vertexIdCount, id );
			GetTextureForVertex	( &v, fbxMesh, vertexIdCount, id );
			GetColorForVertex	( &v, fbxMesh, vertexIdCount, id );

			v.SkinIndices = skinIndices[id];
			v.SkinWeights = skinWeights[id];

			//Log::Message("{0,5} {1,5} {2,5} {3,5} : {4,5:0.000} {5,5:0.000} {6,5:0.000} {7,5:0.000}", 
			//	v.SkinIndices.X, v.SkinIndices.Y, v.SkinIndices.Z, v.SkinIndices.W, 
			//	v.SkinWeights.X, v.SkinWeights.Y, v.SkinWeights.Z, v.SkinWeights.W );
			 
			v.TexCoord0.Y = 1 - v.TexCoord0.Y;
			v.TexCoord1.Y = 1 - v.TexCoord1.Y;

			nodeMesh->Vertices->Add( v );

			if (j==0) tri.Index0 = vertexIdCount;
			if (j==1) tri.Index1 = vertexIdCount;
			if (j==2) tri.Index2 = vertexIdCount;

			vertexIdCount++;
		}

		nodeMesh->Triangles->Add( tri );
	}

	//	
	//	materials :
	//
	int mtrlCount = fbxNode->GetMaterialCount();

	auto mtrlMap = gcnew Dictionary<int,int>();

	for (int i=0; i<mtrlCount; i++) {

		Fusion::Engine::Graphics::Scenes::Material	^mtrl		=	gcnew Fusion::Engine::Graphics::Scenes::Material();
		FbxSurfaceMaterial		*fbxMtrl	=	fbxNode->GetMaterial(i);

		if (fbxMtrl) {
			FbxProperty _property;
			int textureIndex;

			mtrl->Name	=	gcnew String( fbxMtrl->GetName() );

			FbxProperty lProperty;
			String ^texturePath = nullptr;

			//	retrive texture names :
			FBXSDK_FOR_EACH_TEXTURE(textureIndex) {
				_property = fbxMtrl->FindProperty( FbxLayerElement::sTextureChannelNames[textureIndex] );

				TryGetDiffuseTexture( texturePath, _property, i ); 
				mtrl->ColorMap = texturePath;
			}
		}

		if (scene->Materials->Contains( mtrl )) {
			mtrlMap[ i ] = scene->Materials->IndexOf( mtrl );
		} else {
			scene->Materials->Add( mtrl );
			mtrlMap[ i ] = scene->Materials->Count - 1;
		}
	}


	//
	//	Retrive material mapping :
	//	
	const int lPolygonCount = fbxMesh->GetPolygonCount();

	// Count the polygon count of each material
	FbxLayerElementArrayTemplate<int>* lMaterialIndices = NULL;
	FbxGeometryElement::EMappingMode lMaterialMappingMode = FbxGeometryElement::eNone;

	if (fbxMesh->GetElementMaterial())
	{
		lMaterialIndices = &fbxMesh->GetElementMaterial()->GetIndexArray();
		lMaterialMappingMode = fbxMesh->GetElementMaterial()->GetMappingMode();

		if (!lMaterialIndices) {
			throw gcnew Exception( string::Format( "No material indices" ) );
		}

		if (lMaterialMappingMode == FbxGeometryElement::eByPolygon) {

			FBX_ASSERT(lMaterialIndices->GetCount() == lPolygonCount);

			for ( int i=0; i<lPolygonCount; i++) {
				int i0	=	nodeMesh->Triangles[i].Index0;
				int i1	=	nodeMesh->Triangles[i].Index1;
				int i2	=	nodeMesh->Triangles[i].Index2;
				nodeMesh->Triangles[i] = Fusion::Engine::Graphics::Scenes::MeshTriangle( i0, i1, i2, mtrlMap[ lMaterialIndices->GetAt(i) ] );
			}

		} else if (lMaterialMappingMode == FbxGeometryElement::eAllSame) {

			for ( int i=0; i<lPolygonCount; i++) {
				int i0	=	nodeMesh->Triangles[i].Index0;
				int i1	=	nodeMesh->Triangles[i].Index1;
				int i2	=	nodeMesh->Triangles[i].Index2;
				nodeMesh->Triangles[i] = Fusion::Engine::Graphics::Scenes::MeshTriangle( i0, i1, i2, mtrlMap[ lMaterialIndices->GetAt(0) ] );
			}

		} else {
			throw gcnew Exception( string::Format( "Unsupported mapping mode" ) );
		}

	} else {
		throw gcnew Exception( string::Format( "Mesh does not contain material mapping" ) );
	}
}



/*int GetFbxNodeIndex ( Scene ^scene, FbxNode *fbxNode )
{
	for (int i=0; i<scene->Nodes->Count; i++) {
		if ( ((IntPtr)scene->Nodes[i]->Tag).ToPointer() == fbxNode ) {
			return i;
		}
	}
	return -1;
} */

/*-------------------------------------------------------------------------------------------------

	Animation stuff :

-------------------------------------------------------------------------------------------------*/

/*
**	Fusion::Fbx::FbxLoader::HandleAnimation
*/
void Native::Fbx::FbxLoader::HandleSkinning ( Mesh ^nodeMesh, Scene ^scene, Node ^node, FbxNode *fbxNode, Matrix^ meshTransform, array<Int4> ^skinIndices, array<Vector4>	^skinWeights)
{
	FbxMesh		*fbxMesh	=	fbxNode->GetMesh();

	int clusterCount = 0;

	for (int i = 0; i < fbxMesh->GetDeformerCount(FbxDeformer::eSkin); i++) {
		clusterCount += ((FbxSkin *)(fbxMesh->GetDeformer(i, FbxDeformer::eSkin)))->GetClusterCount();
	}


	if (clusterCount > 0) {

		FbxSkin*		skinDeformer = (FbxSkin *)fbxMesh->GetDeformer(0, FbxDeformer::eSkin);
		FbxSkin::EType	skinningType = skinDeformer->GetSkinningType();

		if (skinningType == FbxSkin::eLinear || skinningType == FbxSkin::eRigid || skinningType==FbxSkin::eDualQuaternion ) {


			// Control points
			int* vertexWeightsCounter = new int[fbxMesh->GetControlPointsCount()]();

			
			int deformerCount = fbxMesh->GetDeformerCount(FbxDeformer::eSkin); // Count of skeletons used for skinning
			

			for ( int defInd = 0; defInd < deformerCount; defInd++ ) {
				
				skinDeformer = (FbxSkin *)fbxMesh->GetDeformer(defInd, FbxDeformer::eSkin);

				int clusterCount = skinDeformer->GetClusterCount();							// Count of bones in skeleton
				

				for (int clusterIndex = 0; clusterIndex < clusterCount; ++clusterIndex) {

					FbxCluster* cluster = skinDeformer->GetCluster(clusterIndex);

					if (!cluster->GetLink()) {
						Log::Warning("Missing link");
					}

					FbxNode*	fbxLinkNode	= cluster->GetLink();

					int vertexIndexCount = cluster->GetControlPointIndicesCount();


					for (int k = 0; k < vertexIndexCount; ++k) {

						int index = cluster->GetControlPointIndices()[k];

						// Sometimes, the mesh can have less points than at the time of the skinning
						// because a smooth operator was active when skinning but has been deactivated during export.
						if (index >= fbxMesh->GetControlPointsCount()) {
							Log::Warning("The mesh had less points than at the time of the skinning.");
							continue;
						}

						double weight = cluster->GetControlPointWeights()[k];

						if ( vertexWeightsCounter[index] >= 4 ) {
							Log::Warning("Vertex has more than 4 influences");
						}

						// PVS-Studio : 
						// V550 An odd precise comparison: weight == 0.0. 
						// It's probably better to use a comparison with defined precision: 
						//	fabs(A - B) < Epsilon. fbxloader.cpp 484
						if (weight == 0.0) {
							continue;
						}

						Int4 skinIndex							= skinIndices[index];
						skinIndex[vertexWeightsCounter[index]]	= GetFbxNodeIndex( fbxLinkNode );
						skinIndices[index]						= skinIndex;

						Vector4 skinWeight							= skinWeights[index];
						skinWeight[vertexWeightsCounter[index]++]	= (float)weight;
						skinWeights[index]							= skinWeight;
					}
				}

			}

			delete[] vertexWeightsCounter;
		} else {
			System::Console::WriteLine(gcnew string("Unsupported skinning type deformation"));
		}
	}
}



/*
**	Gets custom properties :
*/
void Native::Fbx::FbxLoader::GetCustomProperties( Fusion::Engine::Graphics::Scenes::Node ^node, FbxNode *fbxNode )
{
	FbxProperty lProperty = fbxNode->GetFirstProperty();
    while (lProperty.IsValid()) {
        if (lProperty.GetFlag(FbxPropertyAttr::eUserDefined)) {
			string^ pName = gcnew string(lProperty.GetName().Buffer());
			//Console::Write("Custom property name: ");
			//Console::WriteLine(pName);
			
			System::Object^ custProp;
			 EFbxType pDataType = lProperty.GetPropertyDataType().GetType();

			 if(pDataType == eFbxBool) {
				 FbxBool propBool = lProperty.Get<FbxBool>();
				 custProp = gcnew bool(propBool);
			 } else if(pDataType == eFbxFloat || pDataType == eFbxDouble) {
				 FbxDouble propDouble = lProperty.Get<FbxDouble>();
				 custProp = gcnew float((float)propDouble);
			 } else if(pDataType == eFbxInt) {
				 FbxInt propInt = lProperty.Get<FbxInt>();
				 custProp = gcnew int(propInt);
			 } else if(pDataType == eFbxString) {
				 FbxString propString = lProperty.Get<FbxString>();
				 custProp = gcnew string(propString.Buffer());
			 } else if(pDataType == eFbxDouble3 || pDataType == eFbxDouble4) {
				 FbxDouble3 propVector = lProperty.Get<FbxDouble3>();
				 custProp = gcnew Vector3((float)propVector[0], (float)propVector[1], (float)propVector[2]);
			 }

			//node->Attributes->Add(pName, custProp);
		}

        lProperty = fbxNode->GetNextProperty(lProperty);
    }
}


/*-----------------------------------------------------------------------------
	Material stuff :
-----------------------------------------------------------------------------*/

/*
**	GetTextureFileName
*/
string^ GetTextureFileName ( FbxTexture* pTexture )
{
	FbxFileTexture *lFileTexture = FbxCast<FbxFileTexture>(pTexture);

	if (lFileTexture) {
		return gcnew string(lFileTexture->GetRelativeFileName());
	} else {
		return nullptr;
	}
}


void TryGetDiffuseTexture ( string ^%textureName, FbxProperty pProperty, int pMaterialIndex )
{
    if ( pProperty.IsValid() ) {

		string ^propertyName = gcnew string(pProperty.GetNameAsCStr());

		if ( propertyName == "DiffuseColor" ) {

			int lTextureCount = pProperty.GetSrcObjectCount<FbxTexture>();

			if (lTextureCount<=0) {
				return;
			}

			FbxTexture* lTexture = pProperty.GetSrcObject<FbxTexture>(0);

			if ( lTexture ) {
				textureName	=	GetTextureFileName( lTexture );
			}
		}
    }
}


/*
**	RetriveTextureDictionaryByProperty
*/
void AddTextureToDictionaryByProperty( Dictionary<string^, string^> ^dict, FbxProperty pProperty, int pMaterialIndex )
{
    if ( pProperty.IsValid() ) {

		string ^propertyName = gcnew string(pProperty.GetNameAsCStr());
		Console::WriteLine( propertyName );

		return;
		
		int lTextureCount = pProperty.GetSrcObjectCount<FbxTexture>();

		for ( int j = 0; j < lTextureCount; ++j ) {

			//Here we have to check if it's layeredtextures, or just textures:
			FbxLayeredTexture *lLayeredTexture = pProperty.GetSrcObject<FbxLayeredTexture>(j);

			if ( lLayeredTexture ) {

				throw gcnew Exception( string::Format( "{0} : Layered textures are not supported", propertyName ) );

				//FbxLayeredTexture *lLayeredTexture = pProperty.GetSrcObject<FbxLayeredTexture>(j);
				//int lNbTextures = lLayeredTexture->GetSrcObjectCount<FbxTexture>();
				//
				//for ( int k =0; k<lNbTextures; ++k ) {
				//	FbxTexture* lTexture = lLayeredTexture->GetSrcObject<FbxTexture>(k);
				//	if ( lTexture ) {
				//		string ^fileName = GetTextureFileName( lTexture);   
				//	}
				//}

            } else {
				//no layered texture simply get on the property
                FbxTexture* lTexture = pProperty.GetSrcObject<FbxTexture>(j);
                if ( lTexture ) {
                    string ^fileName = GetTextureFileName( lTexture );

					dict->Add( propertyName, fileName );
                }
            } 
        }  
    }
}


/*
**	Fusion::Fbx::FbxLoader::HandleMaterial
*/
void Native::Fbx::FbxLoader::HandleMaterial( Fusion::Engine::Graphics::Scenes::MeshSubset ^sg, FbxSurfaceMaterial *material )
{

}


/*-----------------------------------------------------------------------------
	Vertex attribute stuff :
-----------------------------------------------------------------------------*/


void Native::Fbx::FbxLoader::GetColorForVertex( Fusion::Engine::Graphics::Scenes::MeshVertex *vertex, FbxMesh *fbxMesh, int vertexIdCount, int ctrlPointId )
{
	FbxGeometryElementVertexColor *colorElement = fbxMesh->GetElementVertexColor();

	if (!colorElement) {
		return;
	}

	auto mapMode	=	colorElement->GetMappingMode();
	auto refMode	=	colorElement->GetReferenceMode();

	if ( mapMode == FbxGeometryElement::eDirect ) {
		
		int colorIndex = 0;

		if ( refMode == FbxGeometryElement::eDirect ) {
			colorIndex = ctrlPointId;
		}
		if ( refMode == FbxGeometryElement::eIndexToDirect ) {
			colorIndex = colorElement->GetIndexArray().GetAt(ctrlPointId);
		}

		vertex->Color0 =  FbxColorToColor( colorElement->GetDirectArray().GetAt( colorIndex ) );
	}
	else if ( mapMode == FbxGeometryElement::eByPolygonVertex ) {

		int colorIndex = 0;

		if ( refMode == FbxGeometryElement::eDirect ){
			colorIndex = vertexIdCount;
		}

		if ( refMode == FbxGeometryElement::eIndexToDirect ){
			colorIndex = colorElement->GetIndexArray().GetAt(vertexIdCount);
		}

		vertex->Color0 =  FbxColorToColor( colorElement->GetDirectArray().GetAt( colorIndex ) );

	} else {
		throw gcnew Exception(gcnew String("Unsupported color mapping mode"));
	} 

}


/*
**	Fusion::Fbx::FbxLoader::GetNormalForVertex
*/
void Native::Fbx::FbxLoader::GetNormalForVertex( Fusion::Engine::Graphics::Scenes::MeshVertex *vertex, FbxMesh *fbxMesh, int vertexIdCount, int ctrlPointId )
{
	FbxGeometryElementNormal *normalElement = fbxMesh->GetElementNormal();

	if(normalElement){
		if ( normalElement->GetMappingMode() == FbxGeometryElement::eByControlPoint ) {

			int vertCount = fbxMesh->GetControlPointsCount();

			int normalIndex = 0;

			if(normalElement->GetReferenceMode() == FbxGeometryElement::eDirect){
				normalIndex = ctrlPointId;
			}

			if (normalElement->GetReferenceMode() == FbxGeometryElement::eIndexToDirect){
				normalIndex = normalElement->GetIndexArray().GetAt(ctrlPointId);
			}

			FbxVector4 normalVector = normalElement->GetDirectArray().GetAt(normalIndex);

			vertex->Normal =  FbxVector4ToVector( normalVector );
		} 

		else if ( normalElement->GetMappingMode() == FbxGeometryElement::eByPolygonVertex ){

			int normalIndex = 0;

			if( normalElement->GetReferenceMode() == FbxGeometryElement::eDirect ){
				normalIndex = vertexIdCount;
			}

			if(normalElement->GetReferenceMode() == FbxGeometryElement::eIndexToDirect){
				normalIndex = normalElement->GetIndexArray().GetAt(vertexIdCount);
			}

			FbxVector4 normalVector = normalElement->GetDirectArray().GetAt(normalIndex);
			vertex->Normal =  FbxVector4ToVector( normalVector );
		}
	}
}


/*
**	Fusion::Fbx::FbxLoader::GetTextureForVertex
*/
void Native::Fbx::FbxLoader::GetTextureForVertex( Fusion::Engine::Graphics::Scenes::MeshVertex *vertex, FbxMesh *fbxMesh, int vertexIdCount, int vertexId )
{
	FbxStringList NameListOfUV;
	fbxMesh->GetUVSetNames(NameListOfUV);

	for (int setIndexOfUV = 0; setIndexOfUV < NameListOfUV.GetCount(); setIndexOfUV++) {

		const char* nameOfUVSet = NameListOfUV.GetStringAt(setIndexOfUV);
		const FbxGeometryElementUV* elementOfUV = fbxMesh->GetElementUV(nameOfUVSet);

		if(!elementOfUV) {
			continue;
		}

		if (elementOfUV->GetMappingMode() != FbxGeometryElement::eByPolygonVertex &&
			elementOfUV->GetMappingMode() != FbxGeometryElement::eByControlPoint ) {
			return;
		}

		const bool useIndex = elementOfUV->GetReferenceMode() != FbxGeometryElement::eDirect;
		const int indexOfUVCount= (useIndex) ? elementOfUV->GetIndexArray().GetCount() : 0;


		if( elementOfUV->GetMappingMode() == FbxGeometryElement::eByControlPoint ) {
			FbxVector2 valueOfUV;

			int indexOfUV = useIndex ? elementOfUV->GetIndexArray().GetAt(vertexId) : vertexId;

			valueOfUV = elementOfUV->GetDirectArray().GetAt(indexOfUV);

			switch ( setIndexOfUV ) {
				case 0 : { vertex->TexCoord0 = FbxVector2ToVector(valueOfUV); }
				case 1 : { vertex->TexCoord1 = FbxVector2ToVector(valueOfUV); }
			}
		}
		else if ( elementOfUV->GetMappingMode() == FbxGeometryElement::eByPolygonVertex ) {
			if ( vertexIdCount< indexOfUVCount ){
				FbxVector2 valueOfUV;
				int indexOfUV = useIndex ? elementOfUV->GetIndexArray().GetAt(vertexIdCount) : vertexIdCount;

				valueOfUV = elementOfUV->GetDirectArray().GetAt(indexOfUV);

				switch ( setIndexOfUV ) {
					case 0 : {vertex->TexCoord0 = FbxVector2ToVector(valueOfUV); }
					case 1 : {vertex->TexCoord1 = FbxVector2ToVector(valueOfUV); }
				}

			}
		}
	}
}



/*
**	Fusion::Fbx::FbxLoader::HandleNull
*/
void Native::Fbx::FbxLoader::HandleCamera( Fusion::Engine::Graphics::Scenes::Scene ^scene, Fusion::Engine::Graphics::Scenes::Node ^node, FbxNode *fbxNode )
{

}


/*
**	Fusion::Fbx::FbxLoader::HandleLight
*/
void Native::Fbx::FbxLoader::HandleLight( Fusion::Engine::Graphics::Scenes::Scene ^scene, Fusion::Engine::Graphics::Scenes::Node ^node, FbxNode *fbxNode )
{

}





/*
**	main
**	Entry point
*/
int main(array<System::String ^> ^args)
{
	#if 0
	auto options	=	gcnew Options();
	auto parser		=	gcnew CommandLineParser( Options::typeid, nullptr );

	auto sw = gcnew Stopwatch();

	try { 

		parser->ParseCommandLine( options, args );

		if ( options->Output == nullptr ) {
			options->Output = Path::ChangeExtension( options->Input, ".scene" );
		}

		sw->Restart();

		auto loader	=	gcnew FbxLoader();
		auto scene	=	loader->LoadScene( options->Input, true, true );

		for each ( Fusion::Engine::Graphics::Mesh ^mesh in scene->Meshes ) {
					
			if (mesh!=nullptr) {
				mesh->Prepare( scene, options->MergeTolerance );
			}
		}

		scene->StripNamespaces();

		if (options->BaseDirectory!=nullptr) {
			scene->ResolveTexturePathToBaseDirectory( options->Input, options->BaseDirectory );
		}

		
		//Log::Message("Reading: {0}", sw->Elapsed);

		sw->Restart();

		auto stream = File::Open( options->Output, FileMode::Create, FileAccess::Write );
		scene->Save( stream );
		stream->Close();

		//Log::Message("Saving: {0}", sw->Elapsed);

	} catch ( Exception^ e ) {
		auto errorWriter = Console::Error;
		errorWriter->WriteLine(e->Message);

		if (options->Wait) {
			Console::WriteLine("Press any key to continue...");
			Console::ReadKey();
		}
		return 1;
	}

	if (options->Wait) {
		Console::WriteLine("Press any key to continue...");
		Console::ReadKey();
	}
	#endif

	return 0;
}
