using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;

namespace IronStar.Editor2.Controls {

	public class ListBox : Frame {

		object[] items;

		readonly int itemHeight = 8;

		public event EventHandler	SelectedItemChanged;

		/// <summary>
		/// Negative value means no selection
		/// </summary>
		public int SelectedIndex {	
			get { return selectedIndex; }
			private set {
				if (selectedIndex!=value) {
					selectedIndex = value;
					SelectedItemChanged?.Invoke( this, EventArgs.Empty );
				}
			}
		}
		int selectedIndex = -1;

		/// <summary>
		/// Gets selected item value
		/// </summary>
		public object SelectedItem { 
			get {
				return ( SelectedIndex < 0 ) ? null : items[ SelectedIndex ];
			}
		}

		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public ListBox ( FrameProcessor fp, IEnumerable<object> items ) : base(fp)
		{
			this.items	=	items.ToArray();

			BorderColor	=	Color.Zero; // ColorTheme.BorderColor;
			BackColor	=	Color.Zero; // ColorTheme.BackgroundColorDark;
			ForeColor	=	ColorTheme.TextColorNormal;
			Border		=	0;
			Padding		=	0;

			this.MouseMove  +=ListBox_MouseMove;
			this.MouseIn+=ListBox_MouseIn;
			this.MouseOut+=ListBox_MouseOut;
			this.Click+=ListBox_Click;
			this.MouseDown +=ListBox_MouseDown;
		}

		int mouseX;
		int mouseY;
		bool mouseIn;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="items"></param>
		public void SetItems ( IEnumerable<object> items )
		{
			this.items		=	items.ToArray();
			SelectedIndex	=	-1;
			Parent?.MakeLayoutDirty();
		}

		
		private void ListBox_MouseMove( object sender, MouseEventArgs e )
		{
			mouseX = e.X;
			mouseY = e.Y;
		}


		private void ListBox_MouseOut( object sender, MouseEventArgs e )
		{
			mouseIn = false;
		}


		private void ListBox_MouseIn( object sender, MouseEventArgs e )
		{
			mouseIn = true;
		}

		private void ListBox_MouseDown( object sender, MouseEventArgs e )
		{
			int index = GetItemIndexUnderCursor(e.X, e.Y);
			SelectedIndex = index;
			SelectedItemChanged?.Invoke( this, EventArgs.Empty );
		}


		private void ListBox_Click( object sender, MouseEventArgs e )
		{
			int index = GetItemIndexUnderCursor(e.X, e.Y);
			SelectedIndex = index;
			SelectedItemChanged?.Invoke( this, EventArgs.Empty );
		}


		public override int Height {
			get {
				return (items.Length * itemHeight) + BorderTop + BorderBottom + PaddingBottom + PaddingTop;
			}
			set {
			}
		}


		int GetItemIndexUnderCursor (int x, int y)
		{
			if (!mouseIn) {
				return -1;
			}

			int index = (mouseY - BorderTop - PaddingTop) / itemHeight;
			if (index < 0 || index >= items.Length) {
				index = -1;
			}
			return index;
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			var gp			=	GetPaddedRectangle();
			var count		=	items.Length;
			var hoverIdx	=	GetItemIndexUnderCursor(mouseX, mouseY);

			for (int i=0; i<count; i++) {

				var text	=	items[i]?.ToString() ?? "(null)";
				var hovered	=	false;
				var selected=	false;
				int x		=	gp.X;
				int y		=	gp.Y + i * itemHeight;
				int h		=	itemHeight;
				int w		=	gp.Width;
				var rect	=	new Rectangle(x,y,w,h);
				int yLocal	=	i * itemHeight;

				if ( i == hoverIdx ) {
					hovered = true;
				}

				if ( i == SelectedIndex ) {
					hovered	 = false;
					selected = true;
				}

				var textColor	=	hovered ? ColorTheme.TextColorHovered : ColorTheme.TextColorNormal;
				var highlight	=	ColorTheme.HighlightColor;

				if (selected) {

					spriteLayer.Draw( null, rect, ColorTheme.TextColorNormal, clipRectIndex );
					spriteLayer.DrawDebugString( x, y, text, ColorTheme.BackgroundColorDark, clipRectIndex );

				} else if (hovered) {

					spriteLayer.Draw( null, rect, ColorTheme.HighlightColor, clipRectIndex );
					spriteLayer.DrawDebugString( x, y, text, ColorTheme.TextColorHovered, clipRectIndex );

				} else {

					if ((i&1)==1) {
						spriteLayer.Draw( null, rect, new Color(255,255,255,4), clipRectIndex );
					}
					spriteLayer.DrawDebugString( x, y, text, ColorTheme.TextColorNormal, clipRectIndex );

				}
			}
		}
	}
}
