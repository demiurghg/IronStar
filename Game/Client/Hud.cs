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
using Fusion.Engine.Input;
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
		public void Update ( float elapsedTime, float lerpFactor )
		{
			hudLayer.Clear();

			var rw	= Game.RenderSystem.RenderWorld;
			var vp	= Game.RenderSystem.DisplayBounds;

			//hudLayer.SetSpriteFrame( 0, new Rectangle(10,10,vp.Width-20, vp.Height-20), Color.Red );

			hudLayer.DrawSprite( crosshair, vp.Width/2, vp.Height/2, crosshair.Width, 0, Color.White ); 

			hudLayer.Draw( null, 0, vp.Height-48, vp.Width, 48, new Color(0,0,0,128) );

			int baseLine	=	vp.Height - 16+8;
			int baseLine2	=	vp.Height - 32+8;
			int center		=	vp.Width / 2;

			var dimText		=	new Color(255,255,255,128);
			var fullText	=	new Color(255,255,255,224);

			short	health	=	100;
			short	armor	=	100;
			var		wpn1	=	"Weapon1";
			var		wpn2	=	"Weapon1";
			short	ammo1	=	9999;
			short	ammo2	=	9999;

			
			SmallTextRJ	( hudLayer, "BULLETS",			center - 4, baseLine2, dimText );
			MicroTextRJ	( hudLayer, wpn1.ToString(),	center - 4, baseLine,  dimText );
			BigTextLJ	( hudLayer, ammo1.ToString(),	center + 4, baseLine,  fullText );


			SmallTextRJ	( hudLayer, "HEALTH",			center - 4 - 200, baseLine2, dimText );
			MicroTextRJ	( hudLayer, "NORMAL",			center - 4 - 200, baseLine,  dimText );
			BigTextLJ	( hudLayer, health.ToString(),	center + 4 - 200, baseLine,  fullText );

			SmallTextRJ	( hudLayer, "ARMOR",			center - 4 + 200, baseLine2, dimText );
			MicroTextRJ	( hudLayer, "HEAVY",			center - 4 + 200, baseLine,  dimText );
			BigTextLJ	( hudLayer, health.ToString(),	center + 4 + 200, baseLine,  fullText );
			/*hudFontSmall.DrawString( hudLayer, "Bullets", vp.Width / 2 - 64, baseLine, Color.Gray, -2 );
			hudFont.DrawString( hudLayer, player.Bullets.ToString(), vp.Width / 2 + 16, baseLine, Color.White, -4 );

			hudFont.DrawString( hudLayer, player.Health.ToString(),  vp.Width / 2 - 32 - 128, baseLine, Color.White, -4 );
			hudFont.DrawString( hudLayer, player.Armor.ToString(),  vp.Width / 2 + 32 + 128, baseLine, Color.White, -4 );*/
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
