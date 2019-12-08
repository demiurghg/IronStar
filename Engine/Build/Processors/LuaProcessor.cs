using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;

namespace Fusion.Build.Processors {

	[AssetProcessor("Lua", "Compiles Lua script to binary")]
	public class LuaProcessor : AssetProcessor {
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceStream"></param>
		/// <param name="targetStream"></param>
		public override void Process ( AssetSource assetFile, BuildContext context )
		{
			var resolvedPath	=	assetFile.FullSourcePath;
			var destPath		=	context.GetTempFileFullPath( assetFile.KeyPath, ".luabin" );
			var reportPath		=	context.GetTempFileFullPath( assetFile.KeyPath, ".html" );

			var cmdLine			=	string.Format("-l -o \"{0}\" \"{1}\"", destPath, resolvedPath );

			var stdout	=	"";
			var stderr	=	"";

			var errcode	=	context.RunTool( "Luac.exe", cmdLine, out stdout, out stderr );

			if ( errcode != 0 ) {

				throw new BuildException( stderr );

			}

			if (!string.IsNullOrWhiteSpace(stderr)) {
				Log.Warning( stderr );
			}

			File.WriteAllText(reportPath, "<pre>" + stdout + "</per>");

			using ( var target = assetFile.OpenTargetStream( typeof(byte[]) ) ) {
				context.CopyFileTo( destPath, target );
			}
		}
	}
}
