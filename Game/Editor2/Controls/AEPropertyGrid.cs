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

namespace IronStar.Editor2.Controls {

	public partial class AEPropertyGrid : Frame {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public AEPropertyGrid( FrameProcessor fp ) : base(fp)
		{
			this.BackColor		=	ColorTheme.ColorBackground;
			this.BorderColor	=	ColorTheme.ColorBorder;
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

				if (pi.PropertyType==typeof(string)) {
					AddTextBox( category, name, ()=>(string)(pi.GetValue(obj)), (val)=>pi.SetValue(obj,val) );
				}

				if (pi.PropertyType==typeof(Color)) {
					AddColorPicker( category, name, ()=>(Color)(pi.GetValue(obj)), (val)=>pi.SetValue(obj,val) );
				}

				if (pi.PropertyType.IsEnum) {

					var type	=	pi.PropertyType;
					var value	=	pi.GetValue(obj).ToString();
					var values	=	Enum.GetNames( type );

					AddDropDown( category, name, value, values, ()=>pi.GetValue(obj).ToString(), (val)=>pi.SetValue(obj, Enum.Parse(type, val)) );
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
			AddToCollapseRegion( category, new AECheckBox( this, name, getFunc, setFunc ) );
		}

		public void AddSlider ( string category, string name, Func<float> getFunc, Action<float> setFunc, float min, float max, float step, float pstep )
		{
			AddToCollapseRegion( category, new AESlider( this, name, getFunc, setFunc, min, max, step, pstep ) );
		}

		public void AddTextBox ( string category, string name, Func<string> getFunc, Action<string> setFunc )
		{
			AddToCollapseRegion( category, new AETextBox( this, name, getFunc, setFunc ) );
		}

		public void AddButton ( string category, string name, Action action )
		{
			AddToCollapseRegion( category, new AEButton( this, name, action ) );
		}

		public void AddDropDown ( string category, string name, string value, IEnumerable<string> values, Func<string> getFunc, Action<string> setFunc )
		{
			AddToCollapseRegion( category, new AEDropDown( this, name, value, values, getFunc, setFunc ) );
		}

		public void AddColorPicker ( string category, string name, Func<Color> getFunc, Action<Color> setFunc )
		{
			AddToCollapseRegion( category, new AEColorPicker( this, name, getFunc, setFunc ) );
		}

	}
}
