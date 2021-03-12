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
using IronStar.Editor.Commands;

namespace IronStar.Editor 
{
	public partial class MapEditor 
	{
		MapNode GetNodeUnderCursor ( int x, int y )
		{
			Vector3 p1, p2;
			float d1, d2;

			var node1	=	GetNodeUnderCursorModel( x, y, out p1, out d1 );
			var node2	=	GetNodeUnderCursorNoModel( x, y, out p2, out d2 );

			if (d1<d2) 
			{
				return node1;
			}
			else 
			{
				return node2;
			}
		}


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


		MapNode GetNodeUnderCursorNoModel ( int x, int y, out Vector3 hitPoint, out float distance )
		{
			var ray				=	camera.PointToRay( x, y );
			MapNode pickedItem	=	null;
			hitPoint			=	Vector3.Zero;

			distance			=	float.MaxValue;

			foreach ( var item in map.Nodes ) 
			{
				if (IsSelectable(item)) 
				{
					var bbox	=	DefaultBox;
					var iw		=	Matrix.Invert( item.WorldMatrix );
					float d;

					var rayT	=	Utils.TransformRay( iw, ray );

					if (rayT.Intersects(ref bbox, out d)) 
					{
						if (distance > d) 
						{
							distance = d;
							pickedItem = item;
						}
					}
				}
			}

			return pickedItem;
		}

		
		/*-----------------------------------------------------------------------------------------
		 *	Picking
		-----------------------------------------------------------------------------------------*/

		public void PickSelection( int x, int y, bool add )
		{
			if (!marqueeSelecting)
			{
				var ray = camera.PointToRay( x, y );

				var pickedItem	=	GetNodeUnderCursor( x, y );
				var mode		=	add ? SelectMode.Toggle : SelectMode.Replace;

				Game.Invoker.Execute( new Select(this, mode, pickedItem ) );
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Marquee
		-----------------------------------------------------------------------------------------*/

		bool  marqueeStarted = false;
		bool  marqueeSelecting = false;
		bool  selectingMarqueeAdd = false;
		Point selectingMarqueeStart;
		Point selectingMarqueeEnd;
		bool  marqueeWithinThreshold { get { return Vector2.Distance( selectingMarqueeStart, selectingMarqueeEnd ) < selectingMarqueeThreshold; } }
		const int selectingMarqueeThreshold = 5;

		
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
			selectingMarqueeAdd		= add;
			marqueeStarted			= true;
			selectingMarqueeStart	= new Point(x,y);
			selectingMarqueeEnd		= selectingMarqueeStart;
		}

		
		public void UpdateMarqueeSelection ( int x, int y )
		{
			if (marqueeStarted)
			{
				selectingMarqueeEnd	= new Point(x,y);

				if (!marqueeWithinThreshold)
				{
					marqueeSelecting = true;
				}
			}
		}

		
		public void StopMarqueeSelection ( int x, int y )
		{
			var candidates = new List<MapNode>();
			var mode = SelectMode.Replace;

			if (marqueeSelecting && !marqueeWithinThreshold) 
			{
				if (selectingMarqueeAdd) 
				{
					mode = SelectMode.Append;
				}

				foreach ( var item in map.Nodes ) 
				{
					if (camera.IsInRectangle( item.TranslateVector, SelectionMarquee )) 
					{
						if (IsSelectable(item)) 
						{
							candidates.Add( item );
						}
					}
				}

				Game.Invoker.Execute( new Select( this, mode, candidates.ToArray() ) );
			}

			marqueeStarted = false;
			marqueeSelecting = false;
		}
	}
}
