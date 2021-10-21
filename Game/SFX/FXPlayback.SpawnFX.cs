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
using Fusion.Core.Extensions;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using IronStar.ECS;
using IronStar.Gameplay.Systems;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;

namespace IronStar.SFX 
{
	public partial class FXPlayback
	{
		class FXEntityFactory : IFactory
		{
			readonly string fxName;
			readonly Vector3 origin;
			readonly Vector3 velocity;
			readonly Quaternion rotation;
			readonly Entity target;

			public FXEntityFactory( string fxName, Vector3 origin, Vector3 velocity, Quaternion rotation, Entity target = null )
			{
				this.fxName		=	fxName;
				this.origin		=	origin;
				this.velocity	=	velocity;
				this.rotation	=	rotation;
				this.target		=	target;
			}

			public void Construct( Entity entity, IGameState gs )
			{
				entity.AddComponent( new Transform( origin, rotation, velocity ) );
				entity.AddComponent( new FXComponent( fxName, false ) );

				if (target!=null)
				{
					entity.AddComponent( new AttachmentComponent( target.ID ) );
				}
			}
		}


		//	#TODO #REFACTOR -- fix mess with parameters order
		public static ECS.Entity SpawnFX( IGameState gs, string fxName, Vector3 origin, Vector3 velocity, Quaternion rotation, Entity target = null )
		{
			if (string.IsNullOrWhiteSpace(fxName))
			{
				Log.Warning("SpawnFX: FX name is null or whitespace");
				return null;
			}

			var factory = new FXEntityFactory( fxName, origin, velocity, rotation, target );

			return gs.Spawn( factory );
		}


		public static ECS.Entity SpawnFX( IGameState gs, string fxName, ECS.Entity originEntity )
		{
			var transform	=	originEntity.GetComponent<Transform>();

			if (transform!=null)
			{
				return SpawnFX( gs, fxName, transform.Position, Vector3.Zero, transform.Rotation );
			}
			else
			{
				Log.Warning("SpawnFX: FX has no transform component");
				return null;
			}
		}


		public static ECS.Entity SpawnFX( IGameState gs, string fxName, Vector3 origin, Vector3 velocity, Vector3 forward, Entity target = null )
		{
			var r	=	Matrix.RotationAxis( forward, rand.NextFloat( 0,MathUtil.TwoPi ) );
			var m	=	MathUtil.ComputeAimedBasis( forward );
			
			return SpawnFX( gs, fxName, origin, velocity, Quaternion.RotationMatrix(m*r), target );
		}


		public static ECS.Entity SpawnFX( IGameState gs, string fxName, Vector3 origin, Vector3 forward )
		{
			return SpawnFX( gs, fxName, origin, Vector3.Zero, forward );
		}


		public static ECS.Entity SpawnFX( IGameState gs, string fxName, Vector3 origin )
		{
			return SpawnFX( gs, fxName, origin, Vector3.Zero, Quaternion.Identity );
		}


		public static ECS.Entity AttachFX( IGameState gs, Entity target, string fxName, Vector3 origin, Vector3 forward )
		{
			return SpawnFX( gs, fxName, origin, Vector3.Zero, forward, target );
		}
	}
}
