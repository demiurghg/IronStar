using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Core.Input;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;

namespace SpriteDemo
{
	class SpriteDemo : Game
	{
		SpriteLayer	spriteLayer;

		DiscTexture	textureLena;
		DiscTexture	textureManul;
		DiscTexture	textureLenaLow;
		DiscTexture	textureManulLow;

		public SpriteDemo( string gameId, string gameTitle ) : base( gameId, gameTitle )
		{
			this.AddServiceAndComponent( 100, new RenderSystem(this, true) );
		}


		protected override void Initialize()
		{
			base.Initialize();

			textureLena			=	Content.Load<DiscTexture>("lena");
			textureManul		=	Content.Load<DiscTexture>("manul");
			textureLenaLow		=	Content.Load<DiscTexture>("lena_lowres");
			textureManulLow		=	Content.Load<DiscTexture>("manul_lowres");

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


		float rotationAngle = 0;

		protected override void Update( GameTime gameTime )
		{
			if (Keyboard.IsKeyDown(Keys.Escape))
			{
				Exit();
			}
			if (Keyboard.IsKeyDown(Keys.D1))
			{
				spriteLayer.FilterMode	=	SpriteFilterMode.LinearWrap;
			}
			if (Keyboard.IsKeyDown(Keys.D2))
			{
				spriteLayer.FilterMode	=	SpriteFilterMode.PointWrap;
			}

			rotationAngle += gameTime.ElapsedSec * (-MathUtil.PiOverTwo);
			
			spriteLayer.Projection	=	Matrix.OrthoOffCenterRH(0,0,1024,768,-1,1);
			spriteLayer.Clear();

			spriteLayer.Draw( textureLena,   16, 64, 256, 256, Color.White );
			spriteLayer.Draw( textureManul, 288, 64, 256, 256, Color.White );

			spriteLayer.SetClipRectangle( 0, new Rectangle( 16,338,256,256), Color.Yellow );
			spriteLayer.SetClipRectangle( 1, new Rectangle(288,338,256,256), Color.Red    );

			spriteLayer.DrawSprite( textureLena , 144, 464, 256, rotationAngle, Color.White, 0 );
			spriteLayer.DrawSprite( textureManul, 416, 464, 256, rotationAngle, Color.White, 1 );


			spriteLayer.Draw( textureLenaLow,   16 + 544, 64, 256, 256, Color.White );
			spriteLayer.Draw( textureManulLow, 288 + 544, 64, 256, 256, Color.White );

			spriteLayer.SetClipRectangle( 2, new Rectangle( 16 + 544,338,256,256), Color.Yellow );
			spriteLayer.SetClipRectangle( 3, new Rectangle(288 + 544,338,256,256), Color.Red    );

			spriteLayer.DrawSprite( textureLenaLow , 144 + 544, 464, 256, rotationAngle, Color.White, 2 );
			spriteLayer.DrawSprite( textureManulLow, 416 + 544, 464, 256, rotationAngle, Color.White, 3 );

			
			spriteLayer.DrawDebugString( 16, 8 + 8*0, string.Format("Sprite Demo - FPS = {0}", gameTime.Fps), Color.Orange );
			spriteLayer.DrawDebugString( 16, 8 + 8*2, "[ 1 ] - Linear Filter", Color.White );
			spriteLayer.DrawDebugString( 16, 8 + 8*3, "[ 2 ] - Point Filter", Color.White );
			spriteLayer.DrawDebugString( 16, 8 + 8*4, "[ESC] - Exit", Color.White );
		}
	}
}
