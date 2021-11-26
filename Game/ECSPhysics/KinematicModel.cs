using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using BEPUphysics;
using BEPUphysics.Character;
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.EntityStateManagement;
using BEPUphysics.Entities.Prefabs;
using BEPUphysics.PositionUpdating;
using BEPUphysics.CollisionRuleManagement;
using IronStar.ECS;
using Fusion.Engine.Graphics.Scenes;
using AffineTransform = BEPUutilities.AffineTransform;

namespace IronStar.ECSPhysics
{
	public enum StuckBehavior : byte
	{
		Pause,
		Reverse,
	}

	public enum KinematicState : byte
	{
		Stopped,
		PlayLooped,
		PlayForward,
		PlayBackward,
	}

	public class KinematicModel : IComponent
	{
		public TimeSpan Time;
		public int Damage = 5;
		public bool Stuck = false;
		public StuckBehavior Behavior = StuckBehavior.Pause;
		public KinematicState State   = KinematicState.Stopped;

		public KinematicModel()
		{
		}


		public IComponent Clone()
		{
			return (KinematicModel)this.MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Time		=	reader.Read<TimeSpan>();
			Damage		=	reader.ReadInt32();
			Stuck		=	reader.ReadBoolean();
			Behavior	=	(StuckBehavior)reader.ReadByte();
			State		=	(KinematicState)reader.ReadByte();
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Time );
			writer.Write( Damage );
			writer.Write( Stuck );
			writer.Write( (byte)Behavior );
			writer.Write( (byte)State );
		}
	}
}
