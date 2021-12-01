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
using IronStar.Gameplay.Components;

namespace IronStar.ECSPhysics
{
	public class KinematicSystem : ProcessingSystem<KinematicController, Transform, KinematicComponent, RenderModel, BoneComponent>, ITransformFeeder
	{
		PhysicsCore physics;

		public KinematicSystem( PhysicsCore physics )
		{
			this.physics	=	physics;
		}

		
		protected override KinematicController Create( Entity entity, Transform transform, KinematicComponent kinematic, RenderModel renderModel, BoneComponent bones )
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

		
		protected override void Process( Entity entity, GameTime gameTime, KinematicController controller, Transform transform, KinematicComponent kinematic, RenderModel renderModel, BoneComponent bones )
		{
			bool skipSimulation = entity.gs.Paused;

			var dt = TimeSpan.FromSeconds( gameTime.ElapsedSec );
			var length = controller.AnimLength;
			var time = kinematic.Time;
			var state = kinematic.State;
			var stuck = kinematic.Stuck;

			//Log.Debug("STATE: {0} STUCK: {1}", state.ToString(), stuck ? "YES" : "NO");

			switch (state)
			{
				case KinematicState.StoppedInitial:
					time = TimeSpan.Zero;
					break;

				case KinematicState.StoppedTerminal:
					time = length;
					break;

				case KinematicState.PlayLooped:
					time = WrapTime( time + dt, length );
					break;

				case KinematicState.PlayForward:
					time += dt;

					if (time>=length) 
					{
						time = length;
						state = KinematicState.StoppedTerminal;
					}
					break;

				case KinematicState.PlayBackward:
					time -= dt;

					if (time<=TimeSpan.Zero) 
					{
						time = TimeSpan.Zero;
						state = KinematicState.StoppedInitial;
					}
					break;

				default:
					break;

			}

			kinematic.Time	=	time;
			kinematic.State	=	state;

			controller.Animate( transform.TransformMatrix, kinematic.Time, bones.Bones, skipSimulation );
		}

		
		void FeedTransform( Entity entity, GameTime gameTime, KinematicController controller, Transform transform, KinematicComponent kinematic, RenderModel renderModel, BoneComponent bones )
		{
			#warning Possible numerical issues with inverted transform matrix
			controller.GetTransform( Matrix.Invert( transform.TransformMatrix ), bones.Bones );
			
			kinematic.Stuck = controller.SquishTargets( target => Squish(entity,kinematic,target) );
		}


		void Squish( Entity kinematicEntity, KinematicComponent kinematic, Entity target )
		{
			var health = target.GetComponent<HealthComponent>();
			health?.InflictDamage( kinematic.Damage, kinematicEntity );	
		}

		
		public void FeedTransform( IGameState gs, GameTime gameTime )
		{
			ForEach( gs, gameTime, FeedTransform );
		}


		TimeSpan WrapTime( TimeSpan time, TimeSpan length )
		{
			return new TimeSpan( MathUtil.Wrap( time.Ticks, 0, length.Ticks ) );
		}
	}
}
