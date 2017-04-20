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
using Fusion.Engine.Audio;
using IronStar.Views;

namespace IronStar.SFX {
	public class ModelInstance {

		readonly Matrix preTransform;
		readonly Color4 color;
		readonly ModelManager modelManager;
		readonly Entity entity;
		readonly Scene scene;
		readonly Scene[] clips;
		readonly bool useAnimation;
		readonly string fpvCamera;
		readonly bool fpvEnabled;
		readonly Matrix fpvCameraMatrix;
		readonly Matrix fpvViewMatrix;
		readonly int fpvCameraIndex;

		Matrix[] globalTransforms;
		Matrix[] animSnapshot;
		MeshInstance[] meshInstances;

		readonly int nodeCount;

		public bool Killed {
			get; private set;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelManager"></param>
		/// <param name="descriptor"></param>
		/// <param name="scene"></param>
		/// <param name="entity"></param>
		/// <param name="matrix"></param>
		public ModelInstance ( ModelManager modelManager, ModelDescriptor descriptor, ContentManager content, Entity entity )
		{
			this.scene			=	content.Load<Scene>( descriptor.ScenePath );
			this.clips			=	descriptor.LoadClips( content );

			foreach ( var clip in clips ) {
				Log.Message("...clip: {0} [{1} {2}]", clip.TakeName, clip.FirstTakeFrame, clip.LastTakeFrame);
			}

			this.modelManager   =   modelManager;
			this.preTransform   =   descriptor.ComputePreTransformMatrix();
			this.entity			=	entity;
			this.color			=	descriptor.Color;
			this.useAnimation	=	descriptor.UseAnimation;

			this.fpvEnabled		=	descriptor.FPVEnable;
			this.fpvCamera		=	descriptor.FPVCamera;

			nodeCount			=	scene.Nodes.Count;

			globalTransforms	=	new Matrix[ scene.Nodes.Count ];
			animSnapshot		=	new Matrix[ scene.Nodes.Count ];
			scene.ComputeAbsoluteTransforms( globalTransforms );



			if (fpvEnabled) {
				fpvCameraIndex		=	scene.GetNodeIndex( fpvCamera );

				if (fpvCameraIndex<0) {	
					Log.Warning("Camera node {0} does not exist", fpvCamera);
				} else {
					fpvCameraMatrix	=	Matrix.RotationY( -MathUtil.PiOverTwo ) * globalTransforms[ fpvCameraIndex ];
					fpvViewMatrix	=	Matrix.Invert( fpvCameraMatrix );

					preTransform	=	fpvViewMatrix * Matrix.Scaling( descriptor.Scale );
				}
			} else {
				preTransform	=	Matrix.Scaling( descriptor.Scale );	
			}


			meshInstances		=	new MeshInstance[ scene.Nodes.Count ];

			for ( int i=0; i<nodeCount; i++ ) {
				var meshIndex = scene.Nodes[i].MeshIndex;
				
				if (meshIndex>=0) {
					meshInstances[i]		= new MeshInstance( modelManager.rs, scene, scene.Meshes[meshIndex] );
					meshInstances[i].FPView = fpvEnabled;
					modelManager.rw.Instances.Add( meshInstances[i] );
				} else {
					meshInstances[i] = null;
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="animFrame"></param>
		/// <param name="worldMatrix"></param>
		public void Update ( float dt, float animFrame, Matrix worldMatrix )
		{
			//
			//	do animation stuff :
			//
			if (useAnimation) {

				EvaluateFrame( animFrame );

			} else {

				ResetPose();

			}

			
			//
			//	apply transforms and colors
			//
			for ( int i = 0; i<nodeCount; i++ ) {
				if (meshInstances[i]!=null) {
					meshInstances[i].World = animSnapshot[i] * preTransform * worldMatrix;
					meshInstances[i].Color = color;
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="lerpFactor"></param>
		public void Update ( float dt, float lerpFactor )
		{
			var animFrame = entity.AnimFrame;
			var worldMatrix = entity.GetWorldMatrix( lerpFactor );

			Update( dt, animFrame, worldMatrix );
		}



		/// <summary>
		/// Marks current model instance to remove.
		/// </summary>
		public void Kill ()
		{
			for ( int i = 0; i<nodeCount; i++ ) {
				if (meshInstances[i]!=null) {
					modelManager.rw.Instances.Remove( meshInstances[i] );
				}
			}
			Killed = true;
		}


		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Animation stuff :
		 * 
		-----------------------------------------------------------------------------------------------*/

		/// <summary>
		/// 
		/// </summary>
		public void ResetPose ()
		{
			scene.ComputeAbsoluteTransforms( animSnapshot );
		}



		/// <summary>
		/// 
		/// </summary>
		public void SetBindPose ()
		{
			throw new NotImplementedException();
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		public void EvaluateFrame ( float animFrame )
		{
			if (animFrame>scene.LastFrame) {
				Log.Warning("Anim frame: {0} > {1}", animFrame, scene.LastFrame);
			}

			if (animFrame<scene.FirstFrame) {
				Log.Warning("Anim frame: {0} < {1}", animFrame, scene.FirstFrame);
			}

			animFrame = MathUtil.Clamp( animFrame, scene.FirstFrame, scene.LastFrame );

			scene.GetAnimSnapshot( animFrame, scene.FirstFrame, scene.LastFrame, AnimationMode.Clamp, animSnapshot );
			scene.ComputeAbsoluteTransforms( animSnapshot, animSnapshot );
		}


		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Tracking stuff :
		 * 
		-----------------------------------------------------------------------------------------------*/

		readonly List<AnimTrack22> tracks = new List<AnimTrack22>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>
		/// <param name="immediate"></param>
		public void Stop ( int track, bool immediate )
		{
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="track"></param>
		/// <param name="clip"></param>
		/// <param name="looped"></param>
		/// <param name="fadein"></param>
		/// <param name="fadeout"></param>
		public void Play ( string clip, bool looped, float weight )
		{
			var sourceClip  = clips.FirstOrDefault( scene => scene.TakeName==clip );

			if (sourceClip==null) {
				Log.Warning("Clip {0} does not exist", clip);
			}

			var track = new AnimTrack22( sourceClip, looped, 0.1f, 0.1f, 60 );

			tracks.Add( track );
		}
	}
}
