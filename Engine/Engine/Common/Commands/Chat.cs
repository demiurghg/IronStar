using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Shell;

namespace Fusion.Engine.Common.Commands {
	
	[Command("chat", CommandAffinity.Default)]
	public class Chat : NoRollbackCommand {

		[CommandLineParser.Required()]
		public List<string> Text { get; set; }
			
		public Chat ( Invoker invoker ) : base(invoker) 
		{
			Text = new List<string>();
		}

		public override void Execute ()
		{
			Invoker.Game.GameClient.NotifyServer("*chat " + string.Join(" ", Text ));
		}
	}
}
