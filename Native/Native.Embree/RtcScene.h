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
			RTCScene scene;
		public:
			RtcScene( Rtc ^ rtc, SceneFlags sceneFlags, AlgorithmFlags algorithmFlags );
			~RtcScene();

			unsigned int NewTriangleMesh(GeometryFlags geomFlags, int numTris, int numVerts)
			{
				auto id = rtcNewTriangleMesh2(scene, (RTCGeometryFlags)geomFlags, numTris, numVerts);
				RtcException::CheckError();
				return id;
			}


			IntPtr MapBuffer(unsigned int geometryId, BufferType bufferType)
			{
				auto ptr =  IntPtr( rtcMapBuffer(scene, geometryId, (RTCBufferType)bufferType) );
				RtcException::CheckError();
				return ptr;
			}


			void UnmapBuffer(unsigned int geometryId, BufferType bufferType)
			{
				rtcMapBuffer(scene, geometryId, (RTCBufferType)bufferType);
				RtcException::CheckError();
			}


			void DeleteGeometry ( unsigned int geometryId )
			{
				rtcDeleteGeometry( scene, geometryId );
				RtcException::CheckError();
			}


			void Commit ()
			{
				rtcCommit( scene );
				RtcException::CheckError();
			}


			void Update (unsigned int geometryId, BufferType bufferType)
			{
				rtcUpdate( scene, geometryId );
				RtcException::CheckError();
			}


			void UpdateBuffer (unsigned int geometryId, BufferType bufferType)
			{
				rtcUpdateBuffer(scene, geometryId, (RTCBufferType)bufferType);
				RtcException::CheckError();
			}


			private: RTCRay *allocRay ()
			{
				return (RTCRay*)_aligned_malloc( sizeof(RTCRay), __alignof(RTCRay) );
			}


			private: void freeRay( RTCRay *ray ) 
			{
				_aligned_free( ray );
			}


			public: bool Occluded ( float x, float y, float z, float dx, float dy, float dz )
			{
				//#pragma managed(push off)
				RTCRay *pRay	= allocRay();

				pRay->org[0] = x;
				pRay->org[1] = y;
				pRay->org[2] = z;
				pRay->align0 = 0;

				pRay->dir[0] = dx;
				pRay->dir[1] = dy;
				pRay->dir[2] = dz;
				pRay->align1 = 0;

				pRay->tnear = 0;
				pRay->tfar = INFINITY;

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

				bool result = (pRay->geomID==0);

				freeRay( pRay );

				RtcException::CheckError();

				return result;
			}


			#if 0
			void Intersect ( RtcRay ^ ray )
			{
				RTCRay _ray;

				_ray.org[0] =	ray->X;      
				_ray.org[1] =	ray->Y;      
				_ray.org[2] =	ray->Z;      
				_ray.align0 =	0;

				_ray.dir[0] =	ray->Dx;     
				_ray.dir[1] =	ray->Dx;     
				_ray.dir[2] =	ray->Dx;     
				_ray.align1	=	0;

				_ray.tnear	=	ray->TNear;  
				_ray.tfar	=	ray->TFar;   

				_ray.time	=	ray->Time;   
				_ray.mask	=	ray->Mask;   

				_ray.Ng[0]	=	0; 
				_ray.Ng[1]	=	0; 
				_ray.Ng[2]	=	0; 
				_ray.align2	=	0;
				_ray.u		=	0; 
				_ray.v		=	0; 
				
				_ray.geomID	=	RTC_INVALID_GEOMETRY_ID; 
				_ray.primID	=	RTC_INVALID_GEOMETRY_ID;
				_ray.instID	=	RTC_INVALID_GEOMETRY_ID;

				rtcIntersect( scene, _ray );

				ray->Nx = _ray.Ng[0];
				ray->Ny = _ray.Ng[1];
				ray->Nz = _ray.Ng[2];

				ray->U = _ray.u;
				ray->V = _ray.v;
				ray->GeometryId = _ray.geomID;
				ray->TriangleId = _ray.primID;
				ray->InstanceId = _ray.instID;
			}
			#endif
		};
	}
}

