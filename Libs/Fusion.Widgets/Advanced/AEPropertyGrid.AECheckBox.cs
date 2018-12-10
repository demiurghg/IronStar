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
using Fusion.Widgets;

namespace Fusion.Widgets.Advanced {

	public partial class AEPropertyGrid : Frame {

		const int VerticalPadding = 0;
		const int HorizontalPadding = 4;

		class AECheckBox : AEBaseEditor {

			readonly Frame yesNoButton;

			readonly Func<bool> getFunc;
			readonly Action<bool> setFunc;


			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AECheckBox ( AEPropertyGrid grid, string name, Func<bool> getFunc, Action<bool> setFunc ) : base(grid, name)
			{ 
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;
				
				Width			=	1;
				Height			=	ComputeItemHeight();

				this.StatusChanged +=CheckBox_StatusChanged;

				yesNoButton			= new Frame(this.Frames) {
					Font			= ColorTheme.NormalFont,
					Height			= ComputeItemHeight(), 
					BackColor		= ColorTheme.BackgroundColorDark,
					TextAlignment	= Alignment.MiddleLeft,
					TextOffsetX		= 1,
					PaddingTop		= VerticalPadding,
					PaddingBottom	= VerticalPadding,
					PaddingLeft		= HorizontalPadding,
					PaddingRight	= HorizontalPadding,
				};

				yesNoButton.Click+=YesNoButton_Click;

				this.Add(yesNoButton);

				Update(new GameTime());
			}


			private void YesNoButton_Click( object sender, MouseEventArgs e )
			{
				setFunc( !getFunc() );
			}


			private void CheckBox_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		ForeColor	=	ColorTheme.TextColorNormal; break;
					case FrameStatus.Hovered:	ForeColor	=	ColorTheme.TextColorHovered; break;
					case FrameStatus.Pushed:	ForeColor	=	ColorTheme.TextColorPushed; break;
				}
			}



			public override void RunLayout()
			{
				base.RunLayout();

				yesNoButton.X		=	Width/2;
				yesNoButton.Width	=	Width/2;
			}


			protected override void Update( GameTime gameTime )
			{
			}


			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				var value = getFunc();

				yesNoButton.Text		=	value ? "Yes" : "No";
				yesNoButton.ForeColor	=	value ? ColorTheme.ColorGreen : ColorTheme.ColorRed;

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
