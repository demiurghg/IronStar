using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSLightVolume = Fusion.Engine.Graphics.LightVolume;
using Fusion.Core.Shell;
using Fusion.Core;

namespace IronStar.SFX2
{
	public class LightVolumeSystem : ProcessingSystem<RSLightVolume, LightVolume, KinematicState>
	{
		readonly LightSet ls;
	
		public LightVolumeSystem( RenderSystem rs )
		{
			ls	=	rs.RenderWorld.LightSet;
		}

		
		protected override RSLightVolume Create( Entity entity, LightVolume lightVol, KinematicState transform )
		{
			Process( entity, GameTime.Zero, ls.LightVolume, lightVol, transform );

			return ls.LightVolume;
		}

		
		protected override void Destroy( Entity entity, RSLightVolume resource )
		{
			// ...
		}

		
		protected override void Process( Entity entity, GameTime gameTime, RSLightVolume resource, LightVolume lightVol, KinematicState transform )
		{
			resource.ResolutionX	=	lightVol.ResolutionX;
			resource.ResolutionY	=	lightVol.ResolutionY;
			resource.ResolutionZ	=	lightVol.ResolutionZ;

			float sx	=	lightVol.Width;
			float sy	=	lightVol.Height;
			float sz	=	lightVol.Depth;

			resource.WorldMatrix	=	Matrix.Scaling( sx, sy, sz ) * transform.TransformMatrix;
		}
	}
}
