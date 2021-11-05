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

namespace IronStar.ECSPhysics 
{
	public class DynamicBox : IComponent
	{
		public float			Width	{ get; set; } =	1;
		public float			Height	{ get; set; } =	1;
		public float			Depth	{ get; set; } =	1;
		public float			Mass	{ get; set; } =	1;
		public CollisionGroup	Group	{ get; set; } = CollisionGroup.StaticGroup;

		public DynamicBox()
		{
		}

		public DynamicBox ( float width, float height, float depth, float mass )
		{
			this.Width	=	width	;
			this.Height	=	height	;
			this.Depth	=	depth	;
			this.Mass	=	mass	;
		}

		/*-----------------------------------------------------------------------------------------
		 *	IComponent implementation :
		-----------------------------------------------------------------------------------------*/

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Width			);
			writer.Write( Height		);	
			writer.Write( Depth			);
			writer.Write( Mass			);
			writer.Write( (int)Group	);
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Width	=	reader.ReadSingle();	
			Height	=	reader.ReadSingle();
			Depth	=	reader.ReadSingle();	
			Mass	=	reader.ReadSingle();	
			Group	=	(CollisionGroup)reader.ReadInt32();
		}

		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}
	}
}
