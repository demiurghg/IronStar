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
using BEPUphysics;
using BEPUphysics.Character;
using Fusion.Engine.Frames;

namespace IronStar.UI.HUD {
	public class HudWeapon : Frame {

		public Player Player;

		Frame numberAmmo;
		Frame labelWeapon;
		Frame labelArsenal;

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

			labelWeapon	=	new Frame( Frames, 68,4+0,160,8,"Assault Rifle", Color.Zero) {
				ForeColor	=	HudColors.TextColor,
			};

			numberAmmo		=	new Frame( Frames, 4,4+0,56,8," 32/100", Color.Zero) {
				ForeColor	=	HudColors.AmmoColor,
			};

			Add( labelWeapon );
			Add( numberAmmo );
		
			parent.Add(this);
		}




		protected override void Update( GameTime gameTime )
		{
			base.Update( gameTime );

			var weapon  = Player?.GetCurrentWeapon();

			if (weapon!=null) {

				Visible			=	true;

				var weaponName	=	weapon.NiceName;
				var ammo		=	weapon.GetPlayerAmmo();

				var count		=	(ammo == null) ? 0 : ammo.Count;
				var maxCount	=	(ammo == null) ? 0 : ammo.MaxCount;

				labelWeapon .Text = string.Format("{0}"		, weaponName );
				numberAmmo	.Text = string.Format("{0}/{1}"	, count, maxCount );

			} else {

				Visible		=	false;
			}
		}
		


	}
}
