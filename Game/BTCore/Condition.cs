using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using IronStar.ECS;

namespace IronStar.BTCore
{
	public abstract class Condition : Decorator
	{
		public bool InverseCondition { get; set; } = false;
		public bool Continuous { get; set; } = false;

		public Condition( BTNode node ) : base( node )
		{
		}


		public override bool Initialize( Entity entity )
		{
			bool condition;
	
			condition	=	Check(entity);
			condition	=	InverseCondition ? !condition : condition;

			return condition;
		}

		public override BTStatus Update( GameTime gameTime, Entity entity, bool cancel )
		{
			if (Continuous)
			{
				bool condition;
	
				condition	=	Check(entity);
				condition	=	InverseCondition ? !condition : condition;

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
