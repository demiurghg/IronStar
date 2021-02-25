﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
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
			envLights.Sort( delegate( LightProbe a, LightProbe b ) {

				var sizeA	=	a.BoundingBox.Size();
				var sizeB	=	b.BoundingBox.Size();
				var volA	=	a.BoundingBox.Size().X * a.BoundingBox.Size().Y * a.BoundingBox.Size().Z;
				var volB	=	b.BoundingBox.Size().X * b.BoundingBox.Size().Y * b.BoundingBox.Size().Z;
				
				if ( sizeA.X > sizeB.X && sizeA.Y > sizeB.Y && sizeA.Z > sizeB.Z ) 
				{
					return -1;
				} 
				else if ( sizeA.X < sizeB.X && sizeA.Y < sizeB.Y && sizeA.Z < sizeB.Z ) 
				{
					return  1;
				} 
				else if ( volA > volB ) 
				{
					return -1;
				}
				else if ( volA < volB ) 
				{
					return 1;
				}
				else {
					return a.ImageIndex-b.ImageIndex;
				}
			});
		}
	}
}
