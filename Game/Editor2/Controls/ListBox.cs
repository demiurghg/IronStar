using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core;
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
			this.items		=	items.ToArray();

			BorderColor		=	ColorTheme.BorderColorLight;
			BackColor		=	ColorTheme.BackgroundColorDark;
			ForeColor		=	ColorTheme.TextColorNormal;
			Border			=	1;
			Padding			=	1;

			this.MouseMove  +=	ListBox_MouseMove;
			this.MouseIn	+=	ListBox_MouseIn;
			this.MouseOut	+=	ListBox_MouseOut;
			this.Click		+=	ListBox_Click;
			this.MouseDown	+=	ListBox_MouseDown;
			this.MouseWheel +=	ListBox_MouseWheel;

			UpdateScroll();
		}

		int mouseX;
		int mouseY;
		bool mouseIn;

		int scrollOffset = 0;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="items"></param>
		public void SetItems ( IEnumerable<object> items )
		{
			this.items		=	items.ToArray();
			SelectedIndex	=	-1;
			Parent?.MakeLayoutDirty();
			UpdateScroll();
		}


		private void ListBox_MouseWheel( object sender, MouseEventArgs e )
		{
			int sign = Math.Sign( e.Wheel );
			scrollOffset -= 2 * itemHeight * sign;
			UpdateScroll();
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
			/*int index = GetItemIndexUnderCursor(e.X, e.Y);
			SelectedIndex = index;
			SelectedItemChanged?.Invoke( this, EventArgs.Empty );  */
		}


		int GetItemIndexUnderCursor (int x, int y)
		{
			if (!mouseIn) {
				return -1;
			}

			int index = (mouseY - BorderTop - PaddingTop + scrollOffset) / itemHeight;
			if (index < 0 || index >= items.Length) {
				index = -1;
			}
			return index;
		}


		int ContentHeight {
			get {
				return itemHeight * items.Length;
			}
		}


		Rectangle scrollRect;

		void UpdateScroll ()
		{
			var gp			=	GetPaddedRectangle();
			var height		=	Math.Max( ContentHeight, 1 );

			//	content is completely inside of the frame
			//	set scrollOffset zero and return
			if (height<=gp.Height) {
				scrollOffset = 0;
				return;
			}

			var maxDelta	=	height - gp.Height;
			
			scrollOffset	=	MathUtil.Clamp( scrollOffset, 0, maxDelta );

			var markerHeight	=	(gp.Height    * gp.Height * 2 + 1) / Math.Max( gp.Height, height ) / 2 + 1;
			var markerOffset	=	(scrollOffset * gp.Height * 2 + 1) / Math.Max( gp.Height, height ) / 2;

			scrollRect.Width	=	ColorTheme.ScrollSize;
			scrollRect.Height	=	markerHeight;
			scrollRect.X		=	gp.X + gp.Width - scrollRect.Width;
			scrollRect.Y		=	gp.Y + markerOffset;
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			UpdateScroll();

			var gp			=	GetPaddedRectangle();
			var count		=	items.Length;
			var hoverIdx	=	GetItemIndexUnderCursor(mouseX, mouseY);

			var	drawScroll	=	ContentHeight > gp.Height;

			for (int i=0; i<count; i++) {

				var text	=	items[i]?.ToString() ?? "(null)";
				var hovered	=	false;
				var selected=	false;
				int x		=	gp.X;
				int y		=	gp.Y + i * itemHeight - scrollOffset;
				int h		=	itemHeight;
				int w		=	gp.Width;

				if (drawScroll) {
					w -= (ColorTheme.ScrollSize + 1);
				}

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

				if (drawScroll) {
					spriteLayer.Draw( null, scrollRect, ColorTheme.ScrollMarkerColor, clipRectIndex );
				}

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
