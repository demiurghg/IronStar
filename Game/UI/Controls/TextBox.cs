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
using Fusion.Core;
using Fusion.Widgets.Binding;
using Fusion.Core.Input;

namespace IronStar.UI.Controls {

	public class TextBox : Frame {

		protected readonly IValueBinding binding;

		public Color CursorColor { get; set; } = Color.Gray;

		public event EventHandler ValueChanged;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public TextBox ( UIState ui, IValueBinding binding = null ) : base(ui)
		{ 
			this.binding		=	binding;

			this.Font			=	MenuTheme.NormalFont;

			this.BackColor		=	MenuTheme.BackColor;
			this.Width			=	1;
			this.PaddingRight	=	3;
			this.PaddingLeft	=	3;

			this.TabStop		=	true;

			StatusChanged	+=	TextBox_StatusChanged;

			//KeyDown+=TextBox_KeyDown;
			//KeyUp+=TextBox_KeyUp;
			Click += TextBox_Click;
			TypeWrite+=TextBox_TypeWrite;
			KeyDown+=TextBox_KeyDown;
			KeyUp+=TextBox_KeyUp;
			Activated+=TextBox_Activated;
			Deactivated+=TextBox_Deactivated;
		}


		private void TextBox_Click(object sender, MouseEventArgs e)
		{
			SetCursorFromMouse();
		}


		bool SetValue ( string text )
		{
			ValueChanged?.Invoke(this, EventArgs.Empty);

			if (binding!=null) 
			{
				object value;

				if (StringConverter.TryConvertFromString(binding.ValueType, text, out value))
				{
					if (binding.SetValue(value, ValueSetMode.Default))
					{
						return true;
					}
				}

				Text = GetValue();
				return false;
			}

			return true;
		}


		string GetValue()
		{
			if (binding!=null)
			{
				return StringConverter.ConvertToString( binding.GetValue() );
			}
			else
			{
				return Text;
			}
		}


		private void TextBox_KeyUp( object sender, KeyEventArgs e )
		{
			e.Handled = true;
		}

		private void TextBox_KeyDown( object sender, KeyEventArgs e )
		{
			e.Handled = true;
		}

		private void TextBox_Deactivated( object sender, EventArgs e )
		{
			SetValue( Text );
		}

		private void TextBox_Activated( object sender, EventArgs e )
		{
			ResetSelection();
		}

		private void TextBox_StatusChanged( object sender, StatusEventArgs e )
		{
			switch ( e.Status ) 
			{
				case FrameStatus.None:		ForeColor	=	MenuTheme.TextColorNormal; break;
				case FrameStatus.Hovered:	ForeColor	=	MenuTheme.TextColorHovered; break;
				case FrameStatus.Pushed:	ForeColor	=	MenuTheme.TextColorPushed; break;
			}
		}


		protected override void Update( GameTime gameTime )
		{
		}


		void UpdateTextValueFromBinding()
		{
			if (ui.TargetFrame!=this) 
			{
				Text = GetValue ();
			}
		}
		

		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			UpdateTextValueFromBinding();

			var padRect	= GetPaddedRectangle(true);

			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );

			if (ui.TargetFrame==this) 
			{
				var r		=	ComputeGlobalAlignedTextRectangle();

				var minSel	=	Math.Min( selectionStart, selectionStart + selectionLength );
				var maxSel	=	Math.Max( selectionStart, selectionStart + selectionLength );

				var selRect	=	Font.MeasureSubstring( Text, minSel, maxSel );

				var x		=	r.X + selRect.X; // selectionStart * 8;
				var y		=	r.Top;
				var w		=	selRect.Width; //selectionLength * 8;
				var h		=	r.Height;
				var cx		=	selectionLength < 0 ? x + w : x;

				var color	=	CursorColor;
				var alpha	=	(byte)( color.A * (0.7 + 0.3 * Math.Cos(10*gameTime.Current.TotalSeconds) ) );
				var colorC	=	new Color( color.R, color.G, color.B, alpha / 1 );
				var colorS	=	new Color( color.R, color.G, color.B, alpha / 2 );

				spriteLayer.Draw( null,  x, y, w, h, colorS, clipRectIndex );
				spriteLayer.Draw( null, cx, y, 2, h, colorC, clipRectIndex );
			}
		}



		/*-------------------------------------------------------------------------------------
		* 
		*	Typewriting :
		* 
		-------------------------------------------------------------------------------------*/

		private void TextBox_TypeWrite( object sender, KeyEventArgs e )
		{
			e.Handled = true;

			if (e.Key==Keys.Tab) {
				if (e.Shift) {
					ui.TargetFrame = PrevTabStop();
				} else {
					ui.TargetFrame = NextTabStop();
				}
			}

			if (e.Ctrl) {
				switch (e.Key) {
					case Keys.C: CopyToClipboard(); break;
					case Keys.X: CopyToClipboard(); ClearSelection(); break;
					case Keys.V: PasteFromClipboard(); break;
				}
				return;
			}

			if (e.Key==Keys.Enter) {	
				SetValue( Text );
				Parent.FocusTarget();
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
				if (binding!=null) {
					Text = (string)binding.GetValue();
				}
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

		int selectionStart;
		int selectionLength = 0;

		public override string Text {
			get {
				return base.Text;
			}

			set {
				base.Text = value;
				ResetSelection();
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


		void SetCursorFromMouse ()
		{
			var r = ComputeGlobalAlignedTextRectangle();
			var x = ui.MousePosition.X;
			var y = ui.MousePosition.Y;
			
			var i = Font.FindIndexUnderCursor( Text, x - r.X );

			selectionStart = i;
			selectionLength = 0;

			CheckSelection();
		}


		void MoveCursor ( int value, bool shift )
		{
			if (shift) {

				if ( selectionStart + value > Text.Length ) {
					value = Text.Length - selectionStart;
				}
				if ( selectionStart + value < 0 ) {
					value = -selectionStart;
				}

				if (value>0) {
					selectionLength -= value;
					selectionStart += value;
				} else {
					selectionLength -= value;
					selectionStart += value;
				}
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

			CheckSelection();

			base.Text = Text.Remove( start, length );

			if (selectionLength>0) {
				selectionStart = selectionStart + 0;
			} else {
				selectionStart = selectionStart + selectionLength;
			}
			selectionLength = 0;

			CheckSelection();
		}


		string GetSelection ()
		{
			int start  = selectionLength > 0 ? selectionStart : selectionStart + selectionLength;
			int length = Math.Abs( selectionLength );
				
			CheckSelection();

			return Text.Substring( start, length );
		}


		void Backspace ()
		{
			CheckSelection();

			if (selectionLength!=0) {
				ClearSelection();
			} else {
				if (selectionStart>0) {
					base.Text = base.Text.Remove(selectionStart-1,1);
					selectionStart--;
					CheckSelection();
				}
			}
		}


		void Delete ()
		{
			CheckSelection();

			if (selectionLength!=0) {
				ClearSelection();
			} else {
				if (selectionStart<Text.Length) {
					base.Text = base.Text.Remove(selectionStart,1);
				}
			}
		}


		void InsertText ( string value )
		{
			ClearSelection();
			base.Text = base.Text.Insert( selectionStart, value );
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
