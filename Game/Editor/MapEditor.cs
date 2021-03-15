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

namespace IronStar.Editor 
{
	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor : GameComponent 
	{
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

		ECS.GameState	gameState;
		public ECS.GameState GameState { get { return gameState; } }
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

			gameState	=	IronStar.CreateGameState( Game, Content, mapName, map );
			gameState.AddSystem( new EditorEntityRenderSystem( this, rs.RenderWorld.Debug ) );
			gameState.AddSystem( new EditorLightRenderSystem( this, rs.RenderWorld.Debug ) );
			gameState.AddSystem( new EditorPhysicsRenderSystem( this, rs.RenderWorld.Debug ) );
			gameState.AddSystem( new EditorModelRenderSystem( this, rs.RenderWorld.Debug ) );
			gameState.AddSystem( new EditorCharacterRenderSystem( this, rs.RenderWorld.Debug ) );
			gameState.Update( GameTime.MSec16 );

			//world.SimulateWorld( GameTime.MSec16 );
			//world.PresentWorld( GameTime.MSec16, 1, null, null );

			ResetWorld();

			RegisterCommands();

			Game.Reloading += Game_Reloading;
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

		public void CreateNodeUI ( MapNode newNode )
		{
			Game.Invoker.Execute( new CreateNode(this, newNode) );
			/*Map.Nodes.Add( newNode );
			newNode.SpawnNodeECS( GameState );
			Select_Deprecated( newNode );*/
		}


		public void DeleteSelection ()
		{
			Game.Invoker.Execute( new DeleteSelection(this) );
			/*foreach ( var se in selection ) 
			{
				se.KillNodeECS( gameState );
				map.Nodes.Remove( se );
			}

			selection.Clear();*/
			//FeedSelection();
		}



		public void DuplicateSelection ()
		{
			Game.Invoker.Execute( new DuplicateSelection(this) );

			/*var newItems = selection
				#warning REMOVE PARAMETER
				.Select( item => {
					var newNode = item.DuplicateNode();
					newNode.TranslateVector = item.TranslateVector + Vector3.One;
					return newNode;
				})
				.ToArray();

			Map.Nodes.AddRange( newItems );

			foreach ( var newItem in newItems ) 
			{
				newItem.SpawnNodeECS(gameState);
			}

			selection.Clear();
			selection.AddRange( newItems );

			//FeedSelection(); */
		}


		public void ResetSelected()
		{
			foreach ( var node in Selection )
			{
				node.ResetNodeECS(GameState);
			}

		}


		/// <summary>
		/// 
		/// </summary>
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
			foreach ( var node in map.Nodes ) 
			{
				node.SpawnNodeECS(gameState);
			}

			gameState.Update( GameTime.Zero );
		}



		/// <summary>
		/// 
		/// </summary>
		public void BakeToEntity ()
		{
			foreach ( var se in selection ) 
			{
				var transform = (se as MapEntity)?.EcsEntity?.GetComponent<ECS.Transform>();

				if (transform!=null) 
				{
					try 
					{
						se.TranslateVector	=	transform.Position;
						se.RotateQuaternion	=	transform.Rotation;
					} 
					catch ( Exception e ) 
					{
						Log.Error("Failed to bake: {0}", e.Message);
						se.TranslateVector	=	transform.Position;
						se.RotateQuaternion	=	Quaternion.Identity;
					}
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="target"></param>
		public void SelectedPropertyChange ( object target )
		{
			var mapNode = target as MapNode;
			mapNode?.ResetNodeECS( gameState );
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

			var matrix	=	Matrix.LookAtRH( aimObj.TranslateVector, tpos, Vector3.Up );
			matrix.Invert();

			aimObj.RotateQuaternion	=	Quaternion.RotationMatrix( matrix );
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
					points.AddRange( node.GetBoundingBox().GetCorners().Select( p => Vector3.TransformCoordinate( p, node.WorldMatrix ) ) );
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
