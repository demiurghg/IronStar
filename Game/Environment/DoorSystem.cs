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
	public class DoorSystem : StatelessSystem<DoorComponent,TriggerComponent,KinematicComponent>
	{
		const string DOOR_SOUND = @"env/doors/door";

		protected override void Process( Entity entity, GameTime gameTime, DoorComponent door, TriggerComponent trigger, KinematicComponent kinematic )
		{
			switch (door.Mode)
			{
				case DoorControlMode.Automatic:			ProcessAutomaticDoor( entity, gameTime, door, trigger, kinematic );	break;
				case DoorControlMode.ExternalToggle:	ProcessToggleDoor( entity, gameTime, door, trigger, kinematic );	break;
				default: throw new NotImplementedException("Door mode " + door.Mode.ToString() + " is not implemented.");
			}
		}


		void ProcessAutomaticDoor( Entity entity, GameTime gameTime, DoorComponent door, TriggerComponent trigger, KinematicComponent kinematic )
		{
			Entity activator;
			bool external;
			var detector = entity.GetComponent<DetectorComponent>();

			bool activatorDetected = detector==null ? false : detector.Touchers.Any();

			if (trigger.IsSet(out activator, out external))
			{
				if (!external && kinematic.State==KinematicState.StoppedInitial)
				{
					kinematic.State =	KinematicState.PlayForward;
					SoundPlayback.PlaySound( entity, DOOR_SOUND );
				}
			}

			//	Assume, door may be stuck on the way back...
			//	otherwice it can stuck in infinite loop on trying to get forward and backward...
			//	- Quake 2 uses such approach
			//	- Doom 2016 closes doors too fast, dynamic objects get stuck in doors
			//	- Void Bastards just pushes everything out
			if ((kinematic.Stuck || activatorDetected) && kinematic.State==KinematicState.PlayBackward)
			{
				kinematic.State = KinematicState.PlayForward;
				SoundPlayback.PlaySound( entity, DOOR_SOUND );
			}

			if (kinematic.State==KinematicState.StoppedTerminal && !activatorDetected)
			{
				door.Timer += gameTime.Elapsed;

				if (door.Timer >= TimeSpan.FromMilliseconds(door.Wait))
				{
					door.Timer = TimeSpan.Zero;
					kinematic.State = KinematicState.PlayBackward;
					SoundPlayback.PlaySound( entity, DOOR_SOUND );
				}
			}
		}


		void ProcessToggleDoor( Entity entity, GameTime gameTime, DoorComponent door, TriggerComponent trigger, KinematicComponent kinematic )
		{
			Entity activator;
			bool external;

			if (trigger.IsSet(out activator, out external))
			{
				if (external)
				{
					if (kinematic.State==KinematicState.StoppedInitial)
					{
						kinematic.State = KinematicState.PlayForward;
						SoundPlayback.PlaySound( entity, DOOR_SOUND );
					}
					if (kinematic.State==KinematicState.StoppedTerminal)
					{
						kinematic.State = KinematicState.PlayBackward;
						SoundPlayback.PlaySound( entity, DOOR_SOUND );
					}
				}
			}

			//	Assume, door may be stuck on the way back...
			//	otherwice it can stuck in infinite loop on trying to get forward and backward...
			//	- Quake 2 uses such approach
			//	- Doom 2016 closes doors too fast, dynamic objects get stuck in doors
			//	- Void Bastards just pushes everything out
			if ((kinematic.Stuck) && kinematic.State==KinematicState.PlayBackward)
			{
				kinematic.State = KinematicState.PlayForward;
				SoundPlayback.PlaySound( entity, DOOR_SOUND );
			}

		}
	}
}
