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
	public enum Door
	{
		LargeVertical,
		LargeSplit,
		LargeVerticalSplit,
		LargeFourway,

		Small,
	}

	class DoorFactory : EntityFactory
	{
		public Door Door { get; set; } = Door.LargeVertical;
		public string TargetName { get; set; } = "";
		public bool Toggle { get; set; }


		public override void Construct( Entity e, IGameState gs )
		{
			string scenePath	=	@"scenes\doors\door" + Door.ToString();

			BoundingBox	bbox	=	ComputeDoorBounds( Door );

			e.AddComponent( new Transform( Position, Rotation, Scaling ) );
			e.AddComponent( new RenderModel( scenePath, 1.0f, Color.White, 10, RMFlags.None ) );
			e.AddComponent( new KinematicComponent( KinematicState.StoppedInitial) );
			e.AddComponent( new BoneComponent() );

			if (string.IsNullOrWhiteSpace(TargetName))
			{
				e.AddComponent( new DoorComponent() { Mode = DoorControlMode.Automatic } );
				e.AddComponent( new TriggerComponent() );
				e.AddComponent( new DetectorComponent( bbox ) );
			}
			else
			{
				e.AddComponent( new DoorComponent() { Mode = DoorControlMode.ExternalToggle } );
				e.AddComponent( new TriggerComponent() { Name = TargetName } );
			}
		}



		BoundingBox ComputeDoorBounds( Door door )
		{
			switch (Door)
			{
				case Door.LargeFourway:
				case Door.LargeVertical:
				case Door.LargeVerticalSplit:
				case Door.LargeSplit:
					return CreateBBox( 24, 12, 16 );
				case Door.Small:
					return CreateBBox( 5, 8, 16 );
				default:
					return CreateBBox( 8, 8, 16 );
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
