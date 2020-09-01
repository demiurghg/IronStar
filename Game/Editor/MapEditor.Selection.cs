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
using IronStar.ECS;

namespace IronStar.Editor {

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor {

		LayerState GetLayerStateForNode ( MapNode node )
		{
			if (node is MapEntity)			return LayerEntities;

			if (node is MapModel)			return LayerGeometry;
			if (node is MapDecal)			return LayerDecals;

			if (node is MapLightProbeBox)		return LayerLightProbes;
			if (node is MapOmniLight)		return LayerLightSet;
			if (node is MapSpotLight)		return LayerLightSet;

			return LayerState.Default;
		}


		bool IsVisible ( MapNode node )
		{
			return (GetLayerStateForNode(node)==LayerState.Frozen || GetLayerStateForNode(node)==LayerState.Default) && node.Visible; 
		}


		bool IsSelectable ( MapNode node )
		{
			return (GetLayerStateForNode(node)==LayerState.Default) && !node.Frozen; 
		}


		public bool IsSelected( Entity entity )
		{
			return selection.Any( node => node.EcsEntity == entity );
		}


		public bool GetRenderProperties( Entity entity, out Color color, out bool selected )
		{
			var node	=	Map.Nodes.FirstOrDefault( n => n.EcsEntity == entity );

			if (node!=null)
			{
				selected	=	selection.Contains(node);
				color		=	selected ? Utils.WireColorSelected : ( IsSelectable(node) ? Utils.WireColor : Utils.GridColor );
				color		=	(selection.LastOrDefault()==node) ? Color.White : color;

				return IsVisible(node);
			}
			else
			{
				color		=	Color.Black;
				selected	=	false;
				return false;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public void Select( int x, int y, bool add )
		{
			var ray = camera.PointToRay( x, y );

			var pickedItem		=	GetNodeUnderCursor( x, y );


			if (add) {

				if (pickedItem==null) {
					return;
				}

				if (selection.Contains(pickedItem)) {
					selection.Remove(pickedItem);
				} else {
					selection.Add(pickedItem);
				}

			} else {

				ClearSelection();

				if (pickedItem!=null) {
					selection.Add(pickedItem);
				}
			}

			FeedSelection();
			Do();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		MapNode GetNodeUnderCursor ( int x, int y )
		{
			Vector3 p1, p2;
			float d1, d2;

			var node1	=	GetNodeUnderCursorModel( x, y, out p1, out d1 );
			var node2	=	GetNodeUnderCursorNoModel( x, y, out p2, out d2 );

			if (d1<d2) {
				return node1;
			} else {
				return node2;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		MapNode GetNodeUnderCursorModel ( int x, int y, out Vector3 hitPoint, out float distance )
		{
			var ray		=	camera.PointToRay( x, y );
			var from	=	ray.Position;
			var to		=	ray.Position + ray.Direction.Normalized() * 3000;
			var n		=	Vector3.Zero;

			var phys	=	gameState.GetService<ECSPhysics.PhysicsCore>();

			ECS.Entity entity	=	phys.RayCastEditor( from, to, out n, out hitPoint, out distance );

			if (entity!=null) 
			{
				return map.Nodes.FirstOrDefault( node => node.HasEntity(entity) && IsSelectable(node) );
			}
			else
			{
				return null;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		MapNode GetNodeUnderCursorNoModel ( int x, int y, out Vector3 hitPoint, out float distance )
		{
			var ray				=	camera.PointToRay( x, y );
			MapNode pickedItem	=	null;
			hitPoint			=	Vector3.Zero;

			distance			=	float.MaxValue;

			foreach ( var item in map.Nodes ) {

				if (IsSelectable(item)) {

					var bbox	=	DefaultBox;
					var iw		=	Matrix.Invert( item.WorldMatrix );
					float d;

					var rayT	=	Utils.TransformRay( iw, ray );

					if (rayT.Intersects(ref bbox, out d)) {
						if (distance > d) {
							distance = d;
							pickedItem = item;
						}
					}
				}
			}

			return pickedItem;
		}



		bool  marqueeSelecting = false;
		bool  selectingMarqueeAdd = false;
		Point selectingMarqueeStart;
		Point selectingMarqueeEnd;

		public Rectangle SelectionMarquee {
			get { 
				if (!marqueeSelecting) {
					return new Rectangle(0,0,0,0);
				} else {
					return new Rectangle( 
						Math.Min( selectingMarqueeStart.X, selectingMarqueeEnd.X ),
						Math.Min( selectingMarqueeStart.Y, selectingMarqueeEnd.Y ),
						Math.Abs( selectingMarqueeStart.X - selectingMarqueeEnd.X ),
						Math.Abs( selectingMarqueeStart.Y - selectingMarqueeEnd.Y )
					);
				}
			}
		}


		public void StartMarqueeSelection ( int x, int y, bool add )
		{
			selectingMarqueeAdd = add;
			marqueeSelecting = true;
			selectingMarqueeStart = new Point(x,y);
			selectingMarqueeEnd	  = selectingMarqueeStart;
		}

		public void UpdateMarqueeSelection ( int x, int y )
		{
			if (marqueeSelecting) {
				selectingMarqueeEnd	  = new Point(x,y);
			}
		}

		public void StopMarqueeSelection ( int x, int y )
		{
			if (marqueeSelecting) {

				if (selectingMarqueeStart==selectingMarqueeEnd) {
					marqueeSelecting = false;
					return;
				}

				if (!selectingMarqueeAdd) {
					ClearSelection();
				}

				foreach ( var item in map.Nodes ) {
					if (camera.IsInRectangle( item.TranslateVector, SelectionMarquee )) {
						if (IsSelectable(item)) {
							selection.Add( item );
						}
					}
				}

				FeedSelection();
				Do();

				marqueeSelecting = false;
			}
		}


	}
}
