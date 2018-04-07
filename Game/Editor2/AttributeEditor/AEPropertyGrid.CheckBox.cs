using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using System.Reflection;

namespace IronStar.Editor2.AttributeEditor {

	public partial class AEPropertyGrid : Frame {

		class CheckBox : BaseElement {

			PropertyInfo propertyInfo;


			/// <summary>
			/// 
			/// </summary>
			/// <param name="grid"></param>
			/// <param name="bindingInfo"></param>
			public CheckBox ( AEPropertyGrid grid, BindingInfo bindingInfo ) : base(grid, bindingInfo)
			{ 
				propertyInfo = bindingInfo.TargetMember as PropertyInfo;

				if (propertyInfo.PropertyType!=typeof(bool)) {
					throw new AEException("Property type must be Bool");
				}

				Width			=	grid.Width;
				Height			=	10;
				TextAlignment	=	Alignment.MiddleLeft;
			}



			protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
			{
				var value = (bool)propertyInfo.GetValue( BindingInfo.TargetObject );
				
				Text	=	value ? "[x] " : "[ ] " + BindingInfo.Name;

				base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
			}

		}

	}
}
