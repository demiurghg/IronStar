using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Shell;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics;
using Fusion.Core.Content;
using Fusion.Engine.Graphics.Scenes;

namespace Fusion.Build.Processors {

	[AssetProcessor("Scenes", "Converts FBX files to scene")]
	public class SceneProcessor : AssetProcessor {

		const float Version = 1.0f;

		/// <summary>
		/// Vertex merge tolerance
		/// </summary>
		[CommandLineParser.Name("merge", "merge tolerance (default=0)")]
		[CommandLineParser.Option]
		public float MergeTolerance { get; set; }

		/// <summary>
		/// Evaluates transform
		/// </summary>
		[CommandLineParser.Name("anim", "import animation")]
		[CommandLineParser.Option]
		public bool ImportAnimation { get; set; }

		/// <summary>
		/// Evaluates transform
		/// </summary>
		[CommandLineParser.Name("geom", "import geometry")]
		[CommandLineParser.Option]
		public bool ImportGeometry { get; set; }

		/// <summary>
		/// Evaluates transform
		/// </summary>
		[CommandLineParser.Name("report", "output html report")]
		[CommandLineParser.Option]
		public bool OutputReport { get; set; }

		/// <summary>
		/// Evaluates transform
		/// </summary>
		[CommandLineParser.Name("genmtrl", "generate missing materials")]
		[CommandLineParser.Option]
		public bool GenerateMissingMaterials { get; set; }

		/// <summary>
		/// Evaluates transform
		/// </summary>
		[CommandLineParser.Name("retarget", "provides scene to retarget animation clips from")]
		[CommandLineParser.Option]
		public string RetargetSource { get; set; }

		[CommandLineParser.Name("scale", "scales entire scene")]
		[CommandLineParser.Option]
		public float Scale { get; set; } = 1;

		public override string GenerateParametersHash()
		{
			return ContentUtils.CalculateMD5Hash(
				GetType().AssemblyQualifiedName
				+ "/" + MergeTolerance.ToString()
				+ "/" + ImportAnimation.ToString()
				+ "/" + ImportGeometry.ToString()
				+ "/" + OutputReport.ToString()
				+ "/" + GenerateMissingMaterials.ToString()
				+ "/" + RetargetSource.ToString()
				+ "/" + Scale.ToString()
				+ "/" + Version.ToString()
			);
		}


		
		/// <summary>
		/// 
		/// </summary>
		public SceneProcessor ( bool geom, bool anim, float merge, bool genMtrls, string retargetSource, float scale = 1.0f )
		{
			ImportAnimation				=	anim;
			ImportGeometry				=	geom;
			MergeTolerance				=	merge;
			GenerateMissingMaterials	=	genMtrls;
			RetargetSource				=	retargetSource;
			OutputReport				=	true;
			Scale						=	scale;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceStream"></param>
		/// <param name="targetStream"></param>
		public override void Process ( AssetSource assetFile, IBuildContext context )
		{
			var resolvedPath	=	assetFile.FullSourcePath;
			var destPath		=	context.GetTempFileFullPath( assetFile.KeyPath, ".scene" );
			var reportPath		=	context.GetTempFileFullPath( assetFile.KeyPath, ".html" );
			var retarget		=	!string.IsNullOrWhiteSpace(RetargetSource);
			var retargetPath	=	retarget ? context.ResolveContentPath(RetargetSource) : null;

			var dependencies	=	new List<string>();

			if (retarget) 
			{
				dependencies.Add( RetargetSource );
			}

			var cmdLine	=	string.Format("\"{0}\" /out:\"{1}\" /base:\"{2}\" /merge:{3} {4} {5} {6} {7} {8}", 
				resolvedPath, 
				destPath, 
				assetFile.BaseDirectory,
				MergeTolerance, 
				ImportAnimation ? "/anim":"", 
				ImportGeometry ? "/geom":"", 
				OutputReport ? "/report:" + "\"" + reportPath + "\"":"",
				retarget ? "/retarget:" + "\"" + retargetPath + "\"":"",
				Scale!=1 ? "/scale:" + Scale.ToString() : ""
			);

			context.RunTool( "FScene.exe", cmdLine );

			using ( var target = assetFile.OpenTargetStream( dependencies, typeof( Scene ) ) ) 
			{
				context.CopyFileTo( destPath, target );
			}
		}
	}
}
