using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.BTCore
{
	[Flags]
	public enum ConditionMode
	{
		Inverse		=	0x001,
		Continuous	=	0x002,
	}

	public abstract class Condition : Decorator
	{
		readonly protected bool inverseCondition;
		readonly protected bool continuous;

		protected GameTime gameTime;

		public Condition( ConditionMode mode, BTNode node ) : base( node )
		{
			inverseCondition	=	mode.HasFlag( ConditionMode.Inverse );
			continuous			=	mode.HasFlag( ConditionMode.Continuous );
		}


		public override bool Initialize( Entity entity )
		{
			bool condition;
	
			condition	=	Check(entity);
			condition	=	inverseCondition ? !condition : condition;

			return condition;
		}

		public override BTStatus Update( GameTime gameTime, Entity entity, bool cancel )
		{
			this.gameTime	=	gameTime;

			if (continuous)
			{
				bool condition;
	
				condition	=	Check(entity);
				condition	=	inverseCondition ? !condition : condition;

				return Node.Tick( gameTime, entity, !condition );
			}
			else
			{
				return Node.Tick( gameTime, entity, false );
			}
		}

		public abstract bool Check(Entity entity);
	}
}
