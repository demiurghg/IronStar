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


	public class MapOmniLight : MapNode {

		[AECategory("Omni-light")]
		[AEValueRange(0, 100, 1, 0.125f)]
		public float OuterRadius { get; set; } = 5;
		
		[AECategory("Omni-light")]
		[AEValueRange(0, 8, 1, 0.125f)]
		public float TubeRadius { get; set; } = 0.125f;

		[AECategory("Omni-light")]
		[AEValueRange(0, 32, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;

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

			light.Intensity		=	LightColor.ToColor4() * LightIntensity;
			light.Position0		=	WorldMatrix.TranslationVector + WorldMatrix.Right * TubeLength * 0.5f;
			light.Position1		=	WorldMatrix.TranslationVector + WorldMatrix.Left  * TubeLength * 0.5f;
			light.RadiusOuter	=	OuterRadius;
			light.RadiusInner	=	TubeRadius;
			light.LightStyle	=	LightStyle;
			light.Ambient		=	Ambient;

			world.Game.RenderSystem.RenderWorld.LightSet.OmniLights.Add( light );
		}



		public override void SpawnNodeECS( GameState gs )
		{
			var e = gs.Spawn();

			e.AddComponent( new Transform( TranslateVector, RotateQuaternion, 1 ) );
			e.AddComponent( CreateOmniLight() );
		}


		SFX2.OmniLight CreateOmniLight()
		{
			var light = new SFX2.OmniLight();

			light.TubeLength		=	TubeLength;
			light.TubeRadius		=	TubeRadius;
			light.OuterRadius		=	OuterRadius;
			light.LightColor		=	LightColor;
			light.LightIntensity	=	MathUtil.Log2( MathUtil.Clamp( LightIntensity, 1/64.0f, 1024 ) );

			return light;
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

			var position	=	WorldMatrix.TranslationVector;
			var position0	=	WorldMatrix.TranslationVector + WorldMatrix.Right * TubeLength * 0.5f;
			var position1	=	WorldMatrix.TranslationVector + WorldMatrix.Left  * TubeLength * 0.5f;

			if (selected) {
				dr.DrawSphere( position0, TubeRadius,  dispColor );
				dr.DrawSphere( position1, TubeRadius,  dispColor );
				dr.DrawSphere( position,  OuterRadius, dispColor );
			} else {
				dr.DrawSphere( position0, TubeRadius, dispColor );
				dr.DrawSphere( position1, TubeRadius, dispColor );
				dr.DrawLine( position0, position1, dispColor, dispColor, 3, 3 );
			}
		}



		public override void KillNode( GameWorld world )
		{
			world.Game.RenderSystem.RenderWorld.LightSet.OmniLights.Remove( light );
		}


		public override MapNode DuplicateNode( GameWorld world )
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
