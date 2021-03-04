﻿using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;
using Fusion.Core;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Build.Mapping;
using Fusion.Engine.Graphics.GI;
using System.IO;
using Fusion.Core.Content;
using Fusion.Build;

namespace Fusion.Engine.Graphics.GI 
{
	public class LightMapGBuffer : DisposableBase
	{
		readonly int size;
		readonly RenderSystem rs;

		public int Width	{ get { return size; } }
		public int Height	{ get { return size; } }

		public Size2 Size { get { return new Size2( size, size ); } }

		public readonly Image<Color>		Albedo;
		public readonly Image<Vector3>		Position;
		public readonly Image<Vector3>		Normal;
		public readonly Image<float>		Area;
		public readonly Image<byte>			Coverage;

		internal Texture2D	albedoTexture	;
		internal Texture2D	positionTexture	;
		internal Texture2D	normalTexture	;

		public ShaderResource AlbedoTexture { get { return albedoTexture; } }
		public ShaderResource PositionTexture { get { return positionTexture; } }
		public ShaderResource NormalTexture { get { return normalTexture; } }

		public IDictionary<string, Rectangle> Regions { get { return regions; } }
		readonly Dictionary<string, Rectangle> regions = new Dictionary<string, Rectangle>();

		/// <summary>
		/// Creates instance of the lightmap g-buffer :
		/// </summary>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public LightMapGBuffer( RenderSystem rs, int size )
		{
			this.rs		=	rs;
			this.size	=	size;

			Albedo			=	new Image<Color>	( Width,  Height, Color.Zero );
			Position		=	new Image<Vector3>	( Width,  Height, Vector3.Zero );
			Normal			=	new Image<Vector3>	( Width,  Height, Vector3.Zero );
			Coverage		=	new Image<byte>		( Width,  Height, 0 );
			Area			=	new Image<float>	( Width,  Height, 0 );

			albedoTexture	=	new Texture2D( rs.Device, Width,  Height, ColorFormat.Rgba8,	1,	false );
			positionTexture	=	new Texture2D( rs.Device, Width,  Height, ColorFormat.Rgb32F,	1,	false );
			normalTexture	=	new Texture2D( rs.Device, Width,  Height, ColorFormat.Rgba8,	1,	false );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				SafeDispose( ref albedoTexture	 );
				SafeDispose( ref positionTexture );
				SafeDispose( ref normalTexture	 );
			}

			base.Dispose( disposing );
		}


		public void UpdateGpuData()
		{
			positionTexture.SetData( Position.RawImageData );
			albedoTexture.SetData( Albedo.RawImageData );
			normalTexture.SetData( Normal.Convert( EncodeNormalRGB8 ).RawImageData );
		}


		Color AverageColor( Color c0, Color c1, Color c2, Color c3 )
		{
			return Color.Lerp( 
				Color.Lerp( c0, c1, 0.5f ),
				Color.Lerp( c2, c3, 0.5f ),
				0.5f);
		}


		Vector3 AverageVector( Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3 )
		{
			return Vector3.Lerp( 
				Vector3.Lerp( v0, v1, 0.5f ),
				Vector3.Lerp( v2, v3, 0.5f ),
				0.5f);
		}


		void SaveDebugImage( Image<Color> image, string name )
		{
			ImageLib.SaveTga( image, name + ".tga" );
		}



		Color EncodeNormalRGB8( Vector3 n )
		{
			n.Normalize();
			n = (n + Vector3.One) * 0.5f;
			return new Color( 
				MathUtil.Lerp( (byte)0, (byte)255, n.X ),
				MathUtil.Lerp( (byte)0, (byte)255, n.Y ),
				MathUtil.Lerp( (byte)0, (byte)255, n.Z ),
				(byte)255 );
			//n = (n + Vector3.One) * 0.5f;
			//return new Color( n.X, n.Y, n.Z, 0.0f );
		}



		Color EncodeSkyRGB8( Vector3 n )
		{
			n = (n + Vector3.One) * 0.5f;
			return new Color( 
				MathUtil.Lerp( (byte)0, (byte)255, n.X ),
				MathUtil.Lerp( (byte)0, (byte)255, n.Y ),
				MathUtil.Lerp( (byte)0, (byte)255, n.Z ),
				(byte)255 );
		}


		public void SaveDebugImages()
		{
			SaveDebugImage( Albedo									, "rad_albedo" );
			SaveDebugImage( Normal.Convert( EncodeNormalRGB8 )		, "rad_normal" );
		}


		/*-----------------------------------------------------------------------------------------
		 *	Lightmap Import :
		-----------------------------------------------------------------------------------------*/

		public void WriteStream ( Stream stream )
		{
			using ( var writer = new BinaryWriter( stream ) )
			{
				//	write header :
				writer.WriteFourCC("LMG1");

				writer.Write( Width );
				writer.Write( Height );

				//	write regions :
				writer.WriteFourCC("RGN1");

				writer.Write( Regions.Count );
				foreach ( var pair in Regions ) {
					writer.Write( pair.Key );
					writer.Write( pair.Value );
				}

				//	write gbuffer :
				writer.WriteFourCC("GBF1");

				writer.WriteFourCC("POS1");
				Position.WriteStream( stream );

				writer.WriteFourCC("NRM1");
				Normal.Convert( EncodeNormalRGB8 ).WriteStream( stream );

				writer.WriteFourCC("ALB1");
				Albedo.WriteStream( stream );
			}
		}
	}
}