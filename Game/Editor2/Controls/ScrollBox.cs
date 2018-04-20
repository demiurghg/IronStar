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
	public class ScrollBox : Panel {

		/// <summary>
		/// Gets and sets size of scroll bar marker
		/// </summary>
		public int ScrollMarkerSize { get; set; } = 3;

		/// <summary>
		/// Gets and sets scroll amount per one wheel event in pixels.
		/// </summary>
		public int ScrollVelocity = 30;

		/// <summary>
		/// Gets and sets scroll marker color
		/// </summary>
		public Color ScrollMarkerColor { get;set; } = ColorTheme.ScrollMarkerColor;


		int offset = 0;

		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public ScrollBox ( FrameProcessor fp, int x, int y, int w, int h ) : base(fp,x,y,w,h)
		{
			BackColor	=	ColorTheme.BackgroundColorDark;
			Border		=	0;
			Padding		=	0;

			this.MouseWheel +=ScrollBox_MouseWheel;
		}


		private void ScrollBox_MouseWheel( object sender, MouseEventArgs e )
		{
			int sign = Math.Sign( e.Wheel );
			offset -= ScrollVelocity * sign;
			MakeLayoutDirty();
		}


		int ScrollBoxWidth {
			get {
				return Width - ScrollMarkerSize - 1 - PaddingLeft - PaddingRight - BorderRight - BorderLeft;
			}
		}

		int ScrollBoxHeight {
			get {
				return Height - PaddingTop - PaddingBottom - BorderTop - BorderBottom;
			}
		}

		int ScrollRange {
			get {				
				var target = Children.FirstOrDefault();

				if (target!=null) {
					return	Math.Max( 0, target.Height - ScrollBoxHeight );
				} else {
					return 0;
				}
			}
		}

		Frame TargetFrame {
			get {
				return Children.FirstOrDefault();
			}
		}



		public override void RunLayout()
		{
			var targetFrame		=	TargetFrame;

			if (targetFrame==null) {
				return;
			}

			targetFrame.Width	=	ScrollBoxWidth;
			targetFrame.Height	=	Math.Max( targetFrame.Height, ScrollBoxHeight );

			offset				=	MathUtil.Clamp( offset, 0, ScrollRange );

			targetFrame.X		=	BorderLeft + PaddingLeft;
			targetFrame.Y		=	BorderTop + PaddingTop - offset;
		}



		protected override void Update( GameTime gameTime )
		{
			base.Update( gameTime );

			var container		=	Children.FirstOrDefault();

			if (container==null) {
				return;
			}

			int clientWidth		=	Width - ScrollMarkerSize - 1 - PaddingLeft - PaddingRight - BorderRight - BorderLeft;
			int clientHeight	=	Height - PaddingTop - PaddingBottom - BorderTop - BorderBottom;

			int	scrollRange		=	Math.Max( 0, container.Height - clientHeight );
			offset				=	MathUtil.Clamp( offset, 0, scrollRange );
		}



		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );

			var targetFrame		=	TargetFrame;

			var gp				=	GetPaddedRectangle();

			var markerHeight	=	ScrollBoxHeight;
			var markerOffset	=	0;

			if (targetFrame!=null) {
				
				markerHeight	=	(ScrollBoxHeight * ScrollBoxHeight * 2 + 1) / Math.Max( ScrollBoxHeight, targetFrame.Height ) / 2 + 1;
				markerOffset	=	(offset			 * ScrollBoxHeight * 2 + 1) / Math.Max( ScrollBoxHeight, targetFrame.Height ) / 2;
			}

			int x = gp.X + gp.Width - ScrollMarkerSize;
			int y = gp.Y + markerOffset;
			int w = ScrollMarkerSize;
			int h = markerHeight;

			spriteLayer.Draw( null, x,y,w,h, ScrollMarkerColor, clipRectIndex ); 
		}
	}
}
