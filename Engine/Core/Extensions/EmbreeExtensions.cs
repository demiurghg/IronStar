using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Native.Embree;

namespace Fusion.Core.Extensions {
	public static class EmbreeExtensions {

		public static void UpdateRay( ref RtcRay ray, ref Vector3 origin, ref Vector3 dir, float near, float far )
		{
			ray.Origin.X	=	origin.X;
			ray.Origin.Y	=	origin.Y;
			ray.Origin.Z	=	origin.Z;

			ray.Direction.X	=	dir.X;
			ray.Direction.Y	=	dir.Y;
			ray.Direction.Z	=	dir.Z;

			ray.TNear		=	near;
			ray.TFar		=	far;

			ray.Time		=	0;
			ray.Mask		=	0xFFFFFFFF;
		}


		public static void UpdateRay( ref RtcRay ray, Vector3 origin, Vector3 dir, float near, float far )
		{
			ray.Origin.X	=	origin.X;
			ray.Origin.Y	=	origin.Y;
			ray.Origin.Z	=	origin.Z;

			ray.Direction.X	=	dir.X;
			ray.Direction.Y	=	dir.Y;
			ray.Direction.Z	=	dir.Z;

			ray.TNear		=	near;
			ray.TFar		=	far;

			ray.Time		=	0;
			ray.Mask		=	0xFFFFFFFF;
		}



		public static Vector3 Convert ( RtcVector3 v )
		{
			return new Vector3( v.X, v.Y, v.Z );
		}


		public static RtcVector3 Convert ( Vector3 v )
		{
			var r = new RtcVector3();
			r.X = v.X;
			r.Y = v.Y;
			r.Z = v.Z;
			return r;
		}


		public static void UpdateGeometryBuffer<T>( this RtcScene scene, uint geometryId, BufferType bufferType, T[] data ) where T : struct
		{
			var pBuffer	=	scene.MapBuffer( geometryId, bufferType );

			SharpDX.Utilities.Write( pBuffer, data, 0, data.Length );
			
			scene.UnmapBuffer( geometryId, bufferType );
			scene.UpdateBuffer( geometryId, bufferType );
		}


	}
}
