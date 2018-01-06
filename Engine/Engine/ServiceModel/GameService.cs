using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Common;
using Fusion.Core.IniParser;
using Fusion.Core.IniParser.Model;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Core;

namespace Fusion.Engine.ServiceModel {

	public abstract class GameService : DisposableBase {

		public abstract void Initialize ();	
		public abstract void Update ( GameTime gameTime );
		

		/// <summary>
		/// Saves configuration to given section data
		/// </summary>
		/// <param name="sectionData"></param>
		public virtual void SaveConfig ( SectionData sectionData )
		{
			var cfgPropsList = this.GetType()
				.GetProperties()
				.Where( p => p.HasAttribute<ConfigAttribute>() )
				.ToList();

			foreach ( var prop in cfgPropsList ) {
				
				var keyName		= prop.Name;
				var keyValue	= "";

				if ( StringConverter.TryConvertToString( prop.GetValue(this), out keyValue ) ) {
					
					sectionData.Keys.AddKey( keyName, keyValue );

				} else {

					Log.Warning("Property {2} {0}.{1} could not be converted to string. Ignored.", GetType(), keyName, prop.PropertyType );

				}

			}
		}


		/// <summary>
		/// Loads configuration from given section data
		/// </summary>
		/// <param name="sectionData"></param>
		public virtual void LoadConfig ( SectionData sectionData )
		{
			foreach ( var keyData in sectionData.Keys ) {

				var prop = this.GetType().GetProperty( keyData.KeyName );

				if (prop==null) {
					Log.Warning("Property {0} does not exist. Ignored.", keyData.KeyName );
					continue;
				}

				if (!prop.HasAttribute<ConfigAttribute>()) {
					Log.Warning("Property {0} does not have [Config] attribute. Ignored.", keyData.KeyName );
					continue;
				}


				object value;

				if (StringConverter.TryConvertFromString( prop.PropertyType, keyData.Value, out value )) {

					prop.SetValue( this, value );
					
				} else {
					Log.Warning("Can not convert key {0} value to {1}. Ignored.", keyData.KeyName, prop.PropertyType );
					continue;
				}
			}
		}
	}
}
