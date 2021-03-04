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
using Fusion.Widgets;
using Fusion.Widgets.Binding;

namespace Fusion.Widgets.Advanced
{
	public class AESliderAttribute : AEEditorAttribute
	{
		readonly float min;
		readonly float max;
		readonly float step;
		readonly float pstep;

		public AESliderAttribute( float min, float max, float step, float preciseStep = 0 )
		{
			this.max	=	max;
			this.min	=	min;
			this.step	=	step;
			this.pstep	=	(preciseStep==0) ? (step / 10.0f) : preciseStep;
		}

		public override Frame CreateEditor( AEPropertyGrid grid, string name, IValueBinding binding )
		{
			return new AESlider( grid, name, binding, min, max, step, pstep ); 
		}
	}


	class AESlider : AEBaseEditor 
	{
		readonly Slider slider;
		readonly IValueBinding binding;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public AESlider ( AEPropertyGrid grid, string name, IValueBinding binding, float min, float max, float step, float pstep ) : base(grid, name)
		{ 
			this.binding	=	binding;
				
			Width			=	grid.Width;
			Height			=	ComputeItemHeight();

			this.StatusChanged +=AESlider_StatusChanged;

			slider				=	new Slider( Frames, binding, min, max, step, pstep )
			{
				Font			=	ColorTheme.NormalFont,
				PaddingLeft		=	AEPropertyGrid.VerticalPadding, // yes, for slider they should be the same
				PaddingRight	=	AEPropertyGrid.VerticalPadding,
				PaddingTop		=	AEPropertyGrid.VerticalPadding,
				PaddingBottom	=	AEPropertyGrid.VerticalPadding,
			};

			slider.StatusChanged +=Slider_StatusChanged;
			slider.Border		=	1;
			slider.BorderColor	=	ColorTheme.BorderColor;
			Add( slider );

			Update(new GameTime());
		}



		private void Slider_StatusChanged( object sender, StatusEventArgs e )
		{
			switch ( e.Status ) 
			{
				case FrameStatus.None:		slider.ForeColor	=	ColorTheme.TextColorNormal;		slider.SliderColor = ColorTheme.ElementColorNormal;	 break;
				case FrameStatus.Hovered:	slider.ForeColor	=	ColorTheme.TextColorHovered;	slider.SliderColor = ColorTheme.ElementColorHovered; break;
				case FrameStatus.Pushed:	slider.ForeColor	=	ColorTheme.TextColorPushed;		slider.SliderColor = ColorTheme.ElementColorPushed;	 break;
			}
		}

		private void AESlider_StatusChanged( object sender, StatusEventArgs e )
		{
			switch ( e.Status ) 
			{
				case FrameStatus.None:		ForeColor	=	ColorTheme.TextColorNormal; break;
				case FrameStatus.Hovered:	ForeColor	=	ColorTheme.TextColorHovered; break;
				case FrameStatus.Pushed:	ForeColor	=	ColorTheme.TextColorPushed; break;
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
			if (binding!=null)
			{
				slider.Text		=	binding.GetValue()?.ToString();
			}

			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
		}

	}
}