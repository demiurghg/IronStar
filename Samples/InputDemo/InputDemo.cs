using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Input;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace SpriteDemo
{
	class SpriteDemo : Game
	{
		SpriteLayer	spriteLayer;


		public SpriteDemo( string gameId, string gameTitle ) : base( gameId, gameTitle )
		{
			this.AddServiceAndComponent( 100, new RenderSystem(this, true) );
		}


		protected override void Initialize()
		{
			base.Initialize();

			spriteLayer	=	new SpriteLayer( RenderSystem, 1024 );

			RenderSystem.SpriteLayers.Add( spriteLayer );

			Keyboard.KeyDown    += Keyboard_KeyDown;
			Keyboard.KeyUp+=Keyboard_KeyUp;
			Mouse.Scroll+=Mouse_Scroll;
			Mouse.Move+=Mouse_Move;
		}

		private void Mouse_Move( object sender, MouseMoveEventArgs e )
		{
			AddEventHistory("Mouse Move : X = {0}, Y = {1}", e.Position.X, e.Position.Y );
		}

		private void Mouse_Scroll( object sender, MouseScrollEventArgs e )
		{
			AddEventHistory("Mouse Scroll : Wheel Delta = {0}", e.WheelDelta );
		}

		private void Keyboard_KeyDown( object sender, KeyEventArgs e )
		{
			AddEventHistory("KeyDown : {0}", e.Key);
		}

		private void Keyboard_KeyUp( object sender, KeyEventArgs e )
		{
			AddEventHistory("KeyUp   : {0}", e.Key);
		}

		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				//	dispose disposable stuff here
			}

			base.Dispose( disposing );
		}


		List<Tuple<Color,string>> debugStrings = new List<Tuple<Color, string>>();
		List<string>	eventHistory = new List<string>();

		void AddDebugString( Color color, string format, params object[] args )
		{
			debugStrings.Add( Tuple.Create( color, string.Format( format, args ) ) );
		}


		void AddSplitter()
		{
			debugStrings.Add( Tuple.Create( Color.Black, "" ) );
		}


		void AddEventHistory( string format, params object[] args )
		{
			eventHistory.Add( string.Format( format, args ) );

			if (eventHistory.Count>16)
			{
				eventHistory.RemoveAt(0);
			}
		}


		protected override void Update( GameTime gameTime )
		{
			if (Keyboard.IsKeyDown(Keys.Escape) && (Keyboard.IsKeyDown(Keys.LeftShift) || Keyboard.IsKeyDown(Keys.RightShift)))
			{
				Exit();
			}

			debugStrings.Clear();


			spriteLayer.Projection	=	Matrix.OrthoOffCenterRH(0,0,1024,768,-1,1);
			spriteLayer.Clear();

			AddDebugString( Color.Orange, "Input Demo - FPS = {0}", gameTime.Fps );
			AddSplitter();

			AddDebugString( Color.White,  "[Shift+ESC] - Exit" );
			AddDebugString( Color.White,  "[ D ]       - Lock Mouse" );
			AddDebugString( Color.White,  "[ C ]       - Clip Mouse" );
			AddDebugString( Color.White,  "[ H ]       - Hide Mouse" );

			AddSplitter();
			AddDebugString( IsActive ? Color.Lime : Color.Red, IsActive ? "ACTIVE" : "INACTIVE");

			PrintMouseInfo();

			PrintKeyboardInfo();

			PrintGamePadInfo(0);
			PrintGamePadInfo(1);
			PrintGamePadInfo(2);
			PrintGamePadInfo(3);

			int line = 0;
			foreach ( var s in debugStrings )
			{
				spriteLayer.DrawDebugString( 16, 8 + line * 8, s.Item2, s.Item1 );
				line++;
			}

			line = 0;
			foreach ( var s in eventHistory )
			{
				spriteLayer.DrawDebugString( 320, 8 + line * 8, s, Color.Gray );
				line++;
			}
		}


		/*-----------------------------------------------------------------------------------------------
		 *	GAMEPAD :
		-----------------------------------------------------------------------------------------------*/

		void PrintGamePadInfo( int gamepadIndex )
		{
			var gamepad		= Gamepads[ gamepadIndex ];
			var isConnected	= gamepad.IsConnected;

			int x = 16 + gamepadIndex * 160;
			int y = 400;

			spriteLayer.DrawDebugString( x, y += 8, "Gamepad " + gamepadIndex.ToString(), isConnected ? Color.White : Color.Red );
				y += 8;

			if (isConnected)
			{
				var ls = gamepad.LeftStick;
				var rs = gamepad.RightStick;
				spriteLayer.DrawDebugString( x, y += 8, string.Format("LS : {0,5:0.00} {1,5:0.00}",	ls.X, ls.Y ), Color.Gray );
				spriteLayer.DrawDebugString( x, y += 8, string.Format("RS : {0,5:0.00} {1,5:0.00}",	rs.X, rs.Y ), Color.Gray );
				spriteLayer.DrawDebugString( x, y += 8, string.Format("LT : {0,5:0.00}",			gamepad.LeftTrigger ), Color.Gray );
				spriteLayer.DrawDebugString( x, y += 8, string.Format("RT : {0,5:0.00}",			gamepad.RightTrigger ), Color.Gray );

				gamepad.SetVibration( gamepad.LeftTrigger, gamepad.RightTrigger );

				y += 8;
				spriteLayer.DrawDebugString( x, y += 8, "Buttons:", Color.Gray );

				var buttons = Enum.GetValues( typeof(GamepadButtons) ).Cast<GamepadButtons>();

				foreach (var button in buttons )
				{
					if (gamepad.IsKeyPressed( button ))
					{
						spriteLayer.DrawDebugString( x, y += 8, button.ToString(), Color.Gray );
					}
				}
			}
			/*spriteLayer.DrawDebugString( 16 + gamepadIndex * 200, 400 + 0, 
				string.Format("Gamepad #{0}: {1}", gamepadIndex, isConnected ? "Connected" : "Disconnected"), isConnected ? Color.Lime : Color.Red );*/
		}

		/*-----------------------------------------------------------------------------------------------
		 *	MOUSE :
		-----------------------------------------------------------------------------------------------*/

		float accumulatedX = 0;
		float accumulatedY = 0;

		void PrintMouseInfo()
		{
			var pos		=	Mouse.Position;

			accumulatedX	+=	Mouse.PositionDelta.X;
			accumulatedY	+=	Mouse.PositionDelta.X;

			AddSplitter();
			AddDebugString( Color.Orange, "MOUSE" );

			Mouse.IsMouseCentered	=	Keyboard.IsKeyDown(Keys.D);
			Mouse.IsMouseClipped	=	Keyboard.IsKeyDown(Keys.C);
			Mouse.IsMouseHidden		=	Keyboard.IsKeyDown(Keys.H);

			AddDebugString( Color.White, "POS: {0}, {1}", pos.X, pos.Y );
			AddDebugString( Color.White, "ACC: {0}, {1}", accumulatedX, accumulatedY );

			string buttons = string.Join(" ", new [] { 
				Keys.LeftButton, Keys.MiddleButton, Keys.RightButton, 
				Keys.MouseButtonX1, Keys.MouseButtonX2, Keys.MouseButtonX3 }
				.Where( key0 => Keyboard.IsKeyDown(key0) )
				.Select( key1 => key1.ToString() )
				);

			AddDebugString( Color.White, "BTN: {0}", buttons );
		}


		/*-----------------------------------------------------------------------------------------------
		 *	KEYBOARD :
		-----------------------------------------------------------------------------------------------*/

		private void PrintKeyboardInfo()
		{
			//	Functional Keys:
			ShowKey (-0.5f, 0.0f, Keys.Escape, "ESC" );
			
			ShowKeys(-0.5f, 2.0f,	Keys.F1, Keys.F2,  Keys.F3,  Keys.F4 );
			ShowKeys(-0.5f, 6.5f,	Keys.F5, Keys.F6,  Keys.F7,  Keys.F8 );
			ShowKeys(-0.5f,11.0f,	Keys.F9, Keys.F10, Keys.F11, Keys.F12 ); 

			//	Numerical Keys
			ShowKey ( 1.0f,  0.0f, Keys.OemTilde,	"~" );
			ShowKeys( 1.0f,  1.0f, Keys.D1, Keys.D2,  Keys.D3,  Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0 );
			ShowKey ( 1.0f, 11.0f, Keys.OemMinus, "-" );
			ShowKey ( 1.0f, 12.0f, Keys.OemPlus,  "+" );
			ShowKey ( 1.0f, 14.0f, Keys.Back, "BS" );

			//	Alphabetical keys #1
			ShowKey ( 2.0f,  0.0f, Keys.Tab, "TAB" );
			ShowKeys( 2.0f,  1.5f, Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P );
			ShowKey ( 2.0f, 11.5f, Keys.OemOpenBrackets, @"[" ); 
			ShowKey ( 2.0f, 12.5f, Keys.OemCloseBrackets, @"]" ); 
			ShowKey ( 2.0f, 14.0f, Keys.OemPipe, @"\" ); 

			//	Alphabetical Keys #2
			ShowKey ( 3.0f,  0.0f, Keys.CapsLock, "CAPS" );
			ShowKeys( 3.0f,  2.0f, Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L );
			ShowKey ( 3.0f, 11.0f, Keys.OemSemicolon, ";" ); 
			ShowKey ( 3.0f, 12.0f, Keys.OemQuotes, "\"" ); 
			ShowKey ( 3.0f, 14.0f, Keys.Enter, "ENTER" ); 

			//	Alphabetical Keys #3
			ShowKey ( 4.0f,  0.0f, Keys.LeftShift, "SHFT" );
			ShowKeys( 4.0f,  2.5f, Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M );
			ShowKey ( 4.0f,  9.5f, Keys.OemComma,    "," );
			ShowKey ( 4.0f, 10.5f, Keys.OemPeriod,   "." );
			ShowKey ( 4.0f, 11.5f, Keys.OemQuestion, "?" );
			ShowKey ( 4.0f, 13.5f, Keys.RightShift, "SHFT" );

			//	Alphabetical Keys #3
			ShowKey ( 5.0f,  0.0f, Keys.LeftControl,	"CTRL" );
			ShowKey ( 5.0f,  3.0f, Keys.LeftAlt,		"ALT" );
			ShowKey ( 5.0f,  7.0f, Keys.Space,			"S P A C E" );
			ShowKey ( 5.0f, 11.0f, Keys.RightAlt,		"ALT" );
			ShowKey ( 5.0f, 14.0f, Keys.RightControl,	"CTRL" );

			//	Special  Keys
			ShowKey(-0.5f, 16.0f, Keys.PrintScreen,	"PS" );
			ShowKey(-0.5f, 17.0f, Keys.Scroll,		"SL" );
			ShowKey(-0.5f, 18.0f, Keys.Pause,		"PB" );

			ShowKey( 1.0f, 16.0f, Keys.Insert,		"INS" );
			ShowKey( 1.0f, 17.0f, Keys.Home,		"HM"  );
			ShowKey( 1.0f, 18.0f, Keys.PageUp,		"PU"  );

			ShowKey( 2.0f, 16.0f, Keys.Delete,		"DEL" );
			ShowKey( 2.0f, 17.0f, Keys.End,			"END" );
			ShowKey( 2.0f, 18.0f, Keys.PageDown,	"PD"  );

			//	Direction Keys :
			ShowKey( 4.0f, 17.0f, Keys.Up,		"UP" );

			ShowKey( 5.0f, 16.0f, Keys.Left,	"LT" );
			ShowKey( 5.0f, 17.0f, Keys.Down,	"DN" );
			ShowKey( 5.0f, 18.0f, Keys.Right,	"RT" );

			//	NumPad :
			ShowKey( 1.0f, 20.0f, Keys.NumLock,		"NUM");
			ShowKey( 1.0f, 21.0f, Keys.Divide,		"/"  );
			ShowKey( 1.0f, 22.0f, Keys.Multiply,	"*"  );
			ShowKey( 1.0f, 23.0f, Keys.Subtract,	"-"  );

			ShowKey( 2.0f, 20.0f, Keys.NumPad7,		"7"  );
			ShowKey( 2.0f, 21.0f, Keys.NumPad8,		"8"  );
			ShowKey( 2.0f, 22.0f, Keys.NumPad9,		"9"  );
			ShowKey( 3.0f, 20.0f, Keys.NumPad4,		"4"  );
			ShowKey( 3.0f, 21.0f, Keys.NumPad5,		"5"  );
			ShowKey( 3.0f, 22.0f, Keys.NumPad6,		"6"  );
			ShowKey( 4.0f, 20.0f, Keys.NumPad1,		"1"  );
			ShowKey( 4.0f, 21.0f, Keys.NumPad2,		"2"  );
			ShowKey( 4.0f, 22.0f, Keys.NumPad3,		"3"  );

			ShowKey( 5.0f, 20.5f, Keys.NumPad0,		"0"  );
			ShowKey( 5.0f, 22.0f, Keys.Decimal,		","  );

			ShowKey( 2.5f, 23.0f, Keys.Add,			"+"  );
			ShowKey( 4.5f, 23.0f, Keys.Enter,		"ENT"  );
		}

		void ShowKey( float level, float offset, Keys key, string name = null )
		{
			var keyDown	 = Keyboard.IsKeyDown(key);
			var keyName  = name ?? key.ToString();
			var printX   = 32  + offset * 32 - keyName.Length * 4;
			var printY   = 200 + level  * 24;
			var keyColor = keyDown ? Color.White : new Color(64,64,64);
			spriteLayer.DrawDebugString( printX, printY, keyName, keyColor );
		}

		void ShowKeys( float level, float offset, params Keys[] keys )
		{
			for (int i=0; i<keys.Length; i++)
			{
				var key		= keys[i];

				if (key!=Keys.None)
				{
					ShowKey( level, offset + i, key, null );
				}
			}
		}
	}
}
