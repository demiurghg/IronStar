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
using IronStar.Editor.Controls;
using IronStar.Editor.Manipulators;
using Fusion.Engine.Frames;
using Fusion.Core.Input;
using IronStar.Items;

namespace IronStar.Editor {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor {

		Workspace workspace;
		FpsCounter fpsCounter = new FpsCounter(60);

		void SetupWorkspace ()
		{
			var rs = Game.RenderSystem;

			workspace		=	new Workspace( this, Game.Frames.RootFrame );

			var upperShelf	=	workspace.UpperShelf;
			var lowerShelf	=	workspace.LowerShelf;

			//- PALETTES & EXPLORERS ---------------------------------------------------

			var entityPalette		=	CreateEntityPalette( workspace );
			var componentPalette	=	CreateComponentPalette( workspace );
			var assetExplorer		=	CreateAssetExplorer( workspace );
			assetExplorer.Visible	=	false;

			//- UPPER SHELF ------------------------------------------------------------

			upperShelf.AddLButton("ST", @"editor\iconToolSelect",	()=> manipulator = new NullTool(this) );
			upperShelf.AddLButton("MT", @"editor\iconToolMove",		()=> manipulator = new MoveTool(this) );
			upperShelf.AddLButton("RT", @"editor\iconToolRotate",	()=> manipulator = new RotateTool(this) );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("FCS", @"editor\iconFocus",		()=> FocusSelection() );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("ENTPLT", null, ()=> workspace.TogglePalette( entityPalette ) );
			upperShelf.AddFatLButton("ASSETS", null, ()=> assetExplorer.Visible = !assetExplorer.Visible );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("UNFRZ", null, ()=> UnfreezeAll() );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("BUILD\rRELOAD", @"editor\iconBuild", ()=> Game.Invoker.ExecuteString("contentBuild") );

			upperShelf.AddRButton("ENV"				, null						, ()=> workspace.FeedProperties(map.Environment) );
			upperShelf.AddRButton("SCR"				, null						, ()=> Game.Invoker.ExecuteString("screenshot") );
			upperShelf.AddRButton("CONFIG"			, @"editor\iconComponents"	, ()=> workspace.TogglePalette( componentPalette ) );
			upperShelf.AddRButton("EDITOR\rCONFIG"	, @"editor\iconSettings"	, ()=> workspace.FeedProperties(this) );
			upperShelf.AddRButton("EXIT"			, @"editor\iconExit"		, ()=> Game.Invoker.ExecuteString("killEditor") );
 
			//- LOWER SHELF ------------------------------------------------------------

			lowerShelf.AddLButton("PLAY\r[SPACE]",	@"editor\iconSimulate2",() => EnableSimulation = !EnableSimulation );
			lowerShelf.AddLButton("RESET\r[ESC]" ,	@"editor\iconReset2",	() => ResetWorld(true) );
			lowerShelf.AddLButton("BAKE\r[B]"	 ,	@"editor\iconBake",		() => BakeToEntity() );
			lowerShelf.AddLSplitter();				 
			lowerShelf.AddLButton("ACT\n[ENTER]" ,	@"editor\iconActivate", () => ActivateSelected() );
			lowerShelf.AddLButton("USE\n[U]"	 ,	@"editor\iconUse"	,	() => UseSelected() );

			lowerShelf.AddFatLButton("Build NavMesh\n[U]"	 ,	null,	() => this.Map.BuildNavMesh(Content) );

			//--------------------------------------------------------------------------

			var simLabel			=	lowerShelf.AddRIndicator("", 96 );
			//simLabel.TextAlignment	=	Alignment.MiddleCenter;

			simLabel.Tick += (s,e) => {
				var sim    = this.EnableSimulation;

				//var text1	=	string.Format("SIMULATION\r\n{0} entities", World.GetEntities().Count());
				//var text2	=	string.Format("EDITOR MODE\r\n{0} nodes",    Map.Nodes.Count);

				simLabel.ForeColor	=	sim ? ColorTheme.ColorRed : ColorTheme.ColorGreen;
				simLabel.Text		=	sim ? "SIMULATION\r\n\r\n" : "EDITOR MODE\r\n\r\n";
			};

			//-----------

			lowerShelf.AddRSplitter();
			var snapLabel	=	lowerShelf.AddRIndicator("", 200 );
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

			//-----------

			lowerShelf.AddRSplitter();
			var statLabel	=	lowerShelf.AddRIndicator("", 200 );

			statLabel.Tick += (s,e) => {
				var curFps = fpsCounter.CurrentFps;
				var minFps = fpsCounter.MinFps;
				var maxFps = fpsCounter.MaxFps;
				var avgFps = fpsCounter.AverageFps;
				var vsync  = rs.VSyncInterval!=0;
				statLabel.Text	=	
					string.Format(
 					  "    FPS: {0,5:000.0} {4}\r\n" +
 					  "Max FPS: {1,5:000.0}\r\n" +
 					  "Avg FPS: {2,5:000.0}\r\n" +
 					  "Min FPS: {3,5:000.0}", curFps, maxFps, avgFps, minFps, vsync ? "VSYNC ON" : "VSYNC OFF" );
			};

			//- HOTKEYS ----------------------------------------------------------------

			workspace.AddHotkey( Keys.F			, ModKeys.None,	() => FocusSelection() );
			workspace.AddHotkey( Keys.F2		, ModKeys.None, () => rs.VSyncInterval = (rs.VSyncInterval==1) ? 0 : 1 );
			
			workspace.AddHotkey( Keys.Q			, ModKeys.None, () => Manipulator = new NullTool(this) );
			workspace.AddHotkey( Keys.W			, ModKeys.None, () => Manipulator = new MoveTool(this) );
			workspace.AddHotkey( Keys.E			, ModKeys.None, () => Manipulator = new RotateTool(this) );
			workspace.AddHotkey( Keys.T			, ModKeys.None, () => TargetSelection() );
			
			workspace.AddHotkey( Keys.Delete	, ModKeys.None, () => DeleteSelection() );
			workspace.AddHotkey( Keys.U			, ModKeys.Ctrl, () => DuplicateSelection() );

			workspace.AddHotkey( Keys.Space		, ModKeys.None, () => EnableSimulation = !EnableSimulation );
			workspace.AddHotkey( Keys.Escape	, ModKeys.None, () => ResetWorld(true) );
			workspace.AddHotkey( Keys.K			, ModKeys.None, () => ResetWorld(true) );
			workspace.AddHotkey( Keys.B			, ModKeys.None, () => BakeToEntity() );
			workspace.AddHotkey( Keys.Enter		, ModKeys.None, () => ActivateSelected() );
			workspace.AddHotkey( Keys.U			, ModKeys.None, () => UseSelected() );
			workspace.AddHotkey( Keys.D			, ModKeys.Ctrl, () => DuplicateSelection() );


			workspace.AddHotkey( Keys.J			, ModKeys.Ctrl, () => RotateToolSnapEnable = !RotateToolSnapEnable );
			workspace.AddHotkey( Keys.J			, ModKeys.None, () => MoveToolSnapEnable = !MoveToolSnapEnable );

			workspace.AddHotkey( Keys.G			, ModKeys.None, () => rs.SkipDebugRendering = !rs.SkipDebugRendering );
			workspace.AddHotkey( Keys.G			, ModKeys.Alt,	() => DrawGrid = !DrawGrid );
			workspace.AddHotkey( Keys.G			, ModKeys.Ctrl, () => DrawGrid = !DrawGrid );

			workspace.AddHotkey( Keys.OemComma	, ModKeys.None, () => MoveToolSnapValue *= 0.5f );
			workspace.AddHotkey( Keys.OemPeriod	, ModKeys.None, () => MoveToolSnapValue *= 2.0f );

			workspace.AddHotkey( Keys.OemComma	, ModKeys.Ctrl, () => RotateToolSnapValue -= 5f );
			workspace.AddHotkey( Keys.OemPeriod	, ModKeys.Ctrl, () => RotateToolSnapValue += 5f );

			workspace.AddHotkey( Keys.OemOpenBrackets,  ModKeys.None, () => CameraFov += 10 );
			workspace.AddHotkey( Keys.OemCloseBrackets, ModKeys.None, () => CameraFov -= 10 );

			workspace.AddHotkey( Keys.F11		, ModKeys.None, () => Game.Invoker.ExecuteString("screenshot") );
		}



