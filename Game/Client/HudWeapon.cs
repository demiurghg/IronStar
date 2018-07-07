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
using IronStar.Core;
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Engine.Frames;
using IronStar.Entities.Players;

namespace IronStar.Views {
	public class HudWeapon : Frame {

		readonly GameWorld	world;

		public string Weapon1;
		public string Weapon2;
		public int Ammo1;
		public int Ammo2;

		Frame numberAmmo1;
		Frame numberAmmo2;

		Frame labelWeapon1;
		Frame labelWeapon2;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public HudWeapon ( Frame parent, int x, int y ) : base( parent.Frames )
		{
			this.BackColor		=	HudColors.BackgroundColor;
			this.BorderColor	=	HudColors.BorderColor;
			this.Border			=	1;
			this.Padding		=	4;

			this.X				=	x;
			this.Y				=	y;
			this.Width			=	200;
			this.Height			=	24;

			this.Anchor			=	FrameAnchor.Bottom | FrameAnchor.Right;

			this.Ghost			=	true;

			labelWeapon1	=	new Frame( Frames, 68,4+0,160,8,"Assault Rifle", Color.Zero) {
				ForeColor	=	HudColors.TextColor,
			};

			labelWeapon2	=	new Frame( Frames, 68,4+8,160,8,"Rocket Launcher", Color.Zero) {
				ForeColor	=	HudColors.TextColorDim,
			};

			numberAmmo1		=	new Frame( Frames, 4,4+0,56,8," 32/100", Color.Zero) {
				ForeColor	=	HudColors.AmmoColor,
			};

			numberAmmo2		=	new Frame( Frames, 4,4+8,56,8," 97/100", Color.Zero) {
				ForeColor	=	HudColors.TextColorDim,
			};

			Add( labelWeapon1 );
			Add( labelWeapon2 );

			Add( numberAmmo1 );
			Add( numberAmmo2 );
		
			parent.Add(this);
		}




		


	}
}
