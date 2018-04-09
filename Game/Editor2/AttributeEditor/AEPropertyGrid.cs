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
using Fusion.Core.Extensions;
using Fusion;
using Fusion.Engine.Frames.Layouts;
using Fusion.Core.Shell;

namespace IronStar.Editor2.AttributeEditor {

	public partial class AEPropertyGrid : Frame {

		static readonly Color	ColorBorder			=	new Color( 10, 10, 10, 192);
		static readonly Color	ColorBackground		=	new Color( 30, 30, 30, 192);

		static readonly Color	TextColorNormal		=	new Color(150,150,150, 192);
		static readonly Color	TextColorHovered	=	new Color(200,200,200, 192);
		static readonly Color	TextColorPushed		=	new Color(220,220,220, 192);

		static readonly Color	ElementColorNormal	=	new Color(120,120,120, 192);
		static readonly Color	ElementColorHovered	=	new Color(150,150,150, 192);
		static readonly Color	ElementColorPushed	=	new Color(180,180,180, 192);

		static readonly Color	ButtonColorNormal	=	new Color( 90, 90, 90, 192);
		static readonly Color	ButtonColorHovered	=	new Color(120,120,120, 192);
		static readonly Color	ButtonColorPushed	=	new Color(150,150,150, 192);
		static readonly Color	ButtonBorderColor	=	new Color( 20, 20, 20, 192);

		static readonly Color	ColorWhite			=	new Color(180,180,180, 255);
		static readonly Color	ColorGreen			=	new Color(144,239,144, 255);
		static readonly Color	ColorRed			=	new Color(239,144,144, 255);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public AEPropertyGrid( FrameProcessor fp ) : base(fp)
		{
			this.BackColor		=	ColorBackground;
			this.BorderColor	=	ColorBorder;
			this.Border			=	1;

			this.Padding		=	1;

			this.Layout			=	new StackLayout(0,1, true) { AllowResize = true };
			
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
			var obj = targetObject;

			Clear();

			if (targetObject==null) {
				return;
			}

			foreach ( var pi in obj.GetType().GetProperties() ) {

				var name		=	pi.GetAttribute<AEDisplayNameAttribute>()?.Name ?? pi.Name;
				var category	=	pi.GetAttribute<AECategoryAttribute>()?.Category ?? "Misc";

				if (pi.PropertyType==typeof(bool)) {
					AddCheckBox( category, name, ()=>(bool)(pi.GetValue(obj)), (val)=>pi.SetValue(obj,val) );
				}

				if (pi.PropertyType==typeof(float)) {
					var min		=	1.0f;
					var max		=	100.0f;
					var step	=	5.0f;
					var pstep	=	1.0f;

					var range	=	pi.GetAttribute<AEValueRangeAttribute>();

					if (range!=null) {
						min		=	range.Min;
						max		=	range.Max;
						step	=	range.RoughStep;
						pstep	=	range.PreciseStep;
					}
					
					AddSlider( category, name, ()=>(float)(pi.GetValue(obj)), (val)=>pi.SetValue(obj,val), min, max, step, pstep );
				}

			}

			foreach ( var mi in obj.GetType().GetMethods(BindingFlags.Public|BindingFlags.Instance) ) {

				var name		=	mi.GetAttribute<AEDisplayNameAttribute>()?.Name ?? mi.Name;
				var category	=	mi.GetAttribute<AECategoryAttribute>()?.Category ?? "Misc";

				if (mi.HasAttribute<AECommandAttribute>()) {
					AddButton( category, name, ()=>mi.Invoke(obj, new object[0]) );
				}
			}

			RunLayout();
			//RunLayout();
		}



		/// <summary>
		/// Removes all control bindings
		/// </summary>
		public void ResetGrid ()
		{
			Clear();
		}


		void AddToCollapseRegion ( string category, Frame frame )
		{
			var region = Children
						.Where( f1 => f1 is AECollapseRegion )
						.Select( f2 => (AECollapseRegion)f2 )
						.FirstOrDefault( f3 => f3.Category == category );

			if (region==null) {
				region = new AECollapseRegion(this, category);
				Add( region );	
			}

			region.Add( frame );
		}


		public void AddCheckBox ( string category, string name, Func<bool> getFunc, Action<bool> setFunc )
		{
			AddToCollapseRegion( category, new AECheckBox( this, category, name, getFunc, setFunc ) );
		}

		public void AddSlider ( string category, string name, Func<float> getFunc, Action<float> setFunc, float min, float max, float step, float pstep )
		{
			AddToCollapseRegion( category, new AESlider( this, category, name, getFunc, setFunc, min, max, step, pstep ) );
		}

		public void AddButton ( string category, string name, Action action )
		{
			AddToCollapseRegion( category, new AEButton( this, category, name, action ) );
		}

	}
}
