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
using BEPUCharacterController = BEPUphysics.Character.CharacterController;
using Fusion.Core.IniParser.Model;
using IronStar.ECS;
using Fusion.Engine.Graphics.Scenes;
using AffineTransform = BEPUutilities.AffineTransform;
using IronStar.SFX2;
using IronStar.Animation;

namespace IronStar.ECSPhysics
{
	public class KinematicSystem : ProcessingSystem<KinematicController, Transform, KinematicModel, RenderModel, BoneComponent>, ITransformFeeder
	{
		PhysicsCore physics;

		public KinematicSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}

		
		protected override KinematicController Create( Entity entity, Transform transform, KinematicModel kinematic, RenderModel renderModel, BoneComponent bones )
		{
			Scene scene;

			if (!entity.gs.Content.TryLoad( renderModel.scenePath, out scene ))
			{
				scene	=	Scene.CreateEmptyScene();
			}

			return new KinematicController( physics, entity, scene, transform.TransformMatrix );
		}

		
		protected override void Destroy( Entity entity, KinematicController controller )
		{
			controller.Destroy( physics );
		}

		
		protected override void Process( Entity entity, GameTime gameTime, KinematicController controller, Transform transform, KinematicModel kinematic, RenderModel renderModel, BoneComponent bones )
		{
			bool skipSimulation = entity.gs.Paused;
			controller.Animate( transform.TransformMatrix, kinematic, bones.Bones, skipSimulation );

			kinematic.Time += TimeSpan.FromSeconds( gameTime.ElapsedSec * 0.1f );
			//kinematic.Time += gameTime.Elapsed;
		}

		
		void FeedTransform( Entity entity, GameTime gameTime, KinematicController controller, Transform transform, KinematicModel kinematic, RenderModel renderModel, BoneComponent bones )
		{
			#warning Possible numerical issues with inverted transform matrix
			controller.GetTransform( Matrix.Invert( transform.TransformMatrix ), bones.Bones );
			
			controller.SquishTargets( e => Log.Debug("SQUISHING : {0}", e) );
		}

		
		public void FeedTransform( IGameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, FeedTransform );
		}
	}
}
