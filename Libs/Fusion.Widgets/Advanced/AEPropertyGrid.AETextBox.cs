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
using Fusion.Core.Binding;
using Fusion.Widgets;

namespace Fusion.Widgets.Advanced {

	public partial class AEPropertyGrid : Frame {

		class AETextBox : AEBaseEditor {
			
			TextBox textBox;

			readonly Func<string> getFunc;
			readonly Action<string> setFunc;
			readonly Action<string>	selectFunc;

			Button buttonSelect;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public AETextBox ( AEPropertyGrid grid, string name, Func<string> getFunc, Action<string> setFunc, Action<string> selectFunc ) : base(grid, name)
			{ 
				this.getFunc	=	getFunc;
				this.setFunc	=	setFunc;
				this.selectFunc	=	selectFunc;
				
				Width			=	grid.Width;
				Height			=	ComputeItemHeight();

				this.StatusChanged +=AESlider_StatusChanged;

				textBox	=	new TextBox( Frames, new DelegateBinding<string>( getFunc, setFunc ) ) { 
					TextAlignment = Alignment.MiddleLeft, 
				};

				Add( textBox );

				if (selectFunc!=null) {
					buttonSelect =	new Button( Frames, "[..]", 0,0,10,10, ()=>selectFunc(textBox.Text) );
					buttonSelect.BorderColor = Color.Zero;
					Add( buttonSelect );
				}

				Update(new GameTime());
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
				base.RunLayout();

				textBox.X		=	Width/2;
				textBox.Width	=	Width/2;
				textBox.Height	=	ComputeItemHeight();

				if (buttonSelect!=null) {
					textBox.Width		-= 13;
					buttonSelect.X		= Width-13;
					buttonSelect.Y		= 0;
					buttonSelect.Width	= 13;
					buttonSelect.Height = textBox.Height;
				}
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