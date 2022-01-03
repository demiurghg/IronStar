using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;
using Fusion.Core.Mathematics;

namespace IronStar.Gameplay.Systems
{
	/// <summary>
	/// 1 stride = 2 steps
	/// Stride Frequency	: SF(V) = 0.1*V + 1.1
	/// Stride Length		: SL(V) = V / SF(V)
	/// Stride Period		: SP(V) = 1 / SF(V)
	/// https://www.desmos.com/calculator/cqesebrfny
	/// https://digitalcommons.wku.edu/cgi/viewcontent.cgi?article=2061&context=ijes
	/// </summary>
	class StepSystem : ISystem
	{
		const float STEP_VELOCITY_THRESHOLD = 0.1f;
		const float ACCELERATION_FILTER		= 10.0f;

		const float NOISE_STEP	=	30;
		const float NOISE_JUMP	=	50;
		const float NOISE_LAND	=	80;

		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}
		public Aspect GetAspect() { return Aspect.Empty; }

		Aspect stepAspect = Aspect.Empty.Include<StepComponent,CharacterController,Transform>();


		public void Update( IGameState gs, GameTime gameTime )
		{
			var stepEntities = gs.QueryEntities( stepAspect );

			foreach ( var e in stepEntities )
			{
				var step		=	e.GetComponent<StepComponent>();
				var noise		=	e.GetComponent<NoiseComponent>();

				var wpnState	=	e.GetComponent<WeaponStateComponent>();
				var controller	=	e.GetComponent<CharacterController>();
				var velocity3D	=	e.GetComponent<Transform>().LinearVelocity;
				var transform	=	e.GetComponent<Transform>();

				var oldVelocity		=	new Vector3( step.GroundVelocity.X, step.FallVelocity, step.GroundVelocity.Z );

				step.FallVelocity	=	velocity3D.Y;
				step.GroundVelocity	=	new Vector3( velocity3D.X, 0, velocity3D.Z );

				var velocity		=	step.GroundVelocity.Length();

				step.WeaponState	=	wpnState==null ? WeaponState.Inactive : wpnState.State;

				//
				//	update striding/stepping :
				//
				var stepPeriod	=	0.3f;
				step.LeftStep	=	false;
				step.RightStep	=	false;

				if ( controller.HasTraction && (velocity > STEP_VELOCITY_THRESHOLD) )
				{
					if (step.StepTimer<0)
					{
						step.Counter++;

						step.LeftStep	=	MathUtil.IsEven( step.Counter );
						step.RightStep	=	MathUtil.IsOdd( step.Counter );

						step.StepTimer	=	stepPeriod;
					}

					step.StepTimer -= gameTime.ElapsedSec;
				}

				//
				//	update traction, jumping and landing :
				//
				step.Jumped			=	 step.HasTraction && ( controller.HasTraction != step.HasTraction );
				step.Landed			=	!step.HasTraction && ( controller.HasTraction != step.HasTraction );

				step.HasTraction	=	controller.HasTraction;

				//
				//	update stances :
				//
				step.Crouched		=	 controller.IsCrouching && ( controller.IsCrouching != step.IsCrouching );
				step.Standed		=	!controller.IsCrouching && ( controller.IsCrouching != step.IsCrouching );

				step.IsCrouching	=	controller.IsCrouching;


				//
				//	acceleration :
				//
				var acceleration		=	( velocity3D - oldVelocity ) / gameTime.ElapsedSec;

				step.LocalAcceleration.MoveTo( ref acceleration, ACCELERATION_FILTER );
				//acceleration		=	Vector3.TransformNormal( acceleration, Matrix.Invert( transform.TransformMatrix ) );
				//step.LocalAcceleration	=	Vector3.Lerp( step.LocalAcceleration, acceleration, ACCELERATION_FILTER );

				//
				//	noise :
				//
				float scale = velocity / controller.standingSpeed;
				if (step.LeftStep)	noise?.MakeNoise( NOISE_STEP  * scale );
				if (step.RightStep) noise?.MakeNoise( NOISE_STEP  * scale );
				if (step.Landed)	noise?.MakeNoise( NOISE_LAND );
				if (step.Jumped)	noise?.MakeNoise( NOISE_JUMP );

				//
				//	debug :
				//
				/*if (step.Jumped)	Log.Message("JUMPED");
				if (step.Landed)	Log.Message("LANDED");
				if (step.LeftStep)	Log.Message("LEFT   *|");
				if (step.RightStep) Log.Message("RIGHT   |*");//*/
			}
		}
	}
}
