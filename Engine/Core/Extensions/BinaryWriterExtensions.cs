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


namespace Fusion.Core.Extensions 
{
	public static class BinaryWriterExtensions 
	{
		public static void WriteFourCC ( this BinaryWriter writer, string fourCC )
		{
			writer.Write( ContentUtils.MakeFourCC(fourCC) );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Math types :
		-----------------------------------------------------------------------------------------*/

		public static void Write( this BinaryWriter writer, Color value )
		{
			writer.Write( value.ToRgba() );
		}

		public static void Write( this BinaryWriter writer, Vector2 value )
		{
			writer.Write( value.X );
			writer.Write( value.Y );
		}

		public static void Write( this BinaryWriter writer, Vector3 value )
		{
			writer.Write( value.X );
			writer.Write( value.Y );
			writer.Write( value.Z );
		}

		public static void Write( this BinaryWriter writer, Vector4 value )
		{
			writer.Write( value.X );
			writer.Write( value.Y );
			writer.Write( value.Z );
			writer.Write( value.W );
		}

		public static void Write( this BinaryWriter writer, Quaternion value )
		{
			writer.Write( value.X );
			writer.Write( value.Y );
			writer.Write( value.Z );
			writer.Write( value.W );
		}

		public static void Write( this BinaryWriter writer, Matrix value )
		{
			writer.Write( value.Column1 );
			writer.Write( value.Column2 );
			writer.Write( value.Column3 );
			writer.Write( value.Column4 );
		}

		/*-----------------------------------------------------------------------------------------
		 *	Structures :
		-----------------------------------------------------------------------------------------*/

		public static void WriteArray( this BinaryWriter writer, object sourceArray, Type elementType, int elementCount )
		{
			var size = elementCount * Marshal.SizeOf( elementType );
			var buffer = new byte[ size ];
			var handle = GCHandle.Alloc( sourceArray, GCHandleType.Pinned );
			var ds = new DataStream( handle.AddrOfPinnedObject(), size, true, false );
			ds.ReadRange( buffer, 0, size );
			writer.Write( buffer );
			ds.Dispose();
			handle.Free();
		}


		public static void Write<T>( this BinaryWriter writer, T structure ) where T : struct
		{
			WriteArray( writer, structure, typeof(T), 1 );
		}


		public static void Write<T>( this BinaryWriter writer, T[] array ) where T : struct
		{
			if (array.Length>0)
			{
				WriteArray( writer, array, typeof(T), array.Length );
			}
		}


		public static void Write<T>( this BinaryWriter writer, T[] array, int count ) where T : struct
		{
			if (count>array.Length) 
			{
				throw new ArgumentOutOfRangeException("count > array.Length");
			}
			WriteArray( writer, array, typeof(T), count );
		}


		public static void Write<T>( this BinaryWriter writer, T[,] array ) where T : struct
		{
			WriteArray( writer, array, typeof(T), array.Length );
		}
	}
}
