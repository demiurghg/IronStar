using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Engine.Audio;
using IronStar.Entities;

namespace IronStar.Views {
	public class GameCamera {

		public readonly Game Game;
		public readonly GameWorld World;
		readonly ShooterClient client;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public GameCamera ( GameWorld world, ShooterClient client ) 
		{
			this.World	=	world;
			this.Game	=	world.Game;
			this.client	=	client;
			currentFov	=	90;//(world.GameClient as ShooterClient).Fov;
		}


		float currentFov;
		Vector3 filteredPos = Vector3.Zero;

		/// <summary>
		/// 
		/// </summary>
		public float Sensitivity {
			get {
				return 5;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( float elapsedTime, float lerpFactor )
		{
			var rw	= Game.RenderSystem.RenderWorld;
			var sw	= Game.SoundSystem.SoundWorld;
			var vp	= Game.RenderSystem.DisplayBounds;

			var aspect	=	(vp.Width) / (float)vp.Height;

			var uc		=	client.UserCommand;
			var m		= 	Matrix.RotationYawPitchRoll( uc.Yaw, uc.Pitch, uc.Roll );

			var player	=	World.GetPlayerEntity( client.UserGuid );

			if (player==null) {
				//Log.Warning("No entity associated with player");
				return;
			}

			var ppos	=	player.LerpPosition(lerpFactor);

			var pos		=	player.GetPOV( lerpFactor );

			var fwd	=	pos + m.Forward;
			var up	=	m.Up;


			var targetFov	=	MathUtil.Clamp( uc.Action.HasFlag( UserAction.Zoom ) ? 30 : 110, 10, 140 );

			currentFov		=	MathUtil.Drift( currentFov, targetFov, 360*elapsedTime, 360*elapsedTime );

			rw.Camera.SetupCameraFov( pos, fwd, up, MathUtil.Rad(currentFov), 0.125f, 1024f, 2, 0.05f, aspect );

			sw.Listener	=	new AudioListener();
			sw.Listener.Position	=	pos;
			sw.Listener.Forward		=	m.Forward;
			sw.Listener.Up			=	m.Up;
			sw.Listener.Velocity	=	Vector3.Zero;
		}
	}
}
