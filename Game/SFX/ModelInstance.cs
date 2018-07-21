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
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;
using IronStar.Views;


namespace IronStar.SFX {


	public class ModelInstance {

		static readonly Scene EmptyScene = Scene.CreateEmptyScene();

		readonly public Entity Entity;

		readonly Matrix preTransform;
		readonly Color4 color;
		readonly ModelManager modelManager;
		readonly Scene scene;
		readonly Scene[] clips;
		readonly string fpvCamera;
		readonly bool fpvEnabled;
		readonly Matrix fpvCameraMatrix;
		readonly Matrix fpvViewMatrix;
		readonly int fpvCameraIndex;

		readonly float boxWidth;
		readonly float boxHeight;
		readonly float boxDepth;
		readonly Color boxColor;

		Matrix[] globalTransforms;
		Matrix[] animSnapshot;
		MeshInstance[] meshInstances;

		Animator	animator;

		readonly int nodeCount;

		public bool Killed {
			get; private set;
		}


		/// <summary>
		/// Gets model's scene
		/// </summary>
		public Scene Scene { get { return scene; } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="modelManager"></param>
		/// <param name="descriptor"></param>
		/// <param name="scene"></param>
		/// <param name="entity"></param>
		/// <param name="matrix"></param>
		public ModelInstance ( Entity entity, ModelManager modelManager, ModelFactory factory, ContentManager content )
		{
			if (string.IsNullOrWhiteSpace(factory.ScenePath)) {
				this.scene		=	EmptyScene;
				this.clips		=	new Scene[0];
			} else {
				this.scene		=	content.Load<Scene>( factory.ScenePath );
			}

			this.Entity			=	entity;

			this.boxWidth		=	factory.BoxWidth	;
			this.boxHeight		=	factory.BoxHeight	;
			this.boxDepth		=	factory.BoxDepth	;
			this.boxColor		=	factory.BoxColor	;

			this.modelManager   =   modelManager;
			this.preTransform   =   factory.ComputePreTransformMatrix();
			this.color			=	factory.Color;

			this.fpvEnabled		=	factory.FPVEnable;
			this.fpvCamera		=	factory.FPVCamera;

			nodeCount			=	scene.Nodes.Count;

			globalTransforms	=	new Matrix[ scene.Nodes.Count ];
			animSnapshot		=	new Matrix[ scene.Nodes.Count ];
			scene.ComputeAbsoluteTransforms( globalTransforms );
			scene.ComputeAbsoluteTransforms( animSnapshot );

			if (factory.AnimEnabled) {
				animator	=	new WeaponAnimator( Entity, this );
				//animation	=	content.Load<AnimatorFactory>(@"animation\" + factory.AnimController)?.Create( scene );
			}

			if (fpvEnabled) {
				fpvCameraIndex		=	scene.GetNodeIndex( fpvCamera );

				if (fpvCameraIndex<0) {	
					Log.Warning("Camera node {0} does not exist", fpvCamera);
				} else {
					fpvCameraMatrix	=	Matrix.RotationY( -MathUtil.PiOverTwo ) * globalTransforms[ fpvCameraIndex ];
					fpvViewMatrix	=	Matrix.Invert( fpvCameraMatrix );
					preTransform	=	fpvViewMatrix * Matrix.Scaling( factory.Scale );
				}
			} else {
				preTransform	=	Matrix.Scaling( factory.Scale );	
			}


			meshInstances	=	new MeshInstance[ scene.Nodes.Count ];

			var instGroup	=	fpvEnabled ? InstanceGroup.Weapon : InstanceGroup.Dynamic;


			for ( int i=0; i<nodeCount; i++ ) {
				var meshIndex = scene.Nodes[i].MeshIndex;
				
				if (meshIndex>=0) {
					meshInstances[i]		= new MeshInstance( modelManager.rs, scene, scene.Meshes[meshIndex] );
					meshInstances[i].Group	= instGroup;
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
		public void Update ( GameTime gameTime, float animFrame )
		{
			var q	=	Entity.Rotation;
			var p	=	Entity.Position;

			var worldMatrix = Matrix.RotationQuaternion( q ) * Matrix.Translation( p );

			if ( fpvEnabled ) {
				var weaponMatrix		=	Matrix.Identity;
				var playerCameraMatrix	=	modelManager.rw.Camera.GetCameraMatrix(Fusion.Drivers.Graphics.StereoEye.Mono);
				
				worldMatrix				= 	playerCameraMatrix;
			}


			if (scene==EmptyScene) {
				modelManager.rw.Debug.DrawBox( new BoundingBox(boxWidth,boxHeight,boxDepth), worldMatrix, boxColor, 2 );
				return;
			}

			if (animator!=null) {
				animator.Update( gameTime, animSnapshot );
				scene.ComputeAbsoluteTransforms( animSnapshot, globalTransforms );
			}

			Update( worldMatrix, globalTransforms );

			#warning old stuff
			//Update( worldMatrix, animSnapshot );
		}


		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="worldMatrix"></param>
		/// <param name="noteTransforms"></param>
		public void Update ( Matrix worldMatrix, Matrix[] nodeTransforms )
		{
			if (nodeTransforms==null) {
				throw new ArgumentNullException("nodeTransforms");
			}
			if (nodeCount>nodeTransforms.Length) {
				throw new ArgumentException("nodeTransforms.Length < nodeCount");
			}

			for ( int i = 0; i<nodeCount; i++ ) {
				if (meshInstances[i]!=null) {
					meshInstances[i].World = nodeTransforms[i] * preTransform * worldMatrix;
					meshInstances[i].Color = color;
				}
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="transforms"></param>
		public void ComputeAbsoluteTransforms ( Matrix[] transforms )
		{
			scene.ComputeAbsoluteTransforms( transforms, transforms );
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
			throw new NotImplementedException();
			/*if (animFrame>scene.LastFrame) {
				Log.Warning("Anim frame: {0} > {1}", animFrame, scene.LastFrame);
			}

			if (animFrame<scene.FirstFrame) {
				Log.Warning("Anim frame: {0} < {1}", animFrame, scene.FirstFrame);
			}

			animFrame = MathUtil.Clamp( animFrame, scene.FirstFrame, scene.LastFrame );

			scene.GetAnimSnapshot( animFrame, scene.FirstFrame, scene.LastFrame, AnimationMode.Clamp, animSnapshot );
			scene.ComputeAbsoluteTransforms( animSnapshot, animSnapshot ); */
		}
	}
}
