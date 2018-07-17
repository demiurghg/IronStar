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
using IronStar.Core;
using Fusion.Engine.Audio;
using BEPUphysics.BroadPhaseEntries;
using IronStar.Views;
using IronStar.Items;

namespace IronStar.SFX {
	public class ModelManager : DisposableBase {

		LinkedList<ModelInstance> models;

		readonly Game			game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly SoundWorld	sw;
		public readonly GameWorld world;

		public Matrix ViewMatrix;
		public Matrix ProjectionMatrix;


		short			weaponModel = 0;
		bool			weaponModelDirty = true;
		ModelInstance	weaponModelInstance = null;



		public ModelManager ( GameWorld world )
		{
			this.world	=	world;
			this.game	=	world.Game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.sw		=	game.SoundSystem.SoundWorld;

			Game_Reloading(this, EventArgs.Empty);
			game.Reloading +=	Game_Reloading;

			models	=	new LinkedList<ModelInstance>();
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing) {
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
			world.ForEachEntity( ent => ent.MakePresentationDirty() );
			weaponModelDirty = true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelAtom"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		public ModelInstance AddModel ( Entity entity, short modelAtom, bool fpv )
		{
			var modelName	=	world.Atoms[ modelAtom ];

			if (string.IsNullOrWhiteSpace(modelName)) {
				return null;
			}

			var modelDesc	=	world.Content.Load<ModelFactory>( @"models\" + modelName );
			var model		=	new ModelInstance( entity, this, modelDesc, world.Content );

			AddModel( model );

			return model;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelInstance"></param>
		public void AddModel ( ModelInstance modelInstance )
		{
			models.AddLast( modelInstance );
		}



		/// <summary>
		/// 
		/// </summary>
		public void KillAllModels ()
		{
			foreach ( var model in models ) {
				model.Kill();
			}
			models.Clear();
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
