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

		class AETextBox : AEBaseEditor {
			
			TextBox textBox;

			readonly Func<string> getFunc;
			readonly Action<string> setFunc;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AETextBox ( AEPropertyGrid grid, string category, string name, Func<string> getFunc, Action<string> setFunc ) : base(grid, category, name)
			{ 
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;
				
				Width			=	grid.Width;
				Height			=	10;

				this.StatusChanged +=AESlider_StatusChanged;

				textBox			=	new TextBox( Frames, getFunc, setFunc );
				Add( textBox );

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


			public override void RunLayout()
			{
				TextAlignment		=	Alignment.MiddleRight;
				TextOffsetX			=	-Width / 2 - 2;

				Text				=	Name;

				textBox.X		=	Width/2;
				textBox.Width	=	Width/2;
				textBox.Height	=	10;
			}


			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				var value = getFunc();

				textBox.Text			=	value ?? "(null)";

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
