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
using Fusion.Engine.Audio;
using IronStar.ECS;

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

			fx.AddComponent( new Transform(origin, rotation) );
			fx.AddComponent( new Velocity(velocity) );
			fx.AddComponent( new FXComponent( fxName, false ) );

			return fx;
		}


		public static ECS.Entity SpawnFX( GameState gs, string fxName, uint parentID, Vector3 origin, Vector3 velocity, Vector3 forward )
		{
			var m = MathUtil.ComputeAimedBasis( forward );
			
			return SpawnFX( gs, fxName, parentID, origin, velocity, Quaternion.RotationMatrix(m) );
		}


		public static ECS.Entity SpawnFX( GameState gs, string fxName, uint parentID, Vector3 origin, Vector3 forward )
		{
			return SpawnFX( gs, fxName, parentID, origin, Vector3.Zero, forward );
		}


		public static ECS.Entity SpawnFX( GameState gs, string fxName, uint parentID, Vector3 origin )
		{
			return SpawnFX( gs, fxName, parentID, origin, Vector3.Zero, Quaternion.Identity );
		}

	}
}
