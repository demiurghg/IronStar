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
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using Fusion.Engine.Audio;
using BEPUphysics.BroadPhaseEntries;
using IronStar.Views;
using IronStar.Items;
using Fusion.Scripting;
using KopiLua;
using IronStar.ECS;

namespace IronStar.SFX2 
{
	public class RenderFacadeSystem : ISystem 
	{
		readonly Game	game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly ContentManager content;


		public RenderFacadeSystem ( Game game )
		{
			this.game	=	game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.content=	game.Content;

			Game_Reloading(this, EventArgs.Empty);
			game.Reloading +=	Game_Reloading;
		}


		public void Intialize( GameState gs )
		{
			throw new NotImplementedException();
		}


		public void Update( GameState gs, GameTime gameTime )
		{
			throw new NotImplementedException();
		}

		
		public void Shutdown( GameState gs )
		{
			throw new NotImplementedException();
		}


		/// <summary>
		/// 
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
				Lua.LuaClose(L);
				KillAllModels();
				game.Reloading -= Game_Reloading;
			}
			base.Dispose( disposing );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Game_Reloading( object sender, EventArgs e )
		{
			world.ForEachEntity( ent => ent?.MakePresentationDirty() );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="elapsedTime"></param>
		/// <param name="lerpFactor"></param>
		public void Update ( GameTime gameTime, float lerpFactor, GameCamera gameCamera, UserCommand userCmd )
		{	
			models.RemoveAll( m => m.Killed );

			foreach ( var model in models ) {
				model.Update( gameTime, lerpFactor );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="current"></param>
		/// <param name="filter">Factor at 60 fps</param>
		/// <param name="factor"></param>
		/// <param name="dt"></param>
		/// <returns></returns>
		float Filter ( float current, float target, float factor, float dt )
		{
			float  factor2	=	(float)( 1 - Math.Pow( 1 - factor, dt * 60 ) );
			return MathUtil.Lerp( current, target, factor2 );
		}
	}
}
