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


			public Context DebugArgs ()
			{
				for (int i=0; i<argList.Count; i++) {
					Log.Message("[{0}] - '{1}'", i, argList[i]);
				}
				return this;
			}


			public Context Option( string argName, out bool result )
			{
				if (string.IsNullOrWhiteSpace(argName)) {
					throw new ArgumentException("argName is empty");
				}

				if (argName[0]!='/') {
					throw new ArgumentException("argName must contains leading '/'");
				}

				result = (argList.Any( s => (s == argName) ));

				return this;
			}


			public Context Option<T>( string argName, out T result )
			{
				if (string.IsNullOrWhiteSpace(argName)) {
					throw new ArgumentException("argName is empty");
				}

				if (argName[0]!='/' || argName[argName.Length-1]!=':') {
					throw new ArgumentException("argName must contains leading '/' and trailng ':'");
				}

				result	= default(T);

				foreach ( var arg in argList ) {

					if (arg.StartsWith( argName )) {

						var value = arg.Replace( argName, "" );

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
