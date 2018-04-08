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
				this.TextAlignment	=	Alignment.MiddleCenter;

				StatusChanged	+=	Slider_StatusChanged;
			}


			private void Slider_StatusChanged( object sender, StatusEventArgs e )
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
