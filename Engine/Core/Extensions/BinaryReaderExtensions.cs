using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;

namespace Fusion.Core.Extensions {
	public static class BinaryReaderExtensions {

		public static void ExpectFourCC ( this BinaryReader reader, string fourCC, string what )
		{
			var readFourCC = reader.ReadFourCC();

			if (readFourCC!=fourCC) {
				throw new IOException(string.Format("Bad {2}: expected {0}, got {1}", fourCC, readFourCC, what)); 
			}
		}


		public static string ReadFourCC ( this BinaryReader reader )
		{
			return ContentUtils.MakeFourCC( reader.ReadUInt32() );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Math types :
		-----------------------------------------------------------------------------------------*/

		public static Color ReadColor( this BinaryReader reader )
		{
			return Color.FromRgba( reader.ReadInt32() );
		}

		public static Vector2 ReadVector2( this BinaryReader reader )
		{
			return new Vector2( reader.ReadSingle(), reader.ReadSingle() );
		}

		public static Vector3 ReadVector3( this BinaryReader reader )
		{
			return new Vector3( reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() );
		}

		public static Vector4 ReadVector4( this BinaryReader reader )
		{
			return new Vector4( reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() );
		}

		public static Quaternion ReadQuaternion( this BinaryReader reader )
		{
			return new Quaternion( reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() );
		}

		public static Matrix ReadMatrix( this BinaryReader reader )
		{
			var value		= new Matrix();
			value.Column1	= ReadVector4( reader );
			value.Column2	= ReadVector4( reader );
			value.Column3	= ReadVector4( reader );
			value.Column4	= ReadVector4( reader );
			return value;
		}

		/*-----------------------------------------------------------------------------------------
		 *	Structures :
		-----------------------------------------------------------------------------------------*/

		public static void Read<T> ( this BinaryReader reader, T[] array, int count ) where T: struct
		{
			var dataSize		=	count * Marshal.SizeOf(typeof(T));
			var buffer			=	new byte[dataSize];
			
			reader.Read( buffer, 0, dataSize );

			var handle			= GCHandle.Alloc( buffer, GCHandleType.Pinned );
			var dataStream		= new DataStream( handle.AddrOfPinnedObject(), buffer.Length, true, false );
			
			dataStream.ReadRange<T>( array, 0, count );

			dataStream.Dispose();
			handle.Free();
		}


		public static T[] Read<T> ( this BinaryReader reader, int count ) where T : struct
		{
			if (count==0) return new T[0];

			var buffer			= reader.ReadBytes( count * Marshal.SizeOf(typeof(T)) );
			var elementCount	= count;
			var handle			= GCHandle.Alloc( buffer, GCHandleType.Pinned );
			var dataStream		= new DataStream( handle.AddrOfPinnedObject(), buffer.Length, true, false );
			
			var range		= dataStream.ReadRange<T>( elementCount );

			dataStream.Dispose();
			handle.Free();

			return range;
		}



		public static T Read<T> ( this BinaryReader reader ) where T : struct
		{
			var size	=	Marshal.SizeOf( typeof( T ) );
			var bytes	=	reader.ReadBytes( size ); 

			var handle	=	GCHandle.Alloc( bytes, GCHandleType.Pinned );

			T structure	=	(T)Marshal.PtrToStructure( handle.AddrOfPinnedObject(), typeof(T) );

			handle.Free();
			
			return structure;
		}
	}
}
