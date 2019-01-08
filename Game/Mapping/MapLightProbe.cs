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

namespace IronStar.Mapping {

	public class MapLightProbe : MapNode {
		
		[Category("Light probe")]
		public float InnerRadius { get; set; } = 1;
		
		[Category("Light probe")]
		public float OuterRadius { get; set; } = 2;


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

			light	=	new LightProbe( WorldMatrix.TranslationVector, InnerRadius, OuterRadius, lightSet.AllocImageIndex() );

			ResetNode( world );

			lightSet.LightProbes.Add( light );
		}



		public override void ActivateNode()
		{
		}



		public override void UseNode()
		{
		}



		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			dr.DrawPoint( WorldMatrix.TranslationVector, 0.5f, color, 1 );

			//var bbox1	=	new BoundingBox( Width, Height, Depth );
			//var bbox2	=	new BoundingBox( 0.5f, 0.5f, 0.5f );

			//if (selected) {
			//	dr.DrawBox( bbox1, WorldMatrix, color );
			//	dr.D
			//} else {
			//	dr.DrawBox( bbox2, WorldMatrix, color );
			//}

			if (selected) {
				dr.DrawSphere( WorldMatrix.TranslationVector, InnerRadius, Color.Cyan, 32 );
				dr.DrawSphere( WorldMatrix.TranslationVector, OuterRadius, Color.Cyan, 32 );
				dr.DrawSphere( WorldMatrix.TranslationVector, 0.33f, color, 16 );
			} else {
				dr.DrawSphere( WorldMatrix.TranslationVector, 0.33f, color, 16 );
			}
		}



		public override void ResetNode( GameWorld world )
		{
			if (light!=null) {
				light.Position		=	WorldMatrix.TranslationVector;
			}
		}



		public override void KillNode( GameWorld world )
		{
			world.Game.RenderSystem.RenderWorld.LightSet.FreeImageIndex( light.ImageIndex );
			world.Game.RenderSystem.RenderWorld.LightSet.LightProbes.Remove( light );
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapLightProbe)MemberwiseClone();
			newNode.light = null;
			return newNode;
		}
	}
}
