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


	public class MapOmniLight : MapNode {

		[AECategory("Omni-light")]
		[AEValueRange(0, 50, 1, 0.125f)]
		public float OuterRadius { get; set; } = 5;
		
		[AECategory("Omni-light")]
		[AEValueRange(0, 50, 1, 0.125f)]
		public float InnerRadius { get; set; } = 0.125f;

		[AECategory("Omni-light")]
		public LightStyle LightStyle { get; set; } = LightStyle.Default;

		[AECategory("Omni-light")]
		public bool Ambient { get; set; } = false;

		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor { get; set; } = Color.White;

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AEValueRange(0, 5000, 10, 1)]
		public float LightIntensity { get; set; } = 100;


		OmniLight	light;


		/// <summary>
		/// 
		/// </summary>
		public MapOmniLight ()
		{
		}



		public override void SpawnNode( GameWorld world )
		{
			light		=	new OmniLight();

			ResetNode( world );

			world.Game.RenderSystem.RenderWorld.LightSet.OmniLights.Add( light );
		}



		public override void ActivateNode()
		{
		}



		public override void UseNode()
		{
		}



		public override void DrawNode( GameWorld world, DebugRender dr, Color color, bool selected )
		{
			var transform	=	WorldMatrix;

			var dispColor   =	LightColor; 

			dr.DrawPoint( transform.TranslationVector, 1, color, 1 );

			if (selected) {
				dr.DrawSphere( transform.TranslationVector, InnerRadius, dispColor );
				dr.DrawSphere( transform.TranslationVector, OuterRadius, dispColor );
			} else {
				dr.DrawSphere( transform.TranslationVector, InnerRadius, dispColor );
			}
		}



		public override void ResetNode( GameWorld world )
		{
			light.Intensity		=	LightColor.ToColor4() * LightIntensity;
			light.Position		=	WorldMatrix.TranslationVector;
			light.RadiusOuter	=	OuterRadius;
			light.RadiusInner	=	InnerRadius;
			light.LightStyle	=	LightStyle;
			light.Ambient		=	Ambient;
		}



		public override void KillNode( GameWorld world )
		{
			world.Game.RenderSystem.RenderWorld.LightSet.OmniLights.Remove( light );
		}


		public override MapNode DuplicateNode()
		{
			var newNode = (MapOmniLight)MemberwiseClone();
			newNode.light = null;
			newNode.NodeGuid = Guid.NewGuid();
			return newNode;
		}



		public override BoundingBox GetBoundingBox()
		{
			float sz = OuterRadius / (float)Math.Sqrt(3);
			return new BoundingBox( sz, sz, sz );
		}
	}
}
