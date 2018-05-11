using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion.Core.Mathematics;

namespace Fusion.Core.Extensions {
	public static class StringExtensions {

		static readonly string[] separators = new[]{ "\r\n", "\r", "\n" };

		/// <summary>
		/// http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
		/// </summary>
		/// <param name="str"></param>
		/// <param name="controller"></param>
		/// <returns></returns>
		public static IEnumerable<string> Split(this string str, Func<char, bool> controller)
		{
			int nextPiece = 0;

			for (int c = 0; c < str.Length; c++) {
				if (controller(str[c])) {
					yield return str.Substring(nextPiece, c - nextPiece);
					nextPiece = c + 1;
				}
			}

			yield return str.Substring(nextPiece);
		}


		/// <summary>
		/// http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
		/// </summary>
		public static string TrimMatchingQuotes(this string input, char quote)
		{
			if ((input.Length >= 2) && 
				(input[0] == quote) && (input[input.Length - 1] == quote))
				return input.Substring(1, input.Length - 2);

			return input;
		}


		/// <summary>
		/// http://stackoverflow.com/questions/298830/split-string-containing-command-line-parameters-into-string-in-c-sharp/298990#298990
		/// </summary>
		/// <param name="commandLine"></param>
		/// <returns></returns>
		public static IEnumerable<string> SplitCommandLine(this string commandLine)
		{
			bool inQuotes = false;

			return commandLine.Split(c =>
									 {
										 if (c == '\"')
											 inQuotes = !inQuotes;

										 return !inQuotes && c == ' ';
									 })
							  .Select(arg => arg.Trim().TrimMatchingQuotes('\"'))
							  .Where(arg => !string.IsNullOrEmpty(arg));
		}



		public static string[] SplitLines (this string text)
		{
			return text.Split( separators, StringSplitOptions.None );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <param name="ch"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool Split ( this string str, char ch, out string left, out string right )
		{
			var list = str.Split( new[] {ch}, 2 );

			if (list.Length!=2) {
				left = list[0];
				right = null;
				return false;
			} else {
				left = list[0];
				right = list[0];
				return false;
			}
		}
	}
}
