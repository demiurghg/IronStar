﻿using System;
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
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Tools;
using Fusion.Engine.Frames;

namespace IronStar.Client {
	public partial class GameInput : GameComponent {

		[Config] public float	Sensitivity		{ get; set; }	=	5;
		[Config] public bool	InvertMouse		{ get; set; }	=	true;
		[Config] public float	PullFactor		{ get; set; }	=	1;
		[Config] public bool	ThirdPerson		{ get; set; }	=	false;
																
		[Config] public float	ZoomFov			{ get; set; }	=	90.0f;
		[Config] public float	Fov				{ get; set; }	=	30.0f;
		[Config] public float	BobHeave		{ get; set; }	=	0.05f;
		[Config] public float	BobPitch		{ get; set; }	=	1.0f;
		[Config] public float	BobRoll			{ get; set; }	=	2.0f;
		[Config] public float	BobStrafe		{ get; set; }	=	5.0f;
		[Config] public float	BobJump			{ get; set; }	=	5.0f;
		[Config] public float	BobLand			{ get; set; }	=	5.0f;

		[Config] public Keys	MoveForward		{ get; set; }	=	Keys.S;
		[Config] public Keys	MoveBackward	{ get; set; }	=	Keys.Z;	
		[Config] public Keys	StrafeRight		{ get; set; }	=	Keys.X;	
		[Config] public Keys	StrafeLeft		{ get; set; }	=	Keys.A;	
		[Config] public Keys	Jump			{ get; set; }	=	Keys.RightButton;	
		[Config] public Keys	Crouch			{ get; set; }	=	Keys.LeftAlt;	
		[Config] public Keys	Walk			{ get; set; }	=	Keys.LeftShift;	
																
		[Config] public Keys	Attack			{ get; set; }	=	Keys.LeftButton;
		[Config] public Keys	Zoom			{ get; set; }	=	Keys.D;
		[Config] public Keys	Use				{ get; set; }	=	Keys.LeftControl;

		[Config] public Keys	MeleeAttack		{ get; set; }	=	Keys.Space;
		[Config] public Keys	SwitchWeapon	{ get; set; }	=	Keys.Q;	
		[Config] public Keys	ReloadWeapon	{ get; set; }	=	Keys.R;
		[Config] public Keys	ThrowGrenade	{ get; set; }	=	Keys.G;
																
		[Config] public Keys	Weapon1			{ get; set; }	=	Keys.D1;
		[Config] public Keys	Weapon2			{ get; set; }	=	Keys.D2;	
		[Config] public Keys	Weapon3			{ get; set; }	=	Keys.D3;
		[Config] public Keys	Weapon4			{ get; set; }	=	Keys.D4;
		[Config] public Keys	Weapon5			{ get; set; }	=	Keys.D5;
		[Config] public Keys	Weapon6			{ get; set; }	=	Keys.D6;	
		[Config] public Keys	Weapon7			{ get; set; }	=	Keys.D7;
		[Config] public Keys	Weapon8			{ get; set; }	=	Keys.D8;
																

		public bool EnableControl {
			get; set;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="cl"></param>
		public GameInput (Game game) : base(game)
		{	
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
		public void Update ( GameTime gameTime, GameWorld world, ref UserCommand userCommand )
		{
			var flags = UserAction.None;
			var console = Game.GetService<GameConsole>();
			var frames	= Game.GetService<FrameProcessor>();
			
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

			userCommand.Weapon = 0;

			if (Game.Keyboard.IsKeyDown( Weapon1	)) userCommand.Weapon = world.Atoms["weapon_machinegun"];
			if (Game.Keyboard.IsKeyDown( Weapon2	)) userCommand.Weapon = world.Atoms["weapon_plasmagun" ];
			if (Game.Keyboard.IsKeyDown( Weapon3	)) userCommand.Weapon = world.Atoms["weapon_plasmagun" ];
			if (Game.Keyboard.IsKeyDown( Weapon4	)) userCommand.Weapon = world.Atoms["weapon_machinegun"];
			if (Game.Keyboard.IsKeyDown( Weapon5	)) userCommand.Weapon = world.Atoms["weapon_machinegun"];
			if (Game.Keyboard.IsKeyDown( Weapon6	)) userCommand.Weapon = world.Atoms["weapon_machinegun"];
			if (Game.Keyboard.IsKeyDown( Weapon7	)) userCommand.Weapon = world.Atoms["weapon_railgun"   ];
			if (Game.Keyboard.IsKeyDown( Weapon8	)) userCommand.Weapon = world.Atoms["weapon_machinegun"];

			//	http://eliteownage.com/mousesensitivity.html 
			//	Q3A: 16200 dot per 360 turn:
			var vp		=	Game.RenderSystem.DisplayBounds;
			//var ui		=	Game.UserInterface.Instance as ShooterInterface;
			//var cam		=	World.GetView<CameraView>();

			if (frames.TargetFrame==null && !Game.Console.IsShown) {

				Game.Mouse.IsMouseCentered	=	true;
				Game.Mouse.IsMouseClipped	=	true;
				Game.Mouse.IsMouseHidden	=	true;

				userCommand.DYaw		=	-2 * MathUtil.Pi * 5 * Game.Mouse.PositionDelta.X / 16200.0f;
				userCommand.DPitch		=	-2 * MathUtil.Pi * 5 * Game.Mouse.PositionDelta.Y / 16200.0f * ( InvertMouse ? -1 : 1 );

				userCommand.Action		=	flags;
				userCommand.Yaw         +=  userCommand.DYaw;
				userCommand.Pitch       +=  userCommand.DPitch;
				userCommand.Roll		=	0;

			} else {
				userCommand.MoveForward		=	0;
				userCommand.MoveRight		=	0;
				userCommand.MoveUp			=	0;
			
				Game.Mouse.IsMouseCentered	=	false;
				Game.Mouse.IsMouseClipped	=	false;
				Game.Mouse.IsMouseHidden	=	false;
			}
		}

		
	}
}
