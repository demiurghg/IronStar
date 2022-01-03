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
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Core.Extensions;
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
using BEPUphysics.DataStructures;
using IronStar.ECS;
using BEPUutilities.DataStructures;

namespace IronStar.ECSPhysics 
{
	public class DetectorComponent : IComponent
	{
		public bool	DetectPlayer	=	true;
		public bool DetectMonsters	=	true;
		public BoundingBox LocalBounds;
		public string Target = "";

		/// <summary>
		/// temporary list of touching entities
		/// never saved
		/// </summary>
		public readonly List<Entity> Touchers;

		public DetectorComponent()
		{
			this.Touchers		=	new List<Entity>(3);
			this.LocalBounds	=	new BoundingBox(0,0,0);
		}

		public DetectorComponent( BoundingBox localBounds )
		{
			this.Touchers		=	new List<Entity>(3);
			this.LocalBounds	=	localBounds;
		}

		public IComponent Clone()
		{
			return (DetectorComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( LocalBounds );
			writer.Write( Target );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			LocalBounds =	reader.Read<BoundingBox>();
			Target		=	reader.ReadString();
		}
	}
}
