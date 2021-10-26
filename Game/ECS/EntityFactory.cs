using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Widgets.Advanced;

namespace IronStar.ECS
{
	public class EntityFactory : IFactory, ICloneable
	{
		[AEIgnore]
		public Vector3 Position { get; set; } = Vector3.Zero;

		[AEIgnore]
		public Quaternion Rotation { get; set; } = Quaternion.Identity;

		[AEIgnore]
		public float Scaling { get; set; } = 1;

		public object Clone()
		{
			return MemberwiseClone();
		}

		public virtual void Construct( Entity e, IGameState gs )
		{
			e.AddComponent( new Transform( Position, Rotation, Scaling ) );
		}
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
