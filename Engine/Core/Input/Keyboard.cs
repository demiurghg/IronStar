using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Drivers.Input;
using Fusion.Engine.Common;
using Fusion.Core.Shell;
using Fusion.Core.IniParser.Model;


namespace Fusion.Core.Input {

	public sealed class Keyboard : DisposableBase {

		readonly Game game;
		readonly InputDevice device;

		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="Game"></param>
		internal Keyboard ( Game game )
		{
			this.game	=	game;
			this.device	=	game.InputDevice;

			device.KeyDown += device_KeyDown;
			device.KeyUp += device_KeyUp;
		}


		/// <summary>
		/// 
		/// </summary>
		public void Initialize ()
		{
		}


		/// <summary>
		/// Indicates whether keyboard should be scanned.
		/// If ScanKeyboard equals false methods IsKeyDown and IsKeyUp indicate that all keys are unpressed.
		/// All events like FormKeyPress, FormKeyDown, FormKeyUp will work.
		/// </summary>
		public bool ScanKeyboard {
			get {
				return scanKeyboard;
			}
			set {
				if (value!=scanKeyboard) {
					if (value) {
						scanKeyboard = true;
					} else {
						scanKeyboard = false;
						device.RemoveAllPressedKeys();
					}
				}
			}
		}

		bool scanKeyboard = true;



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				device.KeyDown -= device_KeyDown;
				device.KeyUp -= device_KeyUp;
			}

			base.Dispose( disposing );
		}


		/// <summary>
		/// Returns whether a specified key is currently being locked.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyLocked ( Keys key )
		{
			return game.InputDevice.IsKeyLocked(key);
		}


		/// <summary>
		/// Returns whether a specified key is currently being pressed. 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyDown ( Keys key )
		{
			return ( scanKeyboard && device.IsKeyDown(key) );
		}
		

		/// <summary>
		/// Returns whether a specified key is currently not pressed. 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyUp ( Keys key )
		{
			return ( !scanKeyboard || device.IsKeyUp( key ) );
		}


		public event KeyDownEventHandler		KeyDown;
		public event KeyUpEventHandler			KeyUp;
		//public event KeyDownEventHandler		FormKeyDown;
		//public event KeyUpEventHandler			FormKeyUp;
		//public event KeyPressEventHandler		FormKeyPress;


		void device_KeyDown ( object sender, InputDevice.KeyEventArgs e )
		{
			if (!scanKeyboard) {
				return;
			}

			KeyDown?.Invoke(sender, new KeyEventArgs() { Key = (Keys)e.Key });
		}


		void device_KeyUp ( object sender, InputDevice.KeyEventArgs e )
		{
			KeyUp?.Invoke(sender, new KeyEventArgs() { Key = (Keys)e.Key });
		}
	}
}
