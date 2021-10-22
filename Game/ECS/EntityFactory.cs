using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public abstract class EntityFactory : IEntityFactory
	{
		public abstract void Construct( Entity e, IGameState gs );
	}


	public class EntityFactoryAttribute : Attribute
	{
		public readonly string ClassName;

		public EntityFactoryAttribute( string className )
		{
			ClassName = className;
		}
	}
}
