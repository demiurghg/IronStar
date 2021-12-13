using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Drivers.Graphics;
using Fusion.Engine.Common;
using System.Runtime.InteropServices;
using Fusion.Engine.Graphics.Ubershaders;

namespace Fusion.Engine.Graphics 
{
	[RequireShader("debugRender", true)]
	public class DebugRenderImpl : DebugRender 
	{
		static FXConstantBuffer<ConstData> regParams = new CRegister( 0, "Batch" );

		[Flags]
		public enum RenderFlags : int {
			LINES		= 0x0001,
			GHOST		= 0x0002,
			MODEL		= 0x0100,
			SOLID		= 0x0200,
			WIREFRAME	= 0x0400,
		}

		[ShaderStructure]
		[StructLayout(LayoutKind.Sequential, Size = 256)]
		struct ConstData {
			public Matrix  View;
			public Matrix  Projection;
			public Matrix  World;
			public Color4  Color;
			public Vector4 ViewPosition;
			public Vector4 PixelSize;
		}

		VertexBuffer		vertexBuffer;
		Ubershader			effect;
		StateFactory		factory;
		ConstantBuffer		constBuffer;

		List<DebugVertex>	vertexDataAccum	= new List<DebugVertex>();
		DebugVertex[]		vertexArray = new DebugVertex[vertexBufferSize];

		const int vertexBufferSize = 4096*4;

		ConstData	constData;

		DebugModelCollection debugModels = new DebugModelCollection();

		DebugRenderAsync asyncRender;

		public DebugRender Async { get { return asyncRender; } }


		/// <summary>
		/// Constructor
		/// </summary>
		public DebugRenderImpl(Game game) : base(game)
		{
			var dev		=	Game.GraphicsDevice;

			LoadContent();
			
			constData	=	new ConstData();
			constBuffer =	new ConstantBuffer(dev, typeof(ConstData));

			//	create vertex buffer :
			vertexBuffer		= new VertexBuffer(dev, typeof(DebugVertex), vertexBufferSize, VertexBufferOptions.Dynamic );
			vertexDataAccum.Capacity = vertexBufferSize;

			Game.Reloading += (s,e) => LoadContent();

			asyncRender	=	new DebugRenderAsync(this);
		}



