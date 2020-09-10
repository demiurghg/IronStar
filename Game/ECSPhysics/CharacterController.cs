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
		const float	scale = 3.0f;

		public float	height					=	1.7f		*	scale; 
		public float	crouchingHeight			=	1.7f * .7f	*	scale; 
		public float	proneHeight				=	1.7f * 0.3f	*	scale; 
		public float	radius					=	0.6f		*	scale; 
		public float	margin					=	0.1f		*	scale; 
		public float	mass					=	10f			;
		public float	maximumTractionSlope	=	0.8f		; 
		public float	maximumSupportSlope		=	1.3f		;
		public float	standingSpeed			=	8f			*	scale; 
		public float	crouchingSpeed			=	3f			*	scale;
		public float	proneSpeed				=	1.5f		*	scale; 
		public float	tractionForce			=	1000		*	scale; 
		public float	slidingSpeed			=	6			*	scale; 
		public float	slidingForce			=	50			*	scale; 
		public float	airSpeed				=	1			*	scale; 
		public float	airForce				=	250			*	scale;
		public float	jumpSpeed				=	4.5f		*	scale; 
		public float	slidingJumpSpeed		=	3			*	scale;
		public float	maximumGlueForce		=	5000		*	scale;
		public float	stepHeight				=	0.1f		*	scale;

		public Vector3 offsetCrouch	{ get { return Vector3.Up * crouchingHeight	/ 2; } }
		public Vector3 offsetStanding	{ get { return Vector3.Up * height	/ 2; } }

		public bool		IsCrouching;
		public bool		HasTraction;

		public Vector3	PovOffset { get { return Vector3.Up * CalcPovHeight(); } }

		public CharacterController ( float heightStanding, float heightCrouching, float radius, float speedStanding, float speedCrouching, float speedJump, float mass, float stepHeight )
		{
			this.height				=	heightStanding	;
			this.crouchingHeight	=	heightCrouching	;
			this.radius				=	radius			;
			this.standingSpeed		=	speedStanding	;
			this.crouchingSpeed		=	speedCrouching	;
			this.jumpSpeed			=	speedJump		;
			this.stepHeight			=	stepHeight		;
			this.mass				=	mass			;
		}


		public static float CalcPovHeight(float standHeight, float crouchHeight, bool crouching)
		{
			//	head size is about 1/6 of body size
			//	eyes are placed in the middle of the head
			float topEyeOffset	=	standHeight / 6.0f / 2.0f;

			return (crouching ? crouchHeight : standHeight) - topEyeOffset;
		}


		float CalcPovHeight()
		{
			return CalcPovHeight( height, crouchingHeight, IsCrouching );
		}
	}
}
