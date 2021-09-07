using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Extensions;
using IronStar.SFX;
using System.ComponentModel;
using Fusion.Core.Shell;
using Fusion.Core;
using Fusion;
using Fusion.Widgets.Advanced;

namespace IronStar 
{
	public enum FuncFXMode 
	{
		Persistent,
		AutoTrigger,
		Trigger,
	}


	/// <summary>
	/// https://www.iddevnet.com/quake4/Entity_FuncFX
	/// </summary>
	public class FuncFXFactory : EntityFactoryContent {

		[AECategory("FX")]
		[Description("Name of the FX object")]
		public string FX { get; set; } = "";

		[AECategory("FX")]
		[Description("FX mode")]
		public FuncFXMode FXMode { get; set; }

		[AECategory("FX")]
		[Description("Indicated that given effect could be trigerred only once")]
		public bool Once { get; set; }

		[AECategory("FX")]
		[Description("Indicated that given effect is enabled by default")]
		public bool Start { get; set; }

		[AECategory("FX")]
		[Description("Min interval (msec) between auto-triggered events")]
		public int MinInterval { get; set; } = 1;

		[AECategory("FX")]
		[Description("Max interval (msec) between auto-triggered events")]
		public int MaxInterval { get; set; } = 1;


		public override ECS.Entity SpawnECS( ECS.IGameState gs )
		{
			Log.Warning("SpawnECS -- {0} -- (not implemented completely)", GetType().Name);
			
			var e = gs.Spawn();

			e.AddComponent( new ECS.Transform() );
			e.AddComponent( new FXComponent(FX,true) );

			return e;
		}
	}
}
