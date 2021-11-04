using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using IronStar.ECS;
using IronStar.Gameplay.Components;
using BEPUutilities.Threading;
using Fusion.Core.Extensions;

namespace IronStar.SFX 
{
	public class FXTracker : StatelessSystem<FXComponent>
	{
		public override void Add( IGameState gs, Entity e )
		{
			base.Add( gs, e );
			var fx		= e.GetComponent<FXComponent>();

			if (fx.FXName!=null && !fx.FXName.StartsWith("*"))
			{
				var ss		=	gs.Game.GetService<SoundSystem>();
				var fact	=	gs.Content.Load<FXFactory>( Path.Combine("fx", fx.FXName) );

				fx.Timeout	=	fact.GetEstimatedLifetime( ss );
			}
			else
			{
				fx.Timeout	=	0;
			}
		}

		protected override void Process( Entity entity, GameTime gameTime, FXComponent fx )
		{
			if (!fx.Looped) 
			{
				if (fx.Timeout<=0)
				{
					entity.Kill();
				}

				fx.Timeout -= gameTime.ElapsedSec;
			}
		}
	}
}
