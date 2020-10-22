using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.BTCore;
using IronStar.ECS;
using IronStar.ECSFactories;
using IronStar.Gameplay;
using Native.NRecast;
using IronStar.BTCore.Actions;

namespace IronStar.AI.Actions
{
	public class Attack : BTAction
	{
		readonly Wait wait;

		readonly string keyEntity;
		readonly int minShots;
		readonly int maxShots;
		readonly float accuracy;

		Entity	targetEntity;
		bool	targetAcquired;

		Vector3	accuracyError;


		public Attack( string keyEntity, int minTime, int maxTime, float accuracy )
		{
			this.keyEntity	=	keyEntity;
			this.accuracy	=	MathUtil.DegreesToRadians( accuracy );
			wait			=	new Wait( minTime, maxTime );
		}


		public override bool Initialize(Entity entity)
		{
			wait.Initialize(entity);
			
			targetEntity	=	entity.GetBlackboard()?.Get<Entity>(keyEntity);
			accuracyError	=	MathUtil.Random.UniformRadialDistribution( 0, accuracy );

			targetAcquired	=	false;

			return targetEntity!=null;
		}

		
		public override void Terminate( Entity entity, BTStatus status )
		{
			wait.Terminate(entity, status);
			targetEntity	=	null;

			var uc = entity.GetComponent<UserCommandComponent>();

			uc.Action &= ~UserAction.Attack;
		}

		
		public override BTStatus Update( GameTime gameTime, Entity entity, bool cancel )
		{
			var status	=	wait.Tick( gameTime, entity, false /* never cancel */ );

			var uc = entity.GetComponent<UserCommandComponent>();

			var originPoint		=	entity.GetPOV();
			var targetPoint		=	0.7f * targetEntity.GetPOV() + 0.3f * targetEntity.Location;  
			var distance		=	Vector3.Distance( originPoint, targetPoint );
			var accuracyBias	=	accuracyError * distance;

			var fraction	=	wait.Fraction;

			float rateYaw	=	gameTime.ElapsedSec * MathUtil.TwoPi;
			float ratePitch	=	gameTime.ElapsedSec * MathUtil.TwoPi;

			var error		=	uc.RotateTo( originPoint, targetPoint + accuracyBias, rateYaw, ratePitch );

			if (error<0.1f)
			{
				targetAcquired  = true;
			}
			
			if (status==BTStatus.InProgress && targetAcquired)
			{
				uc.Action |= UserAction.Attack;
			}
			else
			{
				uc.Action &= ~UserAction.Attack;
			}

			return status;
		}
	}
}
