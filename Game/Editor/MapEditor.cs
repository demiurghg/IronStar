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
using Fusion.Widgets;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
using Fusion.Build;
using BEPUphysics;
using IronStar.Editor.Controls;
using IronStar.Editor.Manipulators;
using Fusion.Engine.Frames;
using IronStar.ECSPhysics;
using IronStar.Editor.Systems;
using IronStar.AI;
using IronStar.Editor.Commands;
using Fusion.Widgets.Dialogs;

namespace IronStar.Editor 
{
	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor : GameComponent 
	{
		public static MapEditor Instance { get; private set; }

		const string Dir = "maps";
		const string Ext = ".json";

		public static readonly BoundingBox DefaultBox = new BoundingBox( Vector3.One * (-0.25f), Vector3.One * 0.25f );

		string mapName;
		string fullPath;
		
		public ContentManager Content { get; private set; }
		readonly RenderSystem rs;

		public EditorCamera	camera;

		readonly Selection<MapNode> selection;

		Map	map = null;

		public Map Map { get { return map; } }

		ECS.IGameState	gameState;
		public ECS.IGameState GameState { get { return gameState; } }
		/*GameWorld world;

		public GameWorld World { get { return world; } }*/


		/// <summary>
		/// Initializes server-side world.
		/// </summary>
		/// <param name="maxPlayers"></param>
		/// <param name="maxEntities"></param>
		public MapEditor ( Game game, string map ) : base(game)
		{
			mapName			=	map;
			selection		=	new Selection<MapNode>();

			selection.Changed += Selection_Changed;

			this.rs			=	Game.RenderSystem;
			Content         =   new ContentManager( Game );
			camera			=	new EditorCamera( this );

			SetupWorkspace();

			Game.Invoker.ClearUndoHistory();

			fullPath	=	GetFullMapPath(map);
		}

		
		private void Selection_Changed( object sender, EventArgs e )
		{
			workspace?.FeedProperties( selection.LastOrDefault() );
			workspace.Manipulator.StopManipulation(0,0);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		string GetFullMapPath( string map )
		{
			return	Path.Combine( Game.GetService<Builder>().GetBaseInputDirectory(), Dir, map + Ext );
		}


		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			if (File.Exists(fullPath)) 
			{
				Log.Message("Opening existing map: {0}", fullPath);
				this.map = (Map)JsonUtils.ImportJson( File.OpenRead( fullPath ) );
			}
			else
			{
				Log.Message("Creating new map: {0}", fullPath);
				this.map = new Map();
			}

			gameState	=	IronStar.CreateGameState( Game, Content, mapName, map, this );

			RegisterCommands();

			Game.Reloading += Game_Reloading;

			Instance	=	this;
		}


		private void Game_Reloading( object sender, EventArgs e )
		{
			ResetWorld();
		}



		/// <summary>
		/// Saved at dispose
		/// </summary>
		public void SaveMap ()
		{
			Log.Message("Saving map: {0}", fullPath);
			File.Delete( fullPath );

			JsonUtils.ExportJson( File.OpenWrite( fullPath ), map );
		}


		/// <summary>
		/// 
		/// </summary>
		public void GeneratePreview ()
		{
			var previewPath = Path.Combine( Path.GetDirectoryName( fullPath ), "thumbnails", Path.GetFileNameWithoutExtension( fullPath ) );
			Game.GetService<RenderSystem>().MakePreviewScreenshot( previewPath );
		}


		/// <summary>
		/// Saved at dispose
		/// </summary>
		public void SaveMapAs ( string newMapName )
		{
			fullPath	=	GetFullMapPath(newMapName);
			mapName		=	newMapName;

			SaveMap();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing ) 
			{
				Instance	=	null;

				Game.Invoker.ClearUndoHistory();

				Game.Reloading -= Game_Reloading;

				selection.Clear();
				//FeedSelection();

				UnregisterCommands();
				//SaveMap();

				workspace?.CloseWorkspace();

				gameState?.Dispose();

				rs.RenderWorld.ClearWorld();

				Game.GetService<Builder>().Build();
			}
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Selection :
		 * 
		-----------------------------------------------------------------------------------------*/

		public Selection<MapNode> Selection 
		{
			get { return selection; }
		}


		public string GetUniqueName( MapNode node )
		{
			var name		=	node.Name;
			var hashSet		=	new HashSet<string>( map.Nodes.Where( n1=>n1!=node ).Select( n2 => n2.Name ) );
			var baseName	=	"";
			var index		=	Misc.GetTrailingNumber(name, out baseName);
			var newName		=	name;

			while (hashSet.Contains(newName))
			{
				index++;
				newName = baseName + index.ToString();
			}

			return newName;
		}

		
		public void CreateNodeUI ( MapNode newNode )
		{
			Game.Invoker.Execute( new CreateNode(this, newNode) );
		}


		public void DeleteSelection ()
		{
			Game.Invoker.Execute( new DeleteSelected(this) );
		}


		public void DuplicateSelection ()
		{
			Game.Invoker.Execute( new DuplicateSelected(this) );
		}


		public void ResetSelected()
		{
			foreach ( var node in Selection )
			{
				node.ResetNodeECS(GameState);
			}
		}


		public void ResetWorld ()
		{
			EnableSimulation = false;

			//	kill node's entities
			foreach ( var node in map.Nodes ) 
			{
				node.KillNodeECS(gameState);
			}
	
			//	kill entities created by other 
			//	entities during simualtion
			gameState.KillAll();

			//	spawn entities again
			map.ActivateGameState(gameState);

			gameState.Update( GameTime.Zero );
		}



		/// <summary>
		/// 
		/// </summary>
		public void BakeToEntity ()
		{
			Game.Invoker.Execute(new BakeCommand(this) );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		public void CommitSelectedNodeChanges ()
		{
			foreach (var node in Selection)
			{
				node?.ResetNodeECS( gameState );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void ActivateSelected ()
		{
			Log.Warning("MapEditor.ActivateSelected -- NOT IMPLEMENTED");

			foreach ( var se in selection ) 
			{
				//se.ActivateNode();
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void UseSelected ()
		{
			Log.Warning("MapEditor.UseSelected -- NOT IMPLEMENTED");

			foreach ( var se in selection ) 
			{
				//se.UseNode();
			}
		}


		public bool EnableSimulation 
		{ 
			get 
			{ 
				return enableSimulation; 
			}
			set 
			{ 
				enableSimulation = value;
				gameState.GetService<PhysicsCore>().Enabled = enableSimulation;
				gameState.GetService<BehaviorSystem>().Enabled = enableSimulation;
			}
		}
		bool enableSimulation;


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Selection :
		 * 
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update(GameTime gameTime)
		{
			fpsCounter.AddTime( gameTime );

			camera.Update( gameTime );

			var dr = rs.RenderWorld.Debug;

			map.Validate();

			//
			//	Simulate & present world
			//
			gameState.Update( gameTime );

			//	draw stuff :
			if (DrawGrid) 
			{
				rs.RenderWorld.Debug.DrawGrid();
			}

			var mp = Game.Mouse.Position;

			workspace.Manipulator?.Update( gameTime, mp.X, mp.Y );
		}


		/// <summary>
		/// 
		/// </summary>
		public void UnfreezeAll ()
		{
			foreach ( var node in map.Nodes ) 
			{
				node.Frozen = false;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void MakePrefab ()
		{
			var saveFileDialog = new SaveFileDialog( workspace.Frames, "prefabs", "*.json" );
			saveFileDialog.Show( (name) => Game.Invoker.Execute( new EditorPrefabCommand(this, name) ) );
		}


		/// <summary>
		/// 
		/// </summary>
		public void FreezeSelected ()
		{
			foreach ( var node in Selection ) 
			{
				node.Frozen = true;
			}
			selection.Clear();
		}


		/// <summary>
		/// 
		/// </summary>
		public void TargetSelection ()
		{
			if (selection.Count<2) 
			{
				Log.Warning("TargetSelection: select at least two objects");
				return;
			}
			
			var targets =	selection.Take(selection.Count-1);
			var aimObj  =	selection.Last();

			var x		=	targets.Average( t => t.TranslateX );
			var y		=	targets.Average( t => t.TranslateY );
			var z		=	targets.Average( t => t.TranslateZ );
			
			var tpos	=	new Vector3(x,y,z);

			var matrix	=	Matrix.LookAtRH( aimObj.Translation, tpos, Vector3.Up );
			matrix.Invert();

			aimObj.Rotation	=	Quaternion.RotationMatrix( matrix );
		}


		/// <summary>
		/// 
		/// </summary>
		public void FocusSelection ()
		{
			var targets = selection.Any() ? selection.ToArray() : map.Nodes.ToArray();

			var points = new List<Vector3>();

			if (!targets.Any()) 
			{
				points.Add( Vector3.One * 30 );
				points.Add( Vector3.One * (-30) );
			} 
			else 
			{
				foreach ( var node in targets ) 
				{
					points.AddRange( node.GetBoundingBox(gameState).GetCorners().Select( p => Vector3.TransformCoordinate( p, node.Transform ) ) );
				}
			}

			var bbox	= BoundingBox.FromPoints( points );

			var halfFov	= MathUtil.DegreesToRadians( camera.Fov / 2 );

			var scaler	= MathUtil.Clamp ( (float)Math.Sin( halfFov ), 0.125f/2.0f, 1.0f );

			var size	= Vector3.Distance( bbox.Minimum, bbox.Maximum ) + 1;
			var center	= bbox.Center();

			camera.Target	= center;
			camera.Distance = (size / scaler) * 1.0f;
		}
	}
}
