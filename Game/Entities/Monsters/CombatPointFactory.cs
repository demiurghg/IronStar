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
using BEPUphysics;
using Fusion.Core.IniParser.Model;
using IronStar.Physics;
using IronStar.Items;

namespace IronStar.Entities.Monsters {
	public class CombatPointFactory : EntityFactory {

		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new CombatPoint(id, clsid, world, this);
		}


		public override void Draw( DebugRender dr, Matrix transform, Color color, bool selected )
		{
			dr.DrawWaypoint( transform.TranslationVector, 1, color, 2 );
			dr.DrawRing( transform.TranslationVector, 0.35f, color, 8 );
			dr.DrawBox( transform.TranslationVector + Vector3.Up * 0.5f, 0.25f, color );
		}
	}
}
