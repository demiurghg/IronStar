using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronStar.ECS;
using IronStar.SFX2;
using IronStar.ECSPhysics;
using Fusion.Core.Mathematics;
using IronStar.Animation;
using IronStar.Gameplay.Components;

namespace IronStar.Environment
{
	public enum Elevator
	{
		Side,
		Small,
	}

	class ElevatorFactory : EntityFactory
	{
		public Elevator Elevator { get; set; } = Elevator.Small;


		public override void Construct( Entity e, IGameState gs )
		{
			string scenePath	=	@"scenes\doors\elevator" + Elevator.ToString();

			BoundingBox	bbox	=	ComputeDoorBounds( Elevator );

			e.AddComponent( new Transform( Position, Rotation, Scaling ) );
			e.AddComponent( new RenderModel( scenePath, 1.0f, Color.White, 10, RMFlags.None ) );
			e.AddComponent( new ElevatorComponent() );
			e.AddComponent( new KinematicComponent( KinematicState.StoppedInitial) );
			e.AddComponent( new BoneComponent() );
			e.AddComponent( new DetectorComponent( bbox ) );
			e.AddComponent( new TriggerComponent() );
		}


		BoundingBox ComputeDoorBounds( Elevator elevatror )
		{
			switch (elevatror)
			{
				case Elevator.Side:		return new BoundingBox( new Vector3(-4,0,0), new Vector3(4,4,4) );
				case Elevator.Small:	return CreateBBox( 8,4,8 );
				default: return CreateBBox(8,8,8);
			}
		}

		
		BoundingBox CreateBBox( float width, float height, float depth )
		{
			return new BoundingBox(
				new Vector3( -width/2,      0, -depth/2 ),
				new Vector3(  width/2, height,  depth/2 )
				);
		}
	}
}
