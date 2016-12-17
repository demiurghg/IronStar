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
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Engine.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;
using IronStar.Views;

namespace IronStar.SFX {
	public class ModelManager {

		Dictionary<string,ModelDescriptor> modelDescriptors;

		LinkedList<ModelInstance> models;

		readonly Game			game;
		public readonly ShooterClient	client;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly SoundWorld	sw;
		public readonly GameWorld world;

		public ModelManager ( ShooterClient client, GameWorld world )
		{
			this.world	=	world;
			this.client	=	client;
			this.game	=	client.Game;
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
		public void Shutdown ()
		{
			KillAllModels();
			game.Reloading -= Game_Reloading;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelAtom"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		public ModelInstance AddModel ( short modelAtom, Entity entity )
		{
			ModelDescriptor modelDesc;

			var modelName	=	client.Atoms[modelAtom];

			if (!modelDescriptors.TryGetValue( modelName, out modelDesc )) {	
				Log.Warning("Model '{0}' does not exist", modelName );
				return null;
			}

			var scene	=	client.Content.Load<Scene>( modelDesc.ModelPath );

			var model	=	new ModelInstance( this, modelDesc, scene, entity );

			models.AddLast(model);

			return model;
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
		public void Update ( float elapsedTime, float lerpFactor )
		{	
			models.RemoveAll( m => m.Killed );

			foreach ( var model in models ) {
				model.Update( elapsedTime, lerpFactor );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Game_Reloading ( object sender, EventArgs e )
		{
			modelDescriptors	=	ModelDescriptor
					.LoadCollectionFromXml( client.Content.Load<string>(@"scripts\models") )
					.ToDictionary( md => md.Name );
		}

	}
}
