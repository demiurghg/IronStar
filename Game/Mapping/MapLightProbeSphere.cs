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
			Translation	=	Vector3.Up * 512;
			Rotation	=	Quaternion.Identity;
			Radius		=	2048;
			Transition	=	8f;
		}

		/// <summary>
		/// 
		/// </summary>
		public MapLightProbeSphere ()
		{
		}



		public override void SpawnNodeECS( IGameState gs )
		{
			var transform		=	new Transform( Translation, Rotation );

			var light			=	new SFX2.LightProbeSphere(Name);
			light.Radius		=	Radius;
			light.Transition	=	Transition;

			ecsEntity			=	gs.Spawn( transform, light );
			ecsEntity.Tag		=	this;
		}


		private Matrix ComputeProbeMatrix ()
		{
			return Matrix.Scaling( Radius, Radius, Radius ) * Transform;
		}



		public override BoundingBox GetBoundingBox( IGameState gs )
		{
			return new BoundingBox( Radius*2, Radius*2, Radius*2 );
		}
	}
}
