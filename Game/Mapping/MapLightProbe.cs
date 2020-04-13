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

namespace IronStar.Mapping {

	public class MapLightProbe : MapNode {
		
		[Category("Light probe")]
		public float InnerRadius { get; set; } = 1;
		
		[Category("Light probe")]
		public float OuterRadius { get; set; } = 2;


		[Category("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Width  { get; set; } = 16;

		[Category("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Height { get; set; } = 16;

		[Category("Light probe")]
		[AEValueRange(0,256,8,0.25f)]
		public float Depth  { get; set; } = 16;

		[Category("Light probe")]
		[AEDisplayName("Transition Width")]
		[AEValueRange(0.25f,32,1,0.25f)]
		public float ShellWidth  { get; set; } = 8f;

		[Category("Light probe")]
		[AEDisplayName("Transition Height")]
		[AEValueRange(0.25f,32,1,0.25f)]
		public float ShellHeight  { get; set; } = 8f;

		[Category("Light probe")]
		[AEDisplayName("Transition Depth")]
		[AEValueRange(0.25f,32,1,0.25f)]
		public float ShellDepth  { get; set; } = 8f;


		[AECommand]
		public void MakeGlobal()
		{
			TranslateVector	=	Vector3.Up * 512;
			RotatePitch		=	0;
			RotateRoll		=	0;
			RotateYaw		=	0;
			Width	=	2048;
			Height	=	2048;
			Depth	=	2048;
		}

		LightProbe	light;


		/// <summary>
		/// 
		/// </summary>
		public MapLightProbe ()
		{
		}



		public override void SpawnNode( GameWorld world )
		{
			var lightSet	=	world.Game.RenderSystem.RenderWorld.LightSet;

			light	=	new LightProbe( NodeGuid, lightSet.AllocImageIndex() );

			light.ProbeMatrix		=	ComputeProbeMatrix();
			light.BoundingBox		=	GetBoundingBox();
			light.NormalizedWidth	=	Math.Max( 0, Width  - ShellWidth  ) / Width	;
			light.NormalizedHeight	=	Math.Max( 0, Height - ShellHeight ) / Height	;
			light.NormalizedDepth	=	Math.Max( 0, Depth  - ShellDepth  ) / Depth	;

			lightSet.LightProbes.Add( light );
		}



		public override void ActivateNode()
		{
		}



		public override void UseNode()
		{
		}



		private Matrix ComputeProbeMatrix ()
		{
			return Matrix.Scaling( Width/2.0f, Height/2.0f, Depth/2.0f ) * WorldMatrix;
		}



		public override BoundingBox GetBoundingBox()
		{
			return new BoundingBox( Width, Height, Depth );
		}


		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			dr.DrawPoint( WorldMatrix.TranslationVector, 2.0f, color, 1 );

			//var bbox1	=	new BoundingBox( Width, Height, Depth );
			//var bbox2	=	new BoundingBox( 0.5f, 0.5f, 0.5f );

			//if (selected) {
			//	dr.DrawBox( bbox1, WorldMatrix, color );
			//	dr.D
			//} else {
			//	dr.DrawBox( bbox2, WorldMatrix, color );
			//}

			if (selected) {
				var box = new BoundingBox( 2, 2, 2 );
				dr.DrawBox( box, ComputeProbeMatrix(), Color.Cyan ); 
				dr.DrawSphere( WorldMatrix.TranslationVector, 1.0f, color, 16 );
			} else {
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
			var newNode = (MapLightProbe)MemberwiseClone();
			newNode.light = null;
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}
	}
}
