using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Mathematics;
using Fusion.Core.Shell;
using Fusion.Core.Extensions;
using Fusion.Engine.Imaging;
using Newtonsoft.Json;

namespace Fusion.Build.Processors {

	[AssetProcessor("TextureAtlas", "Merges multiple textures to single one.")]
	public class TextureAtlasProcessor : AssetProcessor {

		/// <summary>
		/// 
		/// </summary>
		public TextureAtlasProcessor ()
		{
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="buildContext"></param>
		public override void Process ( AssetSource assetFile, BuildContext context )
		{
			var atlas			=	TextureAtlas.ReadFromIniFile( assetFile.FullSourcePath );

			var fullFileDir		=	Path.GetDirectoryName( assetFile.FullSourcePath );
			var fileDir			=	Path.GetDirectoryName( assetFile.KeyPath );

			var fileNames		=	atlas.GetAllFrames()
									.Select( f1 => f1.Name )
									.ToArray();

			var dependencies	=	fileNames.Select( n => Path.Combine( fileDir, n ) ).ToArray();

			atlas.LoadTextures( fullFileDir );

			//
			//	Pack atlas :
			//
			var root	=	new AtlasNode( 0,0, atlas.Width, atlas.Height, atlas.Padding );

			var frames	=	atlas.GetAllFrames()
							.OrderByDescending( f1 => f1.Width * f1.Height )
							.ThenByDescending( f2 => f2.Width )
							.ThenByDescending( f3 => f3.Height )
							.ToArray();

			foreach ( var frame in frames ) {
				var n = root.Insert( frame );
				if (n==null) {
					throw new InvalidOperationException("No enough room to place image");
				}
			}

			//
			//	Create image and fill it with atlas elements :
			//	
			var targetImage	=	new Image( atlas.Width, atlas.Height );
			targetImage.Fill( atlas.FillColor );

			foreach ( var frame in frames ) {
				frame.WriteSubimage( targetImage );
			}

			//
			//	Save and compress :
			//
			var tgaOutput	=	context.GetTempFileFullPath( assetFile.KeyPath, ".tga" );
			var ddsOutput	=	context.GetTempFileFullPath( assetFile.KeyPath, ".dds" );
			Image.SaveTga( targetImage, tgaOutput );

			var compression =	atlas.Compression ? TextureProcessor.TextureCompression.BC3 : TextureProcessor.TextureCompression.RGB;
			TextureProcessor.RunNVCompress( context, tgaOutput, ddsOutput, !atlas.Mips, false, false, true, true, false, compression );


			//
			//	Write binary blob (text + dds texture):
			//
			using ( var fs = assetFile.OpenTargetStream(dependencies) ) {
				using ( var bw = new BinaryWriter( fs ) ) {

					int offset = 0;

					bw.Write( new[] { 'A', 'T', 'L', 'S' } );
					
					bw.Write( atlas.Animations.Count ); 
					bw.Write( atlas.Width );
					bw.Write( atlas.Height );

					foreach ( var anim in atlas.Animations ) {

						bw.Write( anim.Name );
						bw.Write( offset );
						bw.Write( anim.Frames.Length );
						offset += anim.Frames.Length;
					}

					bw.Write( new[] { 'F', 'R', 'M', 'S' } );

					frames = atlas.GetAllFrames();

					bw.Write( frames.Length );

					foreach ( var frame in frames ) {
						bw.Write( frame.Rectangle );
					}

					bw.Write( new[] { 'T', 'E', 'X', '0' } );

					bw.Write( (int)(new FileInfo(ddsOutput).Length) );
				
					using ( var dds = File.OpenRead( ddsOutput ) ) {
						dds.CopyTo( fs );
					}
				}
			}
		}
	}
}
