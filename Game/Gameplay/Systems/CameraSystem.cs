using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Animation;
using Fusion.Engine.Graphics.Scenes;
using System.Runtime.CompilerServices;
using IronStar.Gameplay.Components;
using Fusion.Core.Extensions;

namespace IronStar.Gameplay
{
	public class CameraSystem : ISystem
	{
		const int	MAX_SHAKES	=	15;

		Random	rand = new Random();

		Scene cameraScene;
		AnimationComposer	composer;
		TakeSequencer		mainTrack;
		TakeSequencer		shake0;
		TakeSequencer		shake1;
		TakeSequencer		shake2;
		TakeSequencer		shake3;
		Matrix[]			animData;
		bool isCrouching	=	false;
		bool hasTraction	=	true;

		public CameraSystem()
		{
			cameraScene	=	CreateCameraScene( 6, 4, 0 );
			animData	=	new Matrix[2];

			composer	=	new AnimationComposer( null, null, cameraScene );
			mainTrack	=	new TakeSequencer( cameraScene, null, AnimationBlendMode.Override );
			shake0		=	new TakeSequencer( cameraScene, null, AnimationBlendMode.Additive );
			shake1		=	new TakeSequencer( cameraScene, null, AnimationBlendMode.Additive );
			shake2		=	new TakeSequencer( cameraScene, null, AnimationBlendMode.Additive );
			shake3		=	new TakeSequencer( cameraScene, null, AnimationBlendMode.Additive );

			composer.Tracks.Add( mainTrack );
			composer.Tracks.Add( shake0 );
			composer.Tracks.Add( shake1 );
			composer.Tracks.Add( shake2 );
			composer.Tracks.Add( shake3 );

			mainTrack.Sequence("stand", SequenceMode.Immediate|SequenceMode.Hold);
		}

		
		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}


		public void Add( GameState gs, Entity e ) {}
		public void Remove( GameState gs, Entity e ) {}


		public void Update( GameState gs, GameTime gameTime )
		{
			var	players	=	gs.QueryEntities<PlayerComponent,Transform,UserCommandComponent,CharacterController>();

			if (players.Count()>1)
			{
				Log.Warning("CameraSystem.Update -- multiple players detected");
			}

			foreach ( var player in players)
			{
				SetupPlayerCamera(gameTime, gs, player);
			}
		}


		void SetupPlayerCamera( GameTime gameTime, GameState gs, Entity e )
		{
			var t	=	e.GetComponent<Transform>();
			var v	=	e.GetComponent<Velocity>();
			var uc	=	e.GetComponent<UserCommandComponent>();
			var ch	=	e.GetComponent<CharacterController>();
			var step=	e.GetComponent<StepComponent>();

			var	rs	=	gs.GetService<RenderSystem>();
			var rw	=	rs.RenderWorld;
			var sw	=	gs.Game.SoundSystem;
			var vp	=	gs.Game.RenderSystem.DisplayBounds;

			var aspect	=	(vp.Width) / (float)vp.Height;

			UpdateAnimationState(step);

			composer.Update( gameTime, animData );
			//cameraScene.ComputeAbsoluteTransforms( animData, animData );
			var animatedCameraMatrix = animData[1];

			//	update stuff :
			var translate	=	Matrix.Translation( t.Position );
			var rotateYaw	=	Matrix.RotationYawPitchRoll( uc.Yaw, 0, 0 );
			var rotatePR	=	Matrix.RotationYawPitchRoll( 0, uc.Pitch, uc.Roll );

			var camMatrix	=	rotatePR * animatedCameraMatrix * rotateYaw * translate;

			var cameraPos	=	camMatrix.TranslationVector;

			var cameraFwd	=	camMatrix.Forward;
			var cameraUp	=	camMatrix.Up;

			//	update stuff :
			/*var camMatrix	=	uc.RotationMatrix;
			var cameraPos	=	t.Position + ch.PovOffset;
			var cameraFwd	=	camMatrix.Forward;
			var cameraUp	=	camMatrix.Up;*/
			var velocity	=	v==null ? Vector3.Zero : v.Linear;

			rw.Camera		.LookAt( cameraPos, cameraPos + cameraFwd, cameraUp );
			rw.WeaponCamera	.LookAt( cameraPos, cameraPos + cameraFwd, cameraUp );

			rw.Camera		.SetPerspectiveFov( MathUtil.Rad(90),	0.125f/2.0f, 12288, aspect );
			rw.WeaponCamera	.SetPerspectiveFov( MathUtil.Rad(75),	0.125f/2.0f, 6144, aspect );

			sw.SetListener( cameraPos, cameraFwd, cameraUp, velocity );
		}


