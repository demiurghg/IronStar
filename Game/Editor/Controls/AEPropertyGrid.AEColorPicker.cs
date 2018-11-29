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
using Fusion.Widgets.Dialogs;

namespace IronStar.Editor.Controls {

	public partial class AEPropertyGrid : Frame {

		class AEColorPicker : AEBaseEditor {
			
			readonly Func<Color> getFunc;
			readonly Action<Color> setFunc;

			Frame colorButton;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AEColorPicker ( AEPropertyGrid grid, string name, Func<Color> getFunc, Action<Color> setFunc ) : base(grid, name)
			{ 
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;
				
				Width			=	grid.Width;
				Height			=	ComputeItemHeight();

				this.StatusChanged  +=AEColorPicker_StatusChanged;

				colorButton				=	new Frame( Frames );
				colorButton.Border		=	1;
				colorButton.BorderColor	=	ColorTheme.BorderColor;
				colorButton.BackColor	=	Color.Black;

				colorButton.Click +=ColorButton_Click;

				Add( colorButton );

				Update(new GameTime());
			}


			private void ColorButton_Click( object sender, MouseEventArgs e )
			{
				var button	=	(Frame)sender;
				var rect	=	button.GlobalRectangle;

				ColorPicker.ShowDialog( Frames, rect.X, rect.Y + rect.Height, getFunc(), setFunc );
			}



			private void AEColorPicker_StatusChanged( object sender, StatusEventArgs e )
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

				colorButton.X		=	Width/2;
				colorButton.Width	=	Math.Min(Width/2, 70);
				colorButton.Height	=	ComputeItemHeight();
			}



			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				colorButton.BackColor = getFunc();
				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}
		}

	}
}
