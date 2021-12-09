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
	public class SoundTracker : StatelessSystem<SoundComponent>
	{
		public override void Add( IGameState gs, Entity e )
		{
			base.Add( gs, e );

			var sound		= e.GetComponent<SoundComponent>();

			if (!string.IsNullOrWhiteSpace(sound.SoundName))
			{
				var ss		=	gs.Game.GetService<SoundSystem>();

				sound.Timeout	=	ss.GetEventLength(sound.SoundName) + 0.5f;
			}
			else
			{
				sound.Timeout	=	0;
			}
		}

		protected override void Process( Entity entity, GameTime gameTime, SoundComponent sound )
		{
			if (!sound.Looped) 
			{
				if (sound.Timeout<=0)
				{
					entity.Kill();
				}

				sound.Timeout -= gameTime.ElapsedSec;
			}
		}
	}
}
