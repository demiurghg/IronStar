using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Extensions;
using IronStar.Core;
using IronStar.SFX;
using System.ComponentModel;
using Fusion.Core.Shell;
using Fusion.Core;
using Fusion;

namespace IronStar.Entities {

	public class FuncFX : Entity {
		
		static Random rand = new Random();

		readonly string fx;
		readonly FuncFXMode fxMode;
		readonly bool once;
		readonly float minInterval;
		readonly float maxInterval;
		readonly bool start;
		readonly short atom;

		int activationCount = 0;
		float timer = 0;
		bool enabled;

		public FuncFX( uint id, short clsid, GameWorld world, FuncFXFactory factory ) : base(id, clsid, world, factory)
		{
			fx			=	factory.FX;
			fxMode		=	factory.FXMode;
			once		=	factory.Once;
			minInterval	=	factory.MinInterval;
			maxInterval	=	factory.MaxInterval;
			start		=	factory.Start;
			enabled		=	start;

			atom		=	world.Atoms[ fx ];

			if (fxMode==FuncFXMode.Persistent && enabled) {
				Sfx = atom;
			} else {
				Sfx = 0;
			}
		}


		public override void Activate( Entity activator )
		{
			if (once && activationCount>0) {
				return;
			}

			activationCount ++;

			if (fxMode==FuncFXMode.Trigger) {
				World.SpawnFX( fx, 0, Position, LinearVelocity, Rotation );
			} else {
				enabled = !enabled;
			}
		}


		public override void Update( GameTime gameTime )
		{
			base.Update(gameTime);

			float elapsedTime = gameTime.ElapsedSec;

			if (fxMode==FuncFXMode.AutoTrigger) 
			{
				if (enabled) 
				{
					timer -= elapsedTime;

					if (timer<0) 
					{
						World.SpawnFX( fx, 0, Position, LinearVelocity, Rotation );
						timer = rand.NextFloat( minInterval, maxInterval );
					}
				} else {
					timer = 0;
				}
			}

			if (fxMode==FuncFXMode.Persistent)
			{
				Sfx = enabled ? atom : (short)0;
			}
		}


		public override void Kill()
		{
			base.Kill();
		}

	}



	public enum FuncFXMode {
		Persistent,
		AutoTrigger,
		Trigger,
	}



	/// <summary>
	/// https://www.iddevnet.com/quake4/Entity_FuncFX
	/// </summary>
	public class FuncFXFactory : EntityFactory {

		[AECategory("FX")]
		[Description("Name of the FX object")]
		[AEClassname("fx")]
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


		public override void SpawnECS( ECS.GameState gs )
		{
			Log.Warning("SpawnECS -- {0}", GetType().Name);
		}


		public override Entity Spawn( uint id, short clsid, GameWorld world )
		{
			return new FuncFX( id, clsid, world, this );
		}
	}
}
