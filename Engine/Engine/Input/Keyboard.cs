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


namespace Fusion.Engine.Input {
	public sealed class Keyboard : GameComponent {

		InputDevice device;


		/// <summary>
		/// Gets keyboard bindings.
		/// </summary>
		public IEnumerable<KeyBind> Bindings {
			get {
				return bindings
					.Select( keyvalue => keyvalue.Value )
					.ToArray();
			}
		}


		Dictionary<Keys, KeyBind> bindings = new Dictionary<Keys,KeyBind>();


		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="Game"></param>
		internal Keyboard ( Game Game ) : base(Game)
		{
			this.device	=	Game.InputDevice;

			device.KeyDown += device_KeyDown;
			device.KeyUp += device_KeyUp;

			device.FormKeyDown += device_FormKeyDown;
			device.FormKeyUp += device_FormKeyUp;
			device.FormKeyPress += device_FormKeyPress;

			Game.Invoker.AddCommands( this );
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize ()
		{
		}



		[Command("bind")]
		string Bind_f (string[] args)
		{
			if (args.Length<3) {
				throw new InvokerException("Usage: bind <key> <key down cmd> [<key up command>]");
			}
		
			Keys key;
			if (!Enum.TryParse(args[1], out key)) {
				throw new InvokerException("Bad key: {0}", args[1]);
			}

			if (IsBound(key)) {
				Unbind(key);
			}

			var keydownCmd	=	args[2];
			var keyupCmd	=	(args.Length>3) ? args[3] : "";

			Bind( key, keydownCmd, keyupCmd );
			
			return null;
		}


		[Command("unbind")]
		string Unbind_f (string[] args)
		{
			if (args.Length<2) {
				throw new InvokerException("Usage: unbind <key>");
			}
		
			Keys key;
			if (!Enum.TryParse(args[1], out key)) {
				throw new InvokerException("Bad key: {0}", args[1]);
			}

			if (IsBound(key)) {
				Unbind(key);
			}
			
			return null;
		}


		[Command("listbinds")]
		string Listbinds_f (string[] args)
		{
			Log.Message("");

			foreach ( var bind in Bindings.OrderBy(b=>b.Key) ) {
				Log.Message("{0,-8} = {1} | {2}", bind.Key, bind.KeyDownCommand, bind.KeyUpCommand );
			}

			Log.Message("{0} keys are bound", Bindings.Count() );
			
			return null;
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
		/// Binds command to key.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="keyDownCommand"></param>
		/// <param name="keyUpCommand"></param>
		public void Bind ( Keys key, string keyDownCommand, string keyUpCommand )
		{
			lock(bindings) {
				bindings.Add( key, new KeyBind(key, keyDownCommand, keyUpCommand ) );
			}
		}



		/// <summary>
		/// Unbind commands from key.
		/// </summary>
		/// <param name="key"></param>
		public void Unbind ( Keys key )
		{
			lock(bindings) {
				bindings.Remove(key);
			}
		}


		/// <summary>
		/// Unbind commands from key.
		/// </summary>
		/// <param name="key"></param>
		public void UnbindAll ( Keys key )
		{
			lock(bindings) {
				bindings.Clear();
			}
		}


		/// <summary>
		/// Indicates that given key is already bound.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsBound ( Keys key )
		{
			lock(bindings) {
				return bindings.ContainsKey( key );
			}
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				device.KeyDown -= device_KeyDown;
				device.KeyUp -= device_KeyUp;

				device.FormKeyDown -= device_FormKeyDown;
				device.FormKeyUp -= device_FormKeyUp;
				device.FormKeyPress -= device_FormKeyPress;
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
			return Game.InputDevice.IsKeyLocked( (Drivers.Input.Keys)key );
		}


		/// <summary>
		/// Returns whether a specified key is currently being pressed. 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyDown ( Keys key )
		{
			return ( scanKeyboard && device.IsKeyDown( (Fusion.Drivers.Input.Keys)key ) );
		}
		

		/// <summary>
		/// Returns whether a specified key is currently not pressed. 
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool IsKeyUp ( Keys key )
		{
			return ( !scanKeyboard || device.IsKeyUp( (Fusion.Drivers.Input.Keys)key ) );
		}


		public event KeyDownEventHandler		KeyDown;
		public event KeyUpEventHandler			KeyUp;
		public event KeyDownEventHandler		FormKeyDown;
		public event KeyUpEventHandler			FormKeyUp;
		public event KeyPressEventHandler		FormKeyPress;



		void device_KeyDown ( object sender, InputDevice.KeyEventArgs e )
		{
			if (!scanKeyboard) {
				return;
			}


			KeyBind bind;
			if (bindings.TryGetValue( (Keys)e.Key, out bind )) {
				try {
					if (!string.IsNullOrWhiteSpace(bind.KeyDownCommand)) {
						Game.Invoker.PushCommand( bind.KeyDownCommand );
					}
				} catch ( Exception cmdLineEx ) {
					Log.Error("{0}", cmdLineEx.Message );
				}
			}

			var handler = KeyDown;
			if (handler!=null) {
				handler( sender, new KeyEventArgs(){ Key = (Keys)e.Key } );
			}
		}


		void device_KeyUp ( object sender, InputDevice.KeyEventArgs e )
		{
			KeyBind bind;
			if (bindings.TryGetValue( (Keys)e.Key, out bind )) {
				try {
					if (!string.IsNullOrWhiteSpace(bind.KeyUpCommand)) {
						Game.Invoker.PushCommand( bind.KeyUpCommand );
					}
				} catch ( Exception cmdLineEx ) {
					Log.Error("{0}", cmdLineEx.Message );
				}
			}

			var handler = KeyUp;
			if (handler!=null) {
				handler( sender, new KeyEventArgs(){ Key = (Keys)e.Key } );
			}
		}


		void device_FormKeyDown ( object sender, InputDevice.KeyEventArgs e )
		{
			var handler = FormKeyDown;
			if (handler!=null) {
				handler( sender, new KeyEventArgs(){ Key = (Keys)e.Key } );
			}
		}

		void device_FormKeyUp ( object sender, InputDevice.KeyEventArgs e )
		{
			var handler = FormKeyUp;
			if (handler!=null) {
				handler( sender, new KeyEventArgs(){ Key = (Keys)e.Key } );
			}
		}

		void device_FormKeyPress ( object sender, InputDevice.KeyPressArgs e )
		{
			var handler = FormKeyPress;
			if (handler!=null) {
				handler( sender, new KeyPressArgs(){ KeyChar = e.KeyChar } );
			}
		}
		



	}
}
