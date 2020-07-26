using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Fusion.Core.Mathematics;
using IronStar.Core;
using Fusion.Engine.Graphics;
using IronStar.SFX;
using Fusion.Development;
using System.Drawing.Design;
using Fusion;
using Fusion.Core.Shell;
using IronStar.ECS;

namespace IronStar.Mapping {

	public class MapLightProbeSphere : MapNode {

		[Category("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Radius { get; set; } = 32;

		[Category("Light probe")]
		[AEDisplayName("Transition Width")]
		[AEValueRange(0.25f,64,1,0.25f)]
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



		public override void SpawnNode( GameWorld world )
		{
			var lightSet	=	world.Game.RenderSystem.RenderWorld.LightSet;

			light	=	new LightProbe( NodeGuid, lightSet.AllocImageIndex() );

			light.Mode				=	LightProbeMode.SphereReflection;

			light.ProbeMatrix		=	ComputeProbeMatrix();
			light.BoundingBox		=	GetBoundingBox();
			light.Radius			=	Radius;
			light.NormalizedWidth	=	Math.Max( 0, Radius * 2.0f - 2*Transition ) / (Radius * 2.0f) ;
			light.NormalizedHeight	=	Math.Max( 0, Radius * 2.0f - 2*Transition ) / (Radius * 2.0f) ;
			light.NormalizedDepth	=	Math.Max( 0, Radius * 2.0f - 2*Transition ) / (Radius * 2.0f) ;

			lightSet.LightProbes.Add( light );
		}


		public override void SpawnNodeECS( GameState gs )
		{
			var e = gs.Spawn();
			e.AddComponent( new ECS.Transform( TranslateVector, RotateQuaternion ) );

			var light = new SFX2.LightProbeSphere(NodeGuid);

			light.Radius		=	Radius;
			light.Transition	=	Transition;

			e.AddComponent( light );
			e.AddComponent( new Static() );
		}


		public override void ActivateNode()
		{
		}



		public override void UseNode()
		{
		}



		private Matrix ComputeProbeMatrix ()
		{
			return Matrix.Scaling( Radius, Radius, Radius ) * WorldMatrix;
		}



		public override BoundingBox GetBoundingBox()
		{
			return new BoundingBox( Radius*2, Radius*2, Radius*2 );
		}


		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			dr.DrawPoint( WorldMatrix.TranslationVector, 2.0f, color, 1 );

			if (selected) 
			{
				dr.DrawSphere( WorldMatrix.TranslationVector, Radius			 , color, 32 );
				dr.DrawSphere( WorldMatrix.TranslationVector, Radius - Transition, color, 32 );
			} 
			else 
			{
				dr.DrawSphere( WorldMatrix.TranslationVector, 1.0f, color, 16 );
			}
		}



		public override void KillNode( GameWorld world )
		{
			world.Game.RenderSystem.RenderWorld.LightSet.FreeImageIndex( light.ImageIndex );
			world.Game.RenderSystem.RenderWorld.LightSet.LightProbes.Remove( light );
		}


		public override MapNode DuplicateNode( GameWorld world )
		{
			var newNode = (MapLightProbeSphere)MemberwiseClone();
			newNode.light = null;
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}
	}
}
