#pragma once

using namespace System::Runtime::InteropServices;

namespace Native {
	namespace Embree {

		public enum class SceneFlags : int { 
			Static		=	RTC_SCENE_STATIC		,
			Dynamic		=	RTC_SCENE_DYNAMIC		,
			Compact		=	RTC_SCENE_COMPACT		,
			Coherent	=	RTC_SCENE_COHERENT		,
			Incoherent	=	RTC_SCENE_INCOHERENT	,
			HighQuality	=	RTC_SCENE_HIGH_QUALITY	,
			Robust		=	RTC_SCENE_ROBUST		,
		};


		public enum class AlgorithmFlags : int {
			Intersect1		=	RTC_INTERSECT1		,
			Intersect4		=	RTC_INTERSECT4		,
			Intersect8		=	RTC_INTERSECT8		,
			Intersect16		=	RTC_INTERSECT16		,
			Interpolate		=	RTC_INTERPOLATE		,
			IntersectStream	=	RTC_INTERSECT_STREAM,
		};


		public enum class GeometryFlags : int {
			Static			=	RTC_GEOMETRY_STATIC,
			Deformable		=	RTC_GEOMETRY_DEFORMABLE,
			Dynamic			=	RTC_GEOMETRY_DYNAMIC,
		};


		public enum class BufferType : int {
			IndexBuffer					=	RTC_INDEX_BUFFER	,
			IndexBuffer0				=	RTC_INDEX_BUFFER0	,
			IndexBuffer1				=	RTC_INDEX_BUFFER1	,
			VertexBuffer				=	RTC_VERTEX_BUFFER	,
			VertexBuffer0				=	RTC_VERTEX_BUFFER0	,
			VertexBuffer1				=	RTC_VERTEX_BUFFER1	,
			UserVertexBuffer			=	RTC_USER_VERTEX_BUFFER,
			UserVertexBuffer0			=	RTC_USER_VERTEX_BUFFER0,
			UserVertexBuffer1			=	RTC_USER_VERTEX_BUFFER1,
			FaceBuffer					=	RTC_FACE_BUFFER,
			LevelBuffer					=	RTC_LEVEL_BUFFER,
			EdgeCreaseIndexBuffer		=	RTC_EDGE_CREASE_INDEX_BUFFER,
			EdgeCreaseWeightBuffer		=	RTC_EDGE_CREASE_WEIGHT_BUFFER,
			VertexCreaseIndexBuffer		=	RTC_VERTEX_CREASE_INDEX_BUFFER,
			VertexCreaseWeightBuffer	=	RTC_VERTEX_CREASE_WEIGHT_BUFFER,
			HoleBuffer					=	RTC_HOLE_BUFFER					,
		};


