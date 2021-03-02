﻿using System;
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
using Fusion.Widgets.Binding;

namespace Fusion.Widgets 
{
	public class Slider : Frame 
	{
		readonly IValueBinding binding;

		readonly float min;
		readonly float max;
		readonly float snap;
		readonly float psnap;

		public Color SliderColor;

		public bool Vertical;

		public float Value { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public Slider ( FrameProcessor fp, IValueBinding binding, float min, float max, float snap, float psnap ) : base(fp)
		{ 
			if (binding.ValueType==typeof(float))
			{
				this.binding	=	binding;
			}

			this.min			=	min;
			this.max			=	max;
			this.snap			=	snap;
			this.psnap			=	psnap;

			this.BackColor		=	ColorTheme.BackgroundColorDark;
			this.Width			=	1;
			this.BorderColor	=	ColorTheme.BorderColor;
			this.TextAlignment	=	Alignment.MiddleCenter;

			this.MouseDown  +=Slider_MouseDown;
			this.MouseMove	+=Slider_MouseMove;
			this.MouseUp	+=Slider_MouseUp;
				
			SliderColor	=	ColorTheme.ElementColorNormal;
		}



		bool dragStarted	= false;
		bool dragPrecise	= false;
		int dragXPos		= 0;
		int dragYPos		= 0;
		float storedValue	= 0;



		private void Slider_MouseDown( object sender, MouseEventArgs e )
		{
			dragStarted =	true;

			if (binding!=null)
			{
				Value	=	(float)binding.GetValue();
			}

			storedValue	=	Value;

			dragXPos	=	e.X;
			dragYPos	=	e.Y;

			if (Frames.Game.Keyboard.IsKeyDown( Fusion.Core.Input.Keys.LeftShift ) ) 
			{
				dragPrecise = true;
			}
		}



		private void Slider_MouseMove( object sender, MouseEventArgs e )
		{
			if (dragStarted) 
			{
				var padRect	 = GetPaddedRectangle(false);
				var fraction = 0.0f;
				var newValue = 0.0f;
					
				if (dragPrecise) 
				{
					var origin	=	(float)(Math.Round( storedValue / psnap ) * psnap);
					var delta	=	0;

					if (Vertical) 
					{
						delta	=	(int)((dragYPos - e.Y)/2);
					} 
					else 
					{
						delta	=	(int)((e.X - dragXPos)/2);
					}

					newValue	=	origin + psnap * delta;
				} 
				else 
				{
					if (Vertical) 
					{
						fraction	=	1 - ((e.Y - padRect.Y) / (float)(padRect.Height));
					} 
					else 
					{
						fraction	=	(e.X - padRect.X) / (float)(padRect.Width);
					}

					newValue	=	min + (max-min)*fraction;
					newValue	=	(float)(Math.Round( newValue / snap ) * snap);
				}

				newValue	=	MathUtil.Clamp( newValue, min, max );
				Value		=	newValue;
				binding?.SetValue( newValue );
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
			var value	= binding==null ? Value : (float)binding.GetValue();
			var padRect	= GetPaddedRectangle(true);

			value		=	MathUtil.Clamp( value, min, max );
			var frac	=	(value - min) / (max-min);

			var totalWidth	=	padRect.Width;
			var totalHeight	=	padRect.Height;
			var	sliderWidth	=	(int)(totalWidth * frac);
			var sliderHeight=	(int)(totalHeight * frac);

			var rect		=	padRect;
			var fadeColor	=	new Color( SliderColor.R, SliderColor.G, SliderColor.B, (byte)64 );

			if (Vertical) 
			{
				rect.Height		=	sliderHeight;
				rect.Y			=	padRect.Y + padRect.Height - sliderHeight;
				spriteLayer.DrawGradient( rect, SliderColor, SliderColor, fadeColor, fadeColor, clipRectIndex );
			} 
			else 
			{
				rect.Width		=	sliderWidth;
				spriteLayer.DrawGradient( rect, fadeColor, SliderColor, fadeColor, SliderColor, clipRectIndex );
			}

			this.DrawFrameText( spriteLayer, clipRectIndex );
		}

	}
}
