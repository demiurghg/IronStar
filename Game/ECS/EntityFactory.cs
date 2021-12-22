using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Widgets.Advanced;
using IronStar.Gameplay.Components;

namespace IronStar.ECS
{
	public class EntityFactory : IFactory, ICloneable
	{
		[AEIgnore]
		public string NodeName { get; set; } = null;

		[AEIgnore]
		public Vector3 Position { get; set; } = Vector3.Zero;

		[AEIgnore]
		public Quaternion Rotation { get; set; } = Quaternion.Identity;

		[AEIgnore]
		public Vector3 LinearVelocity { get; set; } = Vector3.Zero;

		[AEIgnore]
		public Vector3 AngularVelocity { get; set; } = Vector3.Zero;

		[AEIgnore]
		public float Scaling { get; set; } = 1;

		public object Clone()
		{
			return MemberwiseClone();
		}

		readonly IComponent[] components;

		public EntityFactory()
		{
		}

		public EntityFactory( params IComponent[] components ) : this( Vector3.Zero, Quaternion.Identity, 1.0f, components )
		{
		}

		public EntityFactory( Vector3 position, Quaternion rotation, float scaling, params IComponent[] components )
		{
			this.Position	=	position;
			this.Rotation	=	rotation;
			this.Scaling	=	scaling;

			this.components	=	new IComponent[components.Length];
			Array.Copy( components, this.components, components.Length );
		}

		public virtual void Construct( Entity e, IGameState gs )
		{
			e.AddComponent( new Transform( Position, Rotation, Scaling, LinearVelocity, AngularVelocity ) );

			if (components!=null)
			{
				for (int i=0; i<components.Length; i++)
				{
					e.AddComponent( components[i] );
				}
			}
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
