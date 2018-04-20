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
				Height			=	10;

				this.StatusChanged +=AESlider_StatusChanged;

				slider				=	new Slider( Frames, getFunc, setFunc, min, max, step, pstep );
				slider.StatusChanged +=Slider_StatusChanged;
				slider.Border		=	1;
				slider.BorderColor	=	ColorTheme.BorderColor;
				Add( slider );

				Update(new GameTime());
			}



			private void Slider_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		slider.ForeColor	=	ColorTheme.TextColorNormal;		slider.SliderColor = ColorTheme.ElementColorNormal;	 break;
					case FrameStatus.Hovered:	slider.ForeColor	=	ColorTheme.TextColorHovered;	slider.SliderColor = ColorTheme.ElementColorHovered; break;
					case FrameStatus.Pushed:	slider.ForeColor	=	ColorTheme.TextColorPushed;		slider.SliderColor = ColorTheme.ElementColorPushed;	 break;
				}
			}

			private void AESlider_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		ForeColor	=	ColorTheme.TextColorNormal; break;
					case FrameStatus.Hovered:	ForeColor	=	ColorTheme.TextColorHovered; break;
					case FrameStatus.Pushed:	ForeColor	=	ColorTheme.TextColorPushed; break;
				}
			}


			public override void RunLayout()
			{
				TextAlignment		=	Alignment.MiddleRight;
				TextOffsetX			=	-Width / 2 - 2;

				Text				=	Name;

				slider.X		=	Width/2;
				slider.Width	=	Width/2;
				slider.Height	=	10;
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
