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
using IronStar.UI.Controls;

namespace IronStar.UI.Controls.Advanced {

	public partial class PropertyGrid : Frame {

		class AEDropDown : AEBaseEditor {
			
			DropDown dropDown;

			readonly Func<string> getFunc;
			readonly Action<string> setFunc;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AEDropDown ( PropertyGrid grid, string name, string value, IEnumerable<string> values, Func<string> getFunc, Action<string> setFunc ) : base(grid, name)
			{ 
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;
				
				Width			=	grid.Width;
				Height			=	ComputeItemHeight();

				dropDown		=	new DropDown( Frames, value, values, getFunc, setFunc ) {
					PaddingLeft		=	HorizontalPadding,
					PaddingRight	=	HorizontalPadding,
					PaddingTop		=	VerticalPadding,
					PaddingBottom	=	VerticalPadding,
				};

				Add( dropDown );

				Update(new GameTime());
			}



			public override void RunLayout()
			{
				base.RunLayout();

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
