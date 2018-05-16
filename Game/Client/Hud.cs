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


namespace IronStar.Views {
	public class Hud : GameComponent {

		readonly GameWorld	world;

		DiscTexture	crosshair;
		SpriteFont	hudFont;
		SpriteFont	hudFontSmall;
		SpriteFont	hudFontMicro;
		DiscTexture	iconMachinegun;

		SpriteLayer	hudLayer;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="game"></param>
		/// <param name="space"></param>
		public Hud ( GameWorld world ) : base( world.Game )
		{
			this.world	=	world;
		}


		public override void Initialize()
		{
			LoadContent();
			Game.Reloading += (s,e) => LoadContent();

			hudLayer	=	new SpriteLayer( Game.RenderSystem, 1024 );
			Game.RenderSystem.SpriteLayers.Add( hudLayer );
		}


		/// <summary>
		/// Loads content
		/// </summary>
		void LoadContent()
		{
			crosshair		=	Game.Content.Load<DiscTexture>(@"hud\crosshairA");
			hudFont			=	Game.Content.Load<SpriteFont>(@"hud\hudFont");
			hudFontSmall	=	Game.Content.Load<SpriteFont>(@"hud\hudFontSmall");
			hudFontMicro	=	Game.Content.Load<SpriteFont>(@"hud\hudFontMicro");
			iconMachinegun	=	Game.Content.Load<DiscTexture>(@"hud\machinegun");
		}


		protected override void Dispose( bool disposing )
		{
			Game.RenderSystem.SpriteLayers.Remove( hudLayer );

			if (disposing) {
				SafeDispose( ref hudLayer );
			}
			base.Dispose( disposing );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update ( float elapsedTime, float lerpFactor, GameWorld world )
		{
			hudLayer.Clear();

			var rw	= Game.RenderSystem.RenderWorld;
			var vp	= Game.RenderSystem.DisplayBounds;

			var snapshotHeader	=	world.snapshotHeader;
			var atoms			=	world.Atoms;


			var dimText			=	new Color(255,255,255,128);
			var fullText		=	new Color(255,255,255,224);

			var health			=	snapshotHeader.HudState[(int)HudElement.Health];
			var armor			=	snapshotHeader.HudState[(int)HudElement.Armor];
			var weapon			=	"Assault Rifle";// atoms[ snapshotHeader.HudState[(int)HudElement.Weapon] ];
			var ammo			=	snapshotHeader.HudState[(int)HudElement.WeaponAmmo];

			//
			//	crosshair :
			//
			hudLayer.DrawSprite( crosshair, vp.Width/2, vp.Height/2, crosshair.Width, 0, Color.White ); 

			//
			//	health and armor : 
			//
			int baseLine		=	vp.Height - 16;
			int baseLine2		=	vp.Height - 32;
			int center			=	vp.Width / 2;

			hudLayer.Draw( null, 8, vp.Height-48-8, 320, 48, new Color(0,0,0,128), 0 );

			SmallTextRJ	( hudLayer, string.Format("Health {0,3:000}", health), 16+120, baseLine2,   fullText );
			MicroTextRJ	( hudLayer, string.Format("Armor     {0,3:000}", armor ), 16+120, baseLine,    fullText );

			hudLayer.Draw( null, 144, vp.Height-48, 176, 16, new Color(255,255,255,128), 0 );
			hudLayer.Draw( null, 144, vp.Height-24, 176,  8, new Color(255,255,255,128), 0 );

			var barHealth = Math.Max( 0, Math.Min(176, 176 * health / 100) );
			var barArmor  = Math.Max( 0, Math.Min(176, 176 * armor  / 100) );

			hudLayer.Draw( null, 144, vp.Height-48, barHealth, 16, Color.White, 0 );
			hudLayer.Draw( null, 144, vp.Height-24, barArmor,  8,  Color.White, 0 );

			//
			//	Weapon :
			//
			hudLayer.Draw( null, vp.Width-320-8, vp.Height-48-8, 320, 48, new Color(0,0,0,128), 0 );

			SmallTextRJ	( hudLayer, string.Format("{0,3:000}", ammo), vp.Width - (144+16), baseLine2,   fullText );
			MicroTextRJ	( hudLayer, string.Format("{0}", weapon )   , vp.Width - (144+16), baseLine,    fullText );

			var ammorBar = Math.Max( 0, Math.Min(120, 120 * ammo  / 100) );
			hudLayer.Draw( null, vp.Width-320, vp.Height-48, 120,		16, new Color(255,255,255,128), 0 );
			hudLayer.Draw( null, vp.Width-320, vp.Height-48, ammorBar,	16, Color.White, 0 );

			hudLayer.Draw( iconMachinegun, vp.Width-144-8, vp.Height-48-8, 144,48, Color.White );
			/*hudLayer.Draw( null, 144, vp.Height-48, 176, 16, new Color(255,255,255,128), 0 );
			hudLayer.Draw( null, 144, vp.Height-24, 176,  8, new Color(255,255,255,128), 0 );

			var barHealth = Math.Max( 0, Math.Min(176, 176 * health / 100) );
			var barArmor  = Math.Max( 0, Math.Min(176, 176 * armor  / 100) );

			hudLayer.Draw( null, 144, vp.Height-48, barHealth, 16, Color.White, 0 );
			hudLayer.Draw( null, 144, vp.Height-24, barArmor,  8,  Color.White, 0 );*/

		}



		void SmallTextLJ ( SpriteLayer layer, string text, int x, int y, Color color )
		{
			var r = hudFontSmall.MeasureStringF( text, -2 );
			hudFontSmall.DrawString( layer, text, x, y, color, 0, -2 );
		}

		void SmallTextRJ ( SpriteLayer layer, string text, int x, int y, Color color )
		{
			var r = hudFontSmall.MeasureStringF( text, -2 );
			hudFontSmall.DrawString( layer, text, x-r.Width, y, color, 0, -2 );
		}

		void MicroTextRJ ( SpriteLayer layer, string text, int x, int y, Color color )
		{
			var r = hudFontMicro.MeasureStringF( text, -1 );
			hudFontMicro.DrawString( layer, text, x-r.Width, y, color, 0, -1 );
		}

		void MicroTextLJ ( SpriteLayer layer, string text, int x, int y, Color color )
		{
			var r = hudFontMicro.MeasureStringF( text, -1 );
			hudFontMicro.DrawString( layer, text, x, y, color, 0, -1 );
		}


		void BigTextLJ ( SpriteLayer layer, string text, int x, int y, Color color )
		{
			var r = hudFont.MeasureStringF( text, -4 );
			hudFont.DrawString( layer, text, x, y, color, 0, -4, true, false );
		}

		void BugTextRJ ( SpriteLayer layer, string text, int x, int y, Color color )
		{
			var r = hudFont.MeasureStringF( text, -4 );
			hudFont.DrawString( layer, text, x-r.Width, y, color, -4 );
		}
	}
}
