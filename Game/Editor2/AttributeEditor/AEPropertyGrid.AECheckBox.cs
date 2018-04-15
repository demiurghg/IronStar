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
				
				Width			=	grid.Width-50;
				Height			=	10;

				this.StatusChanged +=CheckBox_StatusChanged;

				yesNoButton			= new Frame(this.Frames) {
					Height			= 10, 
					BackColor		= ColorBorder,
					TextAlignment	= Alignment.MiddleLeft,
					TextOffsetX		= 1,
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
					case FrameStatus.None:		ForeColor	=	TextColorNormal; break;
					case FrameStatus.Hovered:	ForeColor	=	TextColorHovered; break;
					case FrameStatus.Pushed:	ForeColor	=	TextColorPushed; break;
				}
			}



			public override void RunLayout()
			{
				TextAlignment		=	Alignment.MiddleRight;
				TextOffsetX			=	-Width / 2 - 2;

				Text				=	Name;

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
				yesNoButton.ForeColor	=	value ? ColorGreen : ColorRed;

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
