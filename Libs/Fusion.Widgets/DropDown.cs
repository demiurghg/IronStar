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

namespace Fusion.Widgets {

	public class DropDown : Frame {

		readonly Func<string> getFunc;
		readonly Action<string> setFunc;
		readonly string[] values;

		Frame dropDownList;

		readonly int minDropDownWidth;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="grid"></param>
		/// <param name="bindingInfo"></param>
		public DropDown ( FrameProcessor fp, string value, IEnumerable<string> values, Func<string> getFunc, Action<string> setFunc ) : base(fp)
		{ 
			this.getFunc		=	getFunc;
			this.setFunc		=	setFunc;

			this.Font			=	ColorTheme.NormalFont;

			this.BackColor		=	ColorTheme.ButtonColorNormal;
			this.Width			=	1;
			this.BorderColor	=	ColorTheme.BorderColor;
			this.TextAlignment	=	Alignment.MiddleLeft;
			this.Text			=	value;

			StatusChanged	+=	DropDown_StatusChanged;
			Click += DropDown_Click;

			this.values = values.ToArray();

			dropDownList = CreateDropDownList(40, out minDropDownWidth);
		}


		int ComputeHeight ()
		{
			return GetFontHeight() + BorderTop + BorderBottom + PaddingTop + PaddingBottom;
		}


		public override int Width {
			get {
				return base.Width;
			}

			set {
				base.Width=value;
			}
		}



		public override int Height {
			get {
				return ComputeHeight();
				//return base.Height;
			}

			set {
				base.Height = ComputeHeight();
			}
		}



		Frame CreateDropDownList ( int minWidth, out int minDropDownWidth )
		{
			var dropDownList	= new Frame( Frames ) {
				BackColor		= ColorTheme.DropdownColor,
				PaddingLeft		= 4,
				PaddingRight	= 4,
				PaddingBottom	= 1,
				PaddingTop		= 1,
			};

			foreach ( var value in values ) {

				var textSize	=	MeasureSingleLineString( Font, value );

				var textWidth	=	textSize.Width;
				var textHeight	=	textSize.Height;


				var dropDownElement = new Frame( Frames, 0, 0, textWidth+8, textHeight+4, value, ColorTheme.DropdownButtonNormal );
					dropDownElement.Font = ColorTheme.NormalFont;


				dropDownElement.TextAlignment	= Alignment.MiddleLeft;
				dropDownElement.PaddingLeft		= 4;
				dropDownElement.PaddingRight	= 4;
				dropDownElement.PaddingTop		= 1;
				dropDownElement.PaddingBottom	= 1;
				dropDownElement.ForeColor		= ColorTheme.TextColorNormal;

				minWidth = Math.Max( textWidth, minWidth );

				dropDownList.Add( dropDownElement );
				dropDownElement.StatusChanged += DropDownElement_StatusChanged;
				dropDownElement.Click+=DropDownElement_Click;
			}

			minDropDownWidth	=	minWidth + 8 + 8;

			dropDownList.Width	=	minDropDownWidth;

			dropDownList.Layout =	new StackLayout() { AllowResize = true, EqualWidth = true };

			dropDownList.RunLayout();

			dropDownList.Missclick	+= (s,e) => CloseDropDownList();


			return dropDownList;
		}


		void ShowDropDownList()
		{
			var gr = GetPaddedRectangle(true);

			dropDownList.X = gr.X;
			dropDownList.Y = gr.Y + gr.Height;

			dropDownList.Width	=	Math.Max( this.Width, minDropDownWidth );

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
				case FrameStatus.None:		frame.BackColor	=	ColorTheme.DropdownButtonNormal;	break;
				case FrameStatus.Hovered:	frame.BackColor	=	ColorTheme.DropdownButtonHovered;	break;
				case FrameStatus.Pushed:	frame.BackColor	=	ColorTheme.DropdownButtonPushed;	break;
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
