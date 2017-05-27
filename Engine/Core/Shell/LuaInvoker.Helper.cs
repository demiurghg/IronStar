using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using System.Reflection;


namespace Fusion.Core.Shell {
	public partial class LuaInvoker {

		public class Suggestion {

			List<string> candidates = new List<string>(16);

			public string CommandLine { get; set; }

			public IEnumerable<string> Candidates { get { return candidates; } }

			public Suggestion ( string cmdline ) 
			{
				CommandLine = cmdline;
			}

			public void Set ( string cmdline )
			{
				CommandLine = cmdline;
			}

			public void Add ( string candidate ) 
			{
				candidates.Add( candidate );
			}

			public void Clear () 
			{
				candidates.Clear();
			}

			public void AddRange ( IEnumerable<string> more ) 
			{
				candidates.AddRange( more );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		/// <param name="candidates"></param>
		/// <param name="suggestions">Null if no suggestions</param>
		/// <returns></returns>
		public Suggestion AutoComplete ( string input )
		{
			return new Suggestion(input);
		}



		/*-----------------------------------------------------------------------------------------
		 * 
		 *	String routines :
		 * 
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
