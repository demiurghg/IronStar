using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;

namespace IronStar.SFX
{
	public class FXEntityFactory : EntityFactory
	{
		public string FXName { get; set; } = "";

		public Vector3 Velocity;
		public Entity Target;
		public bool Looped { get; set; } = false;

		public FXEntityFactory()
		{
		}

		public FXEntityFactory( string fxName, Vector3 origin, Vector3 velocity, Quaternion rotation, Entity target = null )
		{
			this.FXName		=	fxName;
			this.Position	=	origin;
			this.Velocity	=	velocity;
			this.Rotation	=	rotation;
			this.Target		=	target;
		}

		public override void Construct( Entity entity, IGameState gs )
		{
			entity.AddComponent( new Transform( Position, Rotation, Velocity ) );
			entity.AddComponent( new FXComponent( FXName, Looped ) );

			if (Target!=null)
			{
				if (Target.GetComponent<StaticCollisionComponent>()==null)
				{
					entity.AddComponent( new AttachmentComponent( Target.ID ) );
				}
			}
		}
	}
}
