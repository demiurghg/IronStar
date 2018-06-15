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
using Fusion.Core;
using Fusion;

namespace IronStar.Editor2.Controls {

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

			this.BackColor		=	ColorTheme.ButtonColorNormal;
			this.Width			=	1;
			this.BorderColor	=	ColorTheme.BorderColor;
			this.TextAlignment	=	Alignment.MiddleLeft;
			this.Text			=	value;

			StatusChanged	+=	DropDown_StatusChanged;
			Click += DropDown_Click;

			this.values = values.ToArray();

			dropDownList = CreateDropDownList(40);
		}




		Frame CreateDropDownList ( int minWidth )
		{
			var dropDownList = new Frame( Frames ) {
				BackColor	=	ColorTheme.BackgroundColor,
				Padding = 1,
				Border = 1,
				BorderColor = ColorTheme.BackgroundColorLight,
			};

			foreach ( var value in values ) {

				var w	=	value.Length * 8 + 6;
				var h	=	8 + 2;

				var dropDownElement = new Frame( Frames, 0,0,w,h, value, ColorTheme.BackgroundColor );
				var textSize = dropDownElement.TextBlockSize;

				dropDownElement.TextAlignment = Alignment.MiddleLeft;
				dropDownElement.Padding = 1;
				dropDownElement.ForeColor = ColorTheme.TextColorNormal;

				minWidth = Math.Max( textSize.Width + 8, minWidth );

				dropDownList.Add( dropDownElement );
				dropDownElement.StatusChanged += DropDownElement_StatusChanged;
				dropDownElement.Click+=DropDownElement_Click;
			}

			dropDownList.Width = minWidth;

			dropDownList.Layout = new StackLayout() { AllowResize = true, EqualWidth = true };

			dropDownList.RunLayout();

			dropDownList.Missclick += (s,e) => CloseDropDownList();

			return dropDownList;
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
			dropDownList.Close();
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
				case FrameStatus.None:		frame.ForeColor	=	ColorTheme.TextColorNormal;	break;
				case FrameStatus.Hovered:	frame.ForeColor	=	ColorTheme.TextColorHovered;break;
				case FrameStatus.Pushed:	frame.ForeColor	=	ColorTheme.TextColorPushed;	break;
			}

			switch ( e.Status ) {
				case FrameStatus.None:		frame.BackColor	=	ColorTheme.ButtonColorDark;		break;
				case FrameStatus.Hovered:	frame.BackColor	=	ColorTheme.ButtonColorHovered;	break;
				case FrameStatus.Pushed:	frame.BackColor	=	ColorTheme.ButtonColorPushed;	break;
			}
		}


		private void DropDownElement_StatusChanged( object sender, StatusEventArgs e )
		{
			var frame = (Frame)sender;

			switch ( e.Status ) {
				case FrameStatus.None:		frame.ForeColor	=	ColorTheme.TextColorNormal;	break;
				case FrameStatus.Hovered:	frame.ForeColor	=	ColorTheme.TextColorHovered;break;
				case FrameStatus.Pushed:	frame.ForeColor	=	ColorTheme.TextColorPushed;	break;
			}

			switch ( e.Status ) {
				case FrameStatus.None:		frame.BackColor	=	ColorTheme.BackgroundColor;	break;
				case FrameStatus.Hovered:	frame.BackColor	=	ColorTheme.ButtonColorHovered;break;
				case FrameStatus.Pushed:	frame.BackColor	=	ColorTheme.ButtonColorPushed;	break;
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
