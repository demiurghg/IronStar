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

	class FScene {

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
				

				//
				//	run fbx loader :
				//
				Log.Message("Reading FBX: {0}", options.Input);

				var loader = new FbxLoader();
				using ( var scene  = loader.LoadScene( options.Input, options ) ) {
				
					//
					//	Save scene :
					//					
					Log.Message("Preparation...");
					foreach ( var mesh in scene.Meshes ) {
						if (mesh!=null) {
							mesh.MergeVertices( options.MergeTolerance );
							mesh.DefragmentSubsets(scene, true);
							mesh.ComputeTangentFrame();
							mesh.ComputeBoundingBox();
						}
					}

					scene.StripNamespaces();

					Log.Message("Merging instances...");
					scene.DetectAndMergeInstances();
					
					Log.Message("Creating missing materials...");
					CreateMissingMaterials( options.Input, scene );

					if (options.BaseDirectory != null) {
						Log.Message("Resolving material paths...");

						var relativePath	=	ContentUtils.MakeRelativePath(options.BaseDirectory + @"\", options.Input);
						var relativeDir		=	Path.GetDirectoryName(relativePath);
						var sceneDir = Path.GetDirectoryName(options.Input);

						foreach (var mtrl in scene.Materials) {
							mtrl.Name	=	Path.Combine( relativeDir, mtrl.Name );
						}
					}

					//
					//	Save scene :
					//					
					Log.Message("Writing binary file: {0}", options.Output);
					using ( var stream = File.OpenWrite( options.Output ) ) {
						scene.Save( stream );
					}


					if (!string.IsNullOrWhiteSpace(options.Report)) {
						var reportPath = options.Report;
						Log.Message("Writing report: {0}", reportPath);
						File.WriteAllText( reportPath, SceneReport.CreateHtmlReport(scene));
					}
				}

				Log.Message("Done.");

			} catch ( Exception e ) {
				parser.PrintError( "{0}", e.ToString() );

				if (options.Wait) {
					Log.Message("Press any key to continue...");
					Console.ReadKey();
				}

				return 1;
			}

			if (options.Wait) {
				Log.Message("Press any key to continue...");
				Console.ReadKey();
			}

			return 0;
		}



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

				/*if (!File.Exists(mtrlPathJson)) {
					Log.Message("...new material: {0}", mtrlPathJson);
					File.WriteAllText( mtrlPathJson, JsonConvert.SerializeObject(mtrl, Formatting.Indented) );
				} */
			}
		}
	}
}
