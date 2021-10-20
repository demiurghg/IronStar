using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;

namespace IronStar.Editor.Systems
{
	partial class EditorLightRenderSystem : ISystem
	{
		readonly DebugRender dr;
		readonly MapEditor editor;

		readonly Aspect aspectOmniLights		=	new Aspect().Include<SFX2.OmniLight>();
		readonly Aspect aspectSpotLights		=	new Aspect().Include<SFX2.SpotLight>();
		readonly Aspect aspectLightProbeBox		=	new Aspect().Include<SFX2.LightProbeBox>();
		readonly Aspect aspectLightProbeSphere	=	new Aspect().Include<SFX2.LightProbeSphere>();
		readonly Aspect aspectLightVolume		=	new Aspect().Include<SFX2.LightVolume>();
		readonly Aspect aspectDecals			=	new Aspect().Include<SFX2.DecalComponent>();

		public EditorLightRenderSystem( MapEditor editor, DebugRender dr )
		{
			this.dr		=	dr;
			this.editor	=	editor;
		}


		public Aspect GetAspect()
		{
			return Aspect.Empty;
		}

		
		public void Add( IGameState gs, Entity e ) {}
		public void Remove( IGameState gs, Entity e ) {}

		
		public void Update( IGameState gs, GameTime gameTime )
		{
			if (gs.Game.RenderSystem.SkipDebugRendering) 
			{
				return;
			}

			var color		=	Color.Black;
			var selected	=	false;

			foreach ( var entity in gs.QueryEntities( aspectOmniLights ) )
			{
				var transform	=	entity.GetComponent<Transform>();
				var omniLight	=	entity.GetComponent<SFX2.OmniLight>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					DrawOmniLight( selected, color, transform.TransformMatrix, omniLight );
				}
			}

			foreach ( var entity in gs.QueryEntities( aspectSpotLights ) )
			{
				var transform	=	entity.GetComponent<Transform>();
				var spotLight	=	entity.GetComponent<SFX2.SpotLight>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					DrawSpotLight( selected, color, transform.TransformMatrix, spotLight );
				}
			}

			foreach ( var entity in gs.QueryEntities( aspectLightProbeBox ) )
			{
				var transform	=	entity.GetComponent<Transform>();
				var lightProbe	=	entity.GetComponent<SFX2.LightProbeBox>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					DrawLightProbeBox( selected, color, transform.TransformMatrix, lightProbe );
				}
			}

			foreach ( var entity in gs.QueryEntities( aspectLightProbeSphere ) )
			{
				var transform	=	entity.GetComponent<Transform>();
				var lightProbe	=	entity.GetComponent<SFX2.LightProbeSphere>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					DrawLightProbeSphere( selected, color, transform.TransformMatrix, lightProbe );
				}
			}

			foreach ( var entity in gs.QueryEntities( aspectDecals ) )
			{
				var transform	=	entity.GetComponent<Transform>();
				var decal		=	entity.GetComponent<SFX2.DecalComponent>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					DrawDecal( selected, color, transform.TransformMatrix, decal );
				}
			}

			foreach ( var entity in gs.QueryEntities( aspectLightVolume ) )
			{
				var transform	=	entity.GetComponent<Transform>();
				var lightVolume	=	entity.GetComponent<SFX2.LightVolume>();

				if (editor.GetRenderProperties(entity, out color, out selected ))
				{
					DrawLightVolume( selected, color, transform.TransformMatrix, lightVolume );
				}
			}
		}


		
		void DrawOmniLight( bool selected, Color color, Matrix transform, SFX2.OmniLight ol )
		{
			var dispColor   =	ol.LightColor; 

			dr.DrawPoint( transform.TranslationVector, 1, color, 1 );

			var position	=	transform.TranslationVector;
			var position0	=	transform.TranslationVector + transform.Right * ol.TubeLength * 0.5f;
			var position1	=	transform.TranslationVector + transform.Left  * ol.TubeLength * 0.5f;

			if (selected) 
			{
				dr.DrawSphere( position0, ol.TubeRadius,  dispColor );
				dr.DrawSphere( position1, ol.TubeRadius,  dispColor );
				dr.DrawSphere( position,  ol.OuterRadius, dispColor );
			}
			else
			{
				dr.DrawSphere( position0, ol.TubeRadius, dispColor );
				dr.DrawSphere( position1, ol.TubeRadius, dispColor );
				dr.DrawLine( position0, position1, dispColor, dispColor, 3, 3 );
			}		
		}


		
		void DrawSpotLight( bool selected, Color color, Matrix transform, SFX2.SpotLight sl )
		{
			var dispColor   =	sl.LightColor;

			dr.DrawPoint( transform.TranslationVector, 1, color, 1 );

			var position	=	transform.TranslationVector;
			var position0	=	transform.TranslationVector + transform.Right * sl.TubeLength * 0.5f;
			var position1	=	transform.TranslationVector + transform.Left  * sl.TubeLength * 0.5f;

			if (selected) 
			{
				dr.DrawSphere( position0, sl.TubeRadius,  dispColor );
				dr.DrawSphere( position1, sl.TubeRadius,  dispColor );
				dr.DrawSphere( position,  sl.OuterRadius, dispColor );

				var proj	=	sl.ComputeSpotMatrix();
				var view	=	Matrix.Invert( transform );

				var frustum = new BoundingFrustum( view * proj );
				
				var points  = frustum.GetCorners();

				dr.DrawLine( points[0], points[1], dispColor );
				dr.DrawLine( points[1], points[2], dispColor );
				dr.DrawLine( points[2], points[3], dispColor );
				dr.DrawLine( points[3], points[0], dispColor );

				dr.DrawLine( points[4], points[5], dispColor );
				dr.DrawLine( points[5], points[6], dispColor );
				dr.DrawLine( points[6], points[7], dispColor );
				dr.DrawLine( points[7], points[4], dispColor );

				dr.DrawLine( points[0], points[4], dispColor );
				dr.DrawLine( points[1], points[5], dispColor );
				dr.DrawLine( points[2], points[6], dispColor );
				dr.DrawLine( points[3], points[7], dispColor );
			} 
			else 
			{
				dr.DrawSphere( position0, sl.TubeRadius, dispColor );
				dr.DrawSphere( position1, sl.TubeRadius, dispColor );
				dr.DrawLine( position0, position1, dispColor, dispColor, 3, 3 );
			}
		}



		void DrawLightProbeSphere( bool selected, Color color, Matrix transform, SFX2.LightProbeSphere lpb )		
		{
			dr.DrawPoint( transform.TranslationVector, 2.0f, color, 1 );

			if (selected) 
			{
				dr.DrawSphere( transform.TranslationVector, lpb.Radius					, color, 32 );
				dr.DrawSphere( transform.TranslationVector, lpb.Radius - lpb.Transition	, color, 32 );
			} 
			else 
			{
				dr.DrawSphere( transform.TranslationVector, 1.0f, color, 16 );
			}
		}



		void DrawLightProbeBox( bool selected, Color color, Matrix transform, SFX2.LightProbeBox lpb )		
		{
			dr.DrawPoint( transform.TranslationVector, 2.0f, color, 1 );

			var probeMatrix = Matrix.Scaling( lpb.Width/2.0f, lpb.Height/2.0f, lpb.Depth/2.0f ) * transform;

			if (selected) 
			{
				var box = new BoundingBox( 2, 2, 2 );
				dr.DrawBox( box, probeMatrix, color ); 
				dr.DrawSphere( transform.TranslationVector, 1.0f, color, 16 );
			} 
			else 
			{
				dr.DrawSphere( transform.TranslationVector, 1.0f, color, 16 );
			}
		}



		void DrawDecal( bool selected, Color color, Matrix transform, SFX2.DecalComponent dcl )		
		{
			var c	= transform.TranslationVector 
					+ transform.Left * dcl.Width * 0.40f
					+ transform.Up   * dcl.Height * 0.40f;

			float len = Math.Min( dcl.Width, dcl.Height ) / 6;

			var x  = transform.Right * len;
			var y  = transform.Down * len;
			var z  = transform.Backward * len;

			var p0 = Vector3.TransformCoordinate( new Vector3(  dcl.Width/2,  dcl.Height/2, 0 ), transform ); 
			var p1 = Vector3.TransformCoordinate( new Vector3( -dcl.Width/2,  dcl.Height/2, 0 ), transform ); 
			var p2 = Vector3.TransformCoordinate( new Vector3( -dcl.Width/2, -dcl.Height/2, 0 ), transform ); 
			var p3 = Vector3.TransformCoordinate( new Vector3(  dcl.Width/2, -dcl.Height/2, 0 ), transform ); 

			var p4 = Vector3.TransformCoordinate( new Vector3( 0, 0,  dcl.Depth ), transform ); 
			var p5 = Vector3.TransformCoordinate( new Vector3( 0, 0, -dcl.Depth ), transform ); 

			dr.DrawLine( p0, p1, color, color, 1, 1 );
			dr.DrawLine( p1, p2, color, color, 1, 1 );
			dr.DrawLine( p2, p3, color, color, 1, 1 );
			dr.DrawLine( p3, p0, color, color, 1, 1 );

			dr.DrawLine( c, c+x, Color.Red  , Color.Red  , 2, 2 );
			dr.DrawLine( c, c+y, Color.Lime , Color.Lime , 2, 2 );
			dr.DrawLine( c, c+z, Color.Blue , Color.Blue , 5, 1 );

			dr.DrawLine( p4, p5, color, color, 2, 2 );
		}
	

		void DrawLightVolume( bool selected, Color color, Matrix transform, SFX2.LightVolume vol )		
		{
			dr.DrawPoint( transform.TranslationVector, 2.0f, color, 1 );

			var probeMatrix  = Matrix.Scaling( vol.Width/2.0f, vol.Height/2.0f, vol.Depth/2.0f ) * transform;

			if (selected) 
			{
				var box = new BoundingBox( 2, 2, 2 );
				dr.DrawBox( box, probeMatrix, color ); 
				dr.DrawBox( box, transform, color );
				dr.DrawPoint( transform.TranslationVector, 4, color ); 
			} 
			else 
			{
				var box = new BoundingBox( 2, 2, 2 );
				dr.DrawBox( box, probeMatrix, color ); 
				dr.DrawBox( box, transform, color ); 
				dr.DrawPoint( transform.TranslationVector, 4, color ); 
			}
		}
	}
}
