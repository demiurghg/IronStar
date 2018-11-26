using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Native.Embree;

namespace Fusion.Core.Extensions {
	public static class EmbreeExtensions {

		public static void UpdateGeometryBuffer<T>( this RtcScene scene, uint geometryId, BufferType bufferType, T[] data ) where T : struct
		{
			var pBuffer	=	scene.MapBuffer( geometryId, bufferType );

			SharpDX.Utilities.Write( pBuffer, data, 0, data.Length );
			
			scene.UnmapBuffer( geometryId, bufferType );
			scene.UpdateBuffer( geometryId, bufferType );
		}


	}
}
