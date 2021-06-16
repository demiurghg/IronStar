﻿#define DIRECTX
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows;
using MediaPixelFormat = System.Windows.Media.PixelFormat;
using System.Windows.Media.Imaging;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using Fusion.Core.Mathematics;


namespace Fusion.Drivers.Graphics 
{
	public class RenderTarget2D : ShaderResource 
	{
		/// <summary>
		/// Samples count
		/// </summary>
		public int	SampleCount { get; private set; }

		/// <summary>
		/// Mipmap levels count
		/// </summary>
		public int	MipCount { get; private set; }

		/// <summary>
		/// Render target format
		/// </summary>
		public ColorFormat	Format { get; private set; }


		D3D.Texture2D			tex2D;
		RenderTargetSurface[]	surfaces;
		ShaderResource[]		mipSrvs;
			

		/// <summary>
		/// 
		/// </summary>
		public Rectangle Bounds { get { return new Rectangle( 0, 0, Width, Height ); } }


		/// <summary>
		/// Creates render target
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="format"></param>
		public RenderTarget2D ( GraphicsDevice device, ColorFormat format, int width, int height, bool enableRWBuffer = false ) : base ( device )
		{
			Create( format, width, height, 1, 1, enableRWBuffer );
		}



		/// <summary>
		/// Internal constructor to create RT for backbuffer.
		/// </summary>
		/// <param name="device"></param>
		/// <param name="backbufColor"></param>
		internal RenderTarget2D ( GraphicsDevice device, D3D.Texture2D backbufColor, RenderTargetViewDescription? desc = null ) : base( device )
		{
			Log.Debug("RenderTarget2D: from backbuffer.");

			if (backbufColor.Description.Format!=DXGI.Format.R8G8B8A8_UNorm) {
				Log.Warning("R8G8B8A8_UNorm");
			}

			Width			=	backbufColor.Description.Width;
			Height			=	backbufColor.Description.Height;
			Format			=	ColorFormat.Rgba8;
			MipCount		=	1;
			SampleCount		=	backbufColor.Description.SampleDescription.Count;
			SRV				=	null;

			tex2D			=	backbufColor;
			surfaces		=	new RenderTargetSurface[1];

			if (desc.HasValue) {
				surfaces[0] = new RenderTargetSurface( device, new RenderTargetView(device.Device, backbufColor, desc.Value), null, tex2D, 0, Format, Width, Height, SampleCount);
			}
			else {
				surfaces[0] = new RenderTargetSurface( device, new RenderTargetView(device.Device, backbufColor), null, tex2D, 0, Format, Width, Height, SampleCount);
			}
		} 



		/// <summary>
		/// Creates render target
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="format"></param>
		public RenderTarget2D ( GraphicsDevice device, ColorFormat format, int width, int height, int samples ) : base ( device )
		{
			Create( format, width, height, samples, 1, false );
		}



		/// <summary>
		/// Creates render target
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="format"></param>
		public RenderTarget2D ( GraphicsDevice device, ColorFormat format, int width, int height, bool mips, bool enableRWBuffer ) : base ( device )
		{
			Create( format, width, height, 1, mips?0:1, enableRWBuffer );
		}



