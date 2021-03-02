using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Core.Shell;
using Fusion.Widgets.Advanced;

namespace IronStar.SFX2
{
	public class OmniLight : Component
	{
		[AECategory("Omni-light")]
		[AESlider(0, 100, 1, 0.125f)]
		public float OuterRadius 
		{ 
			get; set; 
		}
		
		[AECategory("Omni-light")]
		[AESlider(0, 8, 1, 0.125f)]
		public float TubeRadius 
		{ 
			get; set; 
		}

		[AECategory("Omni-light")]
		[AESlider(0, 32, 1, 0.125f)]
		public float TubeLength { get; set; } = 0.0f;

		[AECategory("Light Color")]
		[AEDisplayName("Light Color")]
		public Color LightColor
		{ 
			get; set; 
		}

		[AECategory("Light Color")]
		[AEDisplayName("Intensity")]
		[AESlider(0, 12, 10, 1)]
		public float LightIntensity 
		{ 
			get; set; 
		}
		
		/*Color lightColor = Color.White;
		float lightIntensity = 8;

		RSOmniLight light;
		LightSet lightSet;


		public OmniLight ()
		{
			light	=	new RSOmniLight();
			light.RadiusOuter	=	15;
			light.RadiusInner	=	0.25f;
		}


		public override void Added( GameState gs, Entity entity )
		{
			base.Added( gs, entity );

			lightSet	=	gs.GetService<RenderSystem>().RenderWorld.LightSet;
			lightSet.OmniLights.Add ( light );
		}


		public override void Removed( GameState gs )
		{
			lightSet.OmniLights.Remove ( light );
			base.Removed( gs );
		}


		public void SetTransform( Matrix transform )
		{
			light.Position0	=	transform.TranslationVector + transform.Right * TubeLength * 0.5f;
			light.Position1	=	transform.TranslationVector + transform.Left  * TubeLength * 0.5f;
		}	  */
	}
}
