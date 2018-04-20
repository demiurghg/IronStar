using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core.Mathematics;
using Fusion;
using Fusion.Engine.Input;

namespace IronStar.Editor2.Controls {

	class TextBox : Frame {

		readonly Func<string> getFunc;
		readonly Action<string> setFunc;

		public Color CursorColor { get; set; } = Color.Gray;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public TextBox ( FrameProcessor fp, Func<string> getFunc, Action<string> setFunc ) : base(fp)
		{ 
			this.getFunc		=	getFunc;
			this.setFunc		=	setFunc;

			this.BackColor		=	ColorTheme.BorderColor;
			this.Width			=	1;
			this.BorderColor	=	ColorTheme.BorderColor;
			this.TextAlignment	=	Alignment.MiddleLeft;

			StatusChanged	+=	TextBox_StatusChanged;

			//KeyDown+=TextBox_KeyDown;
			//KeyUp+=TextBox_KeyUp;
			TypeWrite+=TextBox_TypeWrite;
			Activated+=TextBox_Activated;
			Deactivated+=TextBox_Deactivated;
		}

		private void TextBox_Deactivated( object sender, EventArgs e )
		{
			setFunc( Text );
		}

		private void TextBox_Activated( object sender, EventArgs e )
		{
			ResetSelection();
		}

		private void TextBox_StatusChanged( object sender, StatusEventArgs e )
		{
			switch ( e.Status ) {
				case FrameStatus.None:		ForeColor	=	ColorTheme.TextColorNormal; break;
				case FrameStatus.Hovered:	ForeColor	=	ColorTheme.TextColorHovered; break;
				case FrameStatus.Pushed:	ForeColor	=	ColorTheme.TextColorPushed; break;
			}
		}


		protected override void Update( GameTime gameTime )
		{
			if (Frames.TargetFrame==this) {
			} else {
				Text = getFunc();
			}
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			var value	= getFunc();
			var padRect	= GetPaddedRectangle(true);

			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );

			if (Frames.TargetFrame==this) {

				var r		=	MeasureText();

				var x		=	r.X + selectionStart * 8;
				var y		=	r.Top;
				var w		=	selectionLength * 8;
				var h		=	r.Height;
				var cx		=	selectionLength > 0 ? x + w : x;

				var color	=	CursorColor;
				var alpha	=	(byte)( color.A * (0.7 + 0.3 * Math.Cos(10*gameTime.Total.TotalSeconds) ) );
				var colorC	=	new Color( color.R, color.G, color.B, alpha / 1 );
				var colorS	=	new Color( color.R, color.G, color.B, alpha / 2 );

				spriteLayer.Draw( null, x, y, w, h, colorS, clipRectIndex );
				spriteLayer.Draw( null, x, y, 2, h, colorC, clipRectIndex );

			}
		}



		/*-------------------------------------------------------------------------------------
			* 
			*	Typewriting :
			* 
		-------------------------------------------------------------------------------------*/

		private void TextBox_TypeWrite( object sender, KeyEventArgs e )
		{
			if (e.Ctrl) {
				switch (e.Key) {
					case Keys.C: CopyToClipboard(); break;
					case Keys.X: CopyToClipboard(); ClearSelection(); break;
					case Keys.V: PasteFromClipboard(); break;
				}
				return;
			}

			if (e.Key==Keys.Enter) {	
				setFunc( Text );
				return;
			}

			if (e.Key==Keys.Left) {
				MoveCursor( -1, e.Shift );
				return;
			}

			if (e.Key==Keys.Right) {
				MoveCursor( 1, e.Shift );
				return;
			}

			if (e.Key==Keys.Home) {
				MoveCursor( int.MinValue, e.Shift );
				return;
			}

			if (e.Key==Keys.End) {
				MoveCursor( int.MaxValue, e.Shift );
				return;
			}

			if (e.Key==Keys.Delete) {
				Delete();
				return;
			}

			if (e.Key==Keys.Back) {
				Backspace();
				return;
			}

			if (e.Key==Keys.Tab) {
				return;
			}

			if (e.Key==Keys.Escape) {
				Text = getFunc();
				ResetSelection();
			}

			if (e.Symbol!='\0') {
				InsertText( new string( e.Symbol, 1 ) );
			}
		}


		/*-------------------------------------------------------------------------------------
			* 
			*	Editing stuff :
			* 
		-------------------------------------------------------------------------------------*/

		readonly StringBuilder text = new StringBuilder();
		int selectionStart;
		int selectionLength = 0;

		public override string Text {
			get {
				return text.ToString();
			}

			set {
				if (value==null) {
					text.Clear();
				} else {
					text.Clear();
					text.Insert(0, value);
				}
			}
		}


		void ResetSelection ()
		{
			selectionStart = int.MaxValue;
			selectionLength = 0;
			CheckSelection();
		}



		void CheckSelection ()
		{
			selectionStart	= MathUtil.Clamp( selectionStart, 0, Text.Length );
			selectionLength	= MathUtil.Clamp( selectionLength, -selectionStart, Text.Length-selectionStart );
		}


		void MoveCursor ( int value, bool shift )
		{
			if (shift) {
				if ( selectionStart + value > text.Length ) {
					value = text.Length - selectionStart;
				}
				if ( selectionStart + value < 0 ) {
					value = selectionStart;
				}
				selectionLength -= value;
				selectionStart += value;
			} else {
				selectionLength = 0;
				selectionStart += value;
			}

			CheckSelection();
		}



		void ClearSelection ()
		{
			int start  = selectionLength > 0 ? selectionStart : selectionStart + selectionLength;
			int length = Math.Abs( selectionLength );

			text.Remove( start, length );
			selectionLength = 0;
			CheckSelection();
		}


		string GetSelection ()
		{
			int start  = selectionLength > 0 ? selectionStart : selectionStart + selectionLength;
			int length = Math.Abs( selectionLength );
				
			return text.ToString().Substring( start, length );
		}


		void Backspace ()
		{
			if (selectionLength!=0) {
				ClearSelection();
			} else {
				if (selectionStart>0) {
					text.Remove(selectionStart-1,1);
					selectionStart--;
				}
			}
		}


		void Delete ()
		{
			if (selectionLength!=0) {
				ClearSelection();
			} else {
				if (selectionStart<text.Length) {
					text.Remove(selectionStart,1);
					selectionStart--;
				}
			}
		}


		void InsertText ( string value )
		{
			ClearSelection();
			text.Insert( selectionStart, value );
			MoveCursor( value.Length, false );
		}


		void CopyToClipboard ()
		{
			if (selectionLength!=0) {
				var textToCopy = GetSelection();
				System.Windows.Forms.Clipboard.SetText( textToCopy );
			}
		}


		void PasteFromClipboard ()
		{
			if (System.Windows.Forms.Clipboard.ContainsText()) {
				var textToInsert = System.Windows.Forms.Clipboard.GetText();
				InsertText( textToInsert );
			}
		}
	}
}
