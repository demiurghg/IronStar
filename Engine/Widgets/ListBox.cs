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
using Fusion.Widgets.Binding;

namespace Fusion.Widgets 
{
	public class ListBox : Frame 
	{
		readonly HashSet<int> selection = new HashSet<int>();
		private int itemHeight { get { return Font.LineHeight; } }

		public bool AllowMultipleSelection 
		{
			get { return allowMultiSelection; }
			set { allowMultiSelection = value; ResetSelection(); }
		}

		public event EventHandler	SelectedItemChanged;

		public IListBinding Binding 
		{ 
			get { return binding; }
			set { binding = value; }
		}

		public INameProvider NameProvider 
		{ 
			get { return nameProvider; }
			set { nameProvider = value; }
		}

		bool allowMultiSelection;
		IListBinding binding = null;
		INameProvider nameProvider = null;


		public int SelectedIndex { get { return selection.Any() ? selection.Last() : -1; } }
		public object SelectedItem { get { return ( SelectedIndex < 0 ) ? null : binding[ SelectedIndex ]; } }

		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public ListBox ( FrameProcessor fp, IEnumerable<object> items, Func<object,string> nameConverter = null ) : base(fp)
		{
			Binding			=	new ListBinding( items );
			NameProvider	=	new NameProvider( nameConverter );

			Font			=	ColorTheme.Monospaced;

			BorderColor		=	ColorTheme.BorderColorLight;
			BackColor		=	ColorTheme.BackgroundColorDark;
			ForeColor		=	ColorTheme.TextColorNormal;
			Border			=	1;
			Padding			=	1;

			this.MouseMove  +=	ListBox_MouseMove;
			this.MouseIn	+=	ListBox_MouseIn;
			this.MouseOut	+=	ListBox_MouseOut;
			this.MouseDown	+=	ListBox_MouseDown;
			this.MouseWheel +=	ListBox_MouseWheel;

			UpdateScroll();
		}

		int mouseX;
		int mouseY;
		bool mouseIn;

		int scrollOffset = 0;


		public void SetItems ( IEnumerable<object> items )
		{
			Binding			=	new ListBinding(items);
			ResetSelection();
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
			int index	= GetItemIndexUnderCursor(e.X, e.Y);
			var toggle	= e.Ctrl && AllowMultipleSelection;
			var range	= e.Shift && AllowMultipleSelection;

			if (index<0)
			{
				ResetSelection();
			}
			else
			{
				SetSelection( index, toggle, range );
			}
		}


		void SetSelection( int index, bool toggle, bool range )
		{
			if (!toggle && !range) SingleSelection(index);
			if (toggle && !range) ToggleSelection(index);
			if (range) AddSelectionRange(index);

			SelectedItemChanged?.Invoke( this, EventArgs.Empty );
		}

		public void ResetSelection()
		{
			selection.Clear();
			SelectedItemChanged?.Invoke( this, EventArgs.Empty );
		}


		void SingleSelection(int index)
		{
			selection.Clear();
			selection.Add(index);
		}

		void ToggleSelection(int index)
		{
			if (selection.Contains( index )) selection.Remove( index );
										else selection.Add( index );
		}

		void AddSelectionRange(int index)
		{
			int begin = Math.Min(SelectedIndex, index);
			int end   = Math.Max(SelectedIndex, index);

			for (int i=begin; i<=end; i++)
			{
				selection.Add(i);
			}
		}


		int GetItemIndexUnderCursor (int x, int y)
		{
			if (!mouseIn) 
			{
				return -1;
			}

			int index = (mouseY - BorderTop - PaddingTop + scrollOffset) / itemHeight;
			
			if (index < 0 || index >= binding.Count) 
			{
				index = -1;
			}
			
			return index;
		}


		int ContentHeight 
		{
			get { return itemHeight * binding.Count; }
		}


		Rectangle scrollRect;

		void UpdateScroll ()
		{
			var gp			=	GetPaddedRectangle();
			var height		=	Math.Max( ContentHeight, 1 );

			//	content is completely inside of the frame
			//	set scrollOffset zero and return
			if (height<=gp.Height) 
			{
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



		void DrawText ( SpriteLayer spriteLayer, int x, int y, string text, Color color, int clipRectIndex )
		{
			//spriteLayer.DrawDebugString( x, y, text, color, clipRectIndex );
			Font.DrawString( spriteLayer, text, x,y, color, clipRectIndex, 0, false, false );
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			UpdateScroll();

			var gp			=	GetPaddedRectangle();
			var count		=	binding.Count;
			var hoverIdx	=	GetItemIndexUnderCursor(mouseX, mouseY);

			var	drawScroll	=	ContentHeight > gp.Height;

			for (int i=0; i<count; i++) 
			{
				var text	=	NameProvider.GetDisplayName( binding[i] );
				var hovered	=	false;
				var selected=	false;
				int x		=	gp.X;
				int y		=	gp.Y + i * itemHeight - scrollOffset;
				int h		=	itemHeight;
				int w		=	gp.Width;

				if (drawScroll) 
				{
					w -= (ColorTheme.ScrollSize + 1);
				}

				var rect	=	new Rectangle(x,y,w,h);
				int yLocal	=	i * itemHeight;

				if ( i == hoverIdx ) 
				{
					hovered = true;
				}

				if ( selection.Contains(i) ) 
				{
					hovered	 = false;
					selected = true;
				}

				var textColor	=	hovered ? ColorTheme.TextColorHovered : ColorTheme.TextColorNormal;
				var highlight	=	ColorTheme.HighlightColor;

				if (drawScroll) 
				{
					spriteLayer.Draw( null, scrollRect, ColorTheme.ScrollMarkerColor, clipRectIndex );
				}

				if (selected) 
				{
					spriteLayer.Draw( null, rect, ColorTheme.TextColorNormal, clipRectIndex );
					DrawText( spriteLayer, x, y, text, ColorTheme.BackgroundColorDark, clipRectIndex );
				}
				else if (hovered) 
				{
					spriteLayer.Draw( null, rect, ColorTheme.HighlightColor, clipRectIndex );
					DrawText( spriteLayer, x, y, text, ColorTheme.TextColorHovered, clipRectIndex );
				} 
				else 
				{
					if ((i&1)==1) 
					{
						spriteLayer.Draw( null, rect, new Color(255,255,255,4), clipRectIndex );
					}
					DrawText( spriteLayer, x, y, text, ColorTheme.TextColorNormal, clipRectIndex );
				}
			}
		}
	}
}
