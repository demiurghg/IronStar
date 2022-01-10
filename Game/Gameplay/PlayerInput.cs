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
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Tools;
using Fusion.Engine.Frames;
using IronStar.SinglePlayer;

namespace IronStar.Gameplay 
{
	[ConfigClass]
	public partial class PlayerInput : GameComponent 
	{
		[Config] public static float	Sensitivity		{ get; set; }	=	5;
		[Config] public static bool		InvertMouse		{ get; set; }	=	true;
		[Config] public static float	PullFactor		{ get; set; }	=	1;
		[Config] public static bool		ThirdPerson		{ get; set; }	=	false;
																
		[Config] public static float	ZoomFov			{ get; set; }	=	90.0f;
		[Config] public static float	Fov				{ get; set; }	=	30.0f;
		[Config] public static float	BobHeave		{ get; set; }	=	0.05f;
		[Config] public static float	BobPitch		{ get; set; }	=	1.0f;
		[Config] public static float	BobRoll			{ get; set; }	=	2.0f;
		[Config] public static float	BobStrafe		{ get; set; }	=	5.0f;
		[Config] public static float	BobJump			{ get; set; }	=	5.0f;
		[Config] public static float	BobLand			{ get; set; }	=	5.0f;

		[Config] public static Keys	MoveForward		{ get; set; }	=	Keys.S;
		[Config] public static Keys	MoveBackward	{ get; set; }	=	Keys.Z;	
		[Config] public static Keys	StrafeRight		{ get; set; }	=	Keys.X;	
		[Config] public static Keys	StrafeLeft		{ get; set; }	=	Keys.A;	
		[Config] public static Keys	TurnRight		{ get; set; }	=	Keys.Right;	
		[Config] public static Keys	TurnLeft		{ get; set; }	=	Keys.Left;	
		[Config] public static Keys	LookUp			{ get; set; }	=	Keys.Up;	
		[Config] public static Keys	LookDown		{ get; set; }	=	Keys.Down;	
		[Config] public static Keys	Jump			{ get; set; }	=	Keys.RightButton;	
		[Config] public static Keys	Crouch			{ get; set; }	=	Keys.LeftAlt;	
		[Config] public static Keys	Walk			{ get; set; }	=	Keys.LeftShift;	
																
		[Config] public static Keys	Attack			{ get; set; }	=	Keys.LeftButton;
		[Config] public static Keys	Zoom			{ get; set; }	=	Keys.D;
		[Config] public static Keys	Use				{ get; set; }	=	Keys.LeftControl;

		[Config] public static Keys	MeleeAttack		{ get; set; }	=	Keys.Space;
		[Config] public static Keys	SwitchWeapon	{ get; set; }	=	Keys.Q;	
		[Config] public static Keys	ReloadWeapon	{ get; set; }	=	Keys.R;
		[Config] public static Keys	ThrowGrenade	{ get; set; }	=	Keys.MiddleButton;
																
		[Config] public static Keys	Weapon1			{ get; set; }	=	Keys.D1;
		[Config] public static Keys	Weapon2			{ get; set; }	=	Keys.D2;	
		[Config] public static Keys	Weapon3			{ get; set; }	=	Keys.D3;
		[Config] public static Keys	Weapon4			{ get; set; }	=	Keys.D4;
		[Config] public static Keys	Weapon5			{ get; set; }	=	Keys.D5;
		[Config] public static Keys	Weapon6			{ get; set; }	=	Keys.D6;	
		[Config] public static Keys	Weapon7			{ get; set; }	=	Keys.D7;
		[Config] public static Keys	Weapon8			{ get; set; }	=	Keys.D8;


		public PlayerInput( Game game ) : base( game )
		{
		}



