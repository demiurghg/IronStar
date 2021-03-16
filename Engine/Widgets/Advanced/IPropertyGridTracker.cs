using System.Reflection;

namespace Fusion.Widgets.Advanced
{
	public interface IPropertyGridTracker
	{
		void Initiate( object target, PropertyInfo property, object value );
		void Update  ( object target, PropertyInfo property, object value );
		void Commit  ( object target, PropertyInfo property, object value );
		void Cancel  ( object target, PropertyInfo property, object value );
	}
}
