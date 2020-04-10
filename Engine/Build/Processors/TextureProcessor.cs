using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics;
using Fusion.Core.Content;

namespace Fusion.Build.Processors {

	public enum TextureCompression {
		RGB	,  	BC1	,	BC1N,  	BC1A,
		BC2	,  	BC3	,	BC3N,  	BC4	,
		BC5	,		
	}

	[AssetProcessor("Textures", "Converts TGA, PNG and JPEG images to DDS. DDS files will be bypassed.")]
	public class TextureProcessor : AssetProcessor {
			
		[CommandLineParser.Name("nomips", "do not build mip levels")]
		[CommandLineParser.Option]
		public bool NoMips { get; set; }

		[CommandLineParser.Name("fast", "perform fast compression")]
		[CommandLineParser.Option]
		public bool Fast { get; set; }

		[CommandLineParser.Name("tonormal", "build normalmap")]
		[CommandLineParser.Option]
		public bool ToNormal { get; set; }

		[CommandLineParser.Name("color",  "texture contains color data")]
		[CommandLineParser.Option]
		public bool Color { get; set; }

		[CommandLineParser.Name("alpha", "texture contains alpha data")]
		[CommandLineParser.Option]
		public bool Alpha { get; set; }

		[CommandLineParser.Name("normal", "texture contains normalmap")]
		[CommandLineParser.Option]
		public bool Normal { get; set; }

		[CommandLineParser.Name("compression", "compression mode")]
		[CommandLineParser.Option]
		public TextureCompression Compression { get; set; }

		
		/// <summary>
		/// 
		/// </summary>
		public TextureProcessor ()
		{
		}


		public override string GenerateParametersHash()
		{
			return ContentUtils.CalculateMD5Hash(
				GetType().AssemblyQualifiedName
				+ "/" + NoMips.ToString()
				+ "/" + Fast.ToString()
				+ "/" + ToNormal.ToString()
				+ "/" + Color.ToString()
				+ "/" + Alpha.ToString()
				+ "/" + Normal.ToString()
				+ "/" + Compression.ToString()
			);
		}



		public override void Process ( AssetSource assetFile, IBuildContext context )
		{
			var src	=	assetFile.FullSourcePath;
			var dst	=	context.GetTempFileFullPath( assetFile.KeyPath, ".dds" );

			RunNVCompress( context, src, dst, NoMips, Fast, ToNormal, Color, Alpha, Normal, Compression );

			using ( var target = assetFile.OpenTargetStream( typeof(DiscTexture) ) ) {
				context.CopyFileTo( dst, target );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="buildContext"></param>
		/// <param name="src"></param>
		/// <param name="dst"></param>
		/// <param name="noMips"></param>
		/// <param name="fast"></param>
		/// <param name="toNormal"></param>
		/// <param name="color"></param>
		/// <param name="alpha"></param>
		/// <param name="normal"></param>
		/// <param name="compression"></param>
		internal static void RunNVCompress( IBuildContext buildContext, string src, string dst, bool noMips, bool fast, bool toNormal, bool color, bool alpha, bool normal, TextureCompression compression )
		{
			string commandLine = "";

			if ( noMips		) 	commandLine	+=	" -nomips"	;
			if ( fast		) 	commandLine	+=	" -fast"	;
			if ( toNormal	) 	commandLine	+=	" -tonormal";
			if ( color		) 	commandLine	+=	" -color"	;
			if ( alpha		) 	commandLine	+=	" -alpha"	;
			if ( normal		) 	commandLine	+=	" -normal"	;

			commandLine += ( " -" + compression.ToString().ToLower() );
			commandLine += ( " \"" + src + "\"" );
			commandLine += ( " \"" + dst + "\"" );

			buildContext.RunTool( @"nvcompress.exe", commandLine );//*/
		}

	}
}