		/// <summary>
		/// Creates render target
		/// </summary>
		/// <param name="?"></param>
		/// <param name="format"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="samples"></param>
		/// <param name="mips"></param>
		/// <param name="debugName"></param>
		void Create ( ColorFormat format, int width, int height, int samples, int mips, bool enableRWBuffer )
		{
			//Log.Debug("RenderTarget2D: f:{0} w:{1} h:{2} s:{3}{4}{5}", format, width, height, samples, mips?" mips":"", enableRWBuffer?" uav":"" );

			bool msaa	=	samples > 1;

			CheckSamplesCount( samples );

			if (mips!=1 && samples>1) {
				throw new ArgumentException("Render target should be multisampler either mipmapped");
			}

			SampleCount		=	samples;

			Format		=	format;
			SampleCount	=	samples;
			Width		=	width;
			Height		=	height;
			Depth		=	1;
			MipCount	=	mips==0 ? ShaderResource.CalculateMipLevels( width, height ) : mips;

			var	texDesc	=	new Texture2DDescription();
				texDesc.Width				=	width;
				texDesc.Height				=	height;
				texDesc.ArraySize			=	1;
				texDesc.BindFlags			=	BindFlags.RenderTarget | BindFlags.ShaderResource;
				texDesc.CpuAccessFlags		=	CpuAccessFlags.None;
				texDesc.Format				=	Converter.Convert( format );
				texDesc.MipLevels			=	MipCount;
				texDesc.OptionFlags			=	(mips!=1) ? ResourceOptionFlags.GenerateMipMaps : ResourceOptionFlags.None;
				texDesc.SampleDescription	=	new DXGI.SampleDescription(samples, 0);
				texDesc.Usage				=	ResourceUsage.Default;

			if (enableRWBuffer) 
			{
				texDesc.BindFlags |= BindFlags.UnorderedAccess;
			}

			var	srvDesc	=	new ShaderResourceViewDescription();
				srvDesc.Dimension					=	ShaderResourceViewDimension.Texture2D;
				srvDesc.Format						=	Converter.Convert( format );
				srvDesc.Texture2D.MipLevels			=	MipCount;
				srvDesc.Texture2D.MostDetailedMip	=	0;

			#warning Remove block below, dup
			if (enableRWBuffer) 
			{
				texDesc.BindFlags |= BindFlags.UnorderedAccess;
			}

			tex2D	=	new D3D.Texture2D( device.Device, texDesc );
			SRV		=	new ShaderResourceView( device.Device, tex2D, srvDesc );

			//
			//	Create surfaces :
			//
			surfaces	=	new RenderTargetSurface[ MipCount ];
			mipSrvs		=	new ShaderResource[ MipCount ];

			for ( int i=0; i<MipCount; i++ ) 
			{ 
				width	=	GetMipSize( Width,  i );
				height	=	GetMipSize( Height, i );

				var rtvDesc = new RenderTargetViewDescription();
					rtvDesc.Texture2D.MipSlice	=	i;
					rtvDesc.Dimension			=	msaa ? RenderTargetViewDimension.Texture2DMultisampled : RenderTargetViewDimension.Texture2D;
					rtvDesc.Format				=	Converter.Convert( format );

				var rtv	=	new RenderTargetView( device.Device, tex2D, rtvDesc );

				
				srvDesc = new ShaderResourceViewDescription();
					srvDesc.Dimension					=	ShaderResourceViewDimension.Texture2D;
					srvDesc.Format						=	Converter.Convert( format );
					srvDesc.Texture2D.MipLevels			=	1;
					srvDesc.Texture2D.MostDetailedMip	=	i;

				var srv	=	new ShaderResourceView( device.Device, tex2D, srvDesc );

				mipSrvs[i]	=	new ShaderResource( device, srv, width, height, 1 );

				
				UnorderedAccessView uav = null;

				if (enableRWBuffer) 
				{
					var uavDesc = new UnorderedAccessViewDescription();
					uavDesc.Buffer.ElementCount	=	width * height;
					uavDesc.Buffer.FirstElement	=	0;
					uavDesc.Buffer.Flags		=	UnorderedAccessViewBufferFlags.None;
					uavDesc.Dimension			=	UnorderedAccessViewDimension.Texture2D;
					uavDesc.Format				=	Converter.Convert( format );
					uavDesc.Texture2D.MipSlice	=	i;

					uav	=	new UnorderedAccessView( device.Device, tex2D, uavDesc );
				}

				surfaces[i]	=	new RenderTargetSurface( device, rtv, uav, tex2D, i, format, width, height, samples );
			}
		}



		/// <summary>
		/// Gets top mipmap level's surface.
		/// Equivalent for GetSurface(0)
		/// </summary>
		public RenderTargetSurface Surface {
			get {
				return GetSurface( 0 );
			}
		}



		/// <summary>
		/// Gets render target surface for given mip level.
		/// </summary>
		/// <param name="mipLevel"></param>
		/// <returns></returns>
		public RenderTargetSurface GetSurface ( int mipLevel )
		{
			return surfaces[ mipLevel ];
		}



		/// <summary>
		/// Gets render target surface for given mip level.
		/// </summary>
		/// <param name="mipLevel"></param>
		/// <returns></returns>
		public ShaderResource GetShaderResource ( int mipLevel )
		{
			return mipSrvs[ mipLevel ];
		}



		/// <summary>
		/// Builds mipmap chain.
		/// </summary>
		public void BuildMipmaps ()
		{
			lock (device.DeviceContext) 
			{
				device.DeviceContext.GenerateMips( SRV );
			}
		}



