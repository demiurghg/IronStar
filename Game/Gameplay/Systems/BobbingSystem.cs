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
	/// <summary>
	/// Updates user command bobbing properties
	/// Current implementation is for player only and contains state only for single entity.
	/// </summary>
	public class BobbingSystem : StatelessSystem<PlayerComponent,UserCommandComponent>
	{
		static readonly float LAND_PITCH	=	MathUtil.DegreesToRadians( - 3 );
		static readonly float LAND_UP		=	-0.5f;
		static readonly float JUMP_PITCH	=	MathUtil.DegreesToRadians( - 1 );
		static readonly float KICK_TIME		=	0.4f;

		public BobbingSystem()
		{
		}

		struct Bob
		{
			public float Time;
			public float Length;
			public float Amplitude;

			public float Value 
			{ 
				get { 
					if (Length>0) {
						return AnimationUtils.KickCurve( MathUtil.Clamp( Time / Length, 0, 1 ) ) * Amplitude; 
					} else {
						return 0;
					}
				} 
			}

			public void Play( float amplitude, float length )
			{
				if (Length>0) return;
				Time = 0;
				Length = length;
				Amplitude = amplitude;
			}

			public void Tick( float dt )
			{
				Time += dt;
				if (Time>Length) Length = 0;
			}
		}


		Bob	painPitch;
		Bob	painYaw	 ;
		Bob	painRoll ;

		Bob	bobPitch;
		Bob	bobYaw	;
		Bob	bobRoll	;
		Bob	bobUp	;


		protected override void Process( Entity entity, GameTime gameTime, PlayerComponent pc, UserCommandComponent uc )
		{
			var	health	=	entity.GetComponent<HealthComponent>();
			var impulse	=	entity.GetComponent<ImpulseComponent>();
			var step	=	entity.GetComponent<StepComponent>();

			if (step!=null)
			{
				if (step.Landed) bobPitch.Play( LAND_PITCH, KICK_TIME );
				if (step.Landed) bobUp	 .Play( LAND_UP	  , KICK_TIME );
				if (step.Jumped) bobPitch.Play( JUMP_PITCH, KICK_TIME );
			}

			if (health!=null)
			{
				RunPainBobbing( health );
			}

			painPitch.Tick( gameTime.ElapsedSec );
			painYaw	 .Tick( gameTime.ElapsedSec );
			painRoll .Tick( gameTime.ElapsedSec );

			bobPitch .Tick( gameTime.ElapsedSec );
			bobYaw	 .Tick( gameTime.ElapsedSec );
			bobRoll	 .Tick( gameTime.ElapsedSec );
			bobUp	 .Tick( gameTime.ElapsedSec );

			uc.BobPitch	=	bobPitch.Value + painPitch.Value;
			uc.BobYaw	=	bobYaw	.Value + painYaw  .Value;
			uc.BobRoll	=	bobRoll	.Value + painRoll .Value;
			uc.BobUp	=	bobUp	.Value;
		}


		void RunPainBobbing( HealthComponent health )
		{
			if (health.LastDamage>0)
			{
				var factor		=	(float)Math.Sqrt( health.LastDamage / (float)health.MaxHealth );
				var length		=	MathUtil.Lerp( 0.25f, 0.5f, factor );
				var maxPitch	=	MathUtil.DegreesToRadians(  1.0f * factor );
				var maxYaw		=	MathUtil.DegreesToRadians(  5.0f * factor );
				var maxRoll		=	MathUtil.DegreesToRadians( 15.0f * factor );

				var pitch		=	MathUtil.Random.NextFloat( -maxPitch, maxPitch	);
				var yaw			=	MathUtil.Random.NextFloat( -maxYaw	, maxYaw	);
				var roll		=	MathUtil.Random.NextFloat( -maxRoll	, maxRoll	);

				painPitch.Play( pitch, length );
				painYaw	 .Play( yaw  , length );
				painRoll .Play( roll , length );
			}
		}
	}
}
