using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;

namespace IronStar.ECS
{
	class SpawnData
	{
		public readonly Entity Entity;
		public readonly IComponent[] Components;
		public readonly EntityFactory Factory;
		public readonly Vector3 Position;
		public readonly Quaternion Rotation;

		public SpawnData( Entity entity, IComponent[] components, EntityFactory factory, Vector3 position, Quaternion rotation )
		{
			Entity		=	entity;
			Components	=	components?.ToArray();
			Factory		=	factory;
			Position	=	position;
			Rotation	=	rotation;
		}


		public SpawnData( Entity entity ) : this( entity, null, null, Vector3.Zero, Quaternion.Identity )
		{
		}


		public void ConstructEntity( GameState gs )
		{
			Factory?.Construct( Entity, gs );

			if (Components!=null)
			{
				foreach ( var c in Components )
				{
					Entity.AddComponent( c );
				}
			}

			var t = Entity.GetComponent<Transform>();

			if (t!=null)
			{
				t.Position	=	Position;
				t.Rotation	=	Rotation;
			}
		}
	}
}
