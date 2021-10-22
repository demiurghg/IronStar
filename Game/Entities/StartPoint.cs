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
using System.ComponentModel;
using IronStar.ECS;

namespace IronStar {

	public enum StartPointType {
		SinglePlayer,
		IntermissionCamera,
	}


	public class StartPointFactory : EntityFactoryContent {

		public StartPointType StartPointType { get; set; }

		public override ECS.Entity SpawnECS( IGameState gs )
		{
			var e = gs.Spawn( new Gameplay.PlayerStartComponent(), new ECS.Transform() );
			return e;
		}


		public override void Draw( DebugRender dr, Matrix transform, Color color, bool selected )
		{
			var p0 = transform.TranslationVector;
			var p1 = transform.TranslationVector + Vector3.Up*6;
			var pf = transform.TranslationVector + transform.Forward;


			dr.DrawRing( p0, 1.00f, color, 16 );
			dr.DrawRing( p1, 1.00f, color, 16 );
			dr.DrawLine( p0, pf, color, color, 5, 1 );
		}
	}
}