		public void LoadContent ()
		{
			effect		=	Game.Content.Load<Ubershader>("debugRender");
			factory		=	effect.CreateFactory( typeof(RenderFlags), (ps,i) => Enum(ps, (RenderFlags)i ) );

			//factory		=	effect.CreateFactory( typeof(RenderFlags), Primitive.LineList, VertexInputElement.FromStructure( typeof(LineVertex) ), BlendState.AlphaBlend, RasterizerState.CullNone, DepthStencilState.Default );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="ps"></param>
		/// <param name="flags"></param>
		void Enum ( PipelineState ps, RenderFlags flags )
		{
			ps.Primitive			=	Primitive.LineList;
			ps.VertexInputElements	=	VertexInputElement.FromStructure( typeof(DebugVertex) );
			ps.RasterizerState		=	RasterizerState.CullNone;

			if (flags.HasFlag( RenderFlags.MODEL ))
			{
				ps.BlendState			=	BlendState.Opaque;
				ps.DepthStencilState	=	DepthStencilState.Default;

				if (flags.HasFlag( RenderFlags.GHOST ))
				{
					ps.DepthStencilState	=	DepthStencilState.None;
				}
			}

			if (flags.HasFlag( RenderFlags.MODEL ))
			{
				ps.BlendState		=	BlendState.AlphaBlend;
				ps.Primitive		=	Primitive.TriangleList;

				if (flags.HasFlag( RenderFlags.WIREFRAME ))
				{
					ps.RasterizerState	=	RasterizerState.Wireframe;
				}

				if (flags.HasFlag( RenderFlags.SOLID ))
				{
					ps.RasterizerState	=	RasterizerState.CullNone;
				}
			}
		}


		protected override void Dispose(bool disposing)
		{
			if (disposing) 
			{
				vertexBuffer.Dispose();
				constBuffer.Dispose();

				foreach ( var m in debugModels )
				{
					m?.Dispose();
				}
			}
			base.Dispose( disposing );
		}


		public override void AddModel( DebugModel model )
		{
			debugModels.Add( model );
		}


		public override void RemoveModel ( DebugModel model )
		{
			debugModels.Remove( model );
		}


		public override void Submit()
		{
		}


		public override void PushVertex(DebugVertex v)
		{
			vertexDataAccum.Add(v);
		}


		void SetupRender ( RenderTargetSurface colorBuffer, DepthStencilSurface depthBuffer, Camera camera )
		{
			var dev = Game.GraphicsDevice;
			dev.ResetStates();

			dev.SetTargets( depthBuffer, colorBuffer );
			dev.SetViewport( colorBuffer.Bounds );
			dev.SetScissorRect( colorBuffer.Bounds );

			var a = camera.ProjectionMatrix.M11;
			var b = camera.ProjectionMatrix.M22;
			var w = (float)colorBuffer.Width;
			var h = (float)colorBuffer.Height;

			constData.View			=	camera.ViewMatrix;
			constData.Projection	=	camera.ProjectionMatrix;
			constData.World			=	Matrix.Identity;
			constData.ViewPosition	=	camera.GetCameraPosition4(StereoEye.Mono);
			constData.PixelSize		=	new Vector4( 1/w/a, 1/b/h, 1/w, 1/h );
			constBuffer.SetData(ref constData);

			dev.GfxConstants[0]		=	constBuffer ;
		}


		void RenderModels( RenderTargetSurface colorBuffer, DepthStencilSurface depthBuffer, Camera camera )
		{
			var dev = Game.GraphicsDevice;

			dev.PipelineState = factory[ (int)(RenderFlags.MODEL|RenderFlags.SOLID) ];

			foreach ( var debugModel in debugModels ) 
			{
				if (debugModel!=null && debugModel.RenderMode.HasFlag(DebugRenderMode.Solid)) 
				{
					constData.World	=	debugModel.World;
					constData.Color	=	debugModel.Color.ToColor4();	
					constBuffer.SetData(ref constData);
				
					debugModel.Draw( dev );
				}
			}

			dev.PipelineState = factory[ (int)(RenderFlags.MODEL|RenderFlags.WIREFRAME) ];

			foreach ( var debugModel in debugModels ) 
			{
				if (debugModel!=null && debugModel.RenderMode.HasFlag(DebugRenderMode.Wireframe)) 
				{
					constData.World	=	debugModel.World;
					constData.Color	=	debugModel.Color.ToColor4();	
					constBuffer.SetData(ref constData);
				
					debugModel.Draw( dev );
				}
			}
		}


		void RenderLines ( RenderTargetSurface colorBuffer, DepthStencilSurface depthBuffer, Camera camera )
		{
			var dev = Game.GraphicsDevice;

			dev.SetupVertexInput( vertexBuffer, null );

			var flags = new[]{ RenderFlags.LINES, RenderFlags.LINES | RenderFlags.GHOST };

			foreach ( var flag in flags ) {

				if (Game.RenderSystem.SkipGhostDebugRendering && flag.HasFlag(RenderFlags.GHOST)) 
				{
					break;
				}

				dev.PipelineState =	factory[(int)flag];

				int numDPs = MathUtil.IntDivUp(vertexDataAccum.Count, vertexBufferSize);

				for (int i = 0; i < numDPs; i++) {

					int numVerts = i < numDPs - 1 ? vertexBufferSize : vertexDataAccum.Count % vertexBufferSize;

					if (numVerts == 0) {
						break;
					}

					vertexDataAccum.CopyTo(i * vertexBufferSize, vertexArray, 0, numVerts);

					vertexBuffer.SetData(vertexArray, 0, numVerts);

					dev.Draw( numVerts, 0);

				}
			}

			vertexDataAccum.Clear();
		}


		internal void Render ( RenderTargetSurface colorBuffer, DepthStencilSurface depthBuffer, Camera camera )
		{
			DrawTracers();

			asyncRender.Render();

			if (Game.RenderSystem.SkipDebugRendering) {
				vertexDataAccum.Clear();	
				return;
			}

			var dev = Game.GraphicsDevice;
			dev.ResetStates();

			SetupRender( colorBuffer, depthBuffer, camera );

			RenderLines( colorBuffer, depthBuffer, camera );

			RenderModels( colorBuffer, depthBuffer, camera );
		}


		/*-----------------------------------------------------------------------------------------
		 *	Tracers :
		-----------------------------------------------------------------------------------------*/

		class TraceRecord {
			public Vector3 Position;
			public Color Color;
			public float Size;
			public int LifeTime;
		}


		List<TraceRecord> tracers = new List<TraceRecord>();


		public void Trace ( Vector3 position, float size, Color color, int lifeTimeInFrames = 300 )
		{
			tracers.Add( new TraceRecord() {
					Position	=	position,
					Size		=	size,
					Color		=	color,
					LifeTime	=	lifeTimeInFrames,
				});
		}


		void DrawTracers ()
		{
			foreach ( var t in tracers ) {
				t.LifeTime --;

				DrawPoint( t.Position, t.Size, t.Color );
			}

			tracers.RemoveAll( t => t.LifeTime < 0 );
		}
	}
}
