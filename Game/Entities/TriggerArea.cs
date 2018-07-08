using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.Core;
using IronStar.Physics;
using Fusion;
using IronStar.Entities.Players;

namespace IronStar.Entities {

	public enum TriggerFilter {
		All,
		Player,
		Monster,
	}

	public class TriggerArea : Entity {

		DetectorArea area;
		TriggerFilter filter;
		readonly string target; 

		public TriggerArea( uint id, short clsid, GameWorld world, TriggerAreaFactory factory ) : base( id, clsid, world, factory )
		{
			var w	=	factory.Width;
			var h	=	factory.Height;
			var d	=	factory.Depth;

			target	=	factory.Target;

			filter	=	factory.TriggerFilter;

			area = new DetectorArea( world, w, h, d );

			area.Touch +=Area_Touch;
		}

		private void Area_Touch( object sender, EntityEventArgs e )
		{
			switch (filter) {
				case TriggerFilter.All		: ActivateTargets(); break;
				case TriggerFilter.Monster	: Log.Warning("Monsters are not implemented"); break;
				case TriggerFilter.Player	: if (e.Entity is Player) ActivateTargets(); break;
			}
		}


		void ActivateTargets ()
		{
			World.ActivateTargets( this, target );
		}


		public override void Teleport( Vector3 position, Quaternion orient )
		{
			base.Teleport( position, orient );
			area?.Teleport( position, orient );
		}


		public override void Kill()
		{
			base.Kill();
			area?.Destroy();
		}
	}


	public class TriggerAreaFactory : EntityFactory {

		public float  Width  { get; set; } = 1;
		public float  Height { get; set; } = 1;
		public float  Depth  { get; set; } = 1;
		public Color  Color { get; set; } =	Color.Red;

		public string Target { get; set; } = "";

		public TriggerFilter TriggerFilter { get; set; } = TriggerFilter.All;

		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new TriggerArea( id, clsid, world, this );
		}


		public override void Draw( DebugRender dr, Matrix transform, Color color, bool selected )
		{
			var w = Width/2;
			var h = Height/2;
			var d = Depth/2;

			var c = selected ? color : Color;

			dr.DrawBox( new BoundingBox( new Vector3(-w, -h, -d), new Vector3(w, h, d) ), transform, c, 2 );
			dr.DrawPoint( transform.TranslationVector, (w+h+d)/3/2, c, 2 );
		}
	}
}