		Palette CreateEntityPalette ( Workspace workspace )
		{
			var palette = new Palette( workspace.Frames, "Create Node", 0,0, 150,100 );

			var entityTypes	=	Misc.GetAllSubclassesOf( typeof(EntityFactory), false );

			palette.AddButton( "Decal"			,	() => CreateNodeUI( new MapDecal		() ) );
			palette.AddButton( "Static Model"	,	() => CreateNodeUI( new MapModel		() ) );
			palette.AddButton( "Light Probe"	,	() => CreateNodeUI( new MapLightProbe	() ) );
			palette.AddButton( "Omni Light"		,	() => CreateNodeUI( new MapOmniLight	() ) );
			palette.AddButton( "Spot Light"		,	() => CreateNodeUI( new MapSpotLight	() ) );
			palette.AddButton( "Sound"			,	() => CreateNodeUI( new MapSound		() ) );
			palette.AddButton( "Reverb Zone"	,	() => CreateNodeUI( new MapReverb		() ) );

			palette.AddSplitter();

			foreach ( var ent in entityTypes ) {

				string name = ent.Name.Replace("Factory", "");

				Action action = () => { 
					var mapEntity = new MapEntity();
					mapEntity.Factory = (EntityFactory)Activator.CreateInstance(ent);
					CreateNodeUI( mapEntity ); 
				};

				palette.AddButton( name, action );
			}

			return palette;
		}



