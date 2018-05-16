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

namespace IronStar.Editor2.Controls {

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
				Height			=	10;

				this.StatusChanged +=AESlider_StatusChanged;

				textBox			=	new TextBox( Frames, getFunc, setFunc );
				Add( textBox );

				if (selectFunc!=null) {
					buttonSelect =	new Button( Frames, "\x00ff", 0,0,10,10, ()=>selectFunc(textBox.Text) );
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
				TextAlignment		=	Alignment.MiddleRight;
				TextOffsetX			=	-Width / 2 - 2;

				Text				=	Name;

				textBox.X		=	Width/2;
				textBox.Width	=	Width/2;
				textBox.Height	=	10;

				if (buttonSelect!=null) {
					textBox.Width -= 10;
					buttonSelect.X = Width-10;
					buttonSelect.Y = 0;
					buttonSelect.Width = 10;
					buttonSelect.Height = 10;
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
