using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Imaging;
using Fusion.Core;
using Fusion.Core.IniParser.Model;
using Fusion.Core.Content;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Build.Mapping {

	internal class VTTexture {

		readonly IBuildContext context;

		public readonly string	Name;

		public readonly string  BaseColor	;
		public readonly string  NormalMap	;
		public readonly string  Metallic	;
		public readonly string  Roughness	;
		public readonly string	Emission	;
		public readonly string	Occlusion	;
		public readonly bool	Transparent	;
		public readonly bool	MaskEmission;
		public readonly bool	InvertYNormal;

		public int TexelOffsetX;
		public int TexelOffsetY;

		public Color AverageColor		=	Color.Gray;

		public bool TilesDirty;

		public readonly int Width;
		public readonly int Height;

		public int AddressX { get { return TexelOffsetX / VTConfig.PageSize; } }
		public int AddressY { get { return TexelOffsetY / VTConfig.PageSize; } }

		private readonly DateTime sourceLastWriteTime;



		/// <summary>
		/// 
		/// </summary>
		/// <param name="fullPath"></param>
		public VTTexture ( Material material, string name, IBuildContext context, DateTime sourceLastWriteTime )
		{			
			this.context				=	context;
			const int pageSize			=	VTConfig.PageSize;
			this.sourceLastWriteTime	=	sourceLastWriteTime;

			var dir		=	Path.GetDirectoryName(name);

			Name			=	name;
			BaseColor		=   CombinePathIfNotEmpty( dir, material.ColorMap		);
			NormalMap		=   CombinePathIfNotEmpty( dir, material.NormalMap		);
			Metallic		=   CombinePathIfNotEmpty( dir, material.MetallicMap	);
			Roughness		=   CombinePathIfNotEmpty( dir, material.RoughnessMap	);
			Emission		=   CombinePathIfNotEmpty( dir, material.EmissionMap	);
			Occlusion		=	CombinePathIfNotEmpty( dir, material.OcclusionMap	);
			Transparent		=	material.Transparent;
			MaskEmission	=	material.MaskEmission;
			InvertYNormal	=	material.InvertYNormal;


			if (string.IsNullOrWhiteSpace(BaseColor)) {	
				throw new BuildException("BaseColor must be specified for material '" + material.Name + "'");
			}
			
			var fullPath	=	context.ResolveContentPath( BaseColor );

			var size = TakeImageSize( Name, fullPath, context );

			if (size.Height%pageSize!=0) {
				throw new BuildException(string.Format("Width of '{0}' must be multiple of {1}", fullPath, pageSize));
			}
			if (size.Width%pageSize!=0) {
				throw new BuildException(string.Format("Height of '{0}' must be multiple of {1}", fullPath, pageSize));
			}

			Width	=	size.Width;
			Height	=	size.Height;
		}



		string CombinePathIfNotEmpty ( string basePath, string path )
		{
			if (string.IsNullOrWhiteSpace(path)) {
				return null;
			} else {
				return Path.Combine( basePath, path );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void WrapCoordinates ( ref int x, ref int y, int mip )
		{
			var ox = TexelOffsetX >> mip;
			var oy = TexelOffsetY >> mip;
			
			var w  = Width  >> mip;
			var h  = Height >> mip;

			if ( w <= VTConfig.PageSize || h <= VTConfig.PageSize ) {
				return;
			}

			x = MathUtil.Wrap( x, ox, ox + w - 1 );
			y = MathUtil.Wrap( y, oy, oy + h - 1 );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetLastWriteTime"></param>
		/// <param name="sourceLastWriteTime"></param>
		/// <returns></returns>
		public bool IsModified ( DateTime targetLastWriteTime )
		{
			if ( sourceLastWriteTime > targetLastWriteTime ) {
				return true;
			} 
			return  IsTextureModifiedSince( targetLastWriteTime, BaseColor	)
				 || IsTextureModifiedSince( targetLastWriteTime, NormalMap	)
				 || IsTextureModifiedSince( targetLastWriteTime, Metallic	)
				 || IsTextureModifiedSince( targetLastWriteTime, Roughness	)
				 || IsTextureModifiedSince( targetLastWriteTime, Occlusion	)
				 || IsTextureModifiedSince( targetLastWriteTime, Emission	)
				 ;
		}



		public VTSegment GetSegmentInfo ()
		{
			return new VTSegment( 
				Name			,
				TexelOffsetX	,
				TexelOffsetY	,
				Width			,
				Height			,
				AverageColor	,
				Transparent		
			);				
		}


		public static void Write ( VTTexture vtex, BinaryWriter writer )
		{
			writer.Write( vtex.Name );
			writer.Write( vtex.TexelOffsetX );
			writer.Write( vtex.TexelOffsetY );
			writer.Write( vtex.Width );
			writer.Write( vtex.Height );
			writer.Write( vtex.Transparent );
			writer.Write( vtex.AverageColor );
		}


		/*public static VTTexture Read ( BinaryWriter writer )
		{
			writer.Write( vtex.KeyPath );
			writer.Write( vtex.TexelOffsetX );
			writer.Write( vtex.TexelOffsetY );
			writer.Write( vtex.Width );
			writer.Write( vtex.Height );
		} */



		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyPath"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		Size2 TakeImageSize ( string mtrlName, string keyPath, IBuildContext context )
		{
			var fullPath	=	context.ResolveContentPath( keyPath );
			var ext			=	Path.GetExtension(keyPath).ToLowerInvariant();

			using ( var stream = File.OpenRead( fullPath ) ) {
				if ( ext==".tga" ) {
					var header = ImageLib.TakeTga( stream );
					return new Size2( header.width, header.height );
				} else
				if ( ext==".png" ) {
					return ImageLib.TakePngSize( stream );
				} else
				if ( ext==".jpg" ) {
					return ImageLib.TakeJpgSize( stream );
				} else {
					throw new BuildException("Material " + mtrlName + " must refer TGA, JPG or PNG image");
				}
			}
		}


		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="uv"></param>
		/// <returns></returns>
		public Vector2 RemapTexCoords ( Vector2 uv )
		{
			double size	= VTConfig.TextureSize;

			double u = ( MathUtil.Wrap(uv.X,0,1) * Width  + (double)TexelOffsetX ) / size;
			double v = ( MathUtil.Wrap(uv.Y,0,1) * Height + (double)TexelOffsetY ) / size;

			return new Vector2( (float)u, (float)v );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="context"></param>
		/// <param name="pageTable"></param>
		public void SplitIntoPages ( IBuildContext context, VTTextureTable pageTable, VTStorage storage, int threadCount )
		{
			var pageSize		=	VTConfig.PageSize;
			var pageSizeBorder	=	VTConfig.PageSizeBordered;
			var border			=	VTConfig.PageBorderWidth;
			var defaultRoughness=	new Color(0.7f, 0.7f, 0.7f, 1.0f);

			var colorMap		=	LoadTexture( BaseColor, Color.Gray );
			var normalMap		=	LoadTexture( NormalMap, Color.FlatNormals );
			var roughness		=	LoadTexture( Roughness, defaultRoughness );
			var metallic		=	LoadTexture( Metallic,	Color.Black );
			var emission		=	LoadTexture( Emission,	Color.Black );
			var	occlusion		=	LoadTexture( Occlusion,	Color.White );

			AverageColor		=	ImageLib.ComputeAverageColor( colorMap );

			var pageCountX	=	colorMap.Width / pageSize;
			var pageCountY	=	colorMap.Height / pageSize;
			var pOptions	=	new ParallelOptions { MaxDegreeOfParallelism = threadCount };

			Parallel.For( 0, pageCountY, pOptions, y =>
			//for (int y=0; y<pageCountY; y++) 
			{
				for (int x=0; x<pageCountX; x++) 
				{
					var pageC	=	new Image<Color>(pageSizeBorder, pageSizeBorder); 
					var pageN	=	new Image<Color>(pageSizeBorder, pageSizeBorder); 
					var pageS	=	new Image<Color>(pageSizeBorder, pageSizeBorder); 
					
					for ( int i=0; i<pageSizeBorder; i++) 
					{
						for ( int j=0; j<pageSizeBorder; j++) 
						{
							int srcX		=	(x)*pageSize + i - border;
							int srcY		=	(y)*pageSize + j - border;

							var c	=	colorMap .SampleWrap( srcX, srcY );
							var n	=	normalMap.SampleWrap( srcX, srcY );
							var r	=	roughness.SampleWrap( srcX, srcY ).R;
							var m	=	metallic .SampleWrap( srcX, srcY ).R;
							var e	=	emission .SampleWrap( srcX, srcY ).R;
							var o	=	occlusion.SampleWrap( srcX, srcY ).R;

							if (MaskEmission) {
								c	=	Color.Lerp( c, Color.Black, MathUtil.Clamp(e/255.0f * 8, 0, 1) );
							}

							if (InvertYNormal) {
								n	=	new Color( n.R, (byte)(255-n.G), n.B, n.A );
							}

							pageC.SetPixel( i,j, c );
							pageN.SetPixel( i,j, n );
							pageS.SetPixel( i,j, new Color( r,m,e,o ) );
						}
					}

					var address	=	new VTAddress( (short)(x + AddressX), (short)(y + AddressY), 0 );
					var tile	=	new VTTile( address, pageC, pageN, pageS );
					pageTable.SaveTile( address, storage, tile );
				}
			});
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="texturePath"></param>
		/// <returns></returns>
		bool IsTextureModifiedSince( DateTime targetLastWriteTimeUtc, string texturePath )
		{
			if ( string.IsNullOrWhiteSpace(texturePath) ) {
				return false;
			}

			if ( !context.ContentFileExists( texturePath ) ) {
				Log.Warning("{0} does not exist", texturePath);
				return true;  /// or DateTime.Now???
			}

			var fullPath    =   context.ResolveContentPath( texturePath );

			if ( targetLastWriteTimeUtc <= File.GetLastWriteTimeUtc(fullPath) ) {
				return true;
			} else {
				return false;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="baseImage"></param>
		/// <param name="postfix"></param>
		/// <param name="defaultColor"></param>
		/// <returns></returns>
		Image<Color> LoadTexture ( string texturePath, Color defaultColor ) 
		{
			if ( string.IsNullOrWhiteSpace(texturePath) || !context.ContentFileExists( texturePath ) ) {
				return new Image<Color>( Width, Height, defaultColor );
			}

			var fullPath    =   context.ResolveContentPath( texturePath );
			var ext         =   Path.GetExtension(texturePath).ToLowerInvariant();

			Image<Color> image		=	null;

			using ( var stream = File.OpenRead( fullPath ) ) {
				if ( ext==".tga" ) {
					image = ImageLib.LoadTga( stream );
				} else
				if ( ext==".png" ) {
					image = ImageLib.LoadPng( stream );
				} else
				if ( ext==".jpg" ) {
					image = ImageLib.LoadJpg( stream );
				} else {
					throw new BuildException( "Only TGA, JPG or PNG images are supported." );
				}
			}

			if ( image.Width!=Width || image.Height!=image.Height ) {
				Log.Warning( "Size of {0} is not equal to size of {1}. Default image is used.", texturePath, Name );
				return new Image<Color>( Width, Height, defaultColor );
			}

			return image;
		}

	}
}
