using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using System.ComponentModel;

namespace IronStar.Entities {

	public enum StartPointType {
		SinglePlayer,
		IntermissionCamera,
	}


	public class StartPoint : Entity {

		public readonly StartPointType StartPointType;

		public StartPoint( uint id, short clsid, GameWorld world, StartPointFactory factory ) : base( id, clsid, world, factory )
		{
			StartPointType = factory.StartPointType;
		}
	}



	public class StartPointFactory : EntityFactory {

		public StartPointType StartPointType { get; set; }

		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new StartPoint( id, clsid, world, this );
		}


		public override void Draw( DebugRender dr, Matrix transform, Color color, bool selected )
		{
			var p0 = transform.TranslationVector;
			var p1 = transform.TranslationVector + Vector3.Up*2;
			var pf = transform.TranslationVector + transform.Forward;


			dr.DrawRing( p0, 0.50f, color, 16 );
			dr.DrawRing( p1, 0.50f, color, 16 );
			dr.DrawLine( p0, pf, color, color, 5, 1 );
		}
	}
}