		void UpdateAnimationState( StepComponent steps )
		{
			if (steps.Crouched)	mainTrack.Sequence( "crouch", SequenceMode.Hold|SequenceMode.DontPlayTwice );
			if (steps.Standed)	mainTrack.Sequence( "stand" , SequenceMode.Hold|SequenceMode.DontPlayTwice );
			
			if (steps.Landed) PlayShake("landing", 1.0f);
			if (steps.Jumped) PlayShake("jump", 0.5f);

			if (steps.RecoilLight) PlayShake(null, rand.NextFloat(0.2f,0.4f) );
			if (steps.RecoilHeavy) PlayShake(null, rand.NextFloat(0.8f,1.2f) );
		}


		void PlayShake( string takeName, float weight )
		{
			var shakeTrack = composer.GetAdditiveIdleSequencer();
			if (shakeTrack!=null)
			{
				if (takeName==null) takeName = "shake" + rand.Next(MAX_SHAKES);

				shakeTrack.Sequence(takeName, SequenceMode.Immediate);
				shakeTrack.Weight = weight;
			}
		}


		Scene CreateCameraScene( float standHeight, float crouchHeight, float duckAngle )
		{
			var scene	=	new Scene(TimeMode.Frames60);
			var root	=	new Node("root");
			var camera	=	new Node("camera", 0);

			var standEyeHeight	=	CharacterController.CalcPovHeight( standHeight, crouchHeight, false );
			var crouchEyeHeight	=	CharacterController.CalcPovHeight( standHeight, crouchHeight, true );
			var standTransform	=	Matrix.Translation( 0, standEyeHeight, 0 );
			var crouchTransform	=	Matrix.Translation( 0, crouchEyeHeight, 0 );
			var duckTransform	=	Matrix.RotationX( MathUtil.DegreesToRadians(1) );
			var landTransform	=	Matrix.RotationX( -MathUtil.DegreesToRadians(5) );
			var identity		=	Matrix.Identity;
			camera.Transform	=	standTransform;
			
			scene.Nodes.Add( root );
			scene.Nodes.Add( camera );

			//	generate takes :
			var takeStand	=	new AnimationTake("stand"	, 2, 0, 15);
			var takeCrouch	=	new AnimationTake("crouch"	, 2, 0, 15);
			var takeLanding	=	new AnimationTake("landing"	, 2, 0, 30);
			var takeJump	=	new AnimationTake("jump"	, 2, 0, 20);
			
			takeStand.RecordTake( 1, 
				(i,t) =>  AnimationUtils.Lerp( crouchTransform, standTransform, AnimationUtils.SlowFollowThrough(t) )
						* AnimationUtils.Lerp( identity, duckTransform, AnimationUtils.KickCurve(t) ) 
			);

			takeCrouch	.RecordTake( 1, 
				(i,t) =>  AnimationUtils.Lerp( standTransform, crouchTransform, AnimationUtils.SlowFollowThrough(t) ) 
						* AnimationUtils.Lerp( identity, duckTransform, -AnimationUtils.KickCurve(t) ) 
			);

			takeLanding	.RecordTake( 1, 
				(i,t) =>  AnimationUtils.Lerp( identity, landTransform, AnimationUtils.KickCurve(t) ) 
			);

			takeJump	.RecordTake( 1, 
				(i,t) =>  AnimationUtils.Lerp( identity, landTransform, -1*AnimationUtils.KickCurve(t) ) 
			);

			scene.Takes.Add( takeStand		);
			scene.Takes.Add( takeCrouch		);
			scene.Takes.Add( takeLanding	);
			scene.Takes.Add( takeJump		);

			Random rand = new Random(152445);

			for ( int i=0; i<MAX_SHAKES; i++) 
			{
				var amp		=	MathUtil.DegreesToRadians(1);
				var shake	=	Matrix.RotationYawPitchRoll( rand.NextFloat(-amp,amp)*0.5f, rand.NextFloat(amp,2*amp), rand.NextFloat(-amp,amp)*0.25f )
							*	Matrix.Translation(0,0,0.5f);
				var take	=	new AnimationTake("shake" + i.ToString(), 2, 0, 10);
				take.RecordTake( 1, (f,t) => AnimationUtils.Lerp( identity, shake, AnimationUtils.KickCurve(t) ) );
				scene.Takes.Add(take);
			}

			return scene;
		}
	}
}
