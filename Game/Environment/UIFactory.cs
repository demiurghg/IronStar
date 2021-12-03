using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using IronStar.SFX2;
using IronStar.ECSPhysics;
using Fusion.Core.Mathematics;
using IronStar.Animation;
using IronStar.Gameplay.Components;

namespace IronStar.Environment
{
	class UIFactory : EntityFactory
	{
		public override void Construct( Entity e, IGameState gs )
		{
			e.AddComponent( new Transform( Position, Rotation, Scaling ) );
			e.AddComponent( new UIComponent() );
		}
	}


}
