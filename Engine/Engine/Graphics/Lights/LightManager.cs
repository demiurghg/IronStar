using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Core.Configuration;
using Fusion.Engine.Common;
using Fusion.Drivers.Graphics;
using System.Runtime.InteropServices;
using System.IO;
using Fusion.Engine.Graphics.Ubershaders;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using Native.Embree;
using Fusion.Engine.Graphics.Lights;
using Fusion.Engine.Graphics.GI;
using Fusion.Core.Shell;

namespace Fusion.Engine.Graphics {

	internal partial class LightManager : RenderComponent 
	{
		public ShadowMap ShadowMap {
			get { return shadowMap; }
		}
		ShadowMap shadowMap;


		public ConstantBuffer DirectLightData {
			get { return cbDirectLightData; }
		}
		ConstantBuffer	cbDirectLightData;


		public LightGrid LightGrid {
			get { return lightGrid; }
		}
		LightGrid lightGrid;


		/// <summary>
		/// ctor
		/// </summary>
		public LightManager( RenderSystem rs ) : base( rs )
		{
		}


		/// <summary>
		/// Inits stuff
		/// </summary>
		public override void Initialize()
		{
			lightGrid			=	new LightGrid( rs, RenderSystem.LightClusterGridWidth, RenderSystem.LightClusterGridHeight, RenderSystem.LightClusterGridDepth );
			shadowMap			=	new ShadowMap( rs, rs.ShadowQuality );
			cbDirectLightData	=	new ConstantBuffer( rs.Device, typeof(GpuData.DIRECT_LIGHT) );
		}


		/// <summary>
		/// Disposes stuff
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing) 
			{
				SafeDispose( ref lightGrid );
				SafeDispose( ref shadowMap );
				SafeDispose( ref cbDirectLightData );
			}

			base.Dispose( disposing );
		}


		/// <summary>
		/// Updates stuff
		/// </summary>
		public void Update ( GameTime gameTime, LightSet lightSet, IEnumerable<RenderInstance> instances )
		{
			if (shadowMap.ShadowQuality!=rs.ShadowQuality) {
				SafeDispose( ref shadowMap );
				shadowMap	=	new ShadowMap( rs, rs.ShadowQuality );
			}


			foreach ( var omni in lightSet.OmniLights ) {
				omni.Timer += (uint)gameTime.Elapsed.TotalMilliseconds;
				if (omni.Timer<0) omni.Timer = 0;
			}

			foreach ( var spot in lightSet.SpotLights ) {
				spot.Timer += (uint)gameTime.Elapsed.TotalMilliseconds;
				if (spot.Timer<0) spot.Timer = 0;
			}

			//	update direct light CB :
			GpuData.DIRECT_LIGHT directLightData =	new GpuData.DIRECT_LIGHT();
			directLightData.DirectLightDirection	=	new Vector4( rs.RenderWorld.LightSet.DirectLight.Direction, 0 );
			directLightData.DirectLightIntensity	=	rs.RenderWorld.LightSet.DirectLight.Intensity;
			directLightData.DirectLightAngularSize	=	rs.RenderWorld.LightSet.DirectLight.AngularSize;

			cbDirectLightData.SetData( directLightData );
		}

	}
}
