using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Fusion;
using Fusion.Core.Shell;
using Fusion.Core.Content;
using Fusion.Core.IniParser;
using Fusion.Core.Utils;
using Native.Fbx;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics;
using Newtonsoft.Json;

namespace FScene {

	class Options {

		[CommandLineParser.Name("in", "input FBX file")]
		[CommandLineParser.Required()]
		public string Input { get; set; }

		[CommandLineParser.Name("out", "output scene file")]
		[CommandLineParser.Option()]
		public string Output { get; set; }

		[CommandLineParser.Name("merge", "merge tolerance (0.0 is default)")]
		[CommandLineParser.Option()]
		public float MergeTolerance { get; set; }

		[CommandLineParser.Name("base", "root directory")]
		[CommandLineParser.Option()]
		public string BaseDirectory { get; set; }

		[CommandLineParser.Name("anim", "bake and import animation tracks")]
		[CommandLineParser.Option()]
		public bool ImportAnimation { get; set; }

		[CommandLineParser.Name("geom", "import geometry data.")]
		[CommandLineParser.Option()]
		public bool ImportGeometry { get; set; }

		[CommandLineParser.Name("report", "export html build report")]
		[CommandLineParser.Option()]
		public string Report { get; set; }

		[CommandLineParser.Name("retarget", "provides scene to retarget animation clips from")]
		[CommandLineParser.Option()]
		public string RetargetSource { get; set; }
	};



	partial class FScene {

		static int Main ( string[] args )
		{
			Thread.CurrentThread.CurrentCulture	=	System.Globalization.CultureInfo.InvariantCulture;
			Log.AddListener( new StdLogListener() );

			var options = new Options();
			var parser = new CommandLineParser( options.GetType() );

			try {

				//	parse arguments :
				parser.ParseCommandLine( options, args );

				//	change extension of output not set :
				if (options.Output==null) {
					options.Output = Path.ChangeExtension( options.Input, ".scene");
				}

				//	run fbx loader :
				Log.Message("Reading FBX: {0}", options.Input);

				var loader = new FbxLoader();
				using ( var scene  = loader.LoadScene( options.Input, options.ImportGeometry, options.ImportAnimation ) ) {
				
					Log.Message("Preparation...");
					foreach ( var mesh in scene.Meshes ) {
						if (mesh!=null) {
							mesh.MergeVertices( options.MergeTolerance );
							mesh.DefragmentSubsets(scene, true);
							mesh.ComputeTangentFrame();
							mesh.ComputeBoundingBox();
							mesh.BuildAdjacency();
						}
					}

					scene.StripNamespaces();

					Log.Message("Merging instances...");
					scene.DetectAndMergeInstances();
					
					Log.Message("Creating missing materials...");
					CreateMissingMaterials( options.Input, scene );

					Log.Message("Resolving material paths...");
					ResolveMaterialPaths( options, scene );

					var retargetLog = new StringBuilder();
					FSceneRetarget.RetargetAnimation( scene, options, retargetLog );

					//	save scene :
					Log.Message("Writing binary file: {0}", options.Output);
					using ( var stream = File.OpenWrite( options.Output ) ) {
						scene.Save( stream );
					}

					//	write report :
					if (!string.IsNullOrWhiteSpace(options.Report)) {
						var reportPath = options.Report;
						Log.Message("Writing report: {0}", reportPath);
						File.WriteAllText( reportPath, FSceneReport.CreateHtmlReport(scene, retargetLog.ToString()));
					}
				}

				Log.Message("Done.");

			} catch ( Exception e ) {
				parser.PrintError( "{0}", e.ToString() );
				return 1;
			}

			return 0;
		}




		/// <summary>
		/// Creates missing materials.
		/// If scene has reference to particular material but is does not exist,
		/// this function create default material description file.
		/// </summary>
		/// <param name="scenePath"></param>
		/// <param name="scene"></param>
		static void CreateMissingMaterials ( string scenePath, Scene scene )
		{
			var dir = Path.GetDirectoryName( scenePath );
			
			foreach ( var mtrl in scene.Materials ) {

				var mtrlPathIni	 = Path.Combine( dir, mtrl.Name + ".material" );
				var mtrlPathJson = Path.Combine( dir, mtrl.Name + ".json" );

				if (!File.Exists(mtrlPathIni)) {

					Log.Message("...new material: {0}", mtrlPathIni);

					Material.SaveToIniFile( mtrl, File.OpenWrite(mtrlPathIni) );
				}
			}
		}


		/// <summary>
		/// Resolves material paths:
		///	Sample:
		///		'models/weapon/machinegun.fbx' contains material 'machinegun'
		///		-->
		///		'models/weapon/machinegun'
		/// </summary>
		/// <param name="options"></param>
		/// <param name="scene"></param>
		static void ResolveMaterialPaths ( Options options, Scene scene )
		{
			if (options.BaseDirectory != null) {

				var relativePath	=	ContentUtils.MakeRelativePath(options.BaseDirectory + @"\", options.Input);
				var relativeDir		=	Path.GetDirectoryName( relativePath );
				var sceneDir		=	Path.GetDirectoryName( options.Input );

				foreach (var mtrl in scene.Materials) {
					mtrl.Name	=	Path.Combine( relativeDir, mtrl.Name );
				}
			}
		}
	}
}
