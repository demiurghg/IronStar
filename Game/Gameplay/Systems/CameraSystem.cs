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
using Fusion.Core.Configuration;
using System.IO;

namespace IronStar.Gameplay
{
	[ConfigClass]
	public class CameraSystem : ISystem, IGameComponent
	{
		[Config] public static bool ThirdPersonEnable { get; set; }
		[Config] public static float ThirdPersonRange { get; set; } = 8.0f;
		[Config] public static float ThirdPersonAngle { get; set; } = 0.0f;

		const string SOUND_LANDING	=	"player/landing"	;
		const string SOUND_STEP		=	"player/step"		;
		const string SOUND_JUMP		=	"player/jump"		;

		const int	MAX_SHAKES	=	15;

		readonly Random	rand = new Random();
		readonly SFX.FXPlayback fxPlayback;
		readonly PlayerInputSystem playerInput;

		Scene cameraScene;
		AnimationComposer	composer;
		Sequencer		mainTrack;
		Sequencer		shake0;
		Sequencer		shake1;
		Sequencer		shake2;
		Sequencer		shake3;
		Matrix[]		animData;
		bool			dead = false;
		bool			crouched = false;
		bool			traction = false;

		StepComponent	prevStep;

		public bool Enabled { get; set; } = true;

		public Ray ViewRay;

		public CameraSystem(SFX.FXPlayback fxPlayback, PlayerInputSystem playerInput)
		{
			this.fxPlayback		=	fxPlayback;
			this.playerInput	=	playerInput;
			cameraScene			=	CreateCameraScene( 6, 4, 0 );
			animData			=	new Matrix[2];
			ViewRay				=	new Ray( Vector3.Zero, Vector3.Zero );

			composer	=	new AnimationComposer( fxPlayback, cameraScene );
			mainTrack	=	new Sequencer( cameraScene, null, AnimationBlendMode.Override );
			shake0		=	new Sequencer( cameraScene, null, AnimationBlendMode.Additive );
			shake1		=	new Sequencer( cameraScene, null, AnimationBlendMode.Additive );
			shake2		=	new Sequencer( cameraScene, null, AnimationBlendMode.Additive );
			shake3		=	new Sequencer( cameraScene, null, AnimationBlendMode.Additive );

			composer.Tracks.Add( mainTrack );
			composer.Tracks.Add( shake0 );
			composer.Tracks.Add( shake1 );
			composer.Tracks.Add( shake2 );
			composer.Tracks.Add( shake3 );

			mainTrack.Sequence("stand", SequenceMode.Immediate|SequenceMode.Hold);
		}


