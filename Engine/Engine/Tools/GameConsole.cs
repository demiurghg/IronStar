//#define USE_PROFONT
//#define USE_COURIER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fusion.Core;
using Fusion.Core.Input;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Configuration;
using Fusion.Engine.Graphics;
using Fusion.Core.Shell;
using System.IO;
using Fusion.Engine.Frames;
using NLog;

namespace Fusion.Engine.Tools {
	
	public sealed partial class GameConsole : GameComponent, IKeyboardHook 
	{
		class Line 
		{
			public readonly TraceEventType EventType;
			public readonly string Message;

			public Line ( TraceEventType eventType, string message ) 
			{
				EventType	=	eventType;
				Message		=	message;
			}
		}
		
		List<string> lines = new List<string>();

		const string FontName = "conchars";
		UserTexture	consoleFont;
		SpriteLayer consoleLayer;
		SpriteLayer editLayer;
		SpriteLayer	debugTextLayer;

		readonly string releaseInfo;
		

		float showFactor = 0;
		string font;

		EditBox	editBox;


		int scroll = 0;

		bool isShown = false;

		/// <summary>
		/// Show/Hide console.
		/// </summary>
		public bool IsShown { get { return isShown; } }


		Suggestion suggestion = null;


		class DebugString
		{
			public Color Color;
			public string Text;
		}

		readonly List<DebugString> debugStrings = new List<DebugString>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="Game"></param>
		/// <param name="font"></param>
		public GameConsole ( Game Game ) : base(Game)
		{
			SetDefaults();

			this.font		=	FontName;

			editBox		=	new EditBox(this);

			releaseInfo	=	Game.GetReleaseInfo();
		}



		/// <summary>
		/// 
		/// </summary>
		public override void Initialize ()
		{
			editBox.FeedHistory( GetHistory() );

			var rs = Game.GetService<RenderSystem>();

			consoleLayer	=	new SpriteLayer( Game.RenderSystem, 6 * 50 * 50 );
			editLayer		=	new SpriteLayer( Game.RenderSystem, 1024 );
			debugTextLayer	=	new SpriteLayer( Game.RenderSystem, 1024 );
			consoleLayer.Order = LayerOrder;
			consoleLayer.Layers.Add( editLayer );

			rs.SpriteLayers.Add( debugTextLayer );
			rs.SpriteLayers.Add( consoleLayer );

			LoadContent();
			Game.Reloading += (s,e) => LoadContent();

			Game.GraphicsDevice.DisplayBoundsChanged += GraphicsDevice_DisplayBoundsChanged;
			Log.MessageLogged += TraceRecorder_TraceRecorded;

			Game.GetService<FrameProcessor>().Keyboard.KeyboardHook = this;

			using ( var ms = new MemoryStream( Properties.Resources.conchars ) ) {
				consoleFont		=   UserTexture.CreateFromTga( Game.RenderSystem, ms, false );
			}


			RefreshConsole();
			RefreshEdit();
		}


		int charHeight { get { return 9; } }
		int charWidth { get { return 8; } }


