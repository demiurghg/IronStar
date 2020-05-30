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

	public class MapLightProbeBox : MapNode {

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
		public MapLightProbeBox ()
		{
		}



		public override void SpawnNode( GameWorld world )
		{
			var lightSet	=	world.Game.RenderSystem.RenderWorld.LightSet;

			light	=	new LightProbe( NodeGuid, lightSet.AllocImageIndex() );

			light.Mode				=	LightProbeMode.CubeReflection;

			light.ProbeMatrix		=	ComputeProbeMatrix();
			light.BoundingBox		=	GetBoundingBox();
			light.NormalizedWidth	=	Math.Max( 0, Width  - 2*ShellWidth  ) / Width	;
			light.NormalizedHeight	=	Math.Max( 0, Height - 2*ShellHeight ) / Height;
			light.NormalizedDepth	=	Math.Max( 0, Depth  - 2*ShellDepth  ) / Depth	;

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

			if (selected) 
			{
				var box = new BoundingBox( 2, 2, 2 );
				dr.DrawBox( box, ComputeProbeMatrix(), Color.Cyan ); 
				dr.DrawSphere( WorldMatrix.TranslationVector, 1.0f, color, 16 );
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
			var newNode = (MapLightProbeBox)MemberwiseClone();
			newNode.light = null;
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}
	}
}
