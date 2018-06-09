using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
using Fusion.Build;
using BEPUphysics;
using IronStar.Core;
using IronStar.Editor2.Controls;
using IronStar.Editor2.Manipulators;
using Fusion.Engine.Frames;

namespace IronStar.Editor2 {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor {

		Workspace workspace;

		void SetupWorkspace ()
		{
			workspace		=	new Workspace( this, Game.Frames.RootFrame );

			var upperShelf	=	workspace.UpperShelf;
			var lowerShelf	=	workspace.LowerShelf;


			upperShelf.AddLButton("ST", @"editor\iconToolSelect",	()=> manipulator = new NullTool(this) );
			upperShelf.AddLButton("MT", @"editor\iconToolMove",		()=> manipulator = new MoveTool(this) );
			upperShelf.AddLButton("RT", @"editor\iconToolRotate",	()=> manipulator = new RotateTool(this) );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("FCS", @"editor\iconFocus",		()=> FocusSelection() );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("SV", null, null );
			upperShelf.AddLButton("LD", null, null );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("UNFRZ", null, ()=> UnfreezeAll() );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("ENTPLT", null, null /*()=> ToggleShowPalette()*/ );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("SFX"		, null, null /*()=> ToggleAssetsExplorer("sfx"		) */ );
			upperShelf.AddFatLButton("MODEL"	, null, null /*()=> ToggleAssetsExplorer("models"	) */ );
			upperShelf.AddFatLButton("ENTITY"	, null, null /*()=> ToggleAssetsExplorer("entities" ) */ );
			upperShelf.AddFatLButton("ITEM"		, null, null /*()=> ToggleAssetsExplorer("items"	) */ );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("BUILD\rRELOAD", @"editor\iconBuild", ()=> Game.Invoker.ExecuteString("contentBuild") );

			upperShelf.AddRButton("SCR", null, ()=> Game.Invoker.ExecuteString("screenshot") );
			//upperShelf.AddRButton("CONFIG", @"editor\iconComponents", ()=> ToggleShowComponents() );
			//upperShelf.AddRButton("EDITOR\rCONFIG", @"editor\iconSettings", ()=> FeedProperties(editor) );
			upperShelf.AddRButton("EXIT", @"editor\iconExit", ()=> Game.Invoker.ExecuteString("killEditor") );
 

			lowerShelf.AddLButton("PLAY\r[SPACE]",	@"editor\iconSimulate2",() => EnableSimulation = !EnableSimulation );
			lowerShelf.AddLButton("RESET\r[ESC]" ,	@"editor\iconReset2",	() => ResetWorld(true) );
			lowerShelf.AddLButton("BAKE\r[B]"	 ,	@"editor\iconBake",		() => BakeToEntity() );
			lowerShelf.AddLSplitter();				 
			lowerShelf.AddLButton("ACT\n[ENTER]" ,	@"editor\iconActivate", () => ActivateSelected() );
			lowerShelf.AddLButton("USE\n[U]"	 ,	@"editor\iconUse"	,	() => UseSelected() );


			var snapLabel	=	lowerShelf.AddRIndicator("SNAP", 200 );
			snapLabel.Tick += (s,e) => {
				snapLabel.Text = string.Format(
 				  "Move snap   : {0}\r\n" +
 				  "Rotate snap : {1}\r\n" +
 				  "Camera FOV  : {2}\r\n" +
 				  "", 
				  MoveToolSnapEnable   ? MoveToolSnapValue  .ToString("000.00") : "Disabled",
				  RotateToolSnapEnable ? RotateToolSnapValue.ToString("000.00") : "Disabled", 
				  camera.Fov,0 );
			};

			lowerShelf.AddRSplitter();
			var statLabel	=	lowerShelf.AddRIndicator("FPS   : 57.29\r\r", 200 );

			statLabel.Tick += (s,e) => {
				var curFps = 60f;
				var minFps = 60f;
				var maxFps = 60f;
				var avgFps = 60f;
				var vsync  = true;
				statLabel.Text	=	
					string.Format(
 					  "    FPS: {0,5:000.0} {4}\r\n" +
 					  "Max FPS: {1,5:000.0}\r\n" +
 					  "Avg FPS: {2,5:000.0}\r\n" +
 					  "Min FPS: {3,5:000.0}", curFps, maxFps, avgFps, minFps, vsync ? "VSYNC ON" : "VSYNC OFF" );
			};
		}

	}
}
