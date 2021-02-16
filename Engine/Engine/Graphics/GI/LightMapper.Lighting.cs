using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Graphics.Ubershaders;
using Native.Embree;
using System.Runtime.InteropServices;
using Fusion.Core;
using System.Diagnostics;
using Fusion.Engine.Imaging;
using Fusion.Core.Configuration;
using Fusion.Build.Mapping;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Fusion.Engine.Graphics.Scenes;
using System.IO;

namespace Fusion.Engine.Graphics.GI {

	partial class LightMapper 
	{
		/// <summary>
		/// Compute direct light in given point
		/// </summary>
		Color4 ComputeDirectLight ( RtcScene scene, LightSet lightSet, Vector3 position, Vector3 normal )
		{
			var dirLightDir		=	-(lightSet.DirectLight.Direction).Normalized();
			var dirLightColor	=	lightSet.DirectLight.Intensity;

			var directLight		=	Color4.Zero;
			var ray				=	new RtcRay();
			var bias			=	normal / 16.0f;

			position			+=	bias;

			if (true) 
			{
				var nDotL	=	 Math.Max( 0, Vector3.Dot( dirLightDir, normal ) );

				if (nDotL>0) 
				{
					EmbreeExtensions.UpdateRay( ref ray, position, dirLightDir, 0, 9999 );

					var shadow	=	 scene.Occluded( ray ) ? 0 : 1;
		 
					directLight	+=	nDotL * dirLightColor * shadow;
				}
			}

			foreach ( var ol in lightSet.OmniLights ) 
			{
				var dir			=	ol.CenterPosition - position;
				var dist		=	dir.Length();
				var dirN		=	dir.Normalized();
				var falloff		=	MathUtil.Clamp( 1 - dir.Length() / ol.RadiusOuter, 0, 1 );
					falloff		*=	falloff;

				var falloff2	=	MathUtil.Clamp( 1 - dir.Length() / ol.RadiusOuter / 3, 0, 1 );

				var nDotL		=	Math.Max( 0, Vector3.Dot( dirN, normal ) );

				if ( falloff * falloff2 * nDotL > 0 ) {

					EmbreeExtensions.UpdateRay( ref ray, position, dir, 0, 1 );
					var shadow	=	 scene.Occluded( ray ) ? 0 : 1;
		 
					directLight	+=	nDotL * falloff * shadow * ol.Intensity;
				}

			}

			foreach ( var sl in lightSet.SpotLights ) 
			{
				var dir			=	sl.CenterPosition - position;
				var dist		=	dir.Length();
				var dirN		=	dir.Normalized();
				var falloff		=	MathUtil.Clamp( 1 - dir.Length() / sl.RadiusOuter, 0, 1 );
					falloff		*=	falloff;

				var falloff2	=	MathUtil.Clamp( 1 - dir.Length() / sl.RadiusOuter / 3, 0, 1 );

				var nDotL		=	Math.Max( 0, Vector3.Dot( dirN, normal ) );

				var viewProj	=	sl.SpotView * sl.Projection;
				var projPos		=	Vector3.TransformCoordinate( position, viewProj );
				var axialDist	=	new Vector2( projPos.X, projPos.Y ).Length();

				if (axialDist<1) 
				{
					if ( falloff * falloff2 * nDotL > 0 ) 
					{
						EmbreeExtensions.UpdateRay( ref ray, position, dir, 0, 1 );
						var shadow	=	 scene.Occluded( ray ) ? 0 : 1;
		 
						directLight	+=	nDotL * falloff * shadow * sl.Intensity;
					}
				}
			}

			return directLight;
		}
	}
}
