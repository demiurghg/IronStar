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
using IronStar.Editor.Controls;
using IronStar.Editor.Manipulators;
using Fusion.Engine.Frames;
using Fusion.Core.Input;
using Fusion.Widgets;
using IronStar.AI;
using Fusion.Core.Shell;
using Fusion.Engine.Graphics.GI;
using Fusion.Engine.Frames.Layouts;
using Fusion.Widgets.Advanced;
using IronStar.Editor.Commands;
using Fusion.Widgets.Binding;
using IronStar.Gameplay.DataAssets;

namespace IronStar.Editor 
{
	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor 
	{
		Workspace workspace;
		MapOutliner outliner;
		FpsCounter fpsCounter = new FpsCounter(60);

		void SetupWorkspace ()
		{
			var rs = Game.RenderSystem;
			var ai = Game.GetService<AICore>();
			//ColorTheme.Font	=	Content.Load<SpriteFont>(@"fonts\editorRoboto");
			//ColorTheme.Font	=	Content.Load<SpriteFont>(@"fonts\editorArmata");

			workspace		=	new Workspace( Game.Frames, this );
			Game.Frames.ShowFullscreenFrame( workspace );

			var upperShelf	=	workspace.UpperShelf;
			var lowerShelf	=	workspace.LowerShelf;

			SetupGridEvents( workspace.Grid );

			//- PALETTES & EXPLORERS ---------------------------------------------------

			var entityPalette		=	CreateEntityPalette( workspace );
			var componentPalette	=	CreateComponentPalette( workspace );
			var outlinerPanel		=	CreateOutliner( workspace );
			var assetExplorer		=	CreateAssetExplorer( workspace );
			assetExplorer.Visible	=	false;

			outliner	=	outlinerPanel;

			//- UPPER SHELF ------------------------------------------------------------

			upperShelf.AddLButton("ST", @"editor\iconToolSelect",	()=> workspace.Manipulator = new NullTool() );
			upperShelf.AddLButton("MT", @"editor\iconToolMove",		()=> workspace.Manipulator = new MoveTool(this) );
			upperShelf.AddLButton("RT", @"editor\iconToolRotate",	()=> workspace.Manipulator = new RotateTool(this) );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("FCS", @"editor\iconFocus",		()=> FocusSelection() );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("Undo", null,	() => Log.Warning("Undo") );
			upperShelf.AddLButton("Redo", null,	() => Log.Warning("Redo") );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("Palette", null, ()=> workspace.TogglePalette( entityPalette ) );
			upperShelf.AddFatLButton("Assets", null, ()=> assetExplorer.Visible = !assetExplorer.Visible );
			upperShelf.AddFatLButton("Outliner", null, ()=> workspace.TogglePalette( outlinerPanel ) );

			upperShelf.AddLSplitter();
			upperShelf.AddFatLButton("Unfreeze\rAll", null, ()=> UnfreezeAll() );

			upperShelf.AddLSplitter();
			upperShelf.AddLButton("BUILD\rRELOAD"	, @"editor\iconBuild",		()=> Game.Invoker.ExecuteString("contentBuild") );
			upperShelf.AddLButton("MAKE\rSCRSHT"	, @"editor\iconScreenshot", ()=> GeneratePreview() );
			upperShelf.AddLButton("RAD\rCFG"		, @"editor\iconRadCfg",		()=> workspace.FeedProperties( map.RadiositySettings ) );

			upperShelf.AddRButton("ENV"				, null						, ()=> workspace.FeedProperties(map.Environment) );
			upperShelf.AddRButton("SCR"				, null						, ()=> Game.Invoker.ExecuteString("screenshot") );
			upperShelf.AddRButton("CONFIG"			, @"editor\iconComponents"	, ()=> workspace.TogglePalette( componentPalette ) );
			upperShelf.AddRButton("EDITOR\rCONFIG"	, @"editor\iconSettings"	, ()=> workspace.FeedProperties(this) );
			upperShelf.AddRButton("EXIT"			, @"editor\iconExit"		, ()=> Game.Invoker.ExecuteString("editorSave", "wait", "killEditor") );
 
			//- LOWER SHELF ------------------------------------------------------------

			lowerShelf.AddLButton("PLAY\r[SPACE]",	@"editor\iconSimulate2",() => EnableSimulation = !EnableSimulation );
			lowerShelf.AddLButton("RESET\r[ESC]" ,	@"editor\iconReset2",	() => ResetWorld() );
			lowerShelf.AddLButton("BAKE\r[B]"	 ,	@"editor\iconBake",		() => BakeToEntity() );
			lowerShelf.AddLSplitter();				 
			lowerShelf.AddLButton("ACT\n[ENTER]" ,	@"editor\iconActivate", () => ActivateSelected() );
			lowerShelf.AddLButton("USE\n[U]"	 ,	@"editor\iconUse"	,	() => UseSelected() );

			lowerShelf.AddLSplitter();				 
			//lowerShelf.AddFatLButton("Build\nNavMesh"	 ,	null,	() => this.Map.BuildNavMesh(Content) );

			lowerShelf.AddLSplitter();				 
			lowerShelf.AddFatLButton("Bake\nLightmap"		,	null,	BakeLightMap	 );
			lowerShelf.AddFatLButton("Capture\nLight Probes",	null,	BakeLightProbes  );

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
				  MoveToolSnapValue  .ToString("000.00"),
				  RotateToolSnapValue.ToString("000.00"), 
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

			workspace.AddHotkey( Keys.Z			, ModKeys.Ctrl, () => Game.Invoker.ExecuteString("undo") );
			workspace.AddHotkey( Keys.Y			, ModKeys.Ctrl, () => Game.Invoker.ExecuteString("redo") );

			workspace.AddHotkey( Keys.F			, ModKeys.None,	() => FocusSelection() );
			//workspace.AddHotkey( Keys.F2		, ModKeys.None, () => rs.VSyncInterval = (rs.VSyncInterval==1) ? 0 : 1 );
			
			workspace.AddHotkey( Keys.Q			, ModKeys.None, () => workspace.Manipulator = new NullTool() );
			workspace.AddHotkey( Keys.W			, ModKeys.None, () => workspace.Manipulator = new MoveTool(this) );
			workspace.AddHotkey( Keys.E			, ModKeys.None, () => workspace.Manipulator = new RotateTool(this) );
			workspace.AddHotkey( Keys.T			, ModKeys.None, () => TargetSelection() );
			
			workspace.AddHotkey( Keys.D1		, ModKeys.None, ResetViewMode );
			workspace.AddHotkey( Keys.D2		, ModKeys.None, ToggleDiffuse );
			workspace.AddHotkey( Keys.D3		, ModKeys.None, ToggleSpecular );
			workspace.AddHotkey( Keys.D4		, ModKeys.None, ToggleLightProbes );
			workspace.AddHotkey( Keys.D5		, ModKeys.None, ToggleLightVolume );
			workspace.AddHotkey( Keys.D6		, ModKeys.None, ToggleDirectLighting );
			workspace.AddHotkey( Keys.D7		, ModKeys.None, ToggleFiltering );
			
			workspace.AddHotkey( Keys.Delete	, ModKeys.None, () => DeleteSelection() );

			workspace.AddHotkey( Keys.Space		, ModKeys.None, () => EnableSimulation = !EnableSimulation );
			workspace.AddHotkey( Keys.Escape	, ModKeys.None, () => ResetWorld() );
			workspace.AddHotkey( Keys.K			, ModKeys.None, () => ResetSelected() );
			workspace.AddHotkey( Keys.B			, ModKeys.None, () => BakeToEntity() );
			workspace.AddHotkey( Keys.Enter		, ModKeys.None, () => ActivateSelected() );
			workspace.AddHotkey( Keys.U			, ModKeys.None, () => UseSelected() );
			workspace.AddHotkey( Keys.D			, ModKeys.Ctrl, () => DuplicateSelection() );
			workspace.AddHotkey( Keys.N			, ModKeys.None, () => ai.ShowNavigationMesh = !ai.ShowNavigationMesh );

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


		void ResetViewMode ()
		{							
			Game.Invoker.ExecuteString(
				"LightProbeViewer.ShowLightProbes False",
				"LightProbeViewer.ShowLightVolume False",
				"VTSystem.ShowDiffuse False",
				"VTSystem.ShowSpecular False",
				"RenderSystem.SkipDirectLighting False",
				"vtrestart"
			);
		}


		void ToggleDiffuse()
		{
			Game.Invoker.ExecuteString(
				"toggle VTSystem.ShowDiffuse", 
				"VTSystem.ShowSpecular False", 
				"vtrestart"
			);
		}


		void ToggleSpecular()
		{
			Game.Invoker.ExecuteString(
				"toggle VTSystem.ShowSpecular", 
				"VTSystem.ShowDiffuse False", 
				"vtrestart"
			);
		}


		void ToggleLightProbes()
		{
			Game.Invoker.ExecuteString("toggle LightProbeViewer.ShowLightProbes");
		}


		void ToggleLightVolume()
		{
			Game.Invoker.ExecuteString("toggle LightProbeViewer.ShowLightVolume");
		}


		void ToggleDirectLighting()
		{
			Game.Invoker.ExecuteString("toggle RenderSystem.SkipDirectLighting");
		}


		void ToggleFiltering()
		{
			Game.Invoker.ExecuteString(
				"toggle RenderSystem.UsePointLightmapSampling",
				"toggle RenderSystem.UsePointShadowSampling"
			);
		}


		void BakeLightMap()
		{
			Game.Invoker.Execute( new Radiosity.BakeRadiosityCommand( Game, mapName, Map.RadiositySettings ) );
			Game.Invoker.ExecuteString("contentBuild");
			//Game.Invoker.ExecuteString(string.Format("bakeLightMap {0}", mapName), "contentBuild");
		}


		void BakeLightProbes()
		{
			Game.Invoker.ExecuteString(string.Format("bakeLightProbes HdrImage {0}", mapName), "contentBuild");
		}


		Palette CreateEntityPalette ( Workspace workspace )
		{
			var palette = new Palette( workspace.Frames, "Create Node", 0,0, 150,450 );

			var entityTypes	=	Misc.GetAllSubclassesOf( typeof(EntityFactoryContent), false );

			palette.AddButton( "Static Model"			,	() => CreateNodeUI( new MapModel			() ) );
			palette.AddButton( "Prefab		"			,	() => CreateNodeUI( new MapPrefab			() ) );
			palette.AddButton( "Decal"					,	() => CreateNodeUI( new MapDecal			() ) );
			palette.AddButton( "Light Probe (Spherical)",	() => CreateNodeUI( new MapLightProbeSphere	() ) );
			palette.AddButton( "Light Probe (Box)"		,	() => CreateNodeUI( new MapLightProbeBox	() ) );
			palette.AddButton( "Omni Light"				,	() => CreateNodeUI( new MapOmniLight		() ) );
			palette.AddButton( "Spot Light"				,	() => CreateNodeUI( new MapSpotLight		() ) );
			palette.AddButton( "Light Volume"			,	() => CreateNodeUI( new MapLightVolume		() ) );
			//palette.AddButton( "Sound"					,	() => CreateNodeUI( new MapSound			() ) );
			//palette.AddButton( "Reverb Zone"			,	() => CreateNodeUI( new MapReverb			() ) );

			palette.AddSplitter();

			foreach ( var ent in entityTypes ) 
			{
				string name = ent.Name.Replace("Factory", "");

				Action action = () => { 
					var mapEntity = new MapEntity();
					mapEntity.Factory = (EntityFactoryContent)Activator.CreateInstance(ent);
					CreateNodeUI( mapEntity ); 
				};

				palette.AddButton( name, action );
			}

			return palette;
		}



		Palette CreateComponentPalette( Workspace workspace )
		{
			var palette = new Palette( workspace.Frames, "Components", 0,0, 150,450 );

			var componentList = Game.Components.OrderBy( c1 => c1.GetType().Name ).ToArray();

			foreach ( var component in componentList ) 
			{
				string name = component.GetType().Name;

				Action func = () => workspace.FeedProperties( component );

				palette.AddButton( name, func );
			}

			return palette;
		}


		MapOutliner CreateOutliner( Workspace workspace )
		{
			return new MapOutliner(workspace, this, 0,0,200,450 );
		}


		public static Panel CreateAssetExplorer ( Frame parent )
		{
			var types = new List<Type>();

			var typeFX			=	new[] { typeof(FXFactory) };
			var typeEntities	=	Misc.GetAllSubclassesOf(typeof(DataAsset), true);
			//var typeModels		=	Misc.GetAllSubclassesOf(typeof(ModelFactory), true);
			//var typeItems		=	Misc.GetAllSubclassesOf(typeof(ItemFactory), true);
			//var typeWeapon		=	Misc.GetAllSubclassesOf(typeof(Weapon), true);
			//var typeAnimation	=	Misc.GetAllSubclassesOf(typeof(AnimatorFactory), true);

			var assetExplorer	=	new AssetExplorer2( parent, "fx", typeFX, 0,0, 500, 600 );

			assetExplorer.AddToolButton( "FX"			,	() => assetExplorer.SetTargetClass( "fx"		, typeFX		) );
			assetExplorer.AddToolButton( "Data Assets"	,	() => assetExplorer.SetTargetClass( "data"		, typeEntities	) );
			//assetExplorer.AddToolButton( "Models"	,	() => assetExplorer.SetTargetClass( "models"	, typeModels	) );
			//assetExplorer.AddToolButton( "Items"	,	() => assetExplorer.SetTargetClass( "items"		, typeItems		) );
			//assetExplorer.AddToolButton( "Animation",	() => assetExplorer.SetTargetClass( "animation"	, typeAnimation	) );

			return assetExplorer;			  
		}

		
		private void SetupGridEvents( AEPropertyGrid grid )
		{
			grid.PropertyValueChanging+=Grid_PropertyValueChanging;
		}

		SetCommand setPropertyCommand = null;

		private void Grid_PropertyValueChanging( object sender, AEPropertyGrid.PropertyChangedEventArgs e )
		{
			switch ( e.SetMode )
			{
				case ValueSetMode.Default:
					Game.Invoker.Execute( new SetCommand(this, e.Property, e.Value) );
					break;

				case ValueSetMode.InteractiveInitiate:
					setPropertyCommand = new SetCommand(this, e.Property, e.Value);
					break;

				case ValueSetMode.InteractiveUpdate:
					setPropertyCommand.Value = e.Value;
					setPropertyCommand.Execute(); // hacky way to update values for all selected nodes
					CommitSelectedNodeChanges();
					break;

				case ValueSetMode.InteractiveComplete:
					Game.Invoker.Execute( setPropertyCommand );
					break;
			}
		}
	}
}
