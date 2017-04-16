using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;


namespace IronStar.Client {
	public partial class GameInput : GameComponent {

		[Config] public float Sensitivity { get; set; }
		[Config] public bool InvertMouse { get; set; }
		[Config] public float PullFactor { get; set; }
		[Config] public bool ThirdPerson { get; set; }
		
		[Config] public float ZoomFov { get; set; }
		[Config] public float Fov { get; set; }
		[Config] public float BobHeave	{ get; set; }
		[Config] public float BobPitch	{ get; set; }
		[Config] public float BobRoll	{ get; set; }
		[Config] public float BobStrafe  { get; set; }
		[Config] public float BobJump	{ get; set; }
		[Config] public float BobLand	{ get; set; }

		[Config] public Keys	MoveForward		{ get; set; }
		[Config] public Keys	MoveBackward	{ get; set; }
		[Config] public Keys	StrafeRight		{ get; set; }
		[Config] public Keys	StrafeLeft		{ get; set; }
		[Config] public Keys	Jump			{ get; set; }
		[Config] public Keys Crouch			{ get; set; }
		[Config] public Keys Walk			{ get; set; }

		[Config] public Keys Attack			{ get; set; }
		[Config] public Keys Zoom			{ get; set; }
		[Config] public Keys Use			{ get; set; }

		[Config] public Keys MeleeAttack	{ get; set; }
		[Config] public Keys SwitchWeapon	{ get; set; }
		[Config] public Keys ReloadWeapon	{ get; set; }
		[Config] public Keys ThrowGrenade	{ get; set; }


		/// <summary>
		/// 
		/// </summary>
		/// <param name="cl"></param>
		public GameInput (Game game) : base(game)
		{	
			Sensitivity	=	5;
			InvertMouse	=	true;
			PullFactor	=	1;

			Fov			=	90.0f;
			ZoomFov		=	30.0f;

			BobHeave	=	0.05f;
			BobPitch	=	1.0f;
			BobRoll		=	2.0f;
			BobStrafe  	=	5.0f;
			BobJump		=	5.0f;
			BobLand		=	5.0f;


			MoveForward		=	Keys.S;
			MoveBackward	=	Keys.Z;
			StrafeRight		=	Keys.X;
			StrafeLeft		=	Keys.A;
			Jump			=	Keys.RightButton;
			Crouch			=	Keys.LeftAlt;
			Walk			=	Keys.LeftShift;
							
			Attack			=	Keys.LeftButton;
			Zoom			=	Keys.D;

			Use				=	Keys.LeftControl;
								
			SwitchWeapon	=	Keys.Q;
			ReloadWeapon	=	Keys.R;
			ThrowGrenade	=	Keys.G;
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize()
		{
			Game.Keyboard.KeyDown += Keyboard_KeyDown;	
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			Game.Keyboard.KeyDown -= Keyboard_KeyDown;	
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Keyboard_KeyDown ( object sender, KeyEventArgs e )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="userCommand"></param>
		public void Update ( GameTime gameTime, ref UserCommand userCommand )
		{
			var flags = UserAction.None;
			
			userCommand.MoveForward	=	0;
			userCommand.MoveRight	=	0;
			userCommand.MoveUp		=	0;
			
			if (Game.Keyboard.IsKeyDown( MoveForward	)) userCommand.MoveForward++;
			if (Game.Keyboard.IsKeyDown( MoveBackward	)) userCommand.MoveForward--;
			if (Game.Keyboard.IsKeyDown( StrafeRight	)) userCommand.MoveRight++;
			if (Game.Keyboard.IsKeyDown( StrafeLeft		)) userCommand.MoveRight--;
			if (Game.Keyboard.IsKeyDown( Jump			)) userCommand.MoveUp++;
			if (Game.Keyboard.IsKeyDown( Crouch			)) userCommand.MoveUp--;

			if (Game.Keyboard.IsKeyDown( Attack			)) flags |= UserAction.Attack;
			if (Game.Keyboard.IsKeyDown( Zoom			)) flags |= UserAction.Zoom;
			if (Game.Keyboard.IsKeyDown( Use			)) flags |= UserAction.Use;

			if (Game.Keyboard.IsKeyDown( SwitchWeapon	)) flags |= UserAction.SwitchWeapon;
			if (Game.Keyboard.IsKeyDown( ThrowGrenade	)) flags |= UserAction.ThrowGrenade;
			if (Game.Keyboard.IsKeyDown( MeleeAttack	)) flags |= UserAction.MeleeAtack;
			if (Game.Keyboard.IsKeyDown( ReloadWeapon	)) flags |= UserAction.ReloadWeapon;

			//	http://eliteownage.com/mousesensitivity.html 
			//	Q3A: 16200 dot per 360 turn:
			var vp		=	Game.RenderSystem.DisplayBounds;
			var ui		=	Game.UserInterface.Instance as ShooterInterface;
			//var cam		=	World.GetView<CameraView>();

			if (!Game.Console.IsShown) {

				userCommand.DYaw		=	-2 * MathUtil.Pi * 5 * Game.Mouse.PositionDelta.X / 16200.0f;
				userCommand.DPitch		=	-2 * MathUtil.Pi * 5 * Game.Mouse.PositionDelta.Y / 16200.0f * ( InvertMouse ? -1 : 1 );

				userCommand.Action		=	flags;
				userCommand.Yaw         +=  userCommand.DYaw;
				userCommand.Pitch       +=  userCommand.DPitch;
				userCommand.Roll		=	0;
			}
		}

		
	}
}
