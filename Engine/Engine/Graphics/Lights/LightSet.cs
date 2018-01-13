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
		public ICollection<OmniLight> OmniLights {
			get {
				return omniLights;	
			}
		}


		/// <summary>
		/// Collection of spot lights.
		/// </summary>
		public ICollection<SpotLight> SpotLights {
			get {
				return spotLights;	
			}
		}


		/// <summary>
		/// Collection of environment lights.
		/// </summary>
		public ICollection<LightProbe> LightProbes {
			get {
				return envLights;	
			}
		}


		/// <summary>
		/// Collection of environment lights.
		/// </summary>
		public ICollection<Decal> Decals {
			get {
				return decals;	
			}
		}


		/// <summary>
		/// Due to technical limitations only one source of direct 
		/// light is avaiable foreach LightSet.
		/// </summary>
		public DirectLight DirectLight {
			get {
				return directLight;
			}
		}
		


		/// <summary>
		/// Spot-light mask atlas.
		/// </summary>
		public TextureAtlas SpotAtlas {
			get; set;
		}


		/// <summary>
		/// Spot-light mask atlas.
		/// </summary>
		public TextureAtlas DecalAtlas {
			get; set;
		}


		/// <summary>
		/// Average ambient level.
		/// </summary>
		public Color4 AmbientLevel {
			get; set; 
		}


		DirectLight		directLight = new DirectLight();
		List<OmniLight> omniLights = new List<OmniLight>();
		List<SpotLight> spotLights = new List<SpotLight>();
		List<LightProbe>  envLights  = new List<LightProbe>();
		List<Decal>		decals		= new List<Decal>();


		List<bool>	imageIndices;


		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		public LightSet ( RenderSystem rs )
		{
			AmbientLevel	=	Color4.Zero;
			imageIndices	=	Enumerable.Range(0, LightGrid.MaxLightProbes)
								.Select(i=>false)
								.ToList();
		}


		public void SortLightProbes ()
		{
			envLights.Sort( delegate( LightProbe a, LightProbe b ) {
				if (a.OuterRadius>b.OuterRadius) {
					return -1;
				} else
				if (a.OuterRadius<b.OuterRadius) {
					return  1;
				} else
				if (a.OuterRadius==b.OuterRadius) {
					return a.ImageIndex-b.ImageIndex;
				}
				return 0;
			});
		}


		public int AllocImageIndex ()
		{
			int r = imageIndices.IndexOf(false);

			imageIndices[r] = true;

			return r;
		}


		public void FreeImageIndex( int index )
		{
			imageIndices[index] = false;
		}



		public void FreeAllImageIndices ()
		{
			for (int i=0; i<imageIndices.Count; i++) {
				imageIndices[i] = false;
			}
		}

	}
}
