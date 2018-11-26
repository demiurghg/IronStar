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
using Fusion.Core.Mathematics;

namespace FScene {
	static class FSceneRetarget {

		
		/// <summary>
		/// https://gamedev.stackexchange.com/questions/27058/how-to-programatically-retarget-animations-from-one-skeleton-to-another
		/// </summary>
		/// <param name="targetScene"></param>
		/// <param name="options"></param>
		public static void RetargetAnimation ( Scene targetScene, Options options, StringBuilder log )
		{
			var sourceScenePath = options.RetargetSource;

			if (string.IsNullOrWhiteSpace(sourceScenePath)) {
				return;
			}

			log.AppendFormat("Retarget source : {0}\r\n", sourceScenePath );
			Log.Message		("Retarget source : {0}", sourceScenePath );

			Log.Message("...reading FBX");

			var loader = new FbxLoader();
			using ( var sourceScene  = loader.LoadScene( sourceScenePath, false, true ) ) {
				
				foreach ( var srcTake in sourceScene.Takes ) {

					log.AppendFormat("...take: '{0}'\r\n"	, srcTake.Name );
					Log.Message		("...take: '{0}'"		, srcTake.Name );

					if ( targetScene.Takes.Any( tk => tk.Name==srcTake.Name ) ) {
						log.AppendFormat(" * take '{0}' is already exist, skipped.\r\n"	, srcTake.Name );
						Log.Message		(" * take '{0}' is already exist, skipped."		, srcTake.Name );
						continue;
					}
					
					var dstTake = new AnimationTake( srcTake.Name, targetScene.Nodes.Count, srcTake.FirstFrame, srcTake.LastFrame );

					FillTakeWithDefaultAnimation( dstTake, targetScene );

					FillTakeWithSourceAnimation( dstTake, targetScene, srcTake, sourceScene, log );

					targetScene.Takes.Add( dstTake ); 

				}

			}

			Log.Message("Done.");
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="source"></param>
		static void FillTakeWithDefaultAnimation ( AnimationTake take, Scene source )
		{
			for ( int nodeIndex = 0; nodeIndex < source.Nodes.Count; nodeIndex ++ ) {

				for ( int frame = take.FirstFrame; frame<=take.LastFrame; frame++ ) {
				
					take.SetKey( frame, nodeIndex, source.Nodes[ nodeIndex ].Transform );

				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		/// <param name="source"></param>
		static void FillTakeWithSourceAnimation ( AnimationTake dstTake, Scene target, AnimationTake srcTake, Scene source, StringBuilder log )
		{
			for ( int srcNodeIndex = 0; srcNodeIndex < source.Nodes.Count; srcNodeIndex ++ ) {

				var srcNodeName	 = source.Nodes[ srcNodeIndex ].Name;
				int dstNodeIndex = target.GetNodeIndex( srcNodeName );

				Log.Message("...remap '{0}': {1} -> {2}", srcNodeName, srcNodeIndex, dstNodeIndex );

				if ( dstNodeIndex<0) {
					log.AppendFormat(" * dst node '{0}' does not exist, skipped.\r\n"	, srcNodeName );
					Log.Message		(" * dst node '{0}' does not exist, skipped."		, srcNodeName );
					continue;
				}

				for ( int frame = dstTake.FirstFrame; frame<=dstTake.LastFrame; frame++ ) {

					Matrix transform;

					srcTake.GetKey( frame, srcNodeIndex, out transform );
					dstTake.SetKey( frame, dstNodeIndex, transform );

				}
			}
		}

	}
}
