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

		Entity targetEntity;

		float	errorYaw0;
		float	errorYaw1;
		float	errorPitch0;
		float	errorPitch1;


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
			errorPitch0		=	MathUtil.Random.NextFloat( -accuracy, accuracy );
			errorYaw0		=	MathUtil.Random.NextFloat( -accuracy, accuracy );
			errorPitch1		=	MathUtil.Random.NextFloat( -accuracy, accuracy );
			errorYaw1		=	MathUtil.Random.NextFloat( -accuracy, accuracy );

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

			var originPoint	=	entity.GetPOV();
			var targetPoint	=	0.7f * targetEntity.GetPOV() + 0.3f * targetEntity.Location;

			var fraction	=	wait.Fraction;

			float rateYaw	=	gameTime.ElapsedSec * MathUtil.TwoPi;
			float ratePitch	=	gameTime.ElapsedSec * MathUtil.TwoPi;

			uc.RotateTo( originPoint, targetPoint, rateYaw, ratePitch );
			
			if (status==BTStatus.InProgress)
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
