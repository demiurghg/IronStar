﻿using System;
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
			Game.Invoker.RegisterCommand("screenshot",	(args) => new ScreenshotCmd(this, args)	);
			Game.Invoker.RegisterCommand("vtrestart",	(args) => new VTRestartCmd(this)	);
			Game.Invoker.RegisterCommand("buildrad",	(args) => new BuildRadCmd(this)		);
		}


		class ScreenshotCmd : CommandNoHistory {
			readonly RenderSystem rs;

			public ScreenshotCmd ( RenderSystem rs, ArgList args ) 
			{
				this.rs = rs;
				//args.Usage("screenshot [/open]")
				//	.Option("/open", out open)
				//	.Apply();
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


		class BuildRadCmd : CommandNoHistory {
			readonly RenderSystem rs;
			
			public BuildRadCmd ( RenderSystem rs ) {
				this.rs = rs;
			}
			
			public override object Execute()
			{
				if (rs.RenderWorld!=null) {
					rs.RenderWorld?.CaptureRadiance();
				} else {
					throw new InvalidOperationException("BuildRadCmd: Render world is not set");
				}
				return null;
			}
		}



										  
	}
}