		/// <summary>
		/// Sets viewport for given render target
		/// </summary>
		public void SetViewport ()
		{
			lock (device.DeviceContext) 
			{
				device.DeviceContext.Rasterizer.SetViewport( 0,0, Width, Height, 0, 1 );
			}
		}



		/// <summary>
		/// Disposes
		/// </summary>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref surfaces );
				SafeDispose( ref mipSrvs );

				SafeDispose( ref SRV );
				SafeDispose( ref tex2D );
			}

			base.Dispose(disposing);
		}



		/// <summary>
		/// Gets a copy of 2D texture data, specifying a mipmap level, source rectangle, start index, and number of elements.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="level"></param>
		/// <param name="rect"></param>
		/// <param name="data"></param>
		/// <param name="startIndex"></param>
		/// <param name="elementCount"></param>
		public void GetData<T>(int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
		{
			if (data == null || data.Length == 0) {
				throw new ArgumentException("data cannot be null");
			}
			
			if (data.Length < startIndex + elementCount) {
				throw new ArgumentException("The data passed has a length of " + data.Length + " but " + elementCount + " pixels have been requested.");
			}

			if (rect.HasValue) {
				throw new NotImplementedException("Set 'rect' parameter to null.");
			}

			var mipWidth	=	Resource.CalculateMipSize( level, Width );
			var mipHeight	=	Resource.CalculateMipSize( level, Height );

			// Create a temp staging resource for copying the data.
			// 
			// TODO: We should probably be pooling these staging resources
			// and not creating a new one each time.
			//
			var desc = new Texture2DDescription();
				desc.Width						= mipWidth;
				desc.Height						= mipHeight;
				desc.MipLevels					= 1;
				desc.ArraySize					= 1;
				desc.Format						= Converter.Convert(Format);
				desc.BindFlags					= D3D.BindFlags.None;
				desc.CpuAccessFlags				= D3D.CpuAccessFlags.Read;
				desc.SampleDescription.Count	= 1;
				desc.SampleDescription.Quality	= 0;
				desc.Usage						= D3D.ResourceUsage.Staging;
				desc.OptionFlags				= D3D.ResourceOptionFlags.None;

			
			var d3dContext	=	device.DeviceContext;
			var elementSize	=	Marshal.SizeOf(typeof(T));
			var pixelSize	=	Converter.SizeOf(Format);

			lock (device.DeviceContext) 
			{
				using (var stagingTex = new D3D.Texture2D(device.Device, desc)) 
				{
					//
					// Copy the data from the GPU to the staging texture.
					//
					int elementsInRow;
					int rows;
					
					if (rect.HasValue) 
					{
						elementsInRow = rect.Value.Width * pixelSize / elementSize;
						rows = rect.Value.Height;

						var region = new D3D.ResourceRegion( rect.Value.Left, rect.Value.Top, 0, rect.Value.Right, rect.Value.Bottom, 1 );

						d3dContext.CopySubresourceRegion( tex2D, level, region, stagingTex, 0, 0, 0, 0);
					} 
					else 
					{
						elementsInRow = mipWidth * pixelSize / elementSize;
						rows = mipHeight;

						d3dContext.CopySubresourceRegion( tex2D, level, null, stagingTex, 0, 0, 0, 0);
					}


					// Copy the data to the array :
					DataStream stream;
					var databox = d3dContext.MapSubresource(stagingTex, 0, D3D.MapMode.Read, D3D.MapFlags.None, out stream);

					// Some drivers may add pitch to rows.
					// We need to copy each row separatly and skip trailing zeros.
					var currentIndex	=	startIndex;
					
					for (var row = 0; row < rows; row++) 
					{
						stream.ReadRange(data, currentIndex, elementsInRow);
						stream.Seek(databox.RowPitch - (elementSize * elementsInRow), SeekOrigin.Current);
						currentIndex += elementsInRow;

					}
					stream.Dispose();
				}
			}
		}


		public void CopyTo ( RenderTarget2D destination )
		{
			if (destination.Width  != Width ) throw new ArgumentException("destination.Width != Width" );
			if (destination.Height != Height) throw new ArgumentException("destination.Height != Height");
			if (destination.Format != Format) throw new ArgumentException("destination.Format != Format");

			lock (device.DeviceContext) 
			{
				device.DeviceContext.CopySubresourceRegion( tex2D, 0, null, destination.tex2D, 0 );
			}
		}


		/// <summary>
		/// Gets a copy of 2D texture data, specifying a start index and number of elements.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		/// <param name="startIndex"></param>
		/// <param name="elementCount"></param>
		public void GetData<T>(T[] data, int startIndex, int elementCount) where T : struct
		{
			this.GetData(0, null, data, startIndex, elementCount);
		}
		


		/// <summary>
		/// Gets a copy of 2D texture data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		public void GetData<T> (T[] data) where T : struct
		{
			this.GetData(0, null, data, 0, data.Length);
		}



		/// <summary>
		/// Saves rendertarget to file.
		/// Output image format will be get automatically from extension.
		/// BMP, DDS, GIF, JPG, PNG, TIFF, WMP are supported.
		/// </summary>
		/// <param name="path"></param>
		public void SaveToFile ( string path )
		{
			//Log.Error("Screenshot are not implemented!");

			var sw = new Stopwatch();
			sw.Start();

			lock ( device.DeviceContext ) 
			{
				if (SampleCount>1) 
				{
												
					using( var temp = new RenderTarget2D( this.device, this.Format, this.Width, this.Height, false, false ) ) 
					{
						this.device.Resolve( this, temp );
						temp.SaveToFile( path );
					}
				
				} else 
				{

					var pixelCount	=	Width * Height;
					var pixels		=   new Color[ pixelCount ];
					var rawData		=   new byte[ pixelCount * 3 ];
					GetData( pixels );

					for ( int i=0; i<pixelCount; i++ ) 
					{
						rawData[i*3 + 0] = pixels[i].B;
						rawData[i*3 + 1] = pixels[i].G;
						rawData[i*3 + 2] = pixels[i].R;
					}


					var encoder = new PngBitmapEncoder();
					var format  = System.Windows.Media.PixelFormats.Bgr24;
					var source  = BitmapSource.Create( Width, Height, 96,96, format, null, rawData, Width*3 );
					var frame   = BitmapFrame.Create( source );

					encoder.Frames.Add( frame );

					using ( var stream = File.OpenWrite( path ) ) 
					{
						encoder.Save( stream );
					}
				}
			}

			sw.Stop();
			Log.Message("Screenshot: {1} ms, path {0}", path, sw.ElapsedMilliseconds );
		}


		/// <summary>
		/// Sets 2D texture data, specifying a destination rectangle
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="rect"></param>
		/// <param name="data"></param>
		public void SetData<T> ( int level, Rectangle rect, T[] data ) where T: struct
		{
			SetData<T>( level, rect, data, 0, data.Length );
		}


		/// <summary>
		/// Sets 2D texture data.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		public void SetData<T>(T[] data) where T : struct
        {
			this.SetData(0, null, data, 0, data.Length);
        }


		/// <summary>
		/// Sets 2D texture data, specifying a mipmap level, source rectangle, start index, and number of elements.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="level"></param>
		/// <param name="data"></param>
		/// <param name="startIndex"></param>
		/// <param name="elementCount"></param>
		public void SetData<T>( int level, Rectangle? rect, T[] data, int startIndex, int elementCount ) where T: struct
		{
			var elementSizeInByte	=	Marshal.SizeOf(typeof(T));
			var dataHandle			=	GCHandle.Alloc(data, GCHandleType.Pinned);
			// Use try..finally to make sure dataHandle is freed in case of an error
			lock (device.DeviceContext) 
			{
				try {
					var startBytes	=	startIndex * elementSizeInByte;
					var dataPtr		=	(IntPtr)(dataHandle.AddrOfPinnedObject().ToInt64() + startBytes);

					int x, y, w, h;
					if (rect.HasValue) 
					{
						x = rect.Value.X;
						y = rect.Value.Y;
						w = rect.Value.Width;
						h = rect.Value.Height;
					} 
					else 
					{
						x = 0;
						y = 0;
						w = SharpDX.Direct3D11.Resource.CalculateMipSize( level, Width );
						h = SharpDX.Direct3D11.Resource.CalculateMipSize( level, Height );
					}

					var box = new SharpDX.DataBox(dataPtr, w * Converter.SizeOf(Format), 0);

					var region		= new SharpDX.Direct3D11.ResourceRegion();
					region.Top		= y;
					region.Front	= 0;
					region.Back		= 1;
					region.Bottom	= y + h;
					region.Left		= x;
					region.Right	= x + w;

					device.DeviceContext.UpdateSubresource(box, tex2D, level, region);

				} 
				finally 
				{
					dataHandle.Free();
				}
			}
		}
	}
}