		/// <summary>
		/// Gets root console's sprite layer
		/// </summary>
		public SpriteLayer ConsoleSpriteLayer {
			get {
				return consoleLayer;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		void LoadContent ()
		{
			RefreshConsole();
		}



		public void Show ()
		{
			isShown	=	true;
		}


		public void Hide ()
		{
			isShown = false;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				Game.GraphicsDevice.DisplayBoundsChanged -= GraphicsDevice_DisplayBoundsChanged;
				Log.MessageLogged -= TraceRecorder_TraceRecorded;

				SafeDispose( ref consoleFont );

				SafeDispose( ref consoleLayer );
				SafeDispose( ref editLayer );
			}

			base.Dispose( disposing );
		}


		void DrawString ( SpriteLayer layer, int x, int y, string text, Color color )
		{
			#if USE_PROFONT
			consoleFont.DrawString( layer, text, x,y + consoleFont.BaseLine, color );
			#else
			layer.DrawDebugString( x, y, text, color );
			#endif
		}


		public void DrawDebugText( Color color, string frmt, params object[] args )
		{
			debugStrings.Add( new DebugString() { Color = color, Text = string.Format(frmt,args) } );
		}


		void DrawDebugText()
		{
			debugTextLayer.Clear();

			for (int i=0; i<debugStrings.Count; i++)
			{
				var ds = debugStrings[i];
				DrawString( debugTextLayer, 4+1, 4 + i*9+1, ds.Text, Color.Black );
				DrawString( debugTextLayer, 4+0, 4 + i*9+0, ds.Text, ds.Color );
			}

			debugStrings.Clear();
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public override void Update ( GameTime gameTime )
		{
			var vp		=	Game.GraphicsDevice.DisplayBounds;

			DrawDebugText();

			RefreshConsoleLayer();

			if (isShown) {
				showFactor = MathUtil.Clamp( showFactor + FallSpeed * gameTime.ElapsedSec, 0,1 );
			} else {															   
				showFactor = MathUtil.Clamp( showFactor - FallSpeed * gameTime.ElapsedSec, 0,1 );
			}

			consoleLayer.Visible	=	showFactor > 0;
			consoleLayer.Order		=	LayerOrder;

			//Log.Message("{0} {1}", showFactor, Show);
			float offset	=	(int)(- (vp.Height / 2+1) * (1 - showFactor));

			consoleLayer.SetTransform( new Vector2(0, offset), Vector2.Zero, 0 );
			editLayer.SetTransform( 0, vp.Height/2 - charHeight );

			Color cursorColor = CmdLineColor;
			cursorColor.A = (byte)( cursorColor.A * (0.5 + 0.5 * Math.Cos( 2 * CursorBlinkRate * Math.PI * gameTime.Current.TotalSeconds ) > 0.5 ? 1 : 0 ) );

			editLayer.Clear();

			//consoleFont.DrawString( editLayer, "]" + editBox.Text, 0,0, Config.CmdLineColor );
			//consoleFont.DrawString( editLayer, "_", charWidth + charWidth * editBox.Cursor, 0, cursorColor );
			var text	=	"]" + editBox.Text;
			var color	=	CmdLineColor;

			DrawString( editLayer, charWidth/2, -charHeight/2,										text, color );
			DrawString( editLayer, charWidth + charWidth/2 + charWidth * editBox.Cursor,	-charHeight/2,  "_", cursorColor );


			var version = releaseInfo;
			DrawString( editLayer, vp.Width - charWidth/2 - charWidth * version.Length, -charHeight - charHeight/2, version, VersionColor);

			var frameRate = string.Format("fps = {0,7:0.00}", gameTime.Fps);
			DrawString( editLayer, vp.Width - charWidth/2 - charWidth * frameRate.Length, -charHeight/2, frameRate, VersionColor);

			
			//
			//	Draw suggestions :
			//	
			if (isShown && suggestion!=null && suggestion.Candidates.Any()) {

				var candidates = suggestion.Candidates;

				var x = 0;
				var y = charHeight+1;
				var w = (candidates.Max( s => s.Length ) + 4) * charWidth;
				var h = (candidates.Count() + 1) * charHeight;

				w = Math.Max( w, charWidth * 16 );

				editLayer.Draw( null, x, y, w, h, BackColor );

				int line = 0;
				foreach (var candidate in candidates ) {
					DrawString( editLayer, x + charWidth + charWidth/2, y + charHeight * line, candidate, HelpColor );
					line ++;
				}
			}
		}



		/// <summary>
		/// Refreshes edit box.
		/// </summary>
		void RefreshEdit ()
		{
		}


		bool dirty = true;


		void RefreshConsoleLayer ()
		{
			if (!dirty) {
				return;
			}

			var vp		=	Game.GraphicsDevice.DisplayBounds;

			int cols	=	vp.Width / charWidth;
			int rows	=	vp.Height / charHeight / 2;

			int count = 1;

			consoleLayer.Clear();

			//	add small gap below command line...
			consoleLayer.Draw( null, 0,0, vp.Width, vp.Height/2+1, BackColor );

			var lines	=	Log.MemoryLog;

			scroll	=	MathUtil.Clamp( scroll, 0, lines.Count() );

			/*var info = Game.GetReleaseInfo();
			consoleFont.DrawString( consoleLayer, info, vp.Width - consoleFont.MeasureString(info).Width, vp.Height/2 - 1 * charHeight, ErrorColor );*/


			foreach ( var line in lines.Reverse().Skip(scroll) ) 
			{
				var color	= Color.Gray;
				var level	= line.Item1;
				var text	= line.Item2;

				if ( level==LogLevel.Info	) color =  MessageColor;
				if ( level==LogLevel.Error	) color =  ErrorColor;  
				if ( level==LogLevel.Warn	) color =  WarningColor;
				if ( level==LogLevel.Debug	) color =  DebugColor;  
				if ( level==LogLevel.Trace	) color =  TraceColor;
				if ( level==LogLevel.Fatal	) color =  ErrorColor;
				
				DrawString( consoleLayer, charWidth/2, vp.Height/2 - (count+2) * charHeight, text, color );

				if (count>rows) {
					break;
				}

				count++;
			}

			dirty = false;
		}


		/// <summary>
		/// Refreshes console layer.
		/// </summary>
		void RefreshConsole ()
		{
			dirty	=	true;
		}




		void ExecCmd ()
		{
			try {
				var cmd  = editBox.Text;
				Log.Message("]{0}", cmd);

				Game.Invoker.ExecuteString(cmd);

			} catch ( CommandLineParserException pe ) {
				Log.Error(pe.Message);
			} catch ( InvokerException ie ) {
				Log.Error(ie.Message);
			}
		}


		string AutoComplete ()
		{
			var sw = new Stopwatch();
			sw.Start();
			suggestion = Game.Invoker.AutoComplete( editBox.Text );
			sw.Stop();

			if (suggestion.Candidates.Any()) {
				suggestion.Add("");
				suggestion.Add(string.Format("({0} ms)", sw.Elapsed.TotalMilliseconds));
			}

			return suggestion.CommandLine;
		}



		void TabCmd ()
		{
			editBox.Text = AutoComplete();
		}

		
		const char Tilde = (char)'`';
		const char Backspace = (char)8;
		const char Enter = (char)13;
		const char Escape = (char)27;
		const char Tab = (char)9;


		void TraceRecorder_TraceRecorded ( object sender, EventArgs e )
		{
			RefreshConsole();
			scroll	=	0;
		}


		void GraphicsDevice_DisplayBoundsChanged ( object sender, EventArgs e )
		{
			RefreshConsole();
		}


		public bool KeyDown( Keys key, bool shift, bool alt, bool ctrl )
		{
			if (key==Keys.OemTilde) {
				isShown = !isShown;
				return true;
			}
			return isShown;
		}

		public bool KeyUp( Keys key, bool shift, bool alt, bool ctrl )
		{
			return isShown;
		}

		public bool TypeWrite( Keys key, char keyChar, bool shift, bool alt, bool ctrl )
		{
			if (!isShown) {
				return (key==Keys.OemTilde);
			}

			switch (key) {
				case Keys.End		: editBox.Move(int.MaxValue/2); break;
				case Keys.Home		: editBox.Move(int.MinValue/2); break;
				case Keys.Left		: editBox.Move(-1); break;
				case Keys.Right		: editBox.Move( 1); break;
				case Keys.Delete	: editBox.Delete(); break;
				case Keys.Up		: editBox.Prev(); break;
				case Keys.Down		: editBox.Next(); break;
				case Keys.PageUp	: scroll += 2; dirty = true; break;
				case Keys.PageDown	: scroll -= 2; dirty = true; break;
			}

			switch (keyChar) {
				case Tilde		: break;
				case Backspace	: editBox.Backspace(); break;
				case Enter		: ExecCmd(); editBox.Enter(); break;
				case Escape		: break;
				case Tab		: TabCmd(); break;
				default			: editBox.TypeChar( keyChar ); break;
			}

			var newText = AutoComplete();

			RefreshEdit();

			return true;
		}
	}
}
