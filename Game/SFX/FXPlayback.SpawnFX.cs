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

namespace IronStar.SFX {
	public partial class FXPlayback
	{
		public static ECS.Entity SpawnFX( GameState gs, string fxName, uint parentID, Vector3 origin, Vector3 velocity, Quaternion rotation )
		{
			if (string.IsNullOrWhiteSpace(fxName))
			{
				Log.Warning("SpawnFX: FX name is null or whitespace");
				return null;
			}

			var fx = gs.Spawn();

			fx.AddComponent( new Transform(origin, rotation, velocity) );
			fx.AddComponent( new FXComponent( fxName, false ) );

			return fx;
		}


		public static ECS.Entity SpawnFX( GameState gs, string fxName, ECS.Entity originEntity )
		{
			var transform	=	originEntity.GetComponent<Transform>();

			if (transform!=null)
			{
				return SpawnFX( gs, fxName, 0, transform.Position, Vector3.Zero, transform.Rotation );
			}
			else
			{
				Log.Warning("SpawnFX: FX has no transform component");
				return null;
			}
		}


		public static ECS.Entity SpawnFX( GameState gs, string fxName, uint parentID, Vector3 origin, Vector3 velocity, Vector3 forward )
		{
			var r	=	Matrix.RotationAxis( forward, rand.NextFloat( 0,MathUtil.TwoPi ) );
			var m	=	MathUtil.ComputeAimedBasis( forward );
			
			return SpawnFX( gs, fxName, parentID, origin, velocity, Quaternion.RotationMatrix(m*r) );
		}


		public static ECS.Entity SpawnFX( GameState gs, string fxName, uint parentID, Vector3 origin, Vector3 forward )
		{
			return SpawnFX( gs, fxName, parentID, origin, Vector3.Zero, forward );
		}


		public static ECS.Entity SpawnFX( GameState gs, string fxName, uint parentID, Vector3 origin )
		{
			return SpawnFX( gs, fxName, parentID, origin, Vector3.Zero, Quaternion.Identity );
		}


		public static ECS.Entity AttachFX( GameState gs, Entity target, string fxName, uint parentID, Vector3 origin, Vector3 forward )
		{
			var e = SpawnFX( gs, fxName, parentID, origin, Vector3.Zero, forward );

			if (target!=null)
			{
				if (target.GetComponent<StaticCollisionComponent>()==null)
				{
					AttachmentSystem.Attach( e, target );
				}
			}

			return e;
		}
	}
}
