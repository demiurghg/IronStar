﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics;
using Fusion.Core.Content;

namespace Fusion.Build.Processors {

	[AssetProcessor("Fonts", "Converts BMFC files to sprite fonts")]
	public class FontProcessor : AssetProcessor {

		public FontProcessor ()
		{
		}


		public override string GenerateParametersHash()
		{
			return ContentUtils.CalculateMD5Hash(GetType().AssemblyQualifiedName);
		}


		public override void Process ( AssetSource assetFile, IBuildContext context )
		{
			string tempFileName		= context.GetTempFileFullPath( assetFile.KeyPath, ".fnt" );
			string resolvedPath		= assetFile.FullSourcePath;	

			//	Launch 'bmfont.com' with temporary output file :
			context.RunTool( @"bmfont.com",  string.Format("-c \"{0}\" -o \"{1}\"", resolvedPath, tempFileName ) );


			//	load temporary output :
			SpriteFont.FontFile font;
			using ( var stream = File.OpenRead( tempFileName ) ) {
				font = SpriteFont.FontLoader.Load( stream );
			}


			//	perform some checks :
			if (font.Common.Pages!=1) {
				throw new BuildException("Only one page of font image is supported");
			}


			//	patch font description and add children (e.g. "secondary") content :
			using ( var stream = assetFile.OpenTargetStream(typeof(SpriteFont)) ) {

				using ( var sw = new BinaryWriter( stream ) ) {

					var xml = SpriteFont.FontLoader.SaveToString( font );

					sw.Write( xml );

					//	write pages :
					foreach (var p in font.Pages) {

						var pageFile	=	Path.Combine( Path.GetDirectoryName( tempFileName ), p.File );

						if ( Path.GetExtension( pageFile ).ToLower() == ".dds" ) {

							context.CopyFileTo( pageFile, sw );

						} else {

							TextureProcessor.RunNVCompress( context, pageFile, pageFile + ".dds", true, false, false, true, true, false, TextureCompression.RGB );

							context.CopyFileTo( pageFile + ".dds", sw );

						}
					}
				}
			}
		}
	}
}
