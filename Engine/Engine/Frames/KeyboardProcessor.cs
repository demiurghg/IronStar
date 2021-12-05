using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Input;
using Fusion.Engine.Common;
using System.Diagnostics;

namespace Fusion.Engine.Frames
{
	public class KeyboardProcessor 
	{

		public readonly Game Game;
		public UIState ui;

		public IKeyboardHook KeyboardHook 
		{
			get; set;
		}


		/// <summary>
		/// Gets keyboard delay in milliseconds.
		/// 
		/// The keyboard repeat-delay setting, from 0 (approximately 250 millisecond delay)
		/// through 3 (approximately 1 second delay).
		/// </summary>
		static int KeyboardDelay {
			get {
				int kbDelay = System.Windows.Forms.SystemInformation.KeyboardDelay;
				return (kbDelay+1)*250;
			}
		}

		/// <summary>
		/// Gets delay between repetions in milliseconds.
		/// 
		/// The keyboard repeat-speed setting, from 0 (approximately 2.5 repetitions per
		/// second) through 31 (approximately 30 repetitions per second).
		/// </summary>
		static int KeyboardSpeed {					
			get {
				int kbSpeed = System.Windows.Forms.SystemInformation.KeyboardSpeed;
				int rps		= MathUtil.Lerp( 2500, 30000, kbSpeed/30.0f );
					rps		= MathUtil.Clamp( rps, 2500, 30000 );

				return 1000000/rps;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		internal KeyboardProcessor ( Game game, UIState ui )
		{
			this.Game	=	game;
			this.ui		=	ui;

			Game.Keyboard.KeyDown+=Keyboard_KeyDown; ;
			Game.Keyboard.KeyUp+=Keyboard_KeyUp; ;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void UpdateKeyboard ( GameTime gameTime )
		{
			int deltaTimeMSec = (int)(gameTime.Elapsed.TotalMilliseconds);

			if (repeatTimer>0 && repeatKey!=Keys.None) {
				repeatTimer -= deltaTimeMSec;

				if (repeatTimer<=0) {
					CallTypeWrite( ui.TargetFrame, repeatKey );
					repeatTimer += KeyboardSpeed;
				}
			}
		}



		Keys	repeatKey	=	Keys.None;
		int		repeatTimer	=	0;


		private void Keyboard_KeyDown( object sender, KeyEventArgs e )
		{
			CallKeyDown( ui.TargetFrame, e.Key );

			if (repeatKey!=e.Key) {
				repeatTimer = KeyboardDelay;
				repeatKey = e.Key;
				CallTypeWrite( ui.TargetFrame, e.Key );
			}
		}


		private void Keyboard_KeyUp( object sender, KeyEventArgs e )
		{
			CallKeyUp( ui.TargetFrame, e.Key );

			if (repeatKey==e.Key) {
				repeatKey = Keys.None;
			}
		}



		bool IsShiftDown()
		{
			return Game.Keyboard.IsKeyDown(Keys.LeftShift) || Game.Keyboard.IsKeyDown(Keys.RightShift);
		}

		bool IsAltDown()
		{
			return Game.Keyboard.IsKeyDown(Keys.LeftAlt) || Game.Keyboard.IsKeyDown(Keys.RightAlt);
		}

		bool IsControlDown()
		{
			return Game.Keyboard.IsKeyDown(Keys.LeftControl) || Game.Keyboard.IsKeyDown(Keys.RightControl);
		}

		
		void CallKeyDown ( Frame frame, Keys key )
		{
			bool hooked = false;
			bool shift	= IsShiftDown();
			bool alt	= IsAltDown();
			bool ctrl	= IsControlDown();
			
			if (KeyboardHook!=null) {
				hooked = KeyboardHook.KeyDown( key, shift, alt, ctrl );
			}

			if (!hooked) {
				frame?.OnKeyDown( key, shift, alt, ctrl );
			}
		}
		

		void CallKeyUp ( Frame frame, Keys key )
		{
			bool hooked = false;
			bool shift	= IsShiftDown();
			bool alt	= IsAltDown();
			bool ctrl	= IsControlDown();

			if (KeyboardHook!=null) {
				hooked = KeyboardHook.KeyUp( key, shift, alt, ctrl );
			}

			if (!hooked) {
				frame?.OnKeyUp( key, shift, alt, ctrl );
			}
		}


		void CallTypeWrite ( Frame frame, Keys key )
		{											
			char ch		= GetCharCode( ref key, IsShiftDown() );

			bool hooked = false;
			bool shift	= IsShiftDown();
			bool alt	= IsAltDown();
			bool ctrl	= IsControlDown();

			if (KeyboardHook!=null) {
				hooked = KeyboardHook.TypeWrite( key, ch, shift, alt, ctrl );
			}

			if (!hooked) {
				frame?.OnTypeWrite( key, ch, shift, alt, ctrl );
			}
		}



		char NumLock( ref Keys key, bool numLock, char ch, Keys nlKey )
		{
			if (numLock) {
				return ch;
			} else {
				key = nlKey;
				return '\0';
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="shift"></param>
		/// <returns></returns>
		char GetCharCode ( ref Keys key, bool shift )
		{
			bool capsLock = Game.Keyboard.IsKeyLocked( Keys.CapsLock );
			bool numLock  = Game.Keyboard.IsKeyLocked( Keys.NumLock );

			bool caps	= shift ^ capsLock;

			char result		=	'\0';

			switch (key) {

				case Keys.Enter: return '\r';
				case Keys.Back:  return '\b';
				case Keys.Tab:	 return '\t';

				case Keys.NumPad0: return NumLock( ref key, numLock ^ shift, '0', Keys.Insert	);
				case Keys.NumPad1: return NumLock( ref key, numLock ^ shift, '1', Keys.End		);
				case Keys.NumPad2: return NumLock( ref key, numLock ^ shift, '2', Keys.Down		);
				case Keys.NumPad3: return NumLock( ref key, numLock ^ shift, '3', Keys.PageDown );
				case Keys.NumPad4: return NumLock( ref key, numLock ^ shift, '4', Keys.Left		);
				case Keys.NumPad5: return NumLock( ref key, numLock ^ shift, '5', Keys.None		);
				case Keys.NumPad6: return NumLock( ref key, numLock ^ shift, '6', Keys.Right	);
				case Keys.NumPad7: return NumLock( ref key, numLock ^ shift, '7', Keys.Home		);
				case Keys.NumPad8: return NumLock( ref key, numLock ^ shift, '8', Keys.Up		);
				case Keys.NumPad9: return NumLock( ref key, numLock ^ shift, '9', Keys.PageUp	);
				case Keys.Decimal: return NumLock( ref key, numLock ^ shift, '.', Keys.Delete	);
				
				case Keys.Divide:	return '/';
				case Keys.Multiply: return '*';
				case Keys.Add:		return '+';
				case Keys.Subtract: return '-';

				case Keys.D1:	return shift ? '!' : '1';
				case Keys.D2:	return shift ? '@' : '2';
				case Keys.D3:	return shift ? '#' : '3';
				case Keys.D4:	return shift ? '$' : '4';
				case Keys.D5:	return shift ? '%' : '5';
				case Keys.D6:	return shift ? '^' : '6';
				case Keys.D7:	return shift ? '&' : '7';
				case Keys.D8:	return shift ? '*' : '8';
				case Keys.D9:	return shift ? '(' : '9';
				case Keys.D0:	return shift ? ')' : '0';

				case Keys.OemTilde:			return shift ? '~' : '`'  ;
				case Keys.OemMinus:			return shift ? '_' : '-'  ;	
				case Keys.OemPlus:			return shift ? '+' : '='  ;
				case Keys.OemOpenBrackets:	return shift ? '{' : '['  ;
				case Keys.OemCloseBrackets: return shift ? '}' : ']'  ;
				case Keys.OemPipe:			return shift ? '|' : '\\' ;
				case Keys.OemSemicolon:		return shift ? ':' : ';'  ;
				case Keys.OemQuotes:		return shift ? '"' : '\'' ;
				case Keys.OemComma:			return shift ? '<' : ','  ;
				case Keys.OemPeriod:		return shift ? '>' : '.'  ;
				case Keys.OemQuestion:		return shift ? '?' : '/'  ;

				case Keys.Space: return ' ';
													  
				case Keys.A: return caps ? 'A' : 'a'; 
				case Keys.B: return caps ? 'B' : 'b'; 
				case Keys.C: return caps ? 'C' : 'c'; 
				case Keys.D: return caps ? 'D' : 'd'; 
				case Keys.E: return caps ? 'E' : 'e'; 
				case Keys.F: return caps ? 'F' : 'f'; 
				case Keys.G: return caps ? 'G' : 'g'; 
				case Keys.H: return caps ? 'H' : 'h'; 
				case Keys.I: return caps ? 'I' : 'i'; 
				case Keys.J: return caps ? 'J' : 'j'; 
				case Keys.K: return caps ? 'K' : 'k'; 
				case Keys.L: return caps ? 'L' : 'l'; 
				case Keys.M: return caps ? 'M' : 'm'; 
				case Keys.N: return caps ? 'N' : 'n'; 
				case Keys.O: return caps ? 'O' : 'o'; 
				case Keys.P: return caps ? 'P' : 'p'; 
				case Keys.Q: return caps ? 'Q' : 'q'; 
				case Keys.R: return caps ? 'R' : 'r'; 
				case Keys.S: return caps ? 'S' : 's'; 
				case Keys.T: return caps ? 'T' : 't'; 
				case Keys.U: return caps ? 'U' : 'u'; 
				case Keys.V: return caps ? 'V' : 'v'; 
				case Keys.W: return caps ? 'W' : 'w'; 
				case Keys.X: return caps ? 'X' : 'x'; 
				case Keys.Y: return caps ? 'Y' : 'y'; 
				case Keys.Z: return caps ? 'Z' : 'z'; 
			}

			return result;
		}
	}
}
