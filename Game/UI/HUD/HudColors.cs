﻿using System;
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
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Engine.Frames;

namespace IronStar.UI.HUD 
{
	public static class HudColors 
	{
		public static SpriteFont NormalFont = null;
		public static SpriteFont HeaderFont = null;
		public static SpriteFont SmallFont = null;

		internal static void LoadContent( ContentManager content )
		{
			NormalFont	=	content.Load<SpriteFont>(@"fonts\aldrich_16pt");
			HeaderFont	=	content.Load<SpriteFont>(@"fonts\aldrich_24pt");
			SmallFont	=	content.Load<SpriteFont>(@"fonts\aldrich_12pt");
		}

		public static Color TextColor		=	new Color(255,255,255,255);
		public static Color TextColorDim	=	new Color(255,255,255,104);
		public static Color TextShadow		=	new Color(  0,  0,  0,104);

		public static Color BackgroundColor	=	new Color( 0,0,0, 96 );

		public static Color BorderColor		=	new Color( 0,0,0,128 );

		public static Color HealthColor		=	new Color(129,187,207, 255);
		public static Color ArmorColor		=	new Color( 73,192, 98, 255);
		public static Color AmmoColor		=	new Color(211,160, 39, 255);

		
		public static Color ShadowColor		=	new Color(  0,  0,  0, 192);
		public static Color MessageColor	=	new Color(255,255,255, 255);
		public static Color WarningColor	=	new Color(244, 38, 49, 255);


	}
}
