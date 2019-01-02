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
using IronStar.UI.Controls;

namespace IronStar.UI.Controls.Advanced {

	public partial class AEPropertyGrid : Frame {

		class AESlider : AEBaseEditor {

			readonly Slider slider;

			readonly Func<float> getFunc;
			readonly Action<float> setFunc;

			readonly float min;
			readonly float max;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AESlider ( AEPropertyGrid grid, string name, Func<float> getFunc, Action<float> setFunc, float min, float max, float step, float pstep ) : base(grid, name)
			{ 
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;

				this.min		=	min;
				this.max		=	max;
				
				Width			=	grid.Width;
				Height			=	ComputeItemHeight();

				this.StatusChanged +=AESlider_StatusChanged;

				slider				=	new Slider( Frames, getFunc, setFunc, min, max, step, pstep )  {
					Font			=	MenuTheme.NormalFont,
					PaddingLeft		=	VerticalPadding, // yes, for slider they should be the same
					PaddingRight	=	VerticalPadding,
					PaddingTop		=	VerticalPadding,
					PaddingBottom	=	VerticalPadding,
				};

				slider.StatusChanged +=Slider_StatusChanged;
				slider.Border		=	1;
				slider.BorderColor	=	MenuTheme.BorderColor;
				Add( slider );

				Update(new GameTime());
			}



			private void Slider_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		slider.ForeColor	=	MenuTheme.TextColorNormal;		slider.SliderColor = MenuTheme.ElementColorNormal;	 break;
					case FrameStatus.Hovered:	slider.ForeColor	=	MenuTheme.TextColorHovered;	slider.SliderColor = MenuTheme.ElementColorHovered; break;
					case FrameStatus.Pushed:	slider.ForeColor	=	MenuTheme.TextColorPushed;		slider.SliderColor = MenuTheme.ElementColorPushed;	 break;
				}
			}

			private void AESlider_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		ForeColor	=	MenuTheme.TextColorNormal; break;
					case FrameStatus.Hovered:	ForeColor	=	MenuTheme.TextColorHovered; break;
					case FrameStatus.Pushed:	ForeColor	=	MenuTheme.TextColorPushed; break;
				}
			}


			public override void RunLayout()
			{
				base.RunLayout();

				slider.X		=	Width/2;
				slider.Width	=	Width/2;
				slider.Height	=	ComputeItemHeight();
			}


			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				var value = getFunc();

				slider.Text			=	value.ToString();

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
