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

namespace IronStar.Editor {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor : GameComponent {

		const string Ext = ".json";

		public static readonly BoundingBox DefaultBox = new BoundingBox( Vector3.One * (-0.25f), Vector3.One * 0.25f );

		string mapName;
		string fullPath;
		
		public ContentManager Content { get; private set; }
		readonly RenderSystem rs;

		public EditorCamera	camera;
		public Manipulator	manipulator;

		public Manipulator Manipulator {
			get { return manipulator; }
			set {
				if (!manipulator.IsManipulating) {
					manipulator = value;
				}
			}
		}

		readonly Stack<MapNode[]> selectionStack = new Stack<MapNode[]>();
		readonly List<MapNode> selection = new List<MapNode>();

		Map	map = null;

		public Map Map {
			get {
				return map;
			}
		}

		GameWorld world;

		public GameWorld World { get { return world; } }


		/// <summary>
		/// Initializes server-side world.
		/// </summary>
		/// <param name="maxPlayers"></param>
		/// <param name="maxEntities"></param>
		public MapEditor ( Game game, string map ) : base(game)
		{
			this.mapName	=	map;

			this.rs			=	Game.RenderSystem;
			Content         =   new ContentManager( Game );
			camera			=	new EditorCamera( this );
			manipulator		=	new NullTool( this );

			SetupWorkspace();

			fullPath	=	Builder.GetFullPath(@"maps\" + map + Ext);
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			if (File.Exists(fullPath)) {
				Log.Message("Opening existing map: {0}", fullPath);
				this.map = (Map)Game.GetService<Factory>().ImportJson( File.OpenRead( fullPath ) );
			} else {
				Log.Message("Creating new map: {0}", fullPath);
				this.map = new Map();
			}

			world			=	new GameWorld( Game, this.map, Content, new LocalMessageService(), true, new Guid() );

			world.SimulateWorld( GameTime.MSec16 );
			world.PresentWorld( GameTime.MSec16, 1, null, null );

			ResetWorld(true);

			RegisterCommands();

			Game.Reloading += Game_Reloading;
		}


		private void Game_Reloading( object sender, EventArgs e )
		{
			ResetWorld(true);
		}



		/// <summary>
		/// Saved at dispose
		/// </summary>
		public void SaveMap ()
		{
			Log.Message("Saving map: {0}", fullPath);
			File.Delete( fullPath );

			Game.GetService<Factory>().ExportJson( File.OpenWrite( fullPath ), map );
		}


		/// <summary>
		/// Saved at dispose
		/// </summary>
		public void SaveMapAs ( string newMapName )
		{
			fullPath	=	Builder.GetFullPath(@"maps\" + newMapName + Ext);
			mapName		=	newMapName;

			Log.Message("Saving map: {0}", fullPath);
			File.Delete( fullPath );

			Game.GetService<Factory>().ExportJson( File.OpenWrite( fullPath ), map );
		}


		/// <summary>
		/// 
		/// </summary>
		void FeedSelection ()
		{
			workspace?.FeedProperties( selection.FirstOrDefault() );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing ) {

				Game.Reloading -= Game_Reloading;

				selection.Clear();
				FeedSelection();

				UnregisterCommands();
				SaveMap();

				workspace?.CloseWorkspace();

				world?.Dispose();

				rs.RenderWorld.ClearWorld();

				Builder.SafeBuild(false, null, null);
			}
		}


		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Selection :
		 * 
		-----------------------------------------------------------------------------------------*/

		public IEnumerable<MapNode> Selection {
			get { return selection; }
		}

		public MapNode[] GetSelection() 
		{
			return selection.ToArray();
		}


		public void PushSelection ()
		{
			selectionStack.Push( selection.ToArray() );
		}


		public void PopSelection ()
		{
			selection.Clear();
			selection.AddRange( selectionStack.Pop() );
		}

		public void ClearSelection ()
		{
			selection.Clear();
		}



		public void CreateNodeUI ( MapNode newNode )
		{
			Map.Nodes.Add( newNode );
			newNode.SpawnNode( World );
			Select( newNode );
		}


		public void Select( MapNode node )
		{
			if ( node==null ) {
				throw new ArgumentNullException( "node" );
			}
			if ( !map.Nodes.Contains( node ) ) {
				throw new ArgumentException( "Provided node does not exist in current map" );
			}
			selection.Clear();
			selection.Add( node );

			FeedSelection();
		}


		public void DeleteSelection ()
		{
			if (manipulator.IsManipulating) {
				return;
			}

			foreach ( var se in selection ) {
				se.KillNode( world );
				map.Nodes.Remove( se );
			}

			ClearSelection();
			FeedSelection();
		}



		public void DuplicateSelection ()
		{
			if (manipulator.IsManipulating) {
				return;
			}

			var newItems = selection
				.Select( item => item.DuplicateNode() )
				.ToArray();

			Map.Nodes.AddRange( newItems );

			foreach ( var newItem in newItems ) {
				newItem.SpawnNode(world);
			}

			ClearSelection();

			selection.AddRange( newItems );

			FeedSelection();
		}



		/// <summary>
		/// 
		/// </summary>
		public void ResetWorld (bool hardResetSelection)
		{
			if (manipulator.IsManipulating) {
				return;
			}

			EnableSimulation = false;


			foreach ( var node in map.Nodes ) {
				node.ResetNode( world );
			}

			if (hardResetSelection) {
				
				//	kill node's entities
				foreach ( var node in map.Nodes ) {
					node.KillNode(world);
				}
	
				//	kill temporaly created entities
				world.KillAll();

				//	spawn entities again
				foreach ( var node in map.Nodes ) {
					node.SpawnNode(world);
				}
			}



			world.SimulateWorld( GameTime.Zero );
		}



		/// <summary>
		/// 
		/// </summary>
		public void BakeToEntity ()
		{
			if (manipulator.IsManipulating) {
				return;
			}

			foreach ( var se in selection ) {
				var entity = (se as MapEntity)?.Entity;
				if (entity!=null) {
					try {
						se.TranslateVector	=	entity.Position;
						se.RotateQuaternion	=	entity.Rotation;
					} catch ( Exception e ) {
						Log.Error("Failed to bake: {0}", e.Message);
						se.TranslateVector	=	entity.Position;
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
			mapNode?.ResetNode( World );

			if (target is MapEnvironment) {
				map.UpdateEnvironment(world);
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void ActivateSelected ()
		{
			if (manipulator.IsManipulating) {
				return;
			}

			foreach ( var se in selection ) {
				se.ActivateNode();
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void UseSelected ()
		{
			if (manipulator.IsManipulating) {
				return;
			}

			foreach ( var se in selection ) {
				se.UseNode();
			}
		}


		public bool EnableSimulation { get; set; } = false;

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

			//dr.DrawBox( map.Environment.IrradianceVolume, Color.Cyan );

			//
			//	Update nodes :
			//
			foreach ( var item in map.Nodes ) {
				item.Update( gameTime, world );
			}


			//
			//	Simulate & present world
			//
			if (EnableSimulation) {
				world.SimulateWorld( gameTime );
			}
			world.PresentWorld( gameTime, 1, null, null );

			//	draw stuff :
			if (DrawGrid) {
				rs.RenderWorld.Debug.DrawGrid( 10 );
			}

			map.DrawNavMesh( rs.RenderWorld.Debug );

			//
			//	Draw unselected :
			//
			foreach ( var item in map.Nodes ) {

				var color = IsSelectable( item ) ? Utils.WireColor : Utils.GridColor;

				if (IsVisible(item)) {
					item.DrawNode( world, dr, color, false ); 
				}
			}

			//
			//	Draw selected :
			//
			foreach ( var item in selection ) {

				var color = Utils.WireColorSelected;

				if (selection.Last()!=item) {
					color = Color.White;
				}

				if (IsVisible(item)) {
					dr.DrawBasis( item.WorldMatrix, 0.5f, 3 );
					item.DrawNode( world, dr, color, true ); 
				}
			}

			var mp = Game.Mouse.Position;

			manipulator?.Update( gameTime, mp.X, mp.Y );
		}


		/// <summary>
		/// 
		/// </summary>
		public void UnfreezeAll ()
		{
			foreach ( var node in map.Nodes ) {
				node.Frozen = false;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void FreezeSelected ()
		{
			foreach ( var node in Selection ) {
				node.Frozen = true;
			}
			ClearSelection();
		}


		/// <summary>
		/// 
		/// </summary>
		public void TargetSelection ()
		{
			if (manipulator.IsManipulating) {
				return;
			}

			if (selection.Count<2) {
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

			BoundingBox bbox;

			if (!targets.Any()) {
				bbox = new BoundingBox( new Vector3(-10,-10,-10), new Vector3(10,10,10) );
			} else {

				bbox = BoundingBox.FromPoints( targets.Select( t => t.TranslateVector ).ToArray() );

			}

			var halfFov	= MathUtil.DegreesToRadians( camera.Fov / 2 );

			var scaler	= MathUtil.Clamp ( (float)Math.Sin( halfFov ), 0.125f/2.0f, 1.0f );

			var size	= Vector3.Distance( bbox.Minimum, bbox.Maximum ) + 1;
			var center	= bbox.Center();

			camera.Target	= center;
			camera.Distance = (size / scaler) * 1.5f;
		}


	}
}
