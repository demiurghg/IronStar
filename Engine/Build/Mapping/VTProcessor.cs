﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Mathematics;
using Fusion.Core.Shell;
using Fusion.Engine.Imaging;
using Fusion.Build.Processors;
using Fusion.Engine.Graphics;
using Fusion.Core;
using Fusion.Core.IniParser;
using Fusion.Core.IniParser.Model;
using Fusion.Core.Extensions;
using System.Diagnostics;
using Fusion.Core.Content;
using System.Threading;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Build.Mapping 
{
	public class VTGenerator : AssetGenerator 
	{
		string rootDir;

		public VTGenerator ( string rootDir )
		{
			this.rootDir	=	rootDir;
		}

		public override IEnumerable<AssetSource> Generate( IBuildContext context, BuildResult result )
		{
			var dir = Path.GetFullPath(rootDir);
			return new[] { new AssetSource("megatexture", dir, context.FullOutputDirectory, new VTProcessor(), context ) };
		}
	}


	[AssetProcessor("MegaTexture", "Performs megatexture assembly")]
	public class VTProcessor : AssetProcessor 
	{
		//public const string targetMegatexture		=	".megatexture";
		//public const string targetAllocator			=	".allocator";
		public const string targetAllocatorPath		=	"Content\\.vtstorage\\allocator.bin";
		public const string targetMegatexturePath	=	"Content\\.vtstorage\\index.bin";

		/// <summary>
		/// 
		/// </summary>
		public VTProcessor ()
		{
		}


		public override string GenerateParametersHash()
		{
			return ContentUtils.CalculateMD5Hash(GetType().AssemblyQualifiedName);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="buildContext"></param>
		public override void Process ( AssetSource assetFile, IBuildContext context )
		{
			Log.Message("-------- Virtual Texture --------" );

			var stopwatch	=	new Stopwatch();
			stopwatch.Start();

			var iniFiles	=	Directory.EnumerateFiles( assetFile.BaseDirectory, "*.material", SearchOption.AllDirectories).ToList();

			Log.Message("{0} megatexture segments", iniFiles.Count);


			//
			//	Process tiles :
			//
			using ( var tileStorage = context.GetVTStorage() ) 
			{
				var pageTable	=	CreateVTTextureTable( iniFiles, context, tileStorage );

				//
				//	Get allocator and pack/repack textures :
				//	
				Allocator2D allocator = null;

				if (File.Exists( targetAllocatorPath ) && File.Exists( targetMegatexturePath ) ) 
				{
					Log.Message("Loading VT allocator...");

					using ( var allocStream = File.OpenRead( targetAllocatorPath ) ) 
					{
						allocator		=	Allocator2D.LoadState( allocStream );
						Log.Message("Loading VT allocator...");
						var targetTime	=	File.GetLastWriteTimeUtc( targetMegatexturePath );

						Log.Message("Repacking textures to atlas...");
						RepackTextureAtlas( pageTable, allocator, targetTime );
					}

					using ( var vtStream = AssetStream.OpenRead( targetMegatexturePath ) ) 
					{
						var vt = new VirtualTexture( vtStream );
						foreach ( var tex in pageTable.SourceTextures ) 
						{
							tex.AverageColor = vt.GetTextureSegmentInfo( tex.Name ).AverageColor;
						}
					}
				} 
				else 
				{
					allocator = new Allocator2D(VTConfig.VirtualPageCount);

					Log.Message("Packing ALL textures to atlas...");
					PackTextureAtlas( pageTable.SourceTextures, allocator );
				}

				Log.Message("Saving VT allocator...");

				using ( var allocStream = File.OpenWrite( targetAllocatorPath ) ) 
				{
					Allocator2D.SaveState( allocStream, allocator );
				}

				//
				//	Generate top-level pages :
				//
				Log.Message( "Generating pages..." );
				GenerateMostDetailedPages( pageTable.SourceTextures, context, pageTable, tileStorage );


				//
				//	Generate mip-maps :
				//
				Log.Message("Generating mipmaps...");
				for (int mip=0; mip<VTConfig.MipCount-1; mip++) 
				{
					Log.Message("Generating mip level {0}/{1}...", mip, VTConfig.MipCount);
					GenerateMipLevels( context, pageTable, mip, tileStorage );
				}


				//
				//	Write asset :
				//
				using ( var stream = File.OpenWrite( targetMegatexturePath ) ) 
				{
					using ( var assetStream = AssetStream.OpenWrite( stream, "", new[] {""}, typeof(VirtualTexture) ) ) 
					{
						using ( var sw = new BinaryWriter( assetStream ) ) 
						{
							sw.Write( pageTable.SourceTextures.Count );

							foreach ( var tex in pageTable.SourceTextures ) 
							{
								VTTexture.Write( tex, sw );
							}
						}
					}
				}
			}

			stopwatch.Stop();
			Log.Message("{0}", stopwatch.Elapsed.ToString() );

			Log.Message("----------------" );
		}


																				 

		/// <summary>
		/// Creates VT tex table from INI-data and base directory
		/// </summary>
		/// <param name="iniData"></param>
		/// <param name="baseDirectory"></param>
		/// <returns></returns>
		VTTextureTable CreateVTTextureTable ( IEnumerable<string> materialFilePaths, IBuildContext context, IStorage tileStorage )
		{
			var texTable	=	new VTTextureTable();

			foreach ( var mtrlFile in materialFilePaths ) 
			{
				using ( var stream = File.OpenRead( mtrlFile ) ) 
				{
					var name		=	ContentUtils.GetPathWithoutExtension( context.GetRelativePath( mtrlFile ) );
					var content		=	Material.LoadFromIniFile( stream, name );
					var writeTime	=	File.GetLastWriteTimeUtc( mtrlFile );

					/*if (content.SkipProcessing) {
						continue;
					}*/

					try 
					{
						var tex = new VTTexture( content, name, context, writeTime );
						texTable.AddTexture( tex );
					} 
					catch ( Exception e ) 
					{
						Log.Warning("{0}. Skipped.", e.Message );
					}
				}
			}

			return texTable;
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="textures"></param>
		void PackTextureAtlas ( IEnumerable<VTTexture> textures, Allocator2D allocator )
		{
			foreach ( var tex in textures ) 
			{
				var size = Math.Max(tex.Width/128, tex.Height/128);

				var addr = allocator.Alloc( size, tex.Name );

				tex.TexelOffsetX	=	addr.X * VTConfig.PageSize;
				tex.TexelOffsetY	=	addr.Y * VTConfig.PageSize;
				tex.TilesDirty		=	true;

				Log.Message("...add: {0} : {1}x{2} : tile[{3},{4}]", tex.Name, tex.Width, tex.Height, addr.X, addr.Y );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="textures"></param>
		void RepackTextureAtlas ( VTTextureTable vtexTable, Allocator2D allocator, DateTime targetWriteTime )
		{
			//
			//	remove deleted and changes textures from allocator :
			//
			var blockInfo = allocator.GetAllocatedBlockInfo();

			foreach ( var block in blockInfo ) 
			{
				if (!vtexTable.Contains( block.Tag )) 
				{
					Log.Message("...removed: {0}", block.Tag);
					allocator.Free( block.Region );
				} 
				else 
				{
					if (vtexTable[ block.Tag ].IsModified(targetWriteTime)) 
					{
						Log.Message("...changed: {0}", block.Tag );
						allocator.Free( block.Region );
					} 
				}
			}


			//
			//	add missing textures (note, changed are already removed).
			//
			var blockDictionary	=	allocator.GetAllocatedBlockInfo().ToDictionary( bi => bi.Tag );
			var newTextureList		=	new List<VTTexture>();

			foreach ( var tex in vtexTable.SourceTextures ) 
			{
				Allocator2D.BlockInfo bi;

				if (blockDictionary.TryGetValue( tex.Name, out bi )) 
				{
					tex.TexelOffsetX = bi.Address.X * VTConfig.PageSize;
					tex.TexelOffsetY = bi.Address.Y * VTConfig.PageSize;
				}
				else 
				{
					newTextureList.Add( tex );
				}
			}

			PackTextureAtlas( newTextureList, allocator );
		}



		/// <summary>
		/// Split textures on tiles.
		/// </summary>
		/// <param name="textures"></param>
		void GenerateMostDetailedPages ( ICollection<VTTexture> textures, IBuildContext context, VTTextureTable pageTable, IStorage mapStorage )
		{
			int totalCount = textures.Count;
			int counter = 1;

			//foreach (var texture in textures)
			Parallel.ForEach( textures, texture => 
			{
				if (texture.TilesDirty) 
				{
					int counterValue = Interlocked.Increment( ref counter );

					Log.Message("...{0}/{1} - {2}", counterValue, totalCount, texture.Name );
					texture.SplitIntoPages( context, pageTable, mapStorage );
				}
			}
			);
		}


		int RoundUp2( int value, int mip )
		{
			return MathUtil.IntDivUp( value, 2 << mip ) * 2;
		}


		int RoundDown2( int value, int mip )
		{
			return (value >> mip) & 0x7FFFFFFE;
		}


		/// <summary>
		/// Generates mip levels for all tiles.
		/// </summary>
		/// <param name="buildContext"></param>
		/// <param name="pageTable"></param>
		/// <param name="sourceMipLevel"></param>
		/// <param name="mapStorage"></param>
		void GenerateMipLevels( IBuildContext buildContext, VTTextureTable pageTable, int sourceMipLevel, IStorage mapStorage )
		{
			if ( sourceMipLevel>=VTConfig.MipCount ) {
				throw new ArgumentOutOfRangeException( "mipLevel" );
			}

			//int count   = VTConfig.VirtualPageCount >> sourceMipLevel;
			int sizeB   = VTConfig.PageSizeBordered;
			var cache   = new TileSamplerCache( mapStorage );

			foreach ( var vttex in pageTable.SourceTextures ) 
			{
				if (!vttex.TilesDirty) 
				{
					continue;
				}

				int startX	= RoundDown2( vttex.AddressX, sourceMipLevel );
				int startY	= RoundDown2( vttex.AddressY, sourceMipLevel );

				int wTiles  = (vttex.Width / VTConfig.PageSize);
				int hTiles  = (vttex.Height / VTConfig.PageSize);

				int endExX	= RoundUp2( vttex.AddressX + wTiles, sourceMipLevel );
				int endExY	= RoundUp2( vttex.AddressY + hTiles, sourceMipLevel );

				for ( int pageX = startX; pageX < endExX; pageX+=2 ) 
				{
					for ( int pageY = startY; pageY < endExY; pageY+=2 ) 
					{
						var address			=	new VTAddress( pageX/2, pageY/2, sourceMipLevel+1 );

						var tile			=	new VTTile(address);

						var offsetX			=   (pageX) * VTConfig.PageSize;
						var offsetY			=   (pageY) * VTConfig.PageSize;
						var border			=   VTConfig.PageBorderWidth;

						var colorValue		=   Color.Zero;
						var normalValue		=   Color.Zero;
						var specularValue   =   Color.Zero;

						for ( int x = 0; x<sizeB; x++ ) 
						{
							for ( int y = 0; y<sizeB; y++ ) 
							{
								int srcX    =   offsetX + x*2 - border * 2;
								int srcY    =   offsetY + y*2 - border * 2;

								vttex.WrapCoordinates( ref srcX, ref srcY, sourceMipLevel );

								SampleMegatextureQ4( cache, srcX, srcY, sourceMipLevel, ref colorValue, ref normalValue, ref specularValue );

								tile.SetValues( x, y, ref colorValue, ref normalValue, ref specularValue );
							}
						}

						pageTable.SaveTile( address, mapStorage, tile );
					}
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="texelX"></param>
		/// <param name="texelY"></param>
		/// <param name="mipLevel"></param>
		/// <returns></returns>
		void SampleMegatextureQ4 ( TileSamplerCache cache, int texelX, int texelY, int mipLevel, ref Color a, ref Color b, ref Color c )
		{
			int textureSize	=	VTConfig.TextureSize >> mipLevel;
			
			texelX = MathUtil.Clamp( 0, texelX, textureSize );
			texelY = MathUtil.Clamp( 0, texelY, textureSize );

			int pageX	= texelX / VTConfig.PageSize;
			int pageY	= texelY / VTConfig.PageSize;
			int x		= texelX % VTConfig.PageSize;
			int y		= texelY % VTConfig.PageSize;
			int pbw		= VTConfig.PageBorderWidth;

			var address = new VTAddress( pageX, pageY, mipLevel );

			cache.LoadImage( address ).SampleQ4( x+pbw, y+pbw, ref a, ref b, ref c );
		}



		/// <summary>
		/// Generate one big texture where all textures are presented.
		/// </summary>
		void GenerateFallbackImage ( IBuildContext buildContext, VTTextureTable pageTable, int sourceMipLevel, IStorage storage )
		{
			int	pageSize		=	VTConfig.PageSize;
			int	numPages		=	VTConfig.VirtualPageCount >> sourceMipLevel;
			int	fallbackSize	=	VTConfig.TextureSize >> sourceMipLevel;

			var fallbackImage	=	new Image<Color>( fallbackSize, fallbackSize, Color.Black );

			for ( int pageX=0; pageX<numPages; pageX++) {
				for ( int pageY=0; pageY<numPages; pageY++) {

					var addr	=	new VTAddress( pageX, pageY, sourceMipLevel );
					var image	=	pageTable.LoadPage( addr, storage );

					for ( int x=0; x<pageSize; x++) {
						for ( int y=0; y<pageSize; y++) {

							int u = pageX * pageSize + x;
							int v = pageY * pageSize + y;

							fallbackImage.SetPixel( u, v, image.GetPixel( x, y ) );
						}
					}
				}
			}

			ImageLib.SaveTga( fallbackImage, storage.OpenWrite("fallback.tga") );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="image00"></param>
		/// <param name="image01"></param>
		/// <param name="image10"></param>
		/// <param name="image11"></param>
		/// <returns></returns>
		Image<Color> MipImages ( Image<Color> image00, Image<Color> image01, Image<Color> image10, Image<Color> image11 )
		{
			const int pageSize = VTConfig.PageSize;

			if (image00.Width!=image00.Height || image00.Width != pageSize ) {
				throw new ArgumentException("Bad image size");
			}
			if (image01.Width!=image01.Height || image01.Width != pageSize ) {
				throw new ArgumentException("Bad image size");
			}
			if (image10.Width!=image10.Height || image10.Width != pageSize ) {
				throw new ArgumentException("Bad image size");
			}
			if (image11.Width!=image11.Height || image11.Width != pageSize ) {
				throw new ArgumentException("Bad image size");
			}

			var image = new Image<Color>( pageSize, pageSize, Color.Black );

			for ( int i=0; i<pageSize/2; i++) {
				for ( int j=0; j<pageSize/2; j++) {
					image.SetPixel( i,j, image00.SampleMip( i, j, ImageLib.AverageFourSamples ) );
				}
			}

			for ( int i=pageSize/2; i<pageSize; i++) {
				for ( int j=pageSize/2; j<pageSize; j++) {
					image.SetPixel( i,j, image11.SampleMip( i, j, ImageLib.AverageFourSamples ) );
				}
			}

			for ( int i=0; i<pageSize/2; i++) {
				for ( int j=pageSize/2; j<pageSize; j++) {
					image.SetPixel( i,j, image01.SampleMip( i, j, ImageLib.AverageFourSamples ) );
				}
			}

			for ( int i=pageSize/2; i<pageSize; i++) {
				for ( int j=0; j<pageSize/2; j++) {
					image.SetPixel( i,j, image10.SampleMip( i, j, ImageLib.AverageFourSamples ) );
				}
			}

			return image;
		} 


	}
}
