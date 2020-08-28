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

namespace IronStar.UI.HUD 
{
	public class HudHealth : Frame 
	{
		Frame labelArmor;
		Frame labelHealth;

		Frame numberArmor;
		Frame numberHealth;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public HudHealth ( Frame parent, int x, int y ) : base( parent.Frames )
		{
			this.BackColor		=	HudColors.BackgroundColor;
			this.BorderColor	=	HudColors.BorderColor;
			this.Border			=	1;
			this.Padding		=	4;

			this.X				=	x;
			this.Y				=	y;
			this.Width			=	120;
			this.Height			=	24;

			this.Anchor			=	FrameAnchor.Bottom | FrameAnchor.Left;

			this.Ghost			=	true;

			labelArmor		=	new Frame( Frames, 4,4+0,48,8,"Armor", Color.Zero) {
				ForeColor	=	HudColors.TextColor,
			};

			labelHealth		=	new Frame( Frames, 4,4+8,48,8,"Health", Color.Zero) {
				ForeColor	=	HudColors.TextColor,
			};

			numberArmor		=	new Frame( Frames, 60,4+0,56,8," 32/100", Color.Zero) {
				ForeColor	=	HudColors.ArmorColor,
			};

			numberHealth	=	new Frame( Frames, 60,4+8,56,8," 97/100", Color.Zero) {
				ForeColor	=	HudColors.HealthColor,
			};

			Add( labelArmor );
			Add( labelHealth );

			Add( numberArmor );
			Add( numberHealth );
		
			parent.Add(this);
		}


		protected override void Update( GameTime gameTime )
		{
			base.Update( gameTime );

			if (Player!=null) {

				Visible		=	true;

				var health	=	Player.Health;
				var armor	=	Player.Armor;

				numberArmor .Text = string.Format("{0}/{1}", armor , 100 );
				numberHealth.Text = string.Format("{0}/{1}", health, 100 );

			} else {

				Visible		=	false;
			}
		}




	}
}
