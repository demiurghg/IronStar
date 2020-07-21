using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	public abstract class EntityFactory
	{
		public abstract Entity Spawn( GameState gs );
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