		public void UpdateUserInput ( GameTime gameTime, ref UserCommand userCommand, bool guiEnaged )
		{
			var console	=	Game.GetService<GameConsole>();
			var frames	=	Game.GetService<FrameProcessor>();
			var ui		=	Game.GetService<UserInterface>().Instance;
			
			userCommand.Action		=	UserAction.None;

			if (!ui.AllowGameInput()) 
			{
				return;
			}

			if (Game.Keyboard.IsKeyDown( Keys.Escape )) Game.GetService<Mission>().State.Pause();

			float runFactor		=	1;
			userCommand.Move	=	0;
			userCommand.Strafe	=	0;

			if (Game.Keyboard.IsKeyDown( Walk			)) runFactor = 0.33f;
			if (Game.Keyboard.IsKeyDown( MoveForward	)) userCommand.Move		+= runFactor;
			if (Game.Keyboard.IsKeyDown( MoveBackward	)) userCommand.Move		-= runFactor;
			if (Game.Keyboard.IsKeyDown( StrafeRight	)) userCommand.Strafe	+= runFactor;
			if (Game.Keyboard.IsKeyDown( StrafeLeft		)) userCommand.Strafe	-= runFactor;

			if (Game.Keyboard.IsKeyDown( Jump			)) userCommand.Action |= UserAction.Jump		;
			if (Game.Keyboard.IsKeyDown( Crouch			)) userCommand.Action |= UserAction.Crouch		;

			if (Game.Keyboard.IsKeyDown( Attack			)) userCommand.Action |= guiEnaged ? UserAction.PushGUI : UserAction.Attack;
			if (Game.Keyboard.IsKeyDown( Zoom			)) userCommand.Action |= UserAction.Zoom;
			if (Game.Keyboard.IsKeyDown( Use			)) userCommand.Action |= UserAction.Use;

			if (Game.Keyboard.IsKeyDown( SwitchWeapon	)) userCommand.Action |= UserAction.SwitchWeapon;
			if (Game.Keyboard.IsKeyDown( ThrowGrenade	)) userCommand.Action |= UserAction.ThrowGrenade;
			if (Game.Keyboard.IsKeyDown( MeleeAttack	)) userCommand.Action |= UserAction.MeleeAtack;
			if (Game.Keyboard.IsKeyDown( ReloadWeapon	)) userCommand.Action |= UserAction.ReloadWeapon;

			if (guiEnaged							 ) userCommand.Action |= UserAction.HideWeapon;
			if (Game.Keyboard.IsKeyDown( Weapon1	)) userCommand.Action |= UserAction.Weapon1;
			if (Game.Keyboard.IsKeyDown( Weapon2	)) userCommand.Action |= UserAction.Weapon2;
			if (Game.Keyboard.IsKeyDown( Weapon3	)) userCommand.Action |= UserAction.Weapon3;
			if (Game.Keyboard.IsKeyDown( Weapon4	)) userCommand.Action |= UserAction.Weapon4;
			if (Game.Keyboard.IsKeyDown( Weapon5	)) userCommand.Action |= UserAction.Weapon5;
			if (Game.Keyboard.IsKeyDown( Weapon6	)) userCommand.Action |= UserAction.Weapon6;
			if (Game.Keyboard.IsKeyDown( Weapon7	)) userCommand.Action |= UserAction.Weapon7;
			if (Game.Keyboard.IsKeyDown( Weapon8	)) userCommand.Action |= UserAction.Weapon8;

			//	http://eliteownage.com/mousesensitivity.html 
			//	Q3A: 16200 dot per 360 turn:
			var vp			=	Game.RenderSystem.DisplayBounds;
			var deltaYaw	=	-2 * MathUtil.Pi * Sensitivity * Game.Mouse.PositionDelta.X / 16200.0f;
			var deltaPitch	=	-2 * MathUtil.Pi * Sensitivity * Game.Mouse.PositionDelta.Y / 16200.0f * ( InvertMouse ? -1 : 1 );

			//	Q3A: 360 turn around takes approx 2.5 seconds:
			float dt = gameTime.ElapsedSec;
			if (Game.Keyboard.IsKeyDown( TurnLeft	)) deltaYaw		+= MathUtil.TwoPi / 2.5f * dt;
			if (Game.Keyboard.IsKeyDown( TurnRight	)) deltaYaw		-= MathUtil.TwoPi / 2.5f * dt;
			if (Game.Keyboard.IsKeyDown( LookUp		)) deltaPitch	+= MathUtil.TwoPi / 2.5f * dt;
			if (Game.Keyboard.IsKeyDown( LookDown	)) deltaPitch	-= MathUtil.TwoPi / 2.5f * dt;

			userCommand.Yaw			+=	deltaYaw;
			userCommand.Pitch		+=	deltaPitch;
			userCommand.DeltaYaw	=	deltaYaw;
			userCommand.DeltaPitch	=	deltaPitch;
		}

		
	}
}
