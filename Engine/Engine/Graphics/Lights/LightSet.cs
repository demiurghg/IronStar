using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;

namespace Fusion.Engine.Graphics {
	public class LightSet {

		/// <summary>
		/// Collection of omni lights.
		/// </summary>
		public ICollection<OmniLight> OmniLights { get { return omniLights; } }


		/// <summary>
		/// Collection of spot lights.
		/// </summary>
		public ICollection<SpotLight> SpotLights { get { return spotLights; } }


		/// <summary>
		/// Collection of environment lights.
		/// </summary>
		public IList<LightProbe> LightProbes { get { return envLights; } }


		/// <summary>
		/// Collection of environment lights.
		/// </summary>
		public ICollection<Decal> Decals { get { return decals;	} }


		/// <summary>
		/// Due to technical limitations only one source of direct 
		/// light is avaiable foreach LightSet.
		/// </summary>
		public DirectLight DirectLight { get { return directLight; } }
		

		/// <summary>
		/// Gets light volume
		/// </summary>
		public LightVolume LightVolume { get { return lightVolume; } }


		/// <summary>
		/// Spot-light mask atlas.
		/// </summary>
		public TextureAtlas SpotAtlas {	get; set; }


		/// <summary>
		/// Spot-light mask atlas.
		/// </summary>
		public TextureAtlas DecalAtlas { get; set; }


		DirectLight			directLight	= new DirectLight();
		LightVolume			lightVolume	= new LightVolume();
		List<OmniLight>		omniLights	= new List<OmniLight>();
		List<SpotLight>		spotLights	= new List<SpotLight>();
		List<LightProbe>	envLights	= new List<LightProbe>();
		List<Decal>			decals		= new List<Decal>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		public LightSet ( RenderSystem rs )
		{
		}



		
		public void SortLightProbes ()
		{
			//	sort by image index to make lightprobe order persistent between frame :
			envLights.InsertionSort( (a,b) => a.ImageIndex - b.ImageIndex );

			//	sort by size/volume/index criteria :
			envLights.InsertionSort( LightProbeComparison );
		}


		int LightProbeComparison( LightProbe a, LightProbe b )
		{ 
			var sizeA	=	a.BoundingBox.Size();
			var sizeB	=	b.BoundingBox.Size();
			var volA	=	a.BoundingBox.Size().X * a.BoundingBox.Size().Y * a.BoundingBox.Size().Z;
			var volB	=	b.BoundingBox.Size().X * b.BoundingBox.Size().Y * b.BoundingBox.Size().Z;

			//	epsilon required to handle subtle motion 
			//	when bounding box size varies due to rounding error
			var eps		=	1 / 512.0f;
			var eps3	=	1 / 8.0f;

			if ( (sizeA.X > sizeB.X + eps) && (sizeA.Y > sizeB.Y + eps) && (sizeA.Z > sizeB.Z + eps) ) 
			{
				return -1;
			} 
			else if ( ( sizeA.X < sizeB.X - eps ) && ( sizeA.Y < sizeB.Y - eps ) && ( sizeA.Z < sizeB.Z - eps ) ) 
			{
				return 1;
			} 
			else if ( volA > volB + eps3 ) 
			{
				return -1;
			}
			else if ( volA < volB - eps3 ) 
			{
				return 1;
			}
			else {
				return a.ImageIndex - b.ImageIndex;
			}
		}
	}
}
