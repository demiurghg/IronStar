using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using IronStar.ECS;
using IronStar.ECSPhysics;
using IronStar.Gameplay.Components;
using IronStar.SFX;

namespace IronStar.Environment
{
	public class ElevatorSystem : StatelessSystem<ElevatorComponent,TriggerComponent,KinematicComponent>
	{
		const string ELEVATOR_SOUND = @"env/doors/door";

		protected override void Process( Entity entity, GameTime gameTime, ElevatorComponent elevator, TriggerComponent trigger, KinematicComponent kinematic )
		{
			switch (elevator.Mode)
			{
				case ElevatorMode.OneWay:	ProcessOneWayElevator( entity, gameTime, elevator, trigger, kinematic );	break;
				default: throw new NotImplementedException("Door mode " + elevator.Mode.ToString() + " is not implemented.");
			}
		}


		void ProcessOneWayElevator( Entity entity, GameTime gameTime, ElevatorComponent elevator, TriggerComponent trigger, KinematicComponent kinematic )
		{
			Entity activator;
			bool external;
			var detector = entity.GetComponent<DetectorComponent>();
			var wait = TimeSpan.FromMilliseconds(elevator.Wait);

			bool activatorDetected = detector==null ? false : detector.Touchers.Any();

			if (trigger.IsSet(out activator, out external))
			{
				if (!elevator.Engaged && !external && kinematic.State==KinematicState.StoppedInitial)
				{
					elevator.Engaged = true;
					elevator.Timer   = wait;
				}
			}

			if (elevator.Engaged)
			{
				elevator.Timer -= gameTime.Elapsed;

				if (elevator.Timer<TimeSpan.Zero)
				{
					kinematic.State = KinematicState.PlayForward;
					SoundPlayback.PlaySound( entity, ELEVATOR_SOUND );
					elevator.Engaged = false;
				}
			}

			if (kinematic.State==KinematicState.StoppedTerminal && !activatorDetected)
			{
				elevator.Timer += gameTime.Elapsed;

				if (elevator.Timer >= wait)
				{
					elevator.Timer = TimeSpan.Zero;
					kinematic.State = KinematicState.PlayBackward;
					SoundPlayback.PlaySound( entity, ELEVATOR_SOUND );
				}
			}
		}
	}
}
