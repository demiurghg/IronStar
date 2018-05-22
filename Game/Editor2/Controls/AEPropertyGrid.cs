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
using Fusion.Core;
using Fusion.Core.Shell;

namespace IronStar.Editor2.Controls {

	public partial class AEPropertyGrid : Frame {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public AEPropertyGrid( FrameProcessor fp ) : base(fp)
		{
			this.BackColor		=	ColorTheme.BackgroundColor;
			this.BorderColor	=	ColorTheme.BorderColor;
			this.Border			=	1;

			this.Padding		=	1;

			this.Layout			=	new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 1 };
			
		}



		public class PropertyChangedEventArgs : EventArgs {
			public PropertyChangedEventArgs(object target, PropertyInfo property, object value)
			{
				TargetObject = target;
				Property = property;
				Value = value;
			}
			public readonly object TargetObject;
			public readonly PropertyInfo Property;
			public readonly object Value;
		}



		public event EventHandler<PropertyChangedEventArgs>	PropertyChanged;

		protected void OnPropertyChange (object target, PropertyInfo property, object value)
		{
			PropertyChanged?.Invoke( this, new PropertyChangedEventArgs(target, property, value) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="targetObjects"></param>
		public void FeedObjects ( params object[] targetObjects )
		{
			Clear();

			foreach ( var obj in targetObjects ) {
				FeedObject(obj);
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
		void FeedObject ( object obj )
		{
			if (obj==null) {
				return;
			}

			foreach ( var pi in obj.GetType().GetProperties() ) {

				if (!pi.CanWrite) {
					continue;
				}

				Action<object> setFunc  =	delegate (object value) {
					pi.SetValue(obj,value);
					OnPropertyChange(obj,pi,value);
				};

				var name		=	pi.GetAttribute<AEDisplayNameAttribute>()?.Name ?? pi.Name;
				var category	=	pi.GetAttribute<AECategoryAttribute>()?.Category ?? "Misc";

				if (pi.PropertyType==typeof(bool)) {
					AddCheckBox( category, name, ()=>(bool)(pi.GetValue(obj)), (val)=>setFunc(val) );
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
						AddSlider( category, name, ()=>(float)(pi.GetValue(obj)), (val)=>setFunc(val), min, max, step, pstep );
					} else {
						AddTextBoxNum( category, name, 
							()	 => StringConverter.ConvertToString( pi.GetValue(obj) ),
							(val)=>	setFunc( StringConverter.ToSingle(val) ),
							null );
					}
				}

				if (pi.PropertyType==typeof(int)) {
					AddTextBoxNum( category, name, 
						()	 => StringConverter.ConvertToString( pi.GetValue(obj) ),
						(val)=>	setFunc( StringConverter.ToInt32(val) ),
						null );
				}

				if (pi.PropertyType==typeof(string)) {

					if (pi.HasAttribute<AEFileNameAttribute>()) {
						var fna = pi.GetAttribute<AEFileNameAttribute>();
						var ext = fna.Extension;
						var dir = fna.Directory;
						AddTextBox( category, name, 
							()=>(string)(pi.GetValue(obj)), 
							(val)=>setFunc(val), 
							(val)=>FileSelector.ShowDialog( Frames, dir, ext, "", (fnm)=>setFunc(fnm) )
						);
					} else if (pi.HasAttribute<AEAtlasImageAttribute>()) {
						var aia = pi.GetAttribute<AEAtlasImageAttribute>();
						var an  = aia.AtlasName;
						AddTextBox( category, name, 
							()=>(string)(pi.GetValue(obj)), 
							(val)=>setFunc(val), 
							(val)=>AtlasSelector.ShowDialog( Frames, an, "", (fnm)=>setFunc(fnm) )
						);
					} else {
						AddTextBox( category, name, ()=>(string)(pi.GetValue(obj)), (val)=>setFunc(val), null );
					}
				}

				if (pi.PropertyType==typeof(Color)) {
					AddColorPicker( category, name, ()=>(Color)(pi.GetValue(obj)), (val)=>setFunc(val) );
				}

				if (pi.PropertyType.IsEnum) {

					var type	=	pi.PropertyType;
					var value	=	pi.GetValue(obj).ToString();
					var values	=	Enum.GetNames( type );

					AddDropDown( category, name, value, values, ()=>pi.GetValue(obj).ToString(), (val)=>setFunc(Enum.Parse(type, val)) );
				}

			}



			foreach ( var mi in obj.GetType().GetMethods(BindingFlags.Public|BindingFlags.Instance) ) {

				var name		=	mi.GetAttribute<AEDisplayNameAttribute>()?.Name ?? mi.Name;
				var category	=	mi.GetAttribute<AECategoryAttribute>()?.Category ?? "Misc";

				if (mi.HasAttribute<AECommandAttribute>()) {
					AddButton( category, name, ()=>mi.Invoke(obj, new object[0]) );
				}
			}

			//RunLayout();
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

		public void AddTextBox ( string category, string name, Func<string> getFunc, Action<string> setFunc, Action<string> selectFunc )
		{
			AddToCollapseRegion( category, new AETextBox( this, name, getFunc, setFunc, selectFunc, Alignment.MiddleLeft ) );
		}

		public void AddTextBoxNum ( string category, string name, Func<string> getFunc, Action<string> setFunc, Action<string> selectFunc )
		{
			AddToCollapseRegion( category, new AETextBox( this, name, getFunc, setFunc, selectFunc, Alignment.MiddleRight ) );
		}

		public void AddButton ( string category, string name, Action action )
		{
			//AddToCollapseRegion( category, new AEButton( this, name, action ) );
			AddToCollapseRegion( category, new Button( Frames, name, 0,0, 200, 20, action ) { MarginRight = 100 } );
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
