using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSOmniLight = Fusion.Engine.Graphics.OmniLight;
using Fusion.Core.Shell;
using Fusion.Core;

namespace IronStar.SFX2
{
	public class OmniLightSystem : ISystem
	{
		Dictionary<uint,RSOmniLight> lights = new Dictionary<uint, RSOmniLight>();

		readonly LightSet ls;

		public OmniLightSystem( RenderSystem rs )
		{
			this.ls	=	rs.RenderWorld.LightSet;
		}


		public Aspect GetAspect()
		{
			return new Aspect()
				.Include<OmniLight,Transform>()
				;
		}

		
		public void Add( GameState gs, Entity e )
		{
			var light = new RSOmniLight();
			UpdateLight( light, e );
			
			lights.Add( e.ID, light );
			ls.OmniLights.Add( light );
		}

		
		public void Remove( GameState gs, Entity e )
		{
			RSOmniLight ol;
			if ( lights.TryGetValue( e.ID, out ol ) )
			{
				lights.Remove( e.ID );
				ls.OmniLights.Remove( ol );
			}
		}

		
		public void Update( GameState gs, GameTime gameTime )
		{
			var entities = gs.QueryEntities<OmniLight,Transform>();

			foreach ( var e in entities )
			{
				UpdateLight( lights[e.ID], e );
			}
		}


		void UpdateLight( RSOmniLight light, Entity e )
		{
			var t	=	e.GetComponent<Transform>();
			var ol	=	e.GetComponent<OmniLight>();

			var transform		=	t.TransformMatrix;
			light.Position0		=	transform.TranslationVector + transform.Right * ol.TubeLength * 0.5f;
			light.Position1		=	transform.TranslationVector + transform.Left  * ol.TubeLength * 0.5f;

			light.Intensity		=	ol.LightColor.ToColor4() *  MathUtil.Exp2( ol.LightIntensity );
			light.RadiusInner	=	ol.TubeRadius;
			light.RadiusOuter	=	ol.OuterRadius;
		}
	}
}
