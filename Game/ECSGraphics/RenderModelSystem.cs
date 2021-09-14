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
		readonly ConcurrentQueue<RenderModelInstance>	detroyQueue;

		struct LerpData
		{												
			public LerpData( RenderModelInstance r, Transform t )
			{
				target	=	r;
				position0	=	t.PrevPosition;
				position1	=	t.Position;
				rotation0	=	t.PrevRotation;
				rotation1	=	t.Rotation;
				scaling		=	t.Scaling;
			}
			public RenderModelInstance	target;
			public Vector3		position0;
			public Vector3		position1;
			public Quaternion	rotation0;
			public Quaternion	rotation1;
			public Vector3		scaling;
		}

		TimeSpan		writeTimestamp;
		TimeSpan		readTimestamp;
		Bag<LerpData>	writeBuffer	=	new Bag<LerpData>(64);
		Bag<LerpData>	readBuffer	=	new Bag<LerpData>(64);
		object			flipLock	=	new object();

		void Flip()
		{
			lock (flipLock)
			{
				Misc.Swap( ref writeTimestamp,	ref readTimestamp );
				Misc.Swap( ref writeBuffer,		ref readBuffer );
			}
		}

		void Interpolate( GameState gs, TimeSpan currentTime )
		{
			lock (flipLock)
			{
				double timestamp	=	readTimestamp.TotalSeconds;
				double time			=	currentTime.TotalSeconds;
				double timestep		=	gs.TimeStep.TotalSeconds;

				float alpha = MathUtil.Clamp( (float)((time - timestamp)/timestep)  , 0, 1 );

				foreach ( var lerpData in readBuffer )
				{
					var scaling		=	lerpData.scaling;
					var rotation	=	Quaternion.Slerp( lerpData.rotation0, lerpData.rotation1, alpha );
					var position	=	Vector3.Lerp	( lerpData.position0, lerpData.position1, alpha );
					var transform	=	Matrix.Scaling( scaling ) * Matrix.RotationQuaternion( rotation ) * Matrix.Translation( position );
					lerpData.target.SetTransform( transform );
				}
			}
		}

		
		public RenderModelSystem ( Game game )
		{
			this.game	=	game;
			this.rs		=	game.RenderSystem;
			this.rw		=	game.RenderSystem.RenderWorld;
			this.content=	game.Content;

			creationQueue	=	new ConcurrentQueue<RenderModelInstance>();
			detroyQueue		=	new ConcurrentQueue<RenderModelInstance>();
		}


		protected override RenderModelInstance Create( Entity e, Transform t, RenderModel rm )
		{
			var model = new RenderModelInstance( e.gs, rm, t.TransformMatrix );
			creationQueue.Enqueue( model );
			return model;
		}

		
		protected override void Destroy( Entity e, RenderModelInstance model )
		{
			detroyQueue.Enqueue( model );
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

			writeBuffer.Clear();
			writeTimestamp = gameTime.Total;

			ForEach( gs, gameTime, (e,gt,rmi,t,rm) => writeBuffer.Add( new LerpData(rmi,t) ) );

			Flip();
		}


		public void Render( GameState gs, GameTime gameTime )
		{
			RenderModelInstance model;
			
			while (creationQueue.TryDequeue(out model))
			{
				model.AddInstances();
			}

			Interpolate(gs, gameTime.Total);
			
			while (detroyQueue.TryDequeue(out model))
			{
				model.RemoveInstances();
			}
		}
	}
}
