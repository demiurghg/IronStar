using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSSpotLight = Fusion.Engine.Graphics.SpotLight;
using Fusion.Core.Shell;
using Fusion.Core;
using BEPUutilities.Threading;

namespace IronStar.SFX2
{
	public class DecalSystem : ProcessingSystem<Decal,DecalComponent,Transform>
	{
		Dictionary<uint,Decal> lights = new Dictionary<uint, Decal>();

		readonly LightSet ls;

		
		public DecalSystem( RenderSystem rs )
		{
			this.ls	=	rs.RenderWorld.LightSet;
		}

		
		protected override Decal Create( Entity e, DecalComponent ol, Transform t )
		{
			var decal = new Decal();

			Process( e, GameTime.Zero, decal, ol, t );

			ls.Decals.Add( decal );
			return decal;
		}

		
		protected override void Destroy( Entity e, Decal decal )
		{
			ls.Decals.Remove( decal );
		}

		
		protected override void Process( Entity e, GameTime gameTime, Decal decal, DecalComponent dc, Transform t )
		{
			var transform			=	t.TransformMatrix;

			decal.CharacteristicSize=	Math.Max( dc.Width, dc.Height );

			decal.DecalMatrix		=	Matrix.Scaling( dc.Width/2, dc.Height/2, dc.Depth/2 ) * transform;
			decal.DecalMatrixInverse=	Matrix.Invert( decal.DecalMatrix );
									
			decal.Emission			=	dc.EmissionColor.ToColor4() * MathUtil.Exp2( dc.EmissionIntensity );
			decal.BaseColor			=	new Color4( dc.BaseColor.R/255.0f, dc.BaseColor.G/255.0f, dc.BaseColor.B/255.0f, 1 );
			
			decal.Metallic			=	dc.Metallic;
			decal.Roughness			=	dc.Roughness;
			decal.ImageRectangle	=	ls.DecalAtlas.GetNormalizedRectangleByName( dc.ImageName );
			decal.ImageSize			=	ls.DecalAtlas.GetAbsoluteRectangleByName( dc.ImageName ).Size;

			decal.ColorFactor		=	dc.ColorFactor;
			decal.SpecularFactor	=	dc.SpecularFactor;
			decal.NormalMapFactor	=	dc.NormalMapFactor;
			decal.FalloffFactor		=	dc.FalloffFactor;

			decal.Group				=	InstanceGroup.Static;
		}
	}
}
