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

namespace Fusion.Widgets 
{
	public class TextBox : Frame 
	{
		protected readonly StringBindingWrapper binding;

		public Color CursorColor { get; set; } = Color.Gray;

		public event EventHandler ValueChanged;

		bool editingMode = false;

		public bool CommitEditsOnDeactivation = false;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public TextBox ( FrameProcessor fp, IValueBinding binding = null ) : base(fp)
		{ 
			this.binding		=	new StringBindingWrapper( binding, "" );

			this.Font			=	ColorTheme.NormalFont;

			this.BackColor		=	ColorTheme.BackgroundColorDark;
			this.Width			=	1;
			this.Border			=	1;
			this.BorderColor	=	ColorTheme.BorderColor;
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
			EnterEdits();
			SetCursorFromMouse();
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
			if (CommitEditsOnDeactivation)
			{
				CommitEdits();
			}
			else
			{
				CancelEdits();
			}

			BorderColor	=	ColorTheme.BorderColor;
		}

		private void TextBox_Activated( object sender, EventArgs e )
		{
			EnterEdits();
			BorderColor = ColorTheme.FocusColor;
		}

		private void TextBox_StatusChanged( object sender, StatusEventArgs e )
		{
			switch ( e.Status ) 
			{
				case FrameStatus.None:		ForeColor	=	ColorTheme.TextColorNormal; break;
				case FrameStatus.Hovered:	ForeColor	=	ColorTheme.TextColorHovered; break;
				case FrameStatus.Pushed:	ForeColor	=	ColorTheme.TextColorPushed; break;
			}
		}


		protected override void Update( GameTime gameTime )
		{
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			if (!editingMode)
			{
				base.Text	=	binding.GetValue();
			}

			var padRect	= GetPaddedRectangle(true);

			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );

			if (editingMode) 
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
		*	Data binding logic :
		-------------------------------------------------------------------------------------*/

		void EnterEdits()
		{
			if (!editingMode)
			{
				Log.Message("Enter Edit Mode");
				Text		=	binding.GetValue();
				editingMode	=	true;
			}
		}

		void CommitEdits()
		{
			if (editingMode)
			{
				Log.Message("Commit Edits");
				binding.SetValue(Text, ValueSetMode.Default);
				editingMode	=	false;
			}
		}


		void CancelEdits()
		{
			if (editingMode)
			{
				Log.Message("Cancel Edits");
				Text		=	binding.GetValue();
				editingMode	=	false;
			}
		}

		/*-------------------------------------------------------------------------------------
		*	Typewriting :
		-------------------------------------------------------------------------------------*/

		private void TextBox_TypeWrite( object sender, KeyEventArgs e )
		{
			e.Handled = true;

			if (e.Key==Keys.Enter) 
			{	
				if (editingMode) 
				{
					CommitEdits();
				}
				else 
				{
					EnterEdits();
				}
				//Parent.FocusTarget();
				return;
			}

			if (e.Key==Keys.Escape) 
			{
				CancelEdits();
				ResetSelection();
			}

			if (e.Key==Keys.Tab) 
			{
				CommitEdits();
				if (e.Shift) 
				{
					Frames.TargetFrame = PrevTabStop();
				} 
				else 
				{
					Frames.TargetFrame = NextTabStop();
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
				base.Text  = value;
				binding.SetValue( value, ValueSetMode.Default );
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
			var x = Frames.MousePosition.X;
			var y = Frames.MousePosition.Y;
			
			var i = Font.FindIndexUnderCursor( Text, x - r.X );

			selectionStart = i;
			selectionLength = 0;

			CheckSelection();
		}


		void MoveCursor ( int value, bool shift )
		{
			if (shift) 
			{
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
			CheckSelection();

			int start  = selectionLength > 0 ? selectionStart : selectionStart + selectionLength;
			int length = Math.Abs( selectionLength );

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
