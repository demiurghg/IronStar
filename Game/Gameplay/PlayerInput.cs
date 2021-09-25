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
	public partial class PlayerInput : GameComponent 
	{
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


		public PlayerInput( Game game ) : base( game )
		{
		}



		public void UpdateUserInput ( GameTime gameTime, ref UserCommand userCommand )
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

			if (Game.Keyboard.IsKeyDown( Attack			)) userCommand.Action |= UserAction.Attack;
			if (Game.Keyboard.IsKeyDown( Zoom			)) userCommand.Action |= UserAction.Zoom;
			if (Game.Keyboard.IsKeyDown( Use			)) userCommand.Action |= UserAction.Use;

			if (Game.Keyboard.IsKeyDown( SwitchWeapon	)) userCommand.Action |= UserAction.SwitchWeapon;
			if (Game.Keyboard.IsKeyDown( ThrowGrenade	)) userCommand.Action |= UserAction.ThrowGrenade;
			if (Game.Keyboard.IsKeyDown( MeleeAttack	)) userCommand.Action |= UserAction.MeleeAtack;
			if (Game.Keyboard.IsKeyDown( ReloadWeapon	)) userCommand.Action |= UserAction.ReloadWeapon;

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
			var deltaYaw	=	-2 * MathUtil.Pi * 5 * Game.Mouse.PositionDelta.X / 16200.0f;
			var deltaPitch	=	-2 * MathUtil.Pi * 5 * Game.Mouse.PositionDelta.Y / 16200.0f * ( InvertMouse ? -1 : 1 );

			userCommand.Yaw			+=	deltaYaw;
			userCommand.Pitch		+=	deltaPitch;
			userCommand.DeltaYaw	=	deltaYaw;
			userCommand.DeltaPitch	=	deltaPitch;
		}

		
	}
}
