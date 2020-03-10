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
using Fusion.Engine.Graphics.Scenes;
using IronStar.Core;
using Fusion.Engine.Audio;
using IronStar.Views;
using KopiLua;
using Fusion.Scripting;

namespace IronStar.SFX {

	public partial class ModelInstance : LuaScript {

		static readonly Scene EmptyScene = Scene.CreateEmptyScene();

		readonly public Entity Entity;


		public bool IsFPVModel {
			get { return fpvEnabled; }
		}

		readonly GameWorld world;
		readonly ModelManager modelManager;
		readonly ContentManager content;
		
		Color color;
		float intensity;
		float dtime; // send to script

		Scene scene;

		bool fpvEnabled;

		public Matrix preTransform;

		Matrix[] globalTransforms;
		Matrix[] animSnapshot;
		MeshInstance[] meshInstances;
		AnimationComposer composer;

		public bool Killed {
			get; private set;
		}

		int sleepTime = 0;


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
		public ModelInstance ( Entity entity, ModelManager modelManager, byte[] bytecode, string name ) : base(modelManager.L, bytecode, name)
		{
			this.Entity			=	entity;
			this.modelManager   =   modelManager;
			this.world			=	modelManager.world;
			this.content		=	modelManager.content;

			base.Resume(this);
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="animFrame"></param>
		/// <param name="worldMatrix"></param>
		public void Update ( GameTime gameTime, float animFrame )
		{
			dtime	= gameTime.ElapsedSec;

			if (sleepTime<=0) {
				Resume(null);
			} else {
				sleepTime -= gameTime.Milliseconds;
			}


			var worldMatrix	=	ComputeWorldMatrix();

			if (composer!=null) {
				composer.Update( gameTime, animSnapshot );
				//scene.ComputeAbsoluteTransforms( animSnapshot );
			}

			UpdateInternal( worldMatrix, animSnapshot );
		}


		/*-----------------------------------------------------------------------------------------------
		 * 
		 *	Model operations :
		 * 
		-----------------------------------------------------------------------------------------------*/

		void LoadScene ( string path )
		{
			if (string.IsNullOrWhiteSpace(path)) {
				this.scene		=	EmptyScene;
			} else {
				this.scene		=	content.Load<Scene>( path );
			}

			this.preTransform   =   Matrix.Identity;
			this.color			=	Color.White	;
			this.intensity		=	100;

			this.fpvEnabled		=	false;

			globalTransforms	=	new Matrix[ scene.Nodes.Count ];
			animSnapshot		=	new Matrix[ scene.Nodes.Count ];
			scene.ComputeAbsoluteTransforms( globalTransforms );
			scene.ComputeAbsoluteTransforms( animSnapshot );
			
			meshInstances	=	new MeshInstance[ scene.Nodes.Count ];

			for ( int i=0; i<scene.Nodes.Count; i++ ) {
				var meshIndex = scene.Nodes[i].MeshIndex;
				
				if (meshIndex>=0) {
					meshInstances[i]		= new MeshInstance( modelManager.rs, scene, scene.Meshes[meshIndex] );
					meshInstances[i].Group	= InstanceGroup.Dynamic;
					meshInstances[i].Color	= Color4.Zero;
					modelManager.rw.Instances.Add( meshInstances[i] );
				} else {
					meshInstances[i] = null;
				}
			}
		}



		public Matrix ComputeWorldMatrix ()
		{
			var q	=	Entity.Rotation;
			var p	=	Entity.Position;

			var worldMatrix = Matrix.RotationQuaternion( q ) * Matrix.Translation( p );

			if ( fpvEnabled ) {
				var playerCameraMatrix	=	modelManager.rw.Camera.GetCameraMatrix(Fusion.Drivers.Graphics.StereoEye.Mono);
				
				worldMatrix				= 	playerCameraMatrix;
			}

			return worldMatrix;
		}
		


		/// <summary>
		/// 
		/// </summary>
		/// <param name="worldMatrix"></param>
		/// <param name="noteTransforms"></param>
		void UpdateInternal ( Matrix worldMatrix, Matrix[] nodeTransforms )
		{
			if (scene==null || scene==EmptyScene) {
				return;
			}

			if (nodeTransforms==null) {
				throw new ArgumentNullException("nodeTransforms");
			}
			if (scene.Nodes.Count>nodeTransforms.Length) {
				throw new ArgumentException("nodeTransforms.Length < nodeCount");
			}

			var instGroup	=	fpvEnabled ? InstanceGroup.Weapon : InstanceGroup.Dynamic;
			var glowColor	=	color.ToColor4() * intensity;

			for ( int i = 0; i<scene.Nodes.Count; i++ ) {
				if (meshInstances[i]!=null) {
					meshInstances[i].Group	=	instGroup;
					meshInstances[i].World	=	nodeTransforms[i] * preTransform * worldMatrix;
					meshInstances[i].Color	=	glowColor;
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
			Terminate();

			if (scene==null || scene==EmptyScene) {
				return;
			}

			for ( int i = 0; i<scene.Nodes.Count; i++ ) {
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

		//AnimationComposer composer = new AnimationComposer(""
	}
}
