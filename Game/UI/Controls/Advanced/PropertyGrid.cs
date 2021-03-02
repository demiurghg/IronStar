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
using Fusion.Widgets.Binding;
using IronStar.UI.Controls;
using IronStar.UI.Controls.Dialogs;

namespace IronStar.UI.Controls.Advanced {

	public partial class PropertyGrid : Frame {


		class DeferredChange {
			public PropertyInfo Property;
			public object Value;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fp"></param>
		public PropertyGrid( FrameProcessor fp ) : base(fp)
		{
			this.BackColor		=	MenuTheme.Transparent;
			this.Border			=	0;
			this.Padding		=	0;

			this.Layout			=	new StackLayout() { AllowResize = true, EqualWidth = true, Interval = 0 };
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
					Clear();
					CancelChanges();
					FeedObject(targetObject, 0, null);
				}
			}
		}

		object targetObject;


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


		//List<DeferredChange> changes;
		Dictionary<PropertyInfo,object> changes = new Dictionary<PropertyInfo, object>();


		public void CancelChanges ()
		{
			changes.Clear();
		}

		public void CommitChanges ()
		{
			foreach ( var pair in changes ) {

				Log.Message("  property grid commit: {0} -> {1}", pair.Key.Name, pair.Value.ToString() );
				pair.Key.SetValue( TargetObject, pair.Value );

			}
			changes.Clear();
		}


		/// <summary>
		/// 
		/// </summary>
		void FeedObject ( object obj, int nestingLevel, string subcat )
		{
			if (obj==null) {
				return;
			}

			//--------------------------------------------------------------------------

			foreach ( var pi in obj.GetType().GetProperties() ) {

				if (!pi.CanWrite || !pi.CanRead) {
					continue;
				}

				if (pi.HasAttribute<AEIgnoreAttribute>()) {
					continue;
				}

				Action<object> setFunc  =	delegate (object value) {
					if (changes.ContainsKey(pi)) {
						changes[pi] = value;
					} else {
						changes.Add( pi, value );
					}
				};

				Func<object> getFunc = delegate() {	
					object value;
					if (changes.TryGetValue(pi, out value)) {
						return value;
					} else {
						return pi.GetValue(obj);
					}
				};

				var name		=	pi.GetAttribute<AEDisplayNameAttribute>()?.Name ?? pi.Name;

				if (pi.PropertyType==typeof(bool)) {
					AddCheckBox( name, ()=>(bool)getFunc(), (val)=>setFunc(val) );
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
						AddSlider( name, ()=>(float)getFunc(), (val)=>setFunc(val), min, max, step, pstep );
					} else {
						AddTextBoxNum( name, 
							()	 => StringConverter.ConvertToString( getFunc() ),
							(val)=>	setFunc( StringConverter.ToSingle(val) ),
							null );
					}
				}

				if (pi.PropertyType==typeof(int)) {
					AddTextBoxNum( name, 
						()	 => StringConverter.ConvertToString( getFunc() ),
						(val)=>	setFunc( StringConverter.ToInt32(val) ),
						null );
				}

				if (pi.PropertyType==typeof(Color)) {
					AddColorPicker( name, ()=>(Color)getFunc(), (val)=>setFunc(val) );
				}

				if (pi.PropertyType.IsEnum) {

					var type	=	pi.PropertyType;
					var value	=	pi.GetValue(obj).ToString();
					var values	=	Enum.GetNames( type );

					AddDropDown( name, value, values, ()=>getFunc().ToString(), (val)=>setFunc(Enum.Parse(type, val)) );
				}

				if (pi.PropertyType==typeof(string)) {
					AddTextBox( name, ()=>(string)getFunc(), (val)=>setFunc(val), null );
				}
			}

			//--------------------------------------------------------------------------

			foreach ( var mi in obj.GetType().GetMethods(BindingFlags.Public|BindingFlags.Instance) ) {

				var name		=	mi.GetAttribute<AEDisplayNameAttribute>()?.Name ?? mi.Name;

				if (mi.HasAttribute<AECommandAttribute>()) {
					AddButton( name, ()=>mi.Invoke(obj, new object[0]) );
				}
			}
		}



		/// <summary>
		/// Removes all control bindings
		/// </summary>
		public void ResetGrid ()
		{
			Clear();
		}



		public void AddCheckBox ( string name, Func<bool> getFunc, Action<bool> setFunc )
		{
			Add( new AECheckBox( this, name, getFunc, setFunc ) );
		}

		public void AddSlider ( string name, Func<float> getFunc, Action<float> setFunc, float min, float max, float step, float pstep )
		{
			Add( new AESlider( this, name, getFunc, setFunc, min, max, step, pstep ) );
		}

		public void AddTextBox ( string name, Func<string> getFunc, Action<string> setFunc, Action<string> selectFunc )
		{
			var textBox = new AETextBox( this, name, getFunc, setFunc, null );
			var button	= new Button( Frames, "Select...", 0,0, 200, 20, () => selectFunc(textBox.Text) ) { 
				MarginRight = 0,
				MarginLeft = 150,
				MarginBottom = 3,
			};
			
			Add( textBox );
			if (selectFunc!=null) {
				Add( button );
			}
		}

		public void AddTextBoxNum ( string name, Func<string> getFunc, Action<string> setFunc, Action<string> selectFunc )
		{
			Add( new AETextBox( this, name, getFunc, setFunc, selectFunc ) );
		}

		public void AddButton ( string name, Action action )
		{
			Add( new Button( Frames, name, 0,0, 200, MenuTheme.ElementHeight, action ) );
		}

		public void AddDropDown ( string name, string value, IEnumerable<string> values, Func<string> getFunc, Action<string> setFunc )
		{
			Add( new AEDropDown( this, name, value, values, getFunc, setFunc ) );
		}

		public void AddColorPicker ( string name, Func<Color> getFunc, Action<Color> setFunc )
		{
			Add( new AEColorPicker( this, name, getFunc, setFunc ) );
		}

	}
}