		Palette CreateComponentPalette( Workspace workspace )
		{
			var palette = new Palette( workspace.Frames, "Components", 0,0, 150,100 );

			var componentList = Game.Components.OrderBy( c1 => c1.GetType().Name ).ToArray();

			foreach ( var component in componentList ) {

				string name = component.GetType().Name;

				Action func = () => { 
					workspace.FeedProperties( component );
				};

				palette.AddButton( name, func );
			}

			return palette;
		}




		public static Panel CreateAssetExplorer ( Frame parent )
		{
			var types = new List<Type>();

			var typeFX			=	new[] { typeof(FXFactory) };
			var typeEntities	=	Misc.GetAllSubclassesOf(typeof(EntityFactory), true);
			var typeModels		=	Misc.GetAllSubclassesOf(typeof(ModelFactory), true);
			var typeItems		=	Misc.GetAllSubclassesOf(typeof(ItemFactory), true);
			var typeWeapon		=	Misc.GetAllSubclassesOf(typeof(Weapon), true);
			var typeAnimation	=	Misc.GetAllSubclassesOf(typeof(AnimatorFactory), true);

			var assetExplorer	=	new AssetExplorer2( parent, "fx", typeFX, 0,0, 500, 600 );

			assetExplorer.AddToolButton( "FX"		,	() => assetExplorer.SetTargetClass( "fx"		, typeFX		) );
			assetExplorer.AddToolButton( "Entities"	,	() => assetExplorer.SetTargetClass( "entities"	, typeEntities	) );
			assetExplorer.AddToolButton( "Models"	,	() => assetExplorer.SetTargetClass( "models"	, typeModels	) );
			assetExplorer.AddToolButton( "Items"	,	() => assetExplorer.SetTargetClass( "items"		, typeItems		) );
			assetExplorer.AddToolButton( "Animation",	() => assetExplorer.SetTargetClass( "animation"	, typeAnimation	) );

			return assetExplorer;			  
		}
	}
}
