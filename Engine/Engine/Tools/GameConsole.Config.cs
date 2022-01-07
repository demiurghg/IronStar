using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Fusion.Core;
using Fusion.Core.Utils;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics;
using Fusion.Core.Input;
using Fusion.Core.Configuration;
using Fusion.Core.Shell;
using Fusion.Widgets.Advanced;

namespace Fusion.Engine.Tools 
{
	[ConfigClass]
	public partial class GameConsole : GameComponent 
	{
		public int LayerOrder { get; set; } = 0;

		[Config] public static float FallSpeed { get; set; }

		[Config] public static float CursorBlinkRate { get; set; }

		[AECategory("Appearance")] [Config] static public Color MessageColor	{ get; set; }
		[AECategory("Appearance")] [Config] static public Color ErrorColor		{ get; set; }
		[AECategory("Appearance")] [Config] static public Color WarningColor	{ get; set; }
		[AECategory("Appearance")] [Config] static public Color CmdLineColor	{ get; set; }
		[AECategory("Appearance")] [Config] static public Color VersionColor	{ get; set; }
		[AECategory("Appearance")] [Config] static public Color TraceColor		{ get; set; }
		[AECategory("Appearance")] [Config] static public Color DebugColor		{ get; set; }

		[AECategory("Appearance")] [Config] static public Color BackColor		{ get; set; }
		[AECategory("Appearance")] [Config] static public Color HelpColor		{ get; set; }
		[AECategory("Appearance")] [Config] static public Color HintColor		{ get; set; }


		[AECategory("History")] [Config] public static string CommandHistory0 { get; set; }
		[AECategory("History")] [Config] public static string CommandHistory1 { get; set; }
		[AECategory("History")] [Config] public static string CommandHistory2 { get; set; }
		[AECategory("History")] [Config] public static string CommandHistory3 { get; set; }
		[AECategory("History")] [Config] public static string CommandHistory4 { get; set; }
		[AECategory("History")] [Config] public static string CommandHistory5 { get; set; }
		[AECategory("History")] [Config] public static string CommandHistory6 { get; set; }
		[AECategory("History")] [Config] public static string CommandHistory7 { get; set; }


		static GameConsole()
		{
			FallSpeed		=	5;
			
			CursorBlinkRate	=	3;
			
			MessageColor	=	Color.White;
			ErrorColor		=	Color.Red;
			WarningColor	=	Color.Yellow;
			CmdLineColor	=	Color.Orange;
			VersionColor	=	new Color(255,255,255, 64);
			TraceColor		=	new Color(255,255,255, 64);
			DebugColor		=	new Color(255,255,255,128);

			BackColor		=	new Color(0,0,0,224);
			HelpColor		=	Color.Gray;
			HintColor		=	new Color(255,255,255,64);

			CommandHistory0	=	"";
			CommandHistory1	=	"";
			CommandHistory2	=	"";
			CommandHistory3	=	"";
			CommandHistory4	=	"";
			CommandHistory5	=	"";
			CommandHistory6	=	"";
			CommandHistory7	=	"";
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="command"></param>
		internal void UpdateHistory ( IEnumerable<string> commands )
		{
			var list = commands
					.Where(s=>!s.StartsWith("quit"))
					.Take(8)
					.ToArray();

			CommandHistory0	=	( list.Length > 0 ) ? list[0] : "";
			CommandHistory1	=	( list.Length > 1 ) ? list[1] : "";
			CommandHistory2	=	( list.Length > 2 ) ? list[2] : "";
			CommandHistory3	=	( list.Length > 3 ) ? list[3] : "";
			CommandHistory4	=	( list.Length > 4 ) ? list[4] : "";
			CommandHistory5	=	( list.Length > 5 ) ? list[5] : "";
			CommandHistory6	=	( list.Length > 6 ) ? list[6] : "";
			CommandHistory7	=	( list.Length > 7 ) ? list[7] : "";
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string[] GetHistory ()
		{
			return new[]{ 
					CommandHistory0, CommandHistory1, 
					CommandHistory2, CommandHistory3, 
					CommandHistory4, CommandHistory5, 
					CommandHistory6, CommandHistory6 
				}
				.Where( s => !string.IsNullOrWhiteSpace(s) )
				.ToArray();
		}
	}
}
