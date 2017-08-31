using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DXGI = SharpDX.DXGI;
using D3D = SharpDX.Direct3D11;
using SharpDX;
using SharpDX.Direct3D11;
using System.Runtime.InteropServices;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using SharpDX.Direct3D;			
using Native.Dds;
using Native.Wic;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Engine.Storage;


namespace Fusion.Drivers.Graphics {
	internal class Texture2DStaging : DisposableBase {

		D3D.Texture2D	tex2D;
		ColorFormat		format;

		public readonly GraphicsDevice	device;

		public readonly int Width;
		public readonly int Height;
		

		/// <summary>
		/// Creates texture
		/// </summary>
		/// <param name="device"></param>
		public Texture2DStaging ( GraphicsDevice device, int width, int height, ColorFormat format )
		{
			this.device		=	device;

			Width			=	width;
			Height			=	height;
			this.format		=	format;

			var texDesc = new Texture2DDescription();
			texDesc.ArraySize		=	1;
			texDesc.BindFlags		=	BindFlags.ShaderResource;
			texDesc.CpuAccessFlags	=	CpuAccessFlags.Write;
			texDesc.Format			=	Converter.Convert( format );
			texDesc.Height			=	Height;
			texDesc.MipLevels		=	1;
			texDesc.OptionFlags		=	ResourceOptionFlags.None;
			texDesc.SampleDescription.Count	=	1;
			texDesc.SampleDescription.Quality	=	0;
			texDesc.Usage			=	ResourceUsage.Dynamic;
			texDesc.Width			=	Width;
													 
			lock (device.DeviceContext) {
				tex2D	=	new D3D.Texture2D( device.Device, texDesc );
			}
		}
		



		/// <summary>
		/// Disposes
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref tex2D );
				//SafeDispose( ref srgbResource );
				//SafeDispose( ref linearResource );
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dstTexture"></param>
		/// <param name="srcRect"></param>
		/// <param name="dstRect"></param>
		public void CopyToTexture ( Texture2D dstTexture, int level, int x, int y )
		{
			device.DeviceContext.CopySubresourceRegion( tex2D, 0, null, dstTexture.Tex2D, level, x, y );
		}
		


		/// <summary>
		/// Sets 2D texture data, specifying a mipmap level, source rectangle, start index, and number of elements.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="level"></param>
		/// <param name="data"></param>
		/// <param name="startIndex"></param>
		/// <param name="elementCount"></param>
		public void SetData<T>( int level, T[] data ) where T: struct
		{

			var dataBox				=	device.DeviceContext.MapSubresource( tex2D, level, MapMode.WriteDiscard, MapFlags.None );

			var elementSizeInByte	=	Marshal.SizeOf(typeof(T));
			var elementPitch		=	dataBox.RowPitch / elementSizeInByte;
			var pointer				=	dataBox.DataPointer;

			int width				=	Width >> level;
			int height				=	Height >> level;

			for ( int row = 0; row < height; row++ ) {

				Utilities.Write( pointer, data, width * row, width );
				pointer += dataBox.RowPitch;

			}

			device.DeviceContext.UnmapSubresource( tex2D, level );
		}


	}
}
