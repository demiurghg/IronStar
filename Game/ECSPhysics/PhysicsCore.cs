using System;
using System.Collections.Generic;
using BEPUphysics;
using System.Linq;
using BEPUVector3 = BEPUutilities.Vector3;
using IronStar.SFX;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Core.Mathematics;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using BEPUphysics.CollisionRuleManagement;
using IronStar.ECS;
using Fusion.Core;
using Fusion;
using IronStar.Gameplay;

namespace IronStar.ECSPhysics
{
	public class PhysicsCore
	{
		Space physSpace = new Space();

		HashSet<Tuple<Entity,Entity>> touchEvents;

		public CollisionGroup StaticGroup		= new CollisionGroup();
		public CollisionGroup KinematicGroup	= new CollisionGroup();
		public CollisionGroup DymamicGroup		= new CollisionGroup();
		public CollisionGroup PickupGroup		= new CollisionGroup();
		public CollisionGroup CharacterGroup	= new CollisionGroup();
		
		public PhysicsCore ()
		{
			touchEvents	=	new HashSet<Tuple<Entity, Entity>>();

			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	CharacterGroup ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( StaticGroup,	DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( CharacterGroup, DymamicGroup   ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	StaticGroup    ), CollisionRule.Normal );
			CollisionRules.CollisionGroupRules.Add( new CollisionGroupPair( PickupGroup,	CharacterGroup ), CollisionRule.NoSolver );
		}
	}
}
