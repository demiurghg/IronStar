using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using System.Reflection;
using Fusion.Core.Extensions;
using System.Runtime.CompilerServices;

namespace Fusion.Core.Shell {
	public partial class Invoker {

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="candidates"></param>
		/// <param name="suggestions">Null if no suggestions</param>
		/// <returns></returns>
		public Suggestion AutoComplete ( string input )
		{
			if (string.IsNullOrWhiteSpace(input)) {
				return new Suggestion("");
			}

			lock (lockObject) {
				var tailSpace	=	input.Last()==' ';
				var suggestion	=	new Suggestion(input);
				var args		=	input.SplitCommandLine().ToList();

				if (tailSpace) {
					args.Add("");	//	add virtual element
				}

				var candidates		=	GetCommandNameList().Concat( GetVariableNameList() ).ToArray();

				//	only command
				if (args.Count==1) {

					var longest = LongestCommon( args[0], ref candidates );

					if (!string.IsNullOrWhiteSpace(longest)) {
						suggestion.CommandLine	=	longest;
						suggestion.AddRange( candidates );

						if (candidates.Length==1) {
							suggestion.CommandLine += " ";
						}
					}

					return suggestion;

				} else {

					var cmd		= args[0];
					int index	= args.Count-1;
					var arg		= args[index];

					CommandEntry commandEntry;

					if (!commandsRegistry.TryGetValue(cmd, out commandEntry) ) {
						suggestion.Add(string.Format("Unknown command : {0}", cmd));
						return suggestion;
					}

					if (commandEntry.Syntax!=null) {

						if (arg.Length>0 && arg[0]=='/') {
							
							var optCandidates = commandEntry.Syntax.Optional( arg );
							var longest = LongestCommon( arg, ref optCandidates );

							args[index] = longest;
							
							suggestion.CommandLine = string.Join( " ", args);
							suggestion.AddRange( optCandidates );

							return suggestion;

						} else {

							var argCandidates = commandEntry.Syntax.Required( index, arg );
							var longest = LongestCommon( arg, ref argCandidates );

							args[index] = longest;
							
							suggestion.CommandLine = string.Join( " ", args);
							suggestion.AddRange( argCandidates );

							return suggestion;
						}

					} else {
						suggestion.Add("No syntax provided");
						return suggestion;
					}
				
				}

				return suggestion;
			}
		}



		/*-----------------------------------------------------------------------------------------
		 *	Longest common string :
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="candidates"></param>
		/// <returns></returns>
		string LongestCommon ( string input, ref string[] candidates )
		{
			candidates = candidates
				.Where( c => c.StartsWith( input, StringComparison.OrdinalIgnoreCase) )
				.ToArray();

			var longest	=	LongestCommon( candidates );
			
			return longest;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		string LongestCommon( IEnumerable<string> values )
		{
			return longestCommonPrefix( values.ToArray() );
			//string longest = null;
			//foreach ( var value in values ) {
			//	longest = LongestCommon( longest, value );
			//}
			//return longest;
		}


		/// <summary>
		/// http://stackoverflow.com/questions/8578349/longest-common-prefix-for-n-string
		/// </summary>
		/// <param name="strs"></param>
		/// <returns></returns>
		public String longestCommonPrefix(String[] strs) {
			if(strs.Length==0) return null;
			String minStr=strs[0];

			for(int i=1;i<strs.Length;i++){
				if(strs[i].Length<minStr.Length)
					minStr=strs[i];
			}
			int end=minStr.Length;
			for(int i=0;i<strs.Length;i++){
				int j;
				for( j=0;j<end;j++){
					if(char.ToLowerInvariant(minStr[j])!=char.ToLowerInvariant(strs[i][j]))
						break;
				}
				if(j<end)
					end=j;
			}
			return minStr.Substring(0,end);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		string LongestCommon ( string a, string b )
		{
			/*if (a==null) return b;
			if (b==null) return a;*/
			if (string.IsNullOrEmpty(a)) {
				return b;
			}
			if (string.IsNullOrEmpty(b)) {
				return a;
			}

			int len = Math.Min( a.Length, b.Length );

			StringBuilder sb = new StringBuilder();

			for (int i=0; i<len; i++) {
				if (char.ToLower(a[i])==char.ToLower(b[i])) {
					sb.Append(b[i]);
				} else {
					break;
				}
			}

			return sb.ToString();
		}

	}
}
