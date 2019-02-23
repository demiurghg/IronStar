using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Engine.Common;
using Fusion.Core.Configuration;
using System.ComponentModel;
using Fusion.Core.Shell;
using System.IO;

namespace Fusion.Engine.Graphics {

	public partial class RenderSystem : GameComponent {

		void RegisterCommands ()
		{
			Game.Invoker.RegisterCommand("screenshot",	() => new ScreenshotCmd(this)	);
			Game.Invoker.RegisterCommand("vtrestart",	() => new VTRestartCmd(this)	);
			Game.Invoker.RegisterCommand("buildrad",	() => new BuildRadCmd(this)		);
		}


		class ScreenshotCmd : CommandNoHistory {
			readonly RenderSystem rs;

			public ScreenshotCmd ( RenderSystem rs ) 
			{
				this.rs = rs;
			}

			public override object Execute()
			{
				rs.Screenshot(null);

				return null;
			}
		}


		class VTRestartCmd : CommandNoHistory {
			readonly RenderSystem rs;
			
			public VTRestartCmd ( RenderSystem rs ) {
				this.rs = rs;
			}
			
			public override object Execute()
			{
				rs.RenderWorld.VirtualTexture = null;
				rs.RenderWorld.VirtualTexture = rs.Game.Content.Load<VirtualTexture>("*megatexture");
				return null;
			}
		}


		class BuildRadCmd : CommandNoHistory 
		{
			[CommandLineParser.Required]
			[CommandLineParser.Name("quality")]
			public QualityLevel QualityLevel { get; set; }


			readonly RenderSystem rs;
			
			public BuildRadCmd ( RenderSystem rs ) {
				this.rs = rs;
			}
			
			public override object Execute()
			{
				rs.RenderWorld?.BuildRadiance( QualityLevel );

				return null;
			}
		}



										  
	}
}
