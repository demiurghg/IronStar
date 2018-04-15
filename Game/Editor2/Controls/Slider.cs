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

namespace IronStar.Editor2.Controls {

	class Slider : Frame {

		readonly Func<float> getFunc;
		readonly Action<float> setFunc;

		readonly float min;
		readonly float max;
		readonly float snap;
		readonly float psnap;

		public Color SliderColor;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public Slider ( FrameProcessor fp, Func<float> getFunc, Action<float> setFunc, float min, float max, float snap, float psnap ) : base(fp)
		{ 
			this.getFunc		=	getFunc;
			this.setFunc		=	setFunc;

			this.min			=	min;
			this.max			=	max;
			this.snap			=	snap;
			this.psnap			=	psnap;

			this.BackColor		=	ColorTheme.ColorBorder;
			this.Width			=	1;
			this.BorderColor	=	ColorTheme.ColorBorder;
			this.TextAlignment	=	Alignment.MiddleCenter;

			this.MouseDown  +=Slider_MouseDown;
			this.MouseMove+=Slider_MouseMove;
			this.MouseUp+=Slider_MouseUp;
				
			SliderColor	=	ColorTheme.ElementColorNormal;
		}



		bool dragStarted = false;
		bool dragPrecise = false;
		int dragXPos	= 0;
		int dragYPos	= 0;
		float storedValue = 0;



		private void Slider_MouseDown( object sender, MouseEventArgs e )
		{
			dragStarted = true;

			storedValue	= getFunc();

			dragXPos	=	e.X;
			dragYPos	=	e.Y;

			if (Frames.Game.Keyboard.IsKeyDown( Fusion.Engine.Input.Keys.LeftShift ) ) {
				dragPrecise = true;
			}
		}



		private void Slider_MouseMove( object sender, MouseEventArgs e )
		{
			if (dragStarted) {
				var padRect	 = GetPaddedRectangle(false);
				var fraction = 0.0f;
				var newValue = 0.0f;
					
				if (dragPrecise) {

					var origin	=	(float)(Math.Round( storedValue / psnap ) * psnap);

					var delta	=	(int)((e.X - dragXPos)/2);

					newValue	=	origin + psnap * delta;

				} else {

					fraction	=	(e.X - padRect.X) / (float)(padRect.Width);
					newValue	=	min + (max-min)*fraction;
					newValue	=	(float)(Math.Round( newValue / snap ) * snap);
					
				}

				newValue	=	MathUtil.Clamp( newValue, min, max );

				setFunc( newValue );
			}
		}



		private void Slider_MouseUp( object sender, MouseEventArgs e )
		{
			dragStarted		=	false;
			dragPrecise		=	false;
		}



		protected override void Update( GameTime gameTime )
		{
		}


		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			var value	= getFunc();
			var padRect	= GetPaddedRectangle(true);

			value		=	MathUtil.Clamp( value, min, max );
			var frac	=	(value - min) / (max-min);

			var totalWidth	=	padRect.Width;
			var	sliderWidth	=	(int)(totalWidth * frac);

			var rect		=	padRect;
			rect.Width		=	sliderWidth;

			//spriteLayer.Draw( null, rect, SliderColor, clipRectIndex );

			var fadeColor	=	new Color( (byte)(SliderColor.R), (byte)(SliderColor.G), (byte)(SliderColor.B), (byte)64 );

			spriteLayer.DrawGradient( rect, fadeColor, SliderColor, fadeColor, SliderColor, clipRectIndex );


			this.DrawFrameText( spriteLayer, clipRectIndex );
		}

	}
}
