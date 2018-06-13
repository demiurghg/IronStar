﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.RawInput;
using Device = SharpDX.RawInput.Device;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
//using FRBTouch.MultiTouch;
//using FRBTouch.MultiTouch.Win32Helper;
//using SharpDX.DirectInput;


namespace Fusion.Core.Input {

	internal sealed class InputDevice : DisposableBase {

		public	Vector2		RelativeMouseOffset		{ get; private set; }
		public	Vector2		GlobalMouseOffset		{ get; private set; }
		public	Point		MousePosition			{ get { return new Point( (int)GlobalMouseOffset.X, (int)GlobalMouseOffset.Y ); } }
		public	int			TotalMouseScroll		{ get; private set; }
		public	bool		IsMouseCentered			{ get; set; }
		public	bool		IsMouseClipped			{ get; set; }
		public	bool		IsMouseHidden			{ get; set; }
		public	int			MouseWheelScrollLines	{ get { return System.Windows.Forms.SystemInformation.MouseWheelScrollLines; } }
		public	int			MouseWheelScrollDelta	{ get { return System.Windows.Forms.SystemInformation.MouseWheelScrollDelta; } }
		
		HashSet<Keys>		pressed = new HashSet<Keys>();

		public delegate void MouseMoveHandlerDelegate	( object sender, MouseMoveEventArgs e );
		public delegate void MouseScrollEventHandler	( object sender, MouseScrollEventArgs e );
		public delegate void KeyDownEventHandler		( object sender, KeyEventArgs e );
		public delegate void KeyUpEventHandler			( object sender, KeyEventArgs e );
		public delegate void KeyPressEventHandler		( object sender, KeyPressArgs e );


		public class KeyEventArgs : EventArgs {
			public Keys	Key;
		}

		public class KeyPressArgs : EventArgs {
			public char	KeyChar;
		}

		public class MouseScrollEventArgs : EventArgs {
			/// <summary>
			/// See: InputDevice.MouseWheelScrollDelta.
			/// </summary>
			public int WheelDelta;
		}

		public class MouseMoveEventArgs : EventArgs {
			public Vector2	Position;
		}

		public event MouseMoveHandlerDelegate	MouseMove;
		public event MouseScrollEventHandler	MouseScroll;
		public event KeyDownEventHandler		KeyDown;
		public event KeyUpEventHandler			KeyUp;

		[Obsolete]
		public event KeyDownEventHandler		FormKeyDown;
		[Obsolete]
		public event KeyUpEventHandler			FormKeyUp;
		[Obsolete]
		public event KeyPressEventHandler		FormKeyPress;

		public class TouchEventArgs : EventArgs {

			public TouchEventArgs ( int id, Point location )
			{
				PointerID	=	id;
				Location	=	location;
			}
			
			public int PointerID;
			public Point Location;
		}



		public event Action<Vector2> TouchGestureTap;
		public event Action<Vector2> TouchGestureDoubleTap;
		public event Action<Vector2> TouchGestureSecondaryTap;
		public event Action<Vector2, Vector2, float> TouchGestureManipulate;



		static class NativeMethods {
			public static Forms.Cursor LoadCustomCursor(string path) 
			{
				IntPtr hCurs =	LoadCursorFromFile(path);

				if (hCurs == IntPtr.Zero) {
					throw new Win32Exception();
				}
				
				var curs	=	new Forms.Cursor(hCurs);
				// Note: force the cursor to own the handle so it gets released properly
				var fi		=	typeof(Forms.Cursor).GetField("ownHandle", BindingFlags.NonPublic | BindingFlags.Instance);
				
				fi.SetValue(curs, true);
				return curs;
			}

			[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
			private static extern IntPtr LoadCursorFromFile(string path);


			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
			public static extern short GetKeyState(int keyCode);
		}		



		public readonly Game Game;


		/// <summary>
		/// Constrcutor
		/// </summary>
		internal InputDevice ( Game game )
		{
			this.Game		=	game;
		}



		internal void Initialize ()
		{
			IsMouseHidden	=	false;
			IsMouseCentered	=	false;
			IsMouseClipped	=	false;

            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericMouse, DeviceFlags.None);
            Device.MouseInput += new EventHandler<MouseInputEventArgs>(MouseHandler);

            Device.RegisterDevice(UsagePage.Generic, UsageId.GenericKeyboard, DeviceFlags.None);
            Device.KeyboardInput += new EventHandler<KeyboardInputEventArgs>(KeyboardHandle);

			if (Game.GraphicsDevice.Display.Window != null && !Game.GraphicsDevice.Display.Window.IsDisposed) {
				var p				= Game.GraphicsDevice.Display.Window.PointToClient(Forms.Cursor.Position);
				GlobalMouseOffset	= new Vector2(p.X, p.Y);
			}
		}


		
		/// <summary>
		/// Disposes stuff
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				Device.KeyboardInput	-= KeyboardHandle;
				Device.MouseInput		-= MouseHandler;

