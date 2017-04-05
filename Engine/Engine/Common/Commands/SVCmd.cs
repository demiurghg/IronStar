using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;

namespace Fusion.Engine.Common.Commands {
	
	[Command("sv", CommandAffinity.Default)]
	public class CVSmd : NoRollbackCommand {

		[CommandLineParser.Required()]
		public List<string> Text { get; set; }
			
		public CVSmd ( Invoker invoker ) : base(invoker) 
		{
			Text = new List<string>();
		}

		public override void Execute ()
		{
			Invoker.Game.GameClient.NotifyServer("*cmd " + string.Join(" ", Text ));
		}
	}
}
