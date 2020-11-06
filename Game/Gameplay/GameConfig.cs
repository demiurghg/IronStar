using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Tools;
using Fusion.Engine.Frames;
using IronStar.SinglePlayer;

namespace IronStar.Gameplay 
{
	public partial class GameConfig : GameComponent 
	{
		[Config]
		public float ThirdPersonCameraOffset { get; set; }

		public GameConfig( Game game ) : base( game )
		{
		}
	}
}
