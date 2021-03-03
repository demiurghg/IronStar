using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion;
using Fusion.Core.Input;

namespace Fusion.Widgets.Dialogs 
{
	partial class ColorPicker : Frame 
	{
		class ColorField : Frame 
		{
			Func<HSVColor> getFunc;
			Action<HSVColor> setFunc;

			public ColorField ( FrameProcessor fp, int x, int y, int w, int h, Func<HSVColor> getFunc, Action<HSVColor> setFunc ) : base(fp,x,y,w,h,"",Color.Gray)
			{
				Border			=	1;
				BorderColor		=	Color.Black;
				BackColor		=	Color.Black;

				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;

				this.Image		=	fp.Game.Content.Load<DiscTexture>("hsvMap");
				this.ImageColor	=	new Color(255,255,255,255);
				this.ImageMode	=	FrameImageMode.Stretched;

				this.MouseDown+=ColorField_MouseDown;
				this.MouseMove+=ColorField_MouseMove;
				this.MouseUp+=ColorField_MouseUp;
				this.Click+=ColorField_Click;
			}

			private void ColorField_Click( object sender, MouseEventArgs e )
			{
				UpdateColorFromPointOnMap( e.X, e.Y );
			}

			bool trackColor = false;

			private void ColorField_MouseDown( object sender, MouseEventArgs e )
			{
				trackColor = true;
			}

			private void ColorField_MouseMove( object sender, MouseEventArgs e )
			{
				if (trackColor) {
					UpdateColorFromPointOnMap( e.X, e.Y );
				}
			}

			private void ColorField_MouseUp( object sender, MouseEventArgs e )
			{
				trackColor = false;
			}


			void UpdateColorFromPointOnMap( int x, int y )
			{
				var pr	=	GetPaddedRectangle(true);
				var hsv	=	getFunc();

				hsv.H	=	MathUtil.Clamp( (x - 1) / (float)pr.Width, 0, 1 ) * 360;
				hsv.V	=	1 - MathUtil.Clamp( (y - 1) / (float)pr.Height, 0, 1 );

				setFunc( hsv );
			}

			
			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );

				var hsv	=	getFunc();

				var pr	=	GetPaddedRectangle(true);
				var y	=	pr.Y;
				var	h	=	pr.Height;
				var x	=	pr.X;
				var w	=	pr.Width;

				var hue	=	(int)(pr.Width * hsv.H / 360f);
				var val	=	(int)(pr.Height * (1-hsv.V));

				var vLine	=	new Rectangle( x + hue, y,       1, h );
				var hLine	=	new Rectangle( x,       y + val, w, 1 );

				var alpha		=	(byte)(255*(1-hsv.S));
				var one			=	(byte)255;
				var zero		=	(byte)0;
				var topColor	=	new Color( one, one, one,alpha);
				var bottomColor	=	new Color(zero,zero,zero,alpha);
				spriteLayer.DrawGradient( pr, topColor, topColor, bottomColor, bottomColor, clipRectIndex ); 

				spriteLayer.Draw( null, vLine, Color.Black, clipRectIndex );
				spriteLayer.Draw( null, hLine, Color.Black, clipRectIndex );
			}
		}
	}
}
