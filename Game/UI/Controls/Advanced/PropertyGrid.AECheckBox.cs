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

		const int VerticalPadding = 5;
		const int HorizontalPadding = 8;

		class AECheckBox : AEBaseEditor {

			readonly Frame yesNoButton;

			readonly Func<bool> getFunc;
			readonly Action<bool> setFunc;


			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AECheckBox ( PropertyGrid grid, string name, Func<bool> getFunc, Action<bool> setFunc ) : base(grid, name)
			{ 
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;
				
				Width			=	1;
				Height			=	ComputeItemHeight();

				yesNoButton			= new Frame(this.Frames) {
					Font			= MenuTheme.NormalFont,
					Height			= ComputeItemHeight(), 
					BackColor		= MenuTheme.Transparent,
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
				yesNoButton.ForeColor	=	value ? MenuTheme.ColorPositive : MenuTheme.ColorNegative;

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
