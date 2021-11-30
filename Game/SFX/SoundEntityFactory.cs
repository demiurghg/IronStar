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
using Fusion.Engine.Audio;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using BEPUutilities.Threading;
using Fusion.Core.Extensions;
using IronStar.ECSPhysics;

namespace IronStar.SFX 
{
	public class SoundEntityFactory : EntityFactory
	{
		public string SoundPath { get; set; } = "";

		public Vector3 Velocity;
		public Entity Target;
		public bool Looped { get; set; } = false;

		public SoundEntityFactory()
		{
		}

		public SoundEntityFactory( string soundPath, Vector3 origin, Vector3 velocity, Entity target = null )
		{
			this.SoundPath	=	soundPath;
			this.Position	=	origin;
			this.Velocity	=	velocity;
			this.Rotation	=	Quaternion.Identity;
			this.Target		=	target;
		}

		public override void Construct( Entity entity, IGameState gs )
		{
			entity.AddComponent( new Transform( Position, Rotation, Velocity ) );
			entity.AddComponent( new SoundComponent( SoundPath, Looped ) );

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
