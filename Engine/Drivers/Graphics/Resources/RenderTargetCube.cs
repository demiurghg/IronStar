﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Specialized;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Fusion.Core.Mathematics;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using System.Runtime.InteropServices;

namespace Fusion.Drivers.Graphics {


	internal class RenderTargetCube : ShaderResource {

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

		/// <summary>
		/// Texture resource
		/// </summary>
		internal D3D.Texture2D TextureResource {
			get {
				return texCube;
			}
		}

		D3D.Texture2D			texCube;
		D3D.Texture2D			staging;
		RenderTargetSurface[,]	surfaces;
			

		/// <summary>
		///	Gets topmost miplevel as shader resource.
		/// </summary>
		ShaderResource[]	cubeMipShaderResources;


		/// <summary>
		/// Creates render target
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="format"></param>
		public RenderTargetCube ( GraphicsDevice device, ColorFormat format, int size, string debugName = "" ) : base ( device )
		{
			Create( format, size, false, debugName );
		}



		/// <summary>
		/// Creates render target
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="format"></param>
		public RenderTargetCube ( GraphicsDevice device, ColorFormat format, int size, bool mips, string debugName = "" ) : base ( device )
		{
			Create( format, size, mips, debugName );
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
		void Create ( ColorFormat format, int size, bool mips, string debugName )
		{
			SampleCount	=	1;
			Format		=	format;
			Width		=	size;
			Height		=	size;
			Depth		=	1;
			MipCount	=	mips ? ShaderResource.CalculateMipLevels( Width, Height ) : 1;

			var	texDesc	=	new Texture2DDescription();
				texDesc.Width				=	Width;
				texDesc.Height				=	Height;
				texDesc.ArraySize			=	6;
				texDesc.BindFlags			=	BindFlags.RenderTarget | BindFlags.ShaderResource | BindFlags.UnorderedAccess;
				texDesc.CpuAccessFlags		=	CpuAccessFlags.None;
				texDesc.Format				=	Converter.Convert( format );
				texDesc.MipLevels			=	MipCount;
				texDesc.OptionFlags			=	ResourceOptionFlags.TextureCube | (mips ? ResourceOptionFlags.GenerateMipMaps : ResourceOptionFlags.None);
				texDesc.SampleDescription	=	new DXGI.SampleDescription(1, 0);
				texDesc.Usage				=	ResourceUsage.Default;


			texCube	=	new D3D.Texture2D( device.Device, texDesc );
			SRV		=	new ShaderResourceView( device.Device, texCube );

			//
			//	create staging resource using previous description :
			//
			texDesc.BindFlags		=	BindFlags.None;
			texDesc.CpuAccessFlags	=	CpuAccessFlags.Write | CpuAccessFlags.Read;
			texDesc.Usage			=	ResourceUsage.Staging;
			texDesc.OptionFlags		=	ResourceOptionFlags.TextureCube;

			staging	=	new D3D.Texture2D( device.Device, texDesc );


			//
			//	Top mipmap level :
			//
			cubeMipShaderResources = new ShaderResource[MipCount];

			for (int mip=0; mip<MipCount; mip++) {

				var srvDesc = new ShaderResourceViewDescription();
					srvDesc.TextureCube.MipLevels		=	1;
					srvDesc.TextureCube.MostDetailedMip	=	mip;
					srvDesc.Format		=	Converter.Convert( format );
					srvDesc.Dimension	=	ShaderResourceViewDimension.TextureCube;

				cubeMipShaderResources[mip]	=	new ShaderResource( device, new ShaderResourceView(device.Device, texCube, srvDesc), size>>mip, size>>mip, 1 );
			}



			//
			//	Create surfaces :
			//
			surfaces	=	new RenderTargetSurface[ MipCount, 6 ];

			for ( int mip=0; mip<MipCount; mip++ ) { 

				int width	=	GetMipSize( Width,  mip );
				int height	=	GetMipSize( Height, mip );

				for ( int face=0; face<6; face++) {

					var rtvDesc = new RenderTargetViewDescription();
						rtvDesc.Texture2DArray.MipSlice			=	mip;
						rtvDesc.Texture2DArray.FirstArraySlice	=	face;
						rtvDesc.Texture2DArray.ArraySize		=	1;
						rtvDesc.Dimension						=	RenderTargetViewDimension.Texture2DArray;
						rtvDesc.Format							=	Converter.Convert( format );

					var rtv	=	new RenderTargetView( device.Device, texCube, rtvDesc );

					var uavDesc = new UnorderedAccessViewDescription();
						uavDesc.Buffer.ElementCount	=	width * height;
						uavDesc.Buffer.FirstElement	=	0;
						uavDesc.Buffer.Flags		=	UnorderedAccessViewBufferFlags.None;
						uavDesc.Dimension			=	UnorderedAccessViewDimension.Texture2DArray;
						uavDesc.Format				=	Converter.Convert( format );
						uavDesc.Texture2DArray.ArraySize		=	1;
						uavDesc.Texture2DArray.FirstArraySlice	=	face;
						uavDesc.Texture2DArray.MipSlice			=	mip;

					var uav	=	new UnorderedAccessView( device.Device, texCube, uavDesc );

					int subResId	=	Resource.CalculateSubResourceIndex( mip, face, MipCount );

					surfaces[mip,face]	=	new RenderTargetSurface( rtv, uav, texCube, subResId, format, Width, Height, 1 );

					GraphicsDevice.Clear( surfaces[mip,face], Color4.Zero );
				}
			}
		}



		public RenderTargetSurface FacePosX { get {	return GetSurface( 0, CubeFace.FacePosX );	} }
		public RenderTargetSurface FaceNegX { get {	return GetSurface( 0, CubeFace.FaceNegX );	} }
		public RenderTargetSurface FacePosY { get {	return GetSurface( 0, CubeFace.FacePosY );	} }
		public RenderTargetSurface FaceNegY { get {	return GetSurface( 0, CubeFace.FaceNegY );	} }
		public RenderTargetSurface FacePosZ { get {	return GetSurface( 0, CubeFace.FacePosZ );	} }
		public RenderTargetSurface FaceNegZ { get {	return GetSurface( 0, CubeFace.FaceNegZ );	} }



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="face"></param>
		/// <param name="level"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public void GetData<T>( CubeFace face, int level, T[] data ) where T : struct
		{
			int startIndex		=	0;
			int elementCount	=	data.Length;

            if (data == null || data.Length == 0) {
                throw new ArgumentException("data cannot be null");
			}
            
			if (data.Length < startIndex + elementCount) {
                throw new ArgumentException("The data passed has a length of " + data.Length + " but " + elementCount + " pixels have been requested.");
			}

			var d3dContext = device.DeviceContext;


			lock (d3dContext) {

				//
                // Copy the data from the GPU to the staging texture.
				//
                int elementsInRow;
                int rows;
                    
				elementsInRow = Width;
                rows = Height;

				int subres	=	CalcSubresource( level, (int)face, MipCount );

                d3dContext.CopySubresourceRegion( texCube, subres, null, staging, 0, 0, 0, 0);

                // Copy the data to the array :
                DataStream stream;
                var databox = d3dContext.MapSubresource(staging, 0, D3D.MapMode.Read, D3D.MapFlags.None, out stream);

                // Some drivers may add pitch to rows.
                // We need to copy each row separatly and skip trailing zeros.
                var currentIndex	=	startIndex;
                var elementSize		=	Marshal.SizeOf(typeof(T));
                    
				for (var row = 0; row < rows; row++) {

                    stream.ReadRange(data, currentIndex, elementsInRow);
                    stream.Seek(databox.RowPitch - (elementSize * elementsInRow), SeekOrigin.Current);
                    currentIndex += elementsInRow;

                }

				d3dContext.UnmapSubresource( staging, 0 );

                stream.Dispose();
            }
		}





		public ShaderResource GetCubeShaderResource ( int mipLevel )
		{
			return cubeMipShaderResources[mipLevel];
		}


		/// <summary>
		/// Gets render target surface for given mip level.
		/// </summary>
		/// <param name="mipLevel"></param>
		/// <returns></returns>
		public RenderTargetSurface GetSurface ( int mipLevel, CubeFace face )
		{
			return surfaces[ mipLevel, (int)face ];
		}



		/// <summary>
		/// Builds mipmap chain.
		/// </summary>
		public void BuildMipmaps ()
		{
			device.DeviceContext.GenerateMips( SRV );
		}



		/// <summary>
		/// Sets viewport for given render target
		/// </summary>
		public void SetViewport ()
		{
			device.DeviceContext.Rasterizer.SetViewport( 0,0, Width, Height, 0, 1 );
		}



		/// <summary>
		/// Disposes
		/// </summary>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				if (surfaces!=null) {
					
					for (int mip=0; mip<MipCount; mip++) {
						for (int face=0; face<6; face++) {
							var surf = surfaces[mip,face];
							SafeDispose( ref surf );
						}
					}
					surfaces = null;
				}

				SafeDispose( ref cubeMipShaderResources );

				SafeDispose( ref SRV );
				SafeDispose( ref texCube );
				SafeDispose( ref staging );
			}

			base.Dispose(disposing);
		}
	}
}
