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

		static readonly Color	ColorBackground		=	new Color( 26, 26, 26, 192);
		static readonly Color	LabelColor			=	new Color(155,155,155, 192);
		static readonly int		LabelFieldWidth		=	200;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public AEPropertyGrid( FrameProcessor fp ) : base(fp)
		{
			this.BackColor	=	ColorBackground;
		}




		object targetObject = null;


		/// <summary>
		/// 
		/// </summary>
		public object TargetObject {
			get {
				return targetObject;
			} 
			set {
				if (targetObject!=value) {
					targetObject = value;
					RefreshEditor();
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		/// <param name="spriteLayer"></param>
		/// <param name="clipRectIndex"></param>
		protected override void DrawFrame( GameTime gameTime, SpriteLayer spriteLayer, int clipRectIndex )
		{
			base.DrawFrame( gameTime, spriteLayer, clipRectIndex );
		}



		/// <summary>
		/// 
		/// </summary>
		void RefreshEditor ()
		{
			int x = 0;
			int y = 0;

			Clear();

			if (targetObject==null) {
				return;
			}

			foreach ( var mi in targetObject.GetType().GetMembers() ) {

				Log.Message("{0}", mi.Name);

				var frame = ConstructFromProperty(mi, targetObject);

				if (frame==null) {
					continue;
				}

				frame.X	=	x;
				frame.Y	=	y;

				y += frame.Height;

				Add( frame );
			}
		}


		Frame ConstructFromProperty ( MemberInfo mi, object obj )
		{
			if (mi.MemberType==MemberTypes.Property) {

				var pi = mi as PropertyInfo;

				if (pi==null) {
					return null;
				}

				var bi = new BindingInfo("????????", mi.Name, obj, mi );

				if (pi.PropertyType==typeof(bool)) {
					return new CheckBox(this, bi);
				}

			}

			return null;
		}
	}
}
