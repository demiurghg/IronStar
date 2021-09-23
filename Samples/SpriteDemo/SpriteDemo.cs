using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace SpriteDemo
{
	class SpriteDemo : Game
	{
		SpriteLayer	spriteLayer;
		DiscTexture	textureLena;
		DiscTexture	textureManul;

		public SpriteDemo( string gameId, string gameTitle ) : base( gameId, gameTitle )
		{
			this.AddServiceAndComponent( 100, new RenderSystem(this, true) );
		}


		protected override void Initialize()
		{
			base.Initialize();

			textureLena		=	Content.Load<DiscTexture>("lena");
			textureManul	=	Content.Load<DiscTexture>("manul");

			spriteLayer	=	new SpriteLayer( RenderSystem, 1024 );

			RenderSystem.SpriteLayers.Add( spriteLayer );
		}


		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				//	dispose disposable stuff here
			}

			base.Dispose( disposing );
		}


		protected override void Update( GameTime gameTime )
		{
			if (Keyboard.IsKeyDown(Fusion.Core.Input.Keys.Escape))
			{
				Exit();
			}

			spriteLayer.SetClipRectangle(0, new Rectangle(0,0,10000,10000), Color.White);
			spriteLayer.Draw( textureLena, 160,160, 256,256, Color.White );
			spriteLayer.Projection	=	Matrix.OrthoOffCenterRH(0,0,1024,768,-1,1);
		}
	}
}
