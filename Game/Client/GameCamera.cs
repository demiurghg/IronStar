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
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Engine.Audio;
using IronStar.Entities;
using IronStar.Entities.Players;
using IronStar.SFX;
using Fusion.Core.Extensions;

namespace IronStar.Views {
	public class GameCamera {

		public readonly Game Game;
		public readonly GameWorld World;
		public readonly Guid ClientGuid;
		public readonly ShooterClient client;

		readonly Scene camera;
		readonly AnimationComposer composer;
		readonly AnimationTrack mainTrack;
		readonly AnimationTrack shake0;
		readonly AnimationTrack shake1;
		readonly AnimationTrack shake2;
		readonly AnimationTrack shake3;
		readonly AnimationTrack shake4;
		readonly AnimationTrack shake5;
		readonly AnimationTrack[] shakes;

		readonly Matrix[] transforms;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public GameCamera ( GameWorld world, ShooterClient client )
		{
			this.ClientGuid	=	client.UserGuid;
			this.client		=	client;
			this.World		=	world;
			this.Game		=	world.Game;
			currentFov		=	90;//(world.GameClient as ShooterClient).Fov;


			camera		=	world.Content.Load<Scene>(@"scenes\camera");

			composer	=	new AnimationComposer( "Camera", null, camera, world );

			transforms	=	new Matrix[ camera.Nodes.Count ];

			mainTrack	=	new AnimationTrack( camera, null, AnimationBlendMode.Override );
			shake0		=	new AnimationTrack( camera, null, AnimationBlendMode.Additive );
			shake1		=	new AnimationTrack( camera, null, AnimationBlendMode.Additive );
			shake2		=	new AnimationTrack( camera, null, AnimationBlendMode.Additive );
			shake3		=	new AnimationTrack( camera, null, AnimationBlendMode.Additive );
			shake4		=	new AnimationTrack( camera, null, AnimationBlendMode.Additive );
			shake5		=	new AnimationTrack( camera, null, AnimationBlendMode.Additive );

			shakes		=	new[] { shake0, shake1, shake2, shake3, shake4, shake5 };

			composer.Tracks.Add( mainTrack );
			composer.Tracks.Add( shake0 );
			composer.Tracks.Add( shake1 );
			composer.Tracks.Add( shake2 );
			composer.Tracks.Add( shake3 );
			composer.Tracks.Add( shake4 );
			composer.Tracks.Add( shake5 );

			mainTrack.Sequence( "stand", true, false, true );
		}


		float currentPov = float.NaN;
		float currentFov = float.NaN;

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
		public void Update ( GameTime gameTime, float lerpFactor )
		{
			var elapsedTime =  gameTime.ElapsedSec;

			var rw	= Game.RenderSystem.RenderWorld;
			var sw	= Game.SoundSystem;
			var vp	= Game.RenderSystem.DisplayBounds;

			var aspect	=	(vp.Width) / (float)vp.Height;

			var uc		=	client.UserCommand;
			var m		= 	Matrix.RotationYawPitchRoll( uc.Yaw, uc.Pitch, uc.Roll );

			var player	=	World.GetPlayerEntity( ClientGuid ) as Player;

			if (player==null) {
				//Log.Warning("No entity associated with player");
				return;
			}

			var targetFov	=	MathUtil.Clamp( uc.Action.HasFlag( UserAction.Zoom ) ? 30 : 110, 10, 140 );
			currentFov		=	MathUtil.Drift( currentFov, targetFov, 360*elapsedTime, 360*elapsedTime );

			#if false
			var targetPov	=	player.EntityState.HasFlag(EntityState.Crouching) ? GameConfig.PovHeightCrouch : GameConfig.PovHeightStand;
			currentPov		=	MathUtil.Drift( currentPov, targetPov, GameConfig.PovHeightVelocity * elapsedTime );

			var playerPos	=	player.Position;
			var cameraPos	=	player.Position + Vector3.Up * currentPov;

			var cameraFwd	=	cameraPos + m.Forward;
			var cameraUp	=	m.Up;
			#else

			var translate	=	Matrix.Translation( player.LerpPosition(0) );
			var rotateYaw	=	Matrix.RotationYawPitchRoll( uc.Yaw, 0, 0 );
			var rotatePR	=	Matrix.RotationYawPitchRoll( 0, uc.Pitch, uc.Roll );

			var camMatrix	=	rotatePR * UpdateAnimation( player, gameTime ) * rotateYaw * translate;

			var cameraPos	=	camMatrix.TranslationVector;

			var cameraFwd	=	cameraPos + camMatrix.Forward;
			var cameraUp	=	camMatrix.Up;
			#endif

			rw.Camera		.SetupCameraFov( cameraPos, cameraFwd, cameraUp, MathUtil.Rad(currentFov),  0.125f/2.0f, 1024f, 2, 0.05f, aspect );
			rw.WeaponCamera	.SetupCameraFov( cameraPos, cameraFwd, cameraUp, MathUtil.Rad(75),			0.125f/2.0f, 1024f, 2, 0.05f, aspect );

			//
			//	Set player listener :
			//
			sw.SetListener( cameraPos, m.Forward, m.Up, player.LinearVelocity );
		}



		bool oldStanding = true;
		bool oldTraction = true;
		float oldVelocity = 0;

		WeaponState oldWeaponState;


		void RunShakeAnimation ( string name, float weight )
		{
			var track	=	shakes.FirstOrDefault( tr => !tr.Busy );

			if (track!=null) {
				track.Weight = weight;
				track.Sequence( name, true, false );
			}
		}
		

		Random rand = new Random();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="player"></param>
		/// <param name="gameTime"></param>
		Matrix UpdateAnimation ( Player player, GameTime gameTime )
		{
			var newStanding = !player.EntityState.HasFlag( EntityState.Crouching );
			var newTraction = player.EntityState.HasFlag( EntityState.HasTraction );

			var newWpnState = player.WeaponState;

			if ( newStanding!=oldStanding ) {
				if (newStanding) {
					mainTrack.Sequence("stand", true, false, true );
				} else {
					mainTrack.Sequence("crouch", true, false, true );
				}
			}


			if ( newTraction!=oldTraction ) {
				if (newTraction) {				
					RunShakeAnimation("landing", MathUtil.Clamp( oldVelocity/10.0f, 0, 1 ) );
				} else {
					//mainTrack.Sequence("crouch", true, false, true );
				}
			}

			if ( oldWeaponState!=newWpnState ) {
				if (newWpnState==WeaponState.Cooldown || newWpnState==WeaponState.Cooldown2) {
					
					var shake = string.Format("shake{0}", rand.Next(6));
					RunShakeAnimation(shake, rand.NextFloat(0.05f, 0.15f) );
				}
				oldWeaponState = newWpnState;
			}

			oldStanding = newStanding;
			oldTraction = newTraction;
			oldVelocity = Math.Abs( player.LinearVelocity.Y );

			composer.Update( gameTime, transforms );
			return Scene.FixGlobalCameraMatrix( transforms[1] );
		}
	}
}
