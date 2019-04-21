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
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Core.IniParser.Model;
using System.ComponentModel;
using Fusion.Core.Shell;

namespace IronStar.Entities.Players {
	public class PlayerFactory : EntityFactory {

		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new Player( id, clsid,  world, this );
		}


		[Category("General")]
		public int MaxHealth { get; set; } = 100;

		[Category("General")]
		public int MaxArmor { get; set; } = 100;

		[AECategory("Appearance")]
		[AEClassname("models")]
		public string Model  { get; set; } = "";

		[AECategory("Character Controller")]
		public float Height					{ get; set; } = 1.70f	;
		[AECategory("Character Controller")]
		public float CrouchingHeight		{ get; set; } = 1.19f	;
		[AECategory("Character Controller")]
		public float Radius					{ get; set; } = 0.60f	;
		[AECategory("Character Controller")]
		public float Margin					{ get; set; } = 0.10f	;
		[AECategory("Character Controller")]
		public float Mass					{ get; set; } = 10f		;
		[AECategory("Character Controller")]
		public float MaximumTractionSlope	{ get; set; } = 0.80f	;
		[AECategory("Character Controller")]
		public float MaximumSupportSlope	{ get; set; } = 1.30f	;
		[AECategory("Character Controller")]
		public float StandingSpeed			{ get; set; } = 8f		;
		[AECategory("Character Controller")]
		public float CrouchingSpeed			{ get; set; } = 3f		;
		[AECategory("Character Controller")]
		public float TractionForce			{ get; set; } = 1000f	;
		[AECategory("Character Controller")]
		public float SlidingSpeed			{ get; set; } = 6f		;
		[AECategory("Character Controller")]
		public float SlidingForce			{ get; set; } = 50f		;
		[AECategory("Character Controller")]
		public float AirSpeed				{ get; set; } = 1f		;
		[AECategory("Character Controller")]
		public float AirForce				{ get; set; } = 250f	;
		[AECategory("Character Controller")]
		public float JumpSpeed				{ get; set; } = 6f		;
		[AECategory("Character Controller")]
		public float SlidingJumpSpeed		{ get; set; } = 3f		;
		[AECategory("Character Controller")]
		public float MaximumGlueForce		{ get; set; } = 5000f	;

		[AECategory("Character Controller")]
		public float MaxStepHeight			{ get; set; } = 0.5f	;

	}
}
