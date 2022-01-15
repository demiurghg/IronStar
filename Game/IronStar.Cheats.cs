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
using System.Reflection;

namespace IronStar {
	partial class IronStar : Game
	{
		public static bool IsGodMode = false;
		public static bool IsInfiniteAmmo = false;
		public static bool IsNoTarget = false;

		void RegisterCheats()
		{
			Invoker.RegisterCommand("godMode"		, () => new GodMode() );
			Invoker.RegisterCommand("noTarget"		, () => new NoTarget() );
			Invoker.RegisterCommand("giveAll"		, () => new GiveAll() );
		}


		class GodMode : ICommand 
		{
			public object Execute()
			{
				IsGodMode = !IsGodMode;
				Log.Message("God mode " + (IsGodMode ? "enabled" : "disabled"));
				return null;
			}
		}


		class NoTarget : ICommand 
		{
			public object Execute()
			{
				IsNoTarget = !IsNoTarget;
				Log.Message("AI " + (IsNoTarget ? "disabled" : "enabled"));
				return null;
			}
		}

		
		class GiveAll : ICommand 
		{
			public object Execute()
			{
				Gameplay.Systems.PickupSystem.RequestGiveAll = true;
				return null;
			}
		}
	}
}
