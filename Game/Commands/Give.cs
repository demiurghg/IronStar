using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Shell;
using Fusion.Engine.Server;
using IronStar.Entities;
using IronStar.Items;

namespace IronStar.Commands {

	[Command("give", CommandAffinity.Server)]
	public class Give : NoRollbackCommand {

		[CommandLineParser.Required]
		public string ItemName { get; set; } = "";

		/// <summary>
		/// 
		/// </summary>
		/// <param name="invoker"></param>
		public Give( Invoker invoker ) : base(invoker)
		{
			
		}


		/// <summary>
		/// 
		/// </summary>
		public override void Execute ()
		{
		}


		public override void ExecuteServer( IServerInstance serverInstance )
		{
			var ents =  (serverInstance as ShooterServer).World.GetAllPlayerEntities();
			var content = (serverInstance as ShooterServer).World.Content;

			var itemFactory = content.Load<ItemFactory>( @"items\" + ItemName, (ItemFactory)null );

			foreach ( var ent in ents ) {

				if (itemFactory!=null) {
					var item = itemFactory.Spawn();

					item.Pickup( ent );
				}
			}
		}
	}
}
