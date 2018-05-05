using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Core.Shell {

	/// <summary>
	/// This class is used to create and retrieve argument lists 
	/// that can be passed to command class methods that require arglists.
	/// </summary>
	public partial class ArgList : IEnumerable<string> {

		public class Context {

			ArgList argList;
			int index = 1;
			string usage;


			public Context( ArgList argList, string usage )
			{
				this.argList	=	argList;
				this.usage		=	usage;
			}


			public Context Require<T>( string argName, out T result )
			{
				if (index>=argList.Count) {
					Error("missing argument : {0}", argName );
				}

				if (StringConverter.TryConvertFromString<T>( argList[index], out result ) ) {
					index++;
					return this;
				} else {
					Error("failed to convert '{0}' to {1}", argList[index], typeof(T).ToString() );
					return this;
				}
			}


			public Context Option( string argName, out bool result )
			{
				var key = "/" + argName;

				result = (argList.Any( s => (s == key) ));
				return this;
			}


			public Context Option<T>( string argName, out T result )
			{
				var key = "/" + argName + ":";
				result	= default(T);

				foreach ( var arg in argList ) {

					if (arg.StartsWith( key )) {

						var value = arg.Replace( key, "" );

						if (StringConverter.TryConvertFromString<T>( argList[index], out result ) ) {
							return this;
						} else {
							Error("failed to convert '{0}' to {1}", argList[index], typeof(T).ToString() );
							return this;
						}
					}
				}

				return this;
			}


			public void Apply ()
			{
				
			}


			private void Error ( string format, params object[] args )
			{
				throw new ArgListException("error: {0}\r\nusage: {1}", string.Format( format, args ), usage );			
			}
		}




	}
}