		public ref class RtcScene {
		private:
			RTCDevice device;
			RTCScene scene;
			RTCRay *pRay;
		public:
			RtcScene( Rtc ^ rtc, SceneFlags sceneFlags, AlgorithmFlags algorithmFlags )
			{
				scene = rtcDeviceNewScene(rtc->device, (RTCSceneFlags)sceneFlags, (RTCAlgorithmFlags)algorithmFlags);

				device = rtc->device;

				pRay =	allocRay();

				RtcException::CheckError(device);
			}


			~RtcScene()
			{
				freeRay(pRay);
				rtcDeleteScene( scene );
			}


			unsigned int NewTriangleMesh(GeometryFlags geomFlags, int numTris, int numVerts)
			{
				auto id = rtcNewTriangleMesh2(scene, (RTCGeometryFlags)geomFlags, numTris, numVerts, 1);
				RtcException::CheckError(device);
				return id;
			}


			IntPtr MapBuffer(unsigned int geometryId, BufferType bufferType)
			{
				auto ptr =  IntPtr( rtcMapBuffer(scene, geometryId, (RTCBufferType)bufferType) );
				RtcException::CheckError(device);
				return ptr;
			}


			void UnmapBuffer(unsigned int geometryId, BufferType bufferType)
			{
				rtcUnmapBuffer(scene, geometryId, (RTCBufferType)bufferType);
				RtcException::CheckError(device);
			}


			void DeleteGeometry ( unsigned int geometryId )
			{
				rtcDeleteGeometry( scene, geometryId );
				RtcException::CheckError(device);
			}


			void Commit ()
			{
				rtcCommit( scene );
				RtcException::CheckError(device);
			}


			void Update (unsigned int geometryId, BufferType bufferType)
			{
				rtcUpdate( scene, geometryId );
				RtcException::CheckError(device);
			}


			void UpdateBuffer (unsigned int geometryId, BufferType bufferType)
			{
				rtcUpdateBuffer(scene, geometryId, (RTCBufferType)bufferType);
				RtcException::CheckError(device);
			}


			private: RTCRay *allocRay ()
			{
				return (RTCRay*)_aligned_malloc( sizeof(RTCRay), __alignof(RTCRay) );
			}


			private: void freeRay( RTCRay *ray ) 
			{
				_aligned_free( ray );
			}


			public: bool Occluded ( float x, float y, float z, float dx, float dy, float dz, float tnear, float tfar )
			{
				pRay->org[0] = x;
				pRay->org[1] = y;
				pRay->org[2] = z;
				pRay->align0 = 0;

				pRay->dir[0] = dx;
				pRay->dir[1] = dy;
				pRay->dir[2] = dz;
				pRay->align1 = 0;

				pRay->tnear = tnear;
				pRay->tfar = tfar;

				pRay->time = 0;
				pRay->mask = 0xFFFFFFFF;

				pRay->Ng[0] = 0;
				pRay->Ng[1] = 0;
				pRay->Ng[2] = 0;
				pRay->align2 = 0;
				pRay->u = 0;
				pRay->v = 0;

				pRay->geomID = RTC_INVALID_GEOMETRY_ID;
				pRay->primID = RTC_INVALID_GEOMETRY_ID;
				pRay->instID = RTC_INVALID_GEOMETRY_ID;

				rtcOccluded(scene, *pRay);
				RtcException::CheckError(device);

				return (pRay->geomID==0);
			}


			private: void CopyManagedRayToNativeRay ( RtcRay %src, RTCRay *dst )
			{
				dst->org[0] =	src.Origin.X;
				dst->org[1] =	src.Origin.Y;
				dst->org[2] =	src.Origin.Z;
				dst->align0	=	0;

				dst->dir[0] =	src.Direction.X;
				dst->dir[1] =	src.Direction.Y;
				dst->dir[2] =	src.Direction.Z;
				dst->align1 =	0;

				dst->tnear	=	src.TNear;
				dst->tfar	=	src.TFar;
				dst->time	=	src.Time;
				dst->mask	=	src.Mask;
			
				dst->Ng[0]	=	0;
				dst->Ng[1]	=	0;
				dst->Ng[2]	=	0;
				dst->align2 =	0;
				dst->u		=	0;
				dst->v		=	0;

				dst->geomID =	RTC_INVALID_GEOMETRY_ID;
				dst->primID =	RTC_INVALID_GEOMETRY_ID;
				dst->instID =	RTC_INVALID_GEOMETRY_ID;
			}



			private: void CopyResultToManagedRay( RTCRay *src, RtcRay %dst )
			{
				dst.HitNormal.X =	src->Ng[0];
				dst.HitNormal.Y =	src->Ng[1];
				dst.HitNormal.Z =	src->Ng[2];
				dst.HitU		=	src->u;
				dst.HitV		=	src->v;
				dst.InstanceId	=	src->instID;				
				dst.PrimitiveId	=	src->primID;				
				dst.GeometryId	=	src->geomID;	
				dst.TFar		=	src->tfar;			
			}



			public: bool Intersect ( RtcRay% ray )
			{
				CopyManagedRayToNativeRay( ray, pRay );

				rtcIntersect( scene, *pRay );
				RtcException::CheckError(device);

				CopyResultToManagedRay( pRay, ray );

				return ( pRay->geomID!=RTC_INVALID_GEOMETRY_ID );
			}


			public: bool Occluded ( RtcRay %ray )
			{
				CopyManagedRayToNativeRay( ray, pRay );

				rtcOccluded( scene, *pRay );
				RtcException::CheckError(device);

				return ( pRay->geomID==0 );
			}


			public: bool Occluded ( RtcRay ray )
			{
				CopyManagedRayToNativeRay( ray, pRay );

				rtcOccluded( scene, *pRay );
				RtcException::CheckError(device);

				return ( pRay->geomID==0 );
			}


			public: float Intersect(float x, float y, float z, float dx, float dy, float dz, float tnear, float tfar)
			{
				pRay->org[0] = x;
				pRay->org[1] = y;
				pRay->org[2] = z;
				pRay->align0 = 0;

				pRay->dir[0] = dx;
				pRay->dir[1] = dy;
				pRay->dir[2] = dz;
				pRay->align1 = 0;

				pRay->tnear = tnear;
				pRay->tfar = tfar;

				pRay->time = 0;
				pRay->mask = 0xFFFFFFFF;

				pRay->Ng[0] = 0;
				pRay->Ng[1] = 0;
				pRay->Ng[2] = 0;
				pRay->align2 = 0;
				pRay->u = 0;
				pRay->v = 0;

				pRay->geomID = RTC_INVALID_GEOMETRY_ID;
				pRay->primID = RTC_INVALID_GEOMETRY_ID;
				pRay->instID = RTC_INVALID_GEOMETRY_ID;

				rtcIntersect(scene, *pRay);
				RtcException::CheckError(device);

				return (pRay->geomID==RTC_INVALID_GEOMETRY_ID) ? -1 : pRay->tfar;
			}

			//void Intersect ( RtcRay ^ ray )
			//{
			//	RTCRay _ray;

			//	pRay->org[0] =	ray->X;      
			//	pRay->org[1] =	ray->Y;      
			//	pRay->org[2] =	ray->Z;      
			//	pRay->align0 =	0;

			//	pRay->dir[0] =	ray->Dx;     
			//	pRay->dir[1] =	ray->Dx;     
			//	pRay->dir[2] =	ray->Dx;     
			//	pRay->align1	=	0;

			//	pRay->tnear	=	ray->TNear;  
			//	pRay->tfar	=	ray->TFar;   

			//	pRay->time	=	ray->Time;   
			//	pRay->mask	=	ray->Mask;   

			//	pRay->Ng[0]	=	0; 
			//	pRay->Ng[1]	=	0; 
			//	pRay->Ng[2]	=	0; 
			//	pRay->align2	=	0;
			//	pRay->u		=	0; 
			//	pRay->v		=	0; 
			//	
			//	pRay->geomID	=	RTC_INVALID_GEOMETRY_ID; 
			//	pRay->primID	=	RTC_INVALID_GEOMETRY_ID;
			//	pRay->instID	=	RTC_INVALID_GEOMETRY_ID;

			//	rtcIntersect( scene, _ray );

			//	ray->Nx = pRay->Ng[0];
			//	ray->Ny = pRay->Ng[1];
			//	ray->Nz = pRay->Ng[2];

			//	ray->U = pRay->u;
			//	ray->V = pRay->v;
			//	ray->GeometryId = pRay->geomID;
			//	ray->TriangleId = pRay->primID;
			//	ray->InstanceId = pRay->instID;
			//}
		};
	}
}

