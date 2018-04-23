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
	public class ArgList : IEnumerable<string> {

		readonly string[] args;
		
		/// <summary>
		/// Creats instance of ArgList
		/// </summary>
		/// <param name="args"></param>
		public ArgList ( string[] args )
		{
			this.args = args;
		}


		/// <summary>
		/// Gets count of arguments
		/// </summary>
		public int Count {
			get {
				return args.Length;
			}
		}


		/// <summary>
		/// Gets string value at given index.
		/// Note: 0 index points to command name.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public string this[int index] {
			get {
				return args[index];
			}
		}


		public string AsString ( int index )
		{
			return args[index];
		}

		public float AsFloat ( int index )
		{
			return StringConverter.ToSingle( args[index] );
		}


		public bool IsFlagSet ( string flag )
		{
			if (string.IsNullOrWhiteSpace(flag)) {
				throw new ArgumentNullException("flag");
			}
			if (flag[0]!='/') {
				throw new ArgumentException("flag must be started with slash");
			}
			
			return args.Any( a => a==flag );
		}


		public IEnumerator<string> GetEnumerator()
		{
			return ( (IEnumerable<string>)args ).GetEnumerator();
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable<string>)args ).GetEnumerator();
		}
	}
}
