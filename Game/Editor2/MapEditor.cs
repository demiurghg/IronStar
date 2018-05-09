using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
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
using Fusion.Engine.Frames;

namespace IronStar.Editor2 {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor : IEditorInstance {

		public static readonly BoundingBox DefaultBox = new BoundingBox( Vector3.One * (-0.25f), Vector3.One * 0.25f );

		readonly string mapName;
		readonly string fullPath;
		
		public Game Game { get; private set; }
		public ContentManager Content { get; private set; }
		readonly RenderSystem rs;

		public EditorCamera	camera;
		public Manipulator	manipulator;

		readonly Stack<MapNode[]> selectionStack = new Stack<MapNode[]>();
		readonly List<MapNode> selection = new List<MapNode>();

		Map	map = null;

		public Map Map {
			get {
				return map;
			}
		}

		public readonly EditorConfig Config;

		GameWorld world;

		public GameWorld World { get { return world; } }

		Workspace workspace;


		class MessageService : IMessageService {
			public void Push( string message )
			{
				Log.Message("MSG: {0}", message);
			}

			public void Push( Guid client, string message )
			{
				Log.Message("MSG: {0} {1}", client, message);
			}
		}


		/// <summary>
		/// Initializes server-side world.
		/// </summary>
		/// <param name="maxPlayers"></param>
		/// <param name="maxEntities"></param>
		public MapEditor ( GameEditor editor, string map )
		{
			this.mapName	=	map;

			Log.Verbose( "game editor" );
			this.Game       =   editor.Game;
			this.rs			=	Game.RenderSystem;
			Content         =   new ContentManager( Game );

			Config			=	new EditorConfig(this);

			camera			=	new EditorCamera( this );
			manipulator		=	new NullTool( this );
			world			=	new GameWorld( Game, new MessageService(), true, new Guid() );
			world.InitServerAtoms();

			workspace		=	new Workspace( this, Game.Frames.RootFrame );
			

			Game.Keyboard.ScanKeyboard =	true;

			fullPath	=	Builder.GetFullPath(@"maps\" + map + ".map");
		}



		/// <summary>
		/// 
		/// </summary>
		void IEditorInstance.Initialize()
		{
			Game.Config.ApplySettings( Config );
			Game.Invoker.RegisterObject( "EditorConfig", Config );

			if (File.Exists(fullPath)) {
				Log.Message("Opening existing map: {0}", fullPath);
				this.map = Map.LoadFromXml( File.OpenRead( fullPath ) );
			} else {
				Log.Message("Creating new map: {0}", fullPath);
				this.map = new Map();
			}

			map.ActivateMap( world, true );
			world.SimulateWorld( 0 );
			world.PresentWorld( 0.016f, 1, null, null );
		}



		/// <summary>
		/// Saved at dispose
		/// </summary>
		public void SaveMap ()
		{
			Log.Message("Saving map: {0}", fullPath);
			File.Delete( fullPath );
			Map.SaveToXml( map, File.OpenWrite( fullPath ) );
		}


		/// <summary>
		/// 
		/// </summary>
		void FeedSelection ()
		{
			Editors.Editor.GetMapEditor()?.SetSelection( selection, map.Environment );

			workspace.FeedProperties( selection.FirstOrDefault() );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( !disposedValue ) {
				if ( disposing ) {

					Game.Config.RetrieveSettings( Config );
					Game.Invoker.UnregisterObject( "EditorConfig" );

					world?.Dispose();

					SaveMap();

					rs.RenderWorld.ClearWorld();

					workspace.CloseWorkspace();

					Builder.SafeBuild(false, null, null);
				}

				disposedValue = true;
			}
		}

		private bool disposedValue = false;

		public void Dispose()
		{
			Dispose( true );
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
			newNode.SpawnNode( World );
			Map.Nodes.Add( newNode );
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
			foreach ( var se in selection ) {
				se.KillNode( world );
				map.Nodes.Remove( se );
			}

			ClearSelection();
			FeedSelection();
		}



		public void DuplicateSelection ()
		{
			var newItems = selection
				.Select( item => item.DuplicateNode() )
				.ToArray();

			Map.Nodes.AddRange( newItems );

			foreach ( var newItem in newItems ) {
				newItem.SpawnNode(world);
			}

			ResetWorld(true);
			ClearSelection();

			selection.AddRange( newItems );

			FeedSelection();
		}



		/// <summary>
		/// 
		/// </summary>
		public void ResetWorld (bool hardResetSelection)
		{
			EnableSimulation = false;

			if (hardResetSelection) {
				foreach ( var se in selection ) {
					se.HardResetNode( world );
				}
			}

			foreach ( var node in map.Nodes ) {
				node.ResetNode( world );
			}

			world.SimulateWorld(0);
		}



		/// <summary>
		/// 
		/// </summary>
		public void BakeToEntity ()
		{
			foreach ( var se in selection ) {
				var entity = (se as MapEntity)?.Entity;
				if (entity!=null) {
					se.Position	=	entity.Position;
					se.Rotation	=	entity.Rotation;
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public void ActivateSelected ()
		{
			foreach ( var se in selection ) {
				se.ActivateNode();
			}
		}


		/// <summary>
		/// 
		/// </summary>
		public void UseSelected ()
		{
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
		void IEditorInstance.Update( GameTime gameTime )
		{
			camera.Update( gameTime );

			var dr = rs.RenderWorld.Debug;

			dr.DrawBox( map.Environment.IrradianceVolume, Color.Cyan );

			//RefreshAppearance();

			if (EnableSimulation) {
				world.SimulateWorld( gameTime.ElapsedSec );
			}
			world.PresentWorld( gameTime.ElapsedSec, 1, null, null );

			rs.RenderWorld.Debug.DrawGrid( 10 );

			map.DrawNavigationMeshDebug( rs.RenderWorld.Debug );

			//
			//	Draw unselected :
			//
			foreach ( var item in map.Nodes ) {

				var color = item.Frozen ? Utils.GridColor : Utils.WireColor;

				item.DrawNode( world, dr, color, false ); 
			}

			//
			//	Draw selected :
			//
			foreach ( var item in selection ) {

				var color = Utils.WireColorSelected;

				if (selection.Last()!=item) {
					color = Color.White;
				}

				dr.DrawBasis( item.WorldMatrix, 0.5f, 3 );
				item.DrawNode( world, dr, color, true ); 
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
		public void FocusSelection ()
		{
			var targets = selection.Any() ? selection.ToArray() : map.Nodes.ToArray();

			BoundingBox bbox;

			if (!targets.Any()) {
				bbox = new BoundingBox( new Vector3(-10,-10,-10), new Vector3(10,10,10) );
			} else {

				bbox = BoundingBox.FromPoints( targets.Select( t => t.Position ).ToArray() );

			}


			var size	= Vector3.Distance( bbox.Minimum, bbox.Maximum ) + 1;
			var center	= bbox.Center();

			camera.Target	= center;
			camera.Distance = size;
		}


	}
}
