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
using Fusion;

namespace IronStar.Editor2.AttributeEditor {

	public partial class AEPropertyGrid : Frame {

		class TextBox : Frame {

			readonly Func<string> getFunc;
			readonly Action<string> setFunc;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public TextBox ( FrameProcessor fp, Func<string> getFunc, Action<string> setFunc ) : base(fp)
			{ 
				this.getFunc		=	getFunc;
				this.setFunc		=	setFunc;

				this.BackColor		=	ColorBorder;
				this.Width			=	1;
				this.BorderColor	=	ColorBorder;
				this.TextAlignment	=	Alignment.MiddleLeft;

				StatusChanged	+=	TextBox_StatusChanged;
				Activated+=TextBox_Activated;
				Deactivated+=TextBox_Deactivated;

				//KeyDown+=TextBox_KeyDown;
				//KeyUp+=TextBox_KeyUp;
				TypeWrite+=TextBox_TypeWrite;
			}

			private void TextBox_TypeWrite( object sender, KeyEventArgs e )
			{
				Log.Message("TypeWrite: [{0}] '{1}'", e.Key, e.Symbol);
			}

			private void TextBox_KeyUp( object sender, KeyEventArgs e )
			{
				Log.Message("Keyup   : [{0}] - {1} {2} {3}", 
					e.Key, 
					e.Shift? "[Shift]" : "",
					e.Alt?   "[Alt]"   : "",
					e.Ctrl?  "[Ctrl]"  : ""
					);
			}

			private void TextBox_KeyDown( object sender, KeyEventArgs e )
			{
				Log.Message("Keydown : [{0}] - {1} {2} {3}", 
					e.Key, 
					e.Shift? "[Shift]" : "",
					e.Alt?   "[Alt]"   : "",
					e.Ctrl?  "[Ctrl]"  : ""
					);
			}

			private void TextBox_Deactivated( object sender, EventArgs e )
			{
				
			}

			private void TextBox_Activated( object sender, EventArgs e )
			{
				
			}

			private void TextBox_StatusChanged( object sender, StatusEventArgs e )
			{
				switch ( e.Status ) {
					case FrameStatus.None:		ForeColor	=	TextColorNormal; break;
					case FrameStatus.Hovered:	ForeColor	=	TextColorHovered; break;
					case FrameStatus.Pushed:	ForeColor	=	TextColorPushed; break;
				}
			}


			protected override void Update( GameTime gameTime )
			{
			}


			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				var value	= getFunc();
				var padRect	= GetPaddedRectangle(true);

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