				SetCursorVisibility(true);
				Forms.Cursor.Clip		=	new Drawing.Rectangle( int.MinValue, int.MinValue, int.MaxValue, int.MaxValue );
			}

			base.Dispose(disposing);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyLocked ( Keys key )
		{
			switch ( key ) {
				case Keys.CapsLock: return (((ushort)NativeMethods.GetKeyState(0x14)) & 0xffff) != 0;
				case Keys.NumLock:  return (((ushort)NativeMethods.GetKeyState(0x90)) & 0xffff) != 0;
				case Keys.Scroll:   return (((ushort)NativeMethods.GetKeyState(0x91)) & 0xffff) != 0;
				default: return false;
			}
		}


		/// <summary>
		/// Adds key to hash list and fires KeyDown event
		/// </summary>
		/// <param name="key"></param>
		void AddPressedKey ( Keys key )
		{
			if (!Game.IsActive) {
				return;
			}

			if (pressed.Add( key )) {
				KeyDown?.Invoke( this, new KeyEventArgs() { Key = key } );
			}
		}



		/// <summary>
		/// Removes key from hash list and fires KeyUp event
		/// </summary>
		/// <param name="key"></param>
		void RemovePressedKey ( Keys key )
		{
			if (pressed.Remove( key )) {
				KeyUp?.Invoke( this, new KeyEventArgs() { Key = key } );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		internal void RemoveAllPressedKeys()
		{
			foreach ( var key in pressed ) {
				KeyUp?.Invoke( this, new KeyEventArgs() { Key = key } );
			}
			pressed.Clear();
		}


		/// <summary>
		/// Loads cursor image from file
		/// </summary>
		/// <param name="path"></param>
		public void SetCursorImage ( string path )
		{
			NativeMethods.LoadCustomCursor( path );
		}



		/// <summary>
		///	Mouse handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void MouseHandler ( object sender, MouseInputEventArgs e )
		{
			if (Game.GraphicsDevice.Display.Window != null && !Game.GraphicsDevice.Display.Window.IsDisposed) {

				var p				= Game.GraphicsDevice.Display.Window.PointToClient(Forms.Cursor.Position);
			
				GlobalMouseOffset	= new Vector2(p.X, p.Y);

				MouseMove?.Invoke( this, new MouseMoveEventArgs() { Position = GlobalMouseOffset } );
			}


			//Console.WriteLine( "{0} {1} {2}", e.X, e.Y, e.ButtonFlags.ToString() );

			RelativeMouseOffset += new Vector2( e.X, e.Y );

			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.LeftButtonDown		) ) AddPressedKey( Keys.LeftButton );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.RightButtonDown	) )	AddPressedKey( Keys.RightButton );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.MiddleButtonDown	) )	AddPressedKey( Keys.MiddleButton );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.Button4Down		) )	AddPressedKey( Keys.MouseButtonX1 );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.Button5Down		) )	AddPressedKey( Keys.MouseButtonX2 );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.LeftButtonUp		) ) RemovePressedKey( Keys.LeftButton );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.RightButtonUp		) )	RemovePressedKey( Keys.RightButton );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.MiddleButtonUp		) )	RemovePressedKey( Keys.MiddleButton );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.Button4Up			) )	RemovePressedKey( Keys.MouseButtonX1 );
			if ( e.ButtonFlags.HasFlag( MouseButtonFlags.Button5Up			) )	RemovePressedKey( Keys.MouseButtonX2 );

			if ( Game.IsActive ) {
				if ( MouseScroll!=null && e.WheelDelta!=0 ) {
					MouseScroll( this, new MouseScrollEventArgs(){ WheelDelta = e.WheelDelta } );
				}
				TotalMouseScroll	+=	e.WheelDelta;
			}
		}


		
		/// <summary>
		/// Keyboard handler
		/// In general: http://molecularmusings.wordpress.com/2011/09/05/properly-handling-keyboard-input/ 
		/// L/R shift:  http://stackoverflow.com/questions/5920301/distinguish-between-left-and-right-shift-keys-using-rawinput
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void KeyboardHandle ( object sender, KeyboardInputEventArgs e )
		{
		    Keys	key = (Keys)e.Key;

			bool E0		=	e.ScanCodeFlags.HasFlag( ScanCodeFlags.E0 );
			bool E1		=	e.ScanCodeFlags.HasFlag( ScanCodeFlags.E1 );
			bool Make	=	e.ScanCodeFlags.HasFlag( ScanCodeFlags.Make );
			bool Break	=	e.ScanCodeFlags.HasFlag( ScanCodeFlags.Break );

			if (e.Key==Forms.Keys.Menu) {
				key = E0 ? Keys.RightAlt : Keys.LeftAlt;
			}

			if (e.Key==Forms.Keys.ControlKey) {
				key = E0 ? Keys.RightControl : Keys.LeftControl;
			}

			if (e.Key==Forms.Keys.ShiftKey) {
				if ( e.MakeCode==0x2a ) key = Keys.LeftShift;
				if ( e.MakeCode==0x36 ) key = Keys.RightShift;
			}

			if (!E0) {
				if ( e.Key==Forms.Keys.Insert	)	key	=	Keys.NumPad0;
				if ( e.Key==Forms.Keys.End		)	key	=	Keys.NumPad1;
				if ( e.Key==Forms.Keys.Down		)	key	=	Keys.NumPad2;
				if ( e.Key==Forms.Keys.PageDown	)	key	=	Keys.NumPad3;
				if ( e.Key==Forms.Keys.Left		)	key	=	Keys.NumPad4;
				if ( e.Key==Forms.Keys.Clear	)	key	=	Keys.NumPad5;
				if ( e.Key==Forms.Keys.Right	)	key	=	Keys.NumPad6;
				if ( e.Key==Forms.Keys.Home		)	key	=	Keys.NumPad7;
				if ( e.Key==Forms.Keys.Up		)	key	=	Keys.NumPad8;
				if ( e.Key==Forms.Keys.PageUp	)	key	=	Keys.NumPad9;
			}

			if (Enum.IsDefined(typeof(Keys), key)) {
				if (Break) {
					if ( pressed.Contains( key ) ) RemovePressedKey( key );
				} else {
					if ( !pressed.Contains( key ) ) AddPressedKey( key );
				}
			}
		}



		bool cursorHidden = false;

		/// <summary>
		/// Sets cursor visibility
		/// </summary>
		/// <param name="visible"></param>
		void SetCursorVisibility ( bool visible )
		{
			if (visible) {
				if (cursorHidden) {
					Forms.Cursor.Show();
					cursorHidden = false;
				}
			} else {
				if (!cursorHidden) {
					Forms.Cursor.Hide();
					cursorHidden = true;
				}
			}
		}



		/// <summary>
		/// Frame
		/// </summary>
		internal void UpdateInput ()
		{
			if ( Game.GraphicsDevice.Display.Window!=null ) {

			    if ( Game.IsActive ) {

			        System.Drawing.Rectangle rect = Game.GraphicsDevice.Display.Window.ClientRectangle;

					if (IsMouseCentered) {
						Forms.Cursor.Position	=	Game.GraphicsDevice.Display.Window.PointToScreen( new Drawing.Point( rect.Width/2, rect.Height/2 ) );
					}

					if (IsMouseClipped) {
						Forms.Cursor.Clip		=	Game.GraphicsDevice.Display.Window.RectangleToScreen( rect );
					} else {
				        Forms.Cursor.Clip		=	new Drawing.Rectangle( int.MinValue, int.MinValue, int.MaxValue, int.MaxValue );
					}

					SetCursorVisibility( !IsMouseHidden );

			    } else {

			        Forms.Cursor.Clip		=	new Drawing.Rectangle( int.MinValue, int.MinValue, int.MaxValue, int.MaxValue );
					RelativeMouseOffset		=	Vector2.Zero;
					SetCursorVisibility( true );

					RemoveAllPressedKeys();
			    }
			}

			Gamepad.Update();
		}




		/// <summary>
		/// Should be called after everything is updated
		/// </summary>
		internal void EndUpdateInput ()
		{
			RelativeMouseOffset = Vector2.Zero;
		}



		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Form handling :
		 * 
		-----------------------------------------------------------------------------------------*/

		internal void NotifyMouseDown ( Keys key, int x, int y )
		{
			GlobalMouseOffset = new Vector2(x,y);
			AddPressedKey( key );
		}

		[Obsolete]
		internal void NotifyKeyDown ( Keys key, bool alt, bool shift, bool control )
		{
			FormKeyDown?.Invoke( this, new KeyEventArgs() { Key = key } );
		}


		[Obsolete]
		internal void NotifyKeyUp  ( Keys key, bool alt, bool shift, bool control )
		{
			FormKeyUp?.Invoke( this, new KeyEventArgs() { Key = key } );
		}


		[Obsolete]
		internal void NotifyKeyPress ( char keyChar )
		{
			FormKeyPress?.Invoke( this, new KeyPressArgs() { KeyChar = keyChar } );
		}


		public void NotifyTouchTap(Vector2 tapPosition)
		{
			TouchGestureTap?.Invoke( tapPosition );
		}

		public void NotifyTouchDoubleTap(Vector2 tapPosition)
		{
			TouchGestureDoubleTap?.Invoke( tapPosition );
		}


		public void NotifyTouchSecondaryTap(Vector2 tapPosition)
		{
			TouchGestureSecondaryTap?.Invoke( tapPosition );
		}


		public void NotifyTouchManipulation(Vector2 center, Vector2 delta, float scale)
		{
			TouchGestureManipulate?.Invoke( center, delta, scale );
		}


		/// <summary>
		/// Checks whether key is down
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyDown ( Keys key, bool ignoreInputMode = true )
		{
			return ( pressed.Contains( key ) );
		}



		/// <summary>
		/// Checks whether key is down
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyUp ( Keys key )
		{
			return !IsKeyDown( key );
		}
	}
}