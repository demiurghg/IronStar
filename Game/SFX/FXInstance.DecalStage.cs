using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Common;
using Fusion.Core.Input;
using Fusion.Engine.Client;
using Fusion.Engine.Server;
using Fusion.Engine.Graphics;
using IronStar.Core;
using Fusion.Engine.Audio;


namespace IronStar.SFX {

	/// <summary>
	/// 
	/// </summary>
	public partial class FXInstance {

		public class DecalStage : Stage {

			static Random rand = new Random();

			Decal	decalSurface = null;
			Decal	decalEmission = null;
			FXDecalStage stageDesc;

			readonly bool	looped;
			readonly float	overallScale;
			readonly float	maxLifeTime;

			float   timer = 0;
			bool	stopped = false;
			float	intensityScale = 1;
			int		counter;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="instance"></param>
			/// <param name="position"></param>
			/// <param name="color"></param>
			/// <param name="radius"></param>
			/// <param name="fadeInRate"></param>
			/// <param name="fadeOutRate"></param>
			public DecalStage ( FXInstance instance, FXDecalStage stageDesc, FXEvent fxEvent, bool looped ) : base(instance)
			{
				decalSurface		=	new Decal();
				decalEmission		=	new Decal();
				this.stageDesc		=	stageDesc;
				this.overallScale	=	instance.overallScale;
				this.maxLifeTime	=	Math.Max( stageDesc.EmissionLifetime, stageDesc.SurfaceLifetime );

				var radius			=	stageDesc.Size;
				var depth			=	stageDesc.Depth;

				var rotation		=	Matrix.RotationQuaternion( fxEvent.Rotation );
				var scaling			=	Matrix.Scaling( radius / 2.0f, radius / 2.0f, -Math.Abs(depth / 2.0f) );
				var rotation2		=	Matrix.RotationAxis( rotation.Forward, rand.NextFloat( 0,MathUtil.TwoPi ) );
				var translation		=	Matrix.Translation( fxEvent.Origin );
				var decalMatrix		=	scaling * rotation * rotation2 * translation;

				var decalMatrixInv	=	Matrix.Invert( decalMatrix );

				decalSurface.DecalMatrix		=	decalMatrix;
				decalSurface.DecalMatrixInverse	=	decalMatrixInv;
									
				decalSurface.Emission			=	Color4.Zero;
				decalSurface.BaseColor			=	new Color4( stageDesc.BaseColor.R/255.0f, stageDesc.BaseColor.G/255.0f, stageDesc.BaseColor.B/255.0f, 1 );
			
				decalSurface.Metallic			=	stageDesc.Metallic;
				decalSurface.Roughness			=	stageDesc.Roughness;
				decalSurface.ImageRectangle		=	instance.rw.LightSet.DecalAtlas.GetNormalizedRectangleByName( stageDesc.SurfaceDecal );
				decalSurface.ImageSize			=	instance.rw.LightSet.DecalAtlas.GetAbsoluteRectangleByName( stageDesc.SurfaceDecal ).Size;

				decalSurface.ColorFactor		=	stageDesc.ColorFactor;
				decalSurface.SpecularFactor		=	stageDesc.SpecularFactor;
				decalSurface.NormalMapFactor	=	stageDesc.NormalMapFactor;
				decalSurface.FalloffFactor		=	4.0f;

				decalSurface.Group				=	InstanceGroup.Static;


				decalEmission.DecalMatrix		=	decalMatrix;
				decalEmission.DecalMatrixInverse=	decalMatrixInv;
									
				decalEmission.Emission			=	Color4.Zero;
				decalEmission.BaseColor			=	Color4.Zero;
			
				decalEmission.Metallic			=	0;
				decalEmission.Roughness			=	0;
				decalEmission.ImageRectangle	=	instance.rw.LightSet.DecalAtlas.GetNormalizedRectangleByName( stageDesc.EmissionDecal );
				decalEmission.ImageSize			=	instance.rw.LightSet.DecalAtlas.GetAbsoluteRectangleByName( stageDesc.EmissionDecal ).Size;

				decalEmission.ColorFactor		=	0;
				decalEmission.SpecularFactor	=	0;
				decalEmission.NormalMapFactor	=	0;
				decalEmission.FalloffFactor		=	4.0f;

				decalEmission.Group				=	InstanceGroup.Static;

				this.looped						=	looped;

				instance.rw.LightSet.Decals.Add(decalSurface); 
				instance.rw.LightSet.Decals.Add(decalEmission); 
			}


			public override void Stop ( bool immediate )
			{
				stopped	=	true;
			}

			public override bool IsExhausted ()
			{
				return stopped;
			}

			public override void Kill ()
			{
				fxInstance.rw.LightSet.Decals.Remove(decalSurface);
				fxInstance.rw.LightSet.Decals.Remove(decalEmission);
			}

			public override void Update ( float dt, FXEvent fxEvent )
			{
				timer += dt;

				//	compute hard-surface factor :
				float factor	=	MathUtil.Clamp( (1.0f - timer / maxLifeTime) * 5, 0, 1 );

				decalSurface.ColorFactor		=	factor * stageDesc.ColorFactor;
				decalSurface.SpecularFactor		=	factor * stageDesc.SpecularFactor;
				decalSurface.NormalMapFactor	=	factor * stageDesc.NormalMapFactor;

				//	compute glow factor :
				factor	=	MathUtil.Clamp( 1.0f - timer / stageDesc.EmissionLifetime, 0, 1 );
				float intensity				=	MathUtil.Exp2( stageDesc.EmissionIntensity * factor ) * factor;

				decalEmission.Emission		=	stageDesc.EmissionColor.ToColor4() * intensity;


				if (timer>maxLifeTime) 
				{
					stopped = true;
					Kill();
				}
			}
		}

	}
}
