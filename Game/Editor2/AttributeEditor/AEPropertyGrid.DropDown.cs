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
using Fusion.Engine.Frames.Layouts;
using Fusion;

namespace IronStar.Editor2.AttributeEditor {

	public partial class AEPropertyGrid : Frame {

		class DropDown : Frame {

			readonly Func<string> getFunc;
			readonly Action<string> setFunc;
			readonly string[] values;

			Frame dropDownList;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public DropDown ( FrameProcessor fp, string value, IEnumerable<string> values, Func<string> getFunc, Action<string> setFunc ) : base(fp)
			{ 
				this.getFunc		=	getFunc;
				this.setFunc		=	setFunc;

				this.BackColor		=	ColorBorder;
				this.Width			=	1;
				this.BorderColor	=	ColorBorder;
				this.TextAlignment	=	Alignment.MiddleCenter;
				this.Text			=	value;

				StatusChanged	+=	DropDown_StatusChanged;
				Click += DropDown_Click;

				this.values = values.ToArray();

				CreateDropDownList();
			}




			void CreateDropDownList ()
			{
				dropDownList = new Frame( Frames ) {
					BackColor	=	ColorBackground,
					Padding = 1,
				};

				int maxWidth = 40;


				foreach ( var value in values ) {

					var w	=	value.Length * 8 + 2;
					var h	=	8 + 2;

					var dropDownElement = new Frame( Frames, 0,0,w,h, value, ColorBackground );
					var textRect = dropDownElement.MeasureText();

					dropDownElement.TextAlignment = Alignment.MiddleLeft;
					dropDownElement.Padding = 1;
					dropDownElement.ForeColor = TextColorNormal;

					maxWidth = Math.Max( textRect.Width, maxWidth );

					dropDownList.Add( dropDownElement );
					dropDownElement.StatusChanged += DropDown_StatusChanged;
					dropDownElement.Click+=DropDownElement_Click;
				}

				dropDownList.Width = maxWidth + 4;

				dropDownList.Layout = new StackLayout(0,1,true) { AllowResize=true };

				dropDownList.RunLayout();

				dropDownList.Missclick += (s,e) => CloseDropDownList();
			}


			void ShowDropDownList()
			{
				var gr = GetPaddedRectangle(true);

				dropDownList.X = gr.X;
				dropDownList.Y = gr.Y + gr.Height;

				Frames.RootFrame.Add( dropDownList );
				Frames.ModalFrame = dropDownList;
			}



			void CloseDropDownList()
			{
				Frames.RootFrame.Remove( dropDownList );
				Frames.ModalFrame = null;
			}


			private void DropDownElement_Click( object sender, MouseEventArgs e )
			{
				var frame = (Frame)sender;
				var value = frame.Text;

				this.Text = value;

				setFunc( value );

				CloseDropDownList();
			}

			
			private void DropDown_Click( object sender, MouseEventArgs e )
			{
				ShowDropDownList();
			}


			private void DropDown_StatusChanged( object sender, StatusEventArgs e )
			{
				var frame = (Frame)sender;

				switch ( e.Status ) {
					case FrameStatus.None:		frame.ForeColor	=	TextColorNormal;	break;
					case FrameStatus.Hovered:	frame.ForeColor	=	TextColorHovered;	break;
					case FrameStatus.Pushed:	frame.ForeColor	=	TextColorPushed;	break;
				}
			}


			protected override void Update( GameTime gameTime )
			{
			}


			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				Text = getFunc();

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
				/*var value	= getFunc();
				var padRect	= GetPaddedRectangle(true);

				value		=	MathUtil.Clamp( value, min, max );
				var frac	=	(value - min) / (max-min);

				var totalWidth	=	padRect.Width;
				var	sliderWidth	=	(int)(totalWidth * frac);

				var rect		=	padRect;
				rect.Width		=	sliderWidth;

				spriteLayer.Draw( null, rect, sliderColor, clipRectIndex );
				this.DrawFrameText( spriteLayer, clipRectIndex );*/
			}

		}

	}
}
