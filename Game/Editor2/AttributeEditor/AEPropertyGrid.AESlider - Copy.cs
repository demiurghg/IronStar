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

namespace IronStar.Editor2.AttributeEditor {

	public partial class AEPropertyGrid : Frame {

		class AESlider : AEBaseEditor {

			readonly Frame slider;

			readonly AEPropertyGrid	grid;
			readonly Func<float> getFunc;
			readonly Action<float> setFunc;

			readonly float min;
			readonly float max;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AESlider ( AEPropertyGrid grid, string category, string name, Func<float> getFunc, Action<float> setFunc, float min, float max, float step, float pstep ) : base(grid, category, name)
			{ 
				this.grid		=	grid;
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;

				this.min		=	min;
				this.max		=	max;
				
				Width			=	grid.Width;
				Height			=	10;

				this.StatusChanged +=AESlider_StatusChanged;

				slider			=	new Slider( Frames, getFunc, setFunc, min, max, step, pstep );
				Add( slider );

				Update(new GameTime());
			}



			private void AESlider_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		ForeColor	=	TextColorNormal; break;
					case FrameStatus.Hovered:	ForeColor	=	TextColorHovered; break;
					case FrameStatus.Pushed:	ForeColor	=	TextColorPushed; break;
				}
			}


			protected override void Update( GameTime gameTime )
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
