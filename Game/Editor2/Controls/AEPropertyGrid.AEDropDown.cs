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

		class AEDropDown : AEBaseEditor {
			
			DropDown dropDown;

			readonly Func<string> getFunc;
			readonly Action<string> setFunc;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AEDropDown ( AEPropertyGrid grid, string name, string value, IEnumerable<string> values, Func<string> getFunc, Action<string> setFunc ) : base(grid, name)
			{ 
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;
				
				Width			=	grid.Width;
				Height			=	10;

				this.StatusChanged +=AEDropDown_StatusChanged;

				dropDown		=	new DropDown( Frames, value, values, getFunc, setFunc );
				Add( dropDown );

				Update(new GameTime());
			}



			private void AEDropDown_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		ForeColor	=	ColorTheme.TextColorNormal; break;
					case FrameStatus.Hovered:	ForeColor	=	ColorTheme.TextColorHovered; break;
					case FrameStatus.Pushed:	ForeColor	=	ColorTheme.TextColorPushed; break;
				}
			}


			public override void RunLayout()
			{
				TextAlignment	=	Alignment.MiddleRight;
				TextOffsetX		=	-Width / 2 - 2;

				Text			=	Name;

				dropDown.X		=	Width/2;
				dropDown.Width	=	Width/2;
				dropDown.Height	=	10;
			}


			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				var value = getFunc();

				//textBox.Text			=	value ?? "(null)";

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
