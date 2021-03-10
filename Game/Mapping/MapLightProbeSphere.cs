using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using Fusion.Core.Shell;
using IronStar.ECS;
using Fusion.Widgets.Advanced;

namespace IronStar.Mapping {

	public class MapLightProbeSphere : MapNode {

		[Category("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Radius { get; set; } = 32;

		[Category("Light probe")]
		[AEDisplayName("Transition Width")]
		[AESlider(0.25f,64,1,0.25f)]
		public float Transition  { get; set; } = 8f;


		[AECommand]
		public void MakeGlobal()
		{
			TranslateVector	=	Vector3.Up * 512;
			RotatePitch		=	0;
			RotateRoll		=	0;
			RotateYaw		=	0;
			Radius			=	2048;
			Transition		=	8f;
		}

		LightProbe	light;


		/// <summary>
		/// 
		/// </summary>
		public MapLightProbeSphere ()
		{
		}



		public override void SpawnNodeECS( GameState gs )
		{
			ecsEntity = gs.Spawn();
			ecsEntity.AddComponent( new ECS.Transform( TranslateVector, RotateQuaternion ) );

			var light = new SFX2.LightProbeSphere(Name);

			light.Radius		=	Radius;
			light.Transition	=	Transition;

			ecsEntity.AddComponent( light );
			ecsEntity.AddComponent( new Static() );
		}


		private Matrix ComputeProbeMatrix ()
		{
			return Matrix.Scaling( Radius, Radius, Radius ) * WorldMatrix;
		}



		public override BoundingBox GetBoundingBox()
		{
			return new BoundingBox( Radius*2, Radius*2, Radius*2 );
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapLightProbeSphere)MemberwiseClone();
			newNode.light = null;
			newNode.Name = GenerateUniqueName();
			return newNode;
		}
	}
}
