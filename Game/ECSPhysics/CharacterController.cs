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
using IronStar.ECS;


namespace IronStar.ECSPhysics 
{
	public class CharacterController : Component
	{
		public float	heightStanding;
		public float	heightCrouching;
		public float	radius;
		public float	speedStanding;
		public float	speedCrouching;
		public float	speedJump;
		public float	stepHeight;
		public float	mass;
		public Vector3 offsetCrouch	{ get { return Vector3.Up * heightCrouching	/ 2; } }
		public Vector3 offsetStanding	{ get { return Vector3.Up * heightStanding	/ 2; } }

		public bool		IsCrouching;
		public bool		HasTraction;

		public Vector3	PovOffset { get { return Vector3.Up * CalcPovHeight(); } }

		public CharacterController ( float heightStanding, float heightCrouching, float radius, float speedStanding, float speedCrouching, float speedJump, float mass, float stepHeight )
		{
			this.heightStanding		=	heightStanding	;
			this.heightCrouching	=	heightCrouching	;
			this.radius				=	radius			;
			this.speedStanding		=	speedStanding	;
			this.speedCrouching		=	speedCrouching	;
			this.speedJump			=	speedJump		;
			this.stepHeight			=	stepHeight		;
			this.mass				=	mass			;
		}


		float CalcPovHeight()
		{
			float topEyeOffset	=	heightStanding / 6.0f / 2.0f;

			return (IsCrouching ? heightCrouching : heightStanding) - topEyeOffset;
		}
	}
}
