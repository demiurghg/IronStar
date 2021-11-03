using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using IronStar.BTCore;
using IronStar.ECS;

namespace IronStar.AI
{
	public class BehaviorComponent : IComponent
	{
		public TimeSpan ThinkQuantum;
		public TimeSpan ThinkCooldown;

		public float VisibilityFov		=	45.0f;
		public float VisibilityRange	=	450.0f;
		public float HearingRange		=	15.0f;

		public Entity LastSeenTarget	=	null;

		[DoNotSave]
		public readonly Blackboard Blackboard;

		public BehaviorComponent()
		{
			Blackboard		=	new Blackboard();
			ThinkQuantum	=	TimeSpan.FromSeconds(0.35f);
			ThinkCooldown	=	MathUtil.Random.NextTime( TimeSpan.Zero, ThinkQuantum );
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			#warning TODO : SAVE BehaviorComponent
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			#warning TODO : LOAD BehaviorComponent
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float factor )
		{
			return Clone();
		}
	}
}