		public void Initialize()
		{
		}

		
		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}


		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}

		Aspect playerCameraAspect = new Aspect().Include<PlayerComponent,Transform>()
												.Include<UserCommandComponent>();


		/*-----------------------------------------------------------------------------------------
		 *	Update & Draw :
		-----------------------------------------------------------------------------------------*/

		public void Update( IGameState gs, GameTime gameTime )
		{
			if (Enabled)
			{
				var	players		=	gs.QueryEntities(playerCameraAspect);

				foreach ( var player in players)
				{
					UpdatePlayerCamera(gameTime, gs, player);
					DrawPlayerCamera(gameTime, gs, player);
				}
			}
		}


		/*-----------------------------------------------------------------------------------------
		 *	Animation stuff :
		-----------------------------------------------------------------------------------------*/

		void UpdatePlayerCamera( GameTime gameTime, IGameState gs, Entity e )
		{
			var t		=	e.GetComponent<Transform>();
			var v		=	t.LinearVelocity;
			var uc		=	e.GetComponent<UserCommandComponent>();
			var health	=	e.GetComponent<HealthComponent>();
			var camera	=	e.GetComponent<CameraComponent>();

			var step		=	e.GetComponent<StepComponent>();
			var stepEvents	=	StepComponent.DetectEvents( step, prevStep );
			prevStep		=	(StepComponent)step.Clone();

			/*
			var playerInput	=	gs.GetService<PlayerInputSystem>();
			var lastCommand	=	playerInput.LastCommand;
			*/

			//	animate :
			UpdateAnimationState(stepEvents, health);
			composer.Update( gameTime, Matrix.Identity, false, animData );

			//	store animation matrix :
			camera.AnimTransform = animData[1];
		}


		void DrawPlayerCamera( GameTime gameTime, IGameState gs, Entity e )
		{
			var t		=	e.GetComponent<Transform>();
			var v		=	t.LinearVelocity;
			var uc		=	e.GetComponent<UserCommandComponent>();
			var step	=	e.GetComponent<StepComponent>();
			var health	=	e.GetComponent<HealthComponent>();
			var camera	=	e.GetComponent<CameraComponent>();
			var bob		=	e.GetComponent<BobbingComponent>();

			var	rs	=	gs.Game.GetService<RenderSystem>();
			var rw	=	rs.RenderWorld;
			var sw	=	gs.Game.SoundSystem;
			var vp	=	gs.Game.RenderSystem.DisplayBounds;

			var lastCommand	=	playerInput.LastCommand;

			var animMatrix	=	camera.AnimTransform;

			//	update stuff :
			var translate		=	Matrix.Translation( t.Position + Vector3.Up * bob.BobUp );
			var rotateYaw		=	Matrix.RotationYawPitchRoll( lastCommand.Yaw + bob.BobYaw, 0, 0 );
			var rotatePR		=	Matrix.RotationYawPitchRoll( 0, lastCommand.Pitch + bob.BobPitch, lastCommand.Roll + bob.BobRoll );

			var tpvTranslate	=	ThirdPersonEnable ? Matrix.Translation( Vector3.BackwardRH * ThirdPersonRange ) : Matrix.Identity;
			var tpvRotate		=	ThirdPersonEnable ? Matrix.RotationY( MathUtil.DegreesToRadians( ThirdPersonAngle ) ) : Matrix.Identity;
			var camMatrix		=	tpvTranslate * rotatePR * animMatrix * tpvRotate * rotateYaw * translate;

			var cameraPos	=	camMatrix.TranslationVector;

			var cameraFwd	=	camMatrix.Forward;
			var cameraUp	=	camMatrix.Up;

			//	update camera and listener :
			var aspect		=	(vp.Width) / (float)vp.Height;

			rw.Camera		.LookAt( cameraPos, cameraPos + cameraFwd, cameraUp );
			rw.WeaponCamera	.LookAt( cameraPos, cameraPos + cameraFwd, cameraUp );

			rw.Camera		.SetPerspectiveFov( MathUtil.Rad(90),	0.125f/2.0f, 12288, aspect );
			rw.WeaponCamera	.SetPerspectiveFov( MathUtil.Rad(75),	0.125f/2.0f, 6144, aspect );

			sw.SetListener( cameraPos, cameraFwd, cameraUp, t.LinearVelocity );

			ViewRay	=	new Ray( cameraPos, cameraFwd );
		}


		/*-----------------------------------------------------------------------------------------
		 *	Animation stuff :
		-----------------------------------------------------------------------------------------*/

		void UpdateAnimationState( StepEvent steps, HealthComponent health )
		{
			if (health.Health<=0 && !dead)
			{
				dead = true;
				mainTrack.Sequence("death", SequenceMode.Hold );
			}

			var stepsCrouched		=	steps.HasFlag( StepEvent.Crouched	 );
			var stepsStanded		=	steps.HasFlag( StepEvent.Standed	 );
			var stepsRecoilLight	=	steps.HasFlag( StepEvent.RecoilLight );
			var stepsRecoilHeavy	=	steps.HasFlag( StepEvent.RecoilHeavy );
			var stepsLeftStep		=	steps.HasFlag( StepEvent.LeftStep	 );
			var stepsRightStep		=	steps.HasFlag( StepEvent.RightStep	 );
			var stepsLanded			=	steps.HasFlag( StepEvent.Landed		 );

			if (!dead)
			{
				if (stepsCrouched)		mainTrack.Sequence( "crouch", SequenceMode.Hold|SequenceMode.DontPlayTwice );
				if (stepsStanded)		mainTrack.Sequence( "stand" , SequenceMode.Hold|SequenceMode.DontPlayTwice );
			
				if (stepsRecoilLight)	PlayShake((string)null, rand.NextFloat(0.2f,0.4f) );
				if (stepsRecoilHeavy)	PlayShake((string)null, rand.NextFloat(0.8f,1.2f) );

				if (stepsLeftStep)		composer.SequenceSound( SOUND_STEP );
				if (stepsRightStep)		composer.SequenceSound( SOUND_STEP );
				if (stepsLanded)		composer.SequenceSound( SOUND_LANDING );
				//if (stepsJumped) composer.SequenceSound( SOUND_STEP );
			}
		}


		AnimationTake CreatePainAnimation( float amount )
		{
			var length	=	MathUtil.Lerp(15,30, amount);
			var take	=	new AnimationTake("pain", 2, length);

			var maxPitch	=	MathUtil.DegreesToRadians( amount * 1  );
			var maxYaw		=	MathUtil.DegreesToRadians( amount * 5  );
			var maxRoll		=	MathUtil.DegreesToRadians( amount * 15 );
			var identity	=	Matrix.Identity;
			var shake		=	Matrix.RotationYawPitchRoll( rand.NextFloat(-maxYaw,maxYaw), rand.NextFloat(-maxPitch,maxPitch), rand.NextFloat(-maxRoll,maxRoll) );

			take.RecordTake( 1, (f,t) => AnimationUtils.Lerp( identity, shake, AnimationUtils.KickCurve(t) ) );

			return take;
		}


		void PlayShake( AnimationTake take, float weight )
		{
			var shakeTrack = composer.GetAdditiveIdleSequencer();
			if (shakeTrack!=null)
			{
				shakeTrack.Sequence(take, SequenceMode.Immediate);
				shakeTrack.Weight = weight;
			}
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
			var deadEyeHeight	=	0.33f;
			var standTransform	=	Matrix.Translation( 0, standEyeHeight, 0 );
			var crouchTransform	=	Matrix.Translation( 0, crouchEyeHeight, 0 );
			var duckTransform	=	Matrix.RotationX( MathUtil.DegreesToRadians(1) );
			var landTransform	=	Matrix.RotationX( -MathUtil.DegreesToRadians(5) );
			var identity		=	Matrix.Identity;
			camera.Transform	=	standTransform;

			var deathTransform	=	Matrix.Translation( 0, deadEyeHeight, 0 )
								*	Matrix.RotationX( MathUtil.DegreesToRadians(1) )
								*	Matrix.RotationZ( MathUtil.DegreesToRadians(45) );
			
			scene.Nodes.Add( root );
			scene.Nodes.Add( camera );

			//	generate takes :
			var takeStand	=	new AnimationTake("stand"	, 2, 15);
			var takeCrouch	=	new AnimationTake("crouch"	, 2, 15);
			var takeDeath	=	new AnimationTake("death"	, 2, 20);

			
			takeStand.RecordTake( 1, 
				(i,t) =>  AnimationUtils.Lerp( crouchTransform, standTransform, AnimationUtils.SlowFollowThrough(t) )
						* AnimationUtils.Lerp( identity, duckTransform, AnimationUtils.KickCurve(t) ) 
			);

			takeCrouch	.RecordTake( 1, 
				(i,t) =>  AnimationUtils.Lerp( standTransform, crouchTransform, AnimationUtils.SlowFollowThrough(t) ) 
						* AnimationUtils.Lerp( identity, duckTransform, -AnimationUtils.KickCurve(t) ) 
			);

			takeDeath	.RecordTake( 1, 
				(i,t) =>  AnimationUtils.Lerp( standTransform, deathTransform, AnimationUtils.QuadraticStep(t) ) 
			);

			scene.Takes.Add( takeStand		);
			scene.Takes.Add( takeCrouch		);
			scene.Takes.Add( takeDeath		);

			Random rand = new Random(152445);

			for ( int i=0; i<MAX_SHAKES; i++) 
			{
				var amp		=	MathUtil.DegreesToRadians(1);
				var shake	=	Matrix.RotationYawPitchRoll( rand.NextFloat(-amp,amp)*0.5f, rand.NextFloat(amp,2*amp), rand.NextFloat(-amp,amp)*0.25f )
							*	Matrix.Translation(0,0,0.5f);
				var take	=	new AnimationTake("shake" + i.ToString(), 2, 10);
				take.RecordTake( 1, (f,t) => AnimationUtils.Lerp( identity, shake, AnimationUtils.KickCurve(t) ) );
				scene.Takes.Add(take);
			}

			return scene;
		}
	}
}