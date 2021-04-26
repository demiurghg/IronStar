﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX;
using Fusion.Core.Content;


namespace Fusion.Core.Extensions {
	public static class BinaryWriterExtensions {

		public static void WriteFourCC ( this BinaryWriter writer, string fourCC )
		{
			writer.Write( ContentUtils.MakeFourCC(fourCC) );
		}



		static void WriteGeneric<T>( BinaryWriter writer, object src, int elementCount )
		{
			var size = elementCount * Marshal.SizeOf( typeof( T ) );
			var buffer = new byte[ size ];
			var handle = GCHandle.Alloc( src, GCHandleType.Pinned );
			var ds = new DataStream( handle.AddrOfPinnedObject(), size, true, false );
			ds.ReadRange( buffer, 0, size );
			writer.Write( buffer );
			ds.Dispose();
			handle.Free();
		}



		public static void Write<T>( this BinaryWriter writer, T structure ) where T : struct
		{
			WriteGeneric<T>( writer, structure, 1 );
		}



		public static void Write<T>( this BinaryWriter writer, T[] array ) where T : struct
		{
			if (array.Length>0)
			{
				WriteGeneric<T>( writer, array, array.Length );
			}
		}


		public static void Write<T>( this BinaryWriter writer, T[] array, int count ) where T : struct
		{
			if (count>array.Length) 
			{
				throw new ArgumentOutOfRangeException("count > array.Length");
			}
			WriteGeneric<T>( writer, array, count );
		}


		public static void Write<T>( this BinaryWriter writer, T[,] array ) where T : struct
		{
			WriteGeneric<T>( writer, array, array.Length );
		}
	}
}
