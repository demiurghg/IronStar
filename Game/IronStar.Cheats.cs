using Fusion.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using Fusion.Engine.Frames;
using Fusion.Engine.Tools;
using Fusion;
using Fusion.Core.Shell;
using IronStar.Editor;
using Fusion.Build;
using IronStar.SinglePlayer;

namespace IronStar {
	partial class IronStar : Game
	{
		public static bool IsGodMode = false;

		void RegisterCheats()
		{
			Invoker.RegisterCommand("godMode"		, () => new God(this) );
		}

		class God : CommandNoHistory 
		{
			readonly IronStar game;

			public God ( IronStar game )
			{
				this.game	=	game;
			}

			public override object Execute()
			{
				IsGodMode = !IsGodMode;
				Log.Message("God mode " + (IsGodMode ? "enabled" : "disabled"));
				return null;
			}

		}
	}
}
