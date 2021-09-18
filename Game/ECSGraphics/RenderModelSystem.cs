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
using Fusion.Scripting;
using KopiLua;
using IronStar.ECS;
using IronStar.Animation;
using System.Collections.Concurrent;

namespace IronStar.SFX2 
{
	public class RenderModelSystem : ProcessingSystem<RenderModelInstance,Transform,RenderModel>, IRenderer
	{
		readonly Game	game;
		public readonly RenderSystem rs;
		public readonly RenderWorld	rw;
		public readonly ContentManager content;

		readonly Aspect skinnedAspect = new Aspect().Include<RenderModelInstance,Transform,RenderModel,BoneComponent>();
		readonly Aspect rigidAspect   = new Aspect().Include<RenderModelInstance,Transform,RenderModel>()
													.Exclude<BoneComponent>();

		readonly ConcurrentQueue<RenderModelInstance>	creationQueue;
		readonly ConcurrentQueue<RenderModelInstance>	destroyQueue;

		struct LerpData
		{												
			public LerpData( RenderModelInstance r, Transform t )
			{
				target	=	r;
				position0	=	t.Position;
				position1	=	t.Position;
				rotation0	=	t.Rotation;
				rotation1	=	t.Rotation;
				scaling		=	t.Scaling;
			}
			public RenderModelInstance	target;
			public Vector3		position0;
			public Vector3		position1;
			public Quaternion	rotation0;
			public Quaternion	rotation1;
			public Vector3		scaling;

			public void Interpolate( float alpha )
			{
				var rotation	=	Quaternion.Slerp( rotation0, rotation1, alpha );
				var position	=	Vector3.Lerp	( position0, position1, alpha );
				var transform	=	Matrix.Scaling( scaling ) * Matrix.RotationQuaternion( rotation ) * Matrix.Translation( position );
				target.SetTransform( transform );
			}
		}

		BufferedList<LerpData> lerpBuffer;


		
		public RenderModelSystem ( Game game )
		{
			this.game	=	game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.content=	game.Content;

			creationQueue	=	new ConcurrentQueue<RenderModelInstance>();
			destroyQueue		=	new ConcurrentQueue<RenderModelInstance>();
			lerpBuffer		=	new BufferedList<LerpData>(64);
		}


		protected override RenderModelInstance Create( Entity e, Transform t, RenderModel rm )
		{
			var model = new RenderModelInstance( e.gs, rm, t.TransformMatrix );
			creationQueue.Enqueue( model );
			return model;
		}

		
		protected override void Destroy( Entity e, RenderModelInstance model )
		{
			destroyQueue.Enqueue( model );
		}

		
		protected override void Process( Entity e, GameTime gameTime, RenderModelInstance model, Transform t, RenderModel rm )
		{
			/*model.SetTransform( t.TransformMatrix );

			stateBuffer.SetTimeStamp();
			stateBuffer.WriteTransform( model, transform1, transform2 )
			stateBuffer.Flip();

			if (skinnedAspect.Accept(e))
			{
				var bones = e.GetComponent<BoneComponent>()?.Bones;

				if (bones!=null)
				{
					model.SetBoneTransforms( e.GetComponent<BoneComponent>().Bones );
				}
			}  */
		}

		public override void Update( GameState gs, GameTime gameTime )
		{
			base.Update( gs, gameTime );

			ForEach( gs, gameTime, (e,gt,rmi,t,rm) => lerpBuffer.Add( new LerpData(rmi,t) ) );

			lerpBuffer.Flip(gameTime);
		}


		public void Render( GameState gs, GameTime gameTime )
		{
			RenderModelInstance model;
			
			while (creationQueue.TryDequeue(out model))
			{
				model.AddInstances();
			}

			lerpBuffer.Interpolate(gs, gameTime, (data,alpha) => data.Interpolate(alpha));
			
			while (destroyQueue.TryDequeue(out model))
			{
				model.RemoveInstances();
			}
		}
	}
}
