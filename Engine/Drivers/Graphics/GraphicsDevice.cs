using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Windows;
using SharpDX.DXGI;
using D3D = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Native.NvApi;
using Device = SharpDX.Direct3D11.Device;
using System.IO;
using Fusion.Drivers.Graphics;
using Fusion.Drivers.Graphics.Display;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Imaging;

namespace Fusion.Drivers.Graphics {

	public partial class GraphicsDevice : DisposableBase {

		public bool IsInitialized { get; private set; }

		/// <summary>
		/// Current graphics profile.
		/// </summary>
		public GraphicsProfile GraphicsProfile { 
			get; 
			private set; 
		}

		/// <summary>
		///	Game
		/// </summary>
		public readonly Game Game;

		/// <summary>
		/// Gets current display bounds.
		/// </summary>
		public Rectangle DisplayBounds	{ 
			get { 
				return new Rectangle(0,0, display.Bounds.Width, display.Bounds.Height); 
			} 
		}

		/// <summary>
		/// Sets and gets fullscreen mode.
		/// <remarks>Not all stereo modes support fullscreen or windowed mode.</remarks>
		/// </summary>
		public bool FullScreen  { 
			get { return display.Fullscreen; } 
			set { 
				if (true) { // lock (deviceContext) {
					display.Fullscreen = value; 
				}
			} 
		}

		/// <summary>
		/// Raises when display bound changes.
		/// DisplayBounds property is already has actual value when this event raised.
		/// </summary>
		public event EventHandler	DisplayBoundsChanged;

		/// <summary>
		/// Backbuffer color target.
		/// </summary>
		public RenderTarget2D	BackbufferColor	{ 
			get { 
				return display.BackbufferColor; 
			} 
		}

		/// <summary>
		/// Backbuffer depth stencil surface.
		/// </summary>
		public DepthStencil2D	BackbufferDepth	{ 
			get { 
				return display.BackbufferDepth; 
			} 
		}


		#region Samplers
		public SamplerStateCollection	GfxSamplers		{ get; private set; }	
		public SamplerStateCollection	ComputeSamplers	{ get; private set; }	
		#endregion


		#region Shader Resources
		public ShaderResourceCollection	GfxResources		{ get; private set; }	
		public ShaderResourceCollection	ComputeResources	{ get; private set; }	
		#endregion


		#region Constants
		public ConstantBufferCollection	GfxConstants		{ get; private set; }	
		public ConstantBufferCollection	ComputeConstants	{ get; private set; }	
		#endregion


		PipelineState		pipelineState			=	null;
		bool				pipelineStateDirty		=	true;


		/// <summary>
		/// Pipeline state.
		/// </summary>
		public PipelineState PipelineState {
			get {
				return pipelineState;
			}
			set {
				if (value!=pipelineState) {
					pipelineState		= value;
					pipelineStateDirty	= true;
				}
			}
		}


		HashSet<IDisposable>	toDispose = new HashSet<IDisposable>();

		internal Device			Device			{ get { return device; } }			
		internal DeviceContext	DeviceContext	{ get { return deviceContext; } }	
		internal BaseDisplay	Display			{ get { return display; } }

		BaseDisplay				display			=	null;
		Device					device			=	null;
		DeviceContext			deviceContext	=	null;


		/// <summary>
		/// 
		/// </summary>
		public GraphicsDevice ( Game game )
		{
			this.Game	=	game;
			IsInitialized = false;
		}


		bool fullscreenRequest = false;


		/// <summary>
		/// Initializes graphics device
		/// </summary>
		internal void Initialize ( GraphicsParameters parameters )
		{
			IsInitialized			=	false;
			this.GraphicsProfile	=	parameters.GraphicsProfile;

			try {
				if (parameters.StereoMode==StereoMode.Disabled) 	display	=	new GenericDisplay( Game, this, parameters ); else
				if (parameters.StereoMode==StereoMode.NV3DVision)	display	=	new NV3DVisionDisplay( Game, this, parameters ); else 
				if (parameters.StereoMode==StereoMode.DualHead)		display	=	new StereoDualHeadDisplay( Game, this, parameters ); else 
				if (parameters.StereoMode==StereoMode.Interlaced)	display	=	new StereoInterlacedDisplay( Game, this, parameters ); else 
				#if OCULUS
				//if (parameters.StereoMode==StereoMode.OculusRift)	display	=	new OculusRiftDisplay( Game, this, parameters ); else 
				#endif
					throw new ArgumentException("parameters.StereoMode");

			} catch ( GraphicsException e ) {
				Log.Warning("Failed to intialize graphics device.");
				Log.Warning("{0}", e.Message );
				Log.Warning("Attempt to use default parameters...");

				parameters.FullScreen	=	false;
				parameters.StereoMode	=	StereoMode.Disabled;

				display	=	new GenericDisplay( Game, this, parameters ); 
			}
			

			device			=	display.d3dDevice;
			deviceContext	=	device.ImmediateContext;

			display.CreateDisplayResources();


			//
			//	create color buffer :
			//	
			GfxResources		=	new ShaderResourceCollection( this, DeviceContext.PixelShader, DeviceContext.VertexShader, DeviceContext.GeometryShader );
			ComputeResources 	=	new ShaderResourceCollection( this, DeviceContext.ComputeShader		);

			GfxSamplers			=	new SamplerStateCollection	( this, DeviceContext.PixelShader, DeviceContext.VertexShader, DeviceContext.GeometryShader );
			ComputeSamplers		=	new SamplerStateCollection	( this, DeviceContext.ComputeShader		);

			GfxConstants		=	new ConstantBufferCollection( this, DeviceContext.PixelShader, DeviceContext.VertexShader, DeviceContext.GeometryShader	);
			ComputeConstants	=	new ConstantBufferCollection( this, DeviceContext.ComputeShader		);

			ResetStates();

			//FullScreen	=	parameters.FullScreen;

			IsInitialized = true;
		}



		/// <summary>
		/// 
		/// </summary>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {

				deviceContext.Flush();

				SafeDispose( ref deviceContext );
				SafeDispose( ref display );

				GfxResources	.Dispose();
				ComputeResources.Dispose();
										
				GfxSamplers		.Dispose();
				ComputeSamplers	.Dispose();
										
				GfxConstants	.Dispose();
				ComputeConstants.Dispose();
				
				SamplerState.DisposeStates();
			}

			base.Dispose(disposing);

			//GraphicsResource.ReportLiveObjects();
		}



		internal string requestScreenShotPath = null;



		/// <summary>
		/// Makes screenshot and saves image at specified path.
		/// If path is null image will be stored at MyPicures\AppName\Shot-CurrentDate.bmp
		/// </summary>
		/// <param name="path"></param>
		public void Screenshot ( string path = null )
		{
			if (path==null) {
				string userImgs = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
				string appName	= Path.GetFileNameWithoutExtension( AppDomain.CurrentDomain.FriendlyName.Replace(".vshost", "") );
				string fileName = userImgs + @"\" + appName + @"\Screenshots\Shot-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-ffff") + ".png";
				Directory.CreateDirectory( Path.GetDirectoryName( fileName ) );

				path	=	fileName;
			}

			requestScreenShotPath	=	path;
		}



		/// <summary>
		/// 
		/// </summary>
		internal void NotifyViewportChanges ()
		{
			Log.Message("Display bounds changed: {0} {1}", DisplayBounds.Width, DisplayBounds.Height );

			if (DisplayBoundsChanged!=null) {
				if (true) { // lock (deviceContext) {
					DisplayBoundsChanged( this, EventArgs.Empty );
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		internal void Prepare ()
		{
			display.Update();
		}



		/// <summary>
		/// 
		/// </summary>
		internal void Present ( int syncInterval )
		{
			syncInterval	=	MathUtil.Clamp( syncInterval, 0, 3 );

			if (!Game.IsActive)
			{
				syncInterval = 3;
			}

			if (requestScreenShotPath != null ) 
			{
				var path = Path.ChangeExtension(requestScreenShotPath, ".tga");
				requestScreenShotPath = null;

				var image = new Image<Color>( BackbufferColor.Width, BackbufferColor.Height, new Color(255,0,128,255) );
				BackbufferColor.GetData( image.RawImageData );

				ImageLib.SetAlpha( image, 255 );

				using ( var stream = File.OpenWrite( path ) )
				{
					ImageLib.SaveTga( image, stream );
				}
			}

			display.SwapBuffers( syncInterval );
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 * 	Drawing stuff :
		 *	
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Applies all GPU states before draw, dispatch or clear.
		/// Method assumes that deviceContext is already locked.
		/// </summary>
		void ApplyGpuState ()
		{
			if (pipelineStateDirty) {
				pipelineState.Set();
				pipelineStateDirty = false;
			}
		}


		/// <summary>
		/// Sets index and vertex buffer.
		/// </summary>
		/// <param name="vertexBuffer">Vertex buffer to apply</param>
		/// <param name="indexBuffer">Index buffer to apply.</param>
		public void SetupVertexInput ( VertexBuffer vertexBuffer, IndexBuffer indexBuffer )
		{
			SetupVertexInput( new[]{ vertexBuffer }, new[]{ 0 }, indexBuffer );
		}



		/// <summary>
		/// Sets index and vertex buffers.
		/// </summary>
		/// <param name="vertexBuffers">Vertex buffers to apply. Provide null if no vertex buffers are required.</param>
		/// <param name="offsets">Offsets in each vertex buffer.</param>
		/// <param name="indexBuffer">Index buffers to apply.</param>
		public void SetupVertexInput ( VertexBuffer[] vertexBuffers, int[] offsets, IndexBuffer indexBuffer )
		{
			if (true) { // lock (deviceContext) {
				if (indexBuffer!=null) {
					deviceContext.InputAssembler.SetIndexBuffer( indexBuffer.Buffer, DXGI.Format.R32_UInt, 0 );
				} else {	
					deviceContext.InputAssembler.SetIndexBuffer( null, Format.Unknown, 0 );
				}


				if (vertexBuffers==null) {
					deviceContext.InputAssembler.SetVertexBuffers( 0, new VertexBufferBinding( null, 0, 0 ) );
				} else {

					if (vertexBuffers.Length!=offsets.Length) {
						throw new InvalidOperationException("vertexBuffers.Length != offsets.Length");
					}
					if (vertexBuffers.Length>16) {
						throw new InvalidOperationException("vertexBuffers.Length > 16");
					}

					int count = vertexBuffers.Length;

					//#warning Remove allocation!
					var inputVertexBufferBinding = new VertexBufferBinding[count];

					for (int i=0; i<count; i++) {
						inputVertexBufferBinding[i].Buffer	=	( vertexBuffers[i]==null ) ? null : vertexBuffers[i].Buffer;
						inputVertexBufferBinding[i].Stride	=	( vertexBuffers[i]==null ) ? 0 : vertexBuffers[i].Stride;
						inputVertexBufferBinding[i].Offset	=	offsets[i];
					}
					deviceContext.InputAssembler.SetVertexBuffers( 0, inputVertexBufferBinding );
				}
			}
		}



		/// <summary>
		/// Draws primitives.
		/// </summary>
		/// <param name="vertexCount"></param>
		/// <param name="vertexFirstIndex"></param>
		public void Draw ( int vertexCount, int firstIndex )
		{					
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				//deviceContext.InputAssembler.PrimitiveTopology	=	Converter.Convert( primitive );
				deviceContext.Draw( vertexCount, firstIndex );
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="vertexCount"></param>
		/// <param name="vertexFirstIndex"></param>
		public void DrawAuto ()
		{									 
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				//deviceContext.InputAssembler.PrimitiveTopology	=	Converter.Convert( primitive );
				deviceContext.DrawAuto();
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="primitive"></param>
		/// <param name="vertexCountPerInstance"></param>
		/// <param name="instanceCount"></param>
		/// <param name="startVertexLocation"></param>
		/// <param name="startInstanceLocation"></param>
		public void DrawInstanced ( int vertexCountPerInstance, int instanceCount, int startVertexLocation, int startInstanceLocation )
		{
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				//deviceContext.InputAssembler.PrimitiveTopology	=	Converter.Convert( primitive );
				deviceContext.DrawInstanced( vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="indexCount"></param>
		/// <param name="firstIndex"></param>
		/// <param name="baseVertexOffset"></param>
		public void DrawIndexed ( int indexCount, int firstIndex, int baseVertexOffset )
		{
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				//deviceContext.InputAssembler.PrimitiveTopology	=	Converter.Convert( primitive );
				deviceContext.DrawIndexed( indexCount, firstIndex,	baseVertexOffset );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="indexCount"></param>
		/// <param name="firstIndex"></param>
		/// <param name="baseVertexOffset"></param>
		public void DrawInstancedIndexed ( int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation )
		{
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				deviceContext.DrawIndexedInstanced( indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="threadGroupCountX"></param>
		/// <param name="threadGroupCountY"></param>
		/// <param name="threadGroupCountZ"></param>
		public void Dispatch( int threadGroupCountX, int threadGroupCountY = 1, int threadGroupCountZ = 1 )
		{
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				deviceContext.Dispatch( threadGroupCountX, threadGroupCountY, threadGroupCountZ ); 
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="threadGroupCountX"></param>
		/// <param name="threadGroupCountY"></param>
		/// <param name="threadGroupCountZ"></param>
		public void Dispatch( uint threadGroupCountX, uint threadGroupCountY = 1, uint threadGroupCountZ = 1 )
		{
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				deviceContext.Dispatch( (int)threadGroupCountX, (int)threadGroupCountY, (int)threadGroupCountZ ); 
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="threadGroupCountX"></param>
		/// <param name="threadGroupCountY"></param>
		/// <param name="threadGroupCountZ"></param>
		public void Dispatch( Int3 totalSize, Int3 blockSize )
		{
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				int tgx	=	MathUtil.IntDivRoundUp( totalSize.X, blockSize.X );
				int tgy	=	MathUtil.IntDivRoundUp( totalSize.Y, blockSize.Y );
				int tgz	=	MathUtil.IntDivRoundUp( totalSize.Z, blockSize.Z );
				deviceContext.Dispatch( tgx, tgy, tgz ); 
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="threadGroupCountX"></param>
		/// <param name="threadGroupCountY"></param>
		/// <param name="threadGroupCountZ"></param>
		public void Dispatch( Int2 totalSize, Int2 blockSize )
		{
			if (true) { // lock (deviceContext) {
				ApplyGpuState();
				int tgx	=	MathUtil.IntDivRoundUp( totalSize.X, blockSize.X );
				int tgy	=	MathUtil.IntDivRoundUp( totalSize.Y, blockSize.Y );
				int tgz	=	1;
				deviceContext.Dispatch( tgx, tgy, tgz ); 
			}
		}


		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="layout"></param>
		/// <param name="vertexBuffer"></param>
		/// <param name="offset"></param>
		public void SetupVertexOutput ( VertexBuffer vertexBuffer, int offset )
		{
			if (vertexBuffer==null) {
				SetupVertexOutput( null, null );
			} else {
				SetupVertexOutput( new[]{ vertexBuffer }, new[]{ offset } );
			}
		}



		/// <summary>
		/// Setups vertex (stream) output.
		/// </summary>
		/// <param name="vertexBuffers">Vertex buffer to write data.</param>
		/// <param name="offset">Offset where to start writing data. -1 means append.</param>
		public void SetupVertexOutput ( VertexBuffer[] vertexBuffers, int[] offsets )
		{
			if (vertexBuffers==null || offsets==null ) {
				if (true) { // lock (deviceContext) {
					deviceContext.StreamOutput.SetTargets( null );
				}
				return;
			}

			if (vertexBuffers.Length>4) {
				throw new ArgumentException("Length of 'vertexBuffers' must be less or equal 4");
			}

			if (vertexBuffers.Length!=offsets.Length) {
				throw new ArgumentException("Lengths of 'vertexBuffers' and 'offsets' must be the same.");
			}

			if ( !offsets.All( e => (e<0) || (e%4==0) ) ) {
				throw new ArgumentException("SetupVertexOutput: Offsets must be multiple of 4.");
			}

			if ( !vertexBuffers.All( vb => vb.Options==VertexBufferOptions.VertexOutput) ) {
				throw new GraphicsException("SetupVertexOutput: Vertex buffer must be created with enabled vertex output.");
			}

			var outputBinding		=	vertexBuffers.Zip( offsets, (vb,offset) => new StreamOutputBufferBinding( vb.Buffer, offset ) ).ToArray();

			if (true) { // lock (deviceContext) {
				deviceContext.StreamOutput.SetTargets( outputBinding );
			}
		}




		/*-----------------------------------------------------------------------------------------
		 * 
		 * 	State control
		 *	
		-----------------------------------------------------------------------------------------*/

		/// <summary>
		/// Resets all devices states including RTs and DS
		/// </summary>
		public void ResetStates ()
		{
			using ( new CVEvent( "Reset GPU states" ) ) {

				lock ( this.DeviceContext ) {

					using ( new CVEvent( "deviceContext.ClearState" ) ) {
						deviceContext.ClearState();
					}

					SetTargets( null );
					SetupVertexInput( null, null );
					SetupVertexOutput( null, 0 );

					PipelineState   =   null;
				}
			}
		}



		/*-----------------------------------------------------------------------------------------
		 * 
		 *	Targets :
		 * 
		-----------------------------------------------------------------------------------------*/

		RenderTargetSurface[]	renderTargetSurfaces	=	new RenderTargetSurface[8];
		DepthStencilSurface		depthStencilSurface		=	null;


		/// <summary>
		/// 
		/// </summary>				
		public void RestoreBackbuffer ()
		{
			SetTargets( BackbufferDepth, BackbufferColor );

			SetScissorRect( BackbufferColor.Bounds );
			SetViewport( BackbufferColor.Bounds );

			if (true) { // lock (deviceContext) {
				deviceContext.Rasterizer.SetViewport( SharpDXHelper.Convert( new ViewportF( 0,0, BackbufferColor.Width, BackbufferColor.Height ) ) );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="color"></param>
		public void ClearBackbuffer ( Color4 color, float depth = 1, byte stencil = 0 )
		{
			Clear( BackbufferColor.Surface, color );
			Clear( BackbufferDepth.Surface, depth, stencil );
		}



		/// <summary>
		/// Analogue of SetTargets( depthStencil.Surface, renderTarget.Surface )
		/// </summary>
		/// <param name="depthStencil"></param>
		/// <param name="renderTarget"></param>
		public void SetTargets ( DepthStencil2D depthStencil, RenderTarget2D renderTarget )
		{
			SetTargets( depthStencil==null?null:depthStencil.Surface, renderTarget==null?null:renderTarget.Surface );
		}



		/// <summary>
		/// Sets targets.
		/// </summary>
		/// <param name="renderTargets"></param>
		public void SetTargets ( DepthStencilSurface depthStencil, params RenderTargetSurface[] renderTargets )
		{
			int w = -1;
			int h = -1;

			if (renderTargets.Length>8) {
				throw new ArgumentException("Could not bind more than 8 render targets");
			}


			if (true) { // lock (deviceContext) {

				this.depthStencilSurface	=	depthStencil;
				renderTargets.CopyTo( renderTargetSurfaces, 0 );


				if (depthStencil!=null) {
					w	=	depthStencil.Width;
					h	=	depthStencil.Height;
				}

				if (renderTargets.Any()) {
				
					if (w==-1 || h==-1) {
						w	=	renderTargets.First().Width;
						h	=	renderTargets.First().Height;
					}
				
					if ( !renderTargets.All( surf => surf.Width == w && surf.Height == h ) ) {
						throw new ArgumentException("All surfaces must be the same size", "renderTargets");
					}
				}

				DepthStencilView	dsv		=	depthStencil == null ? null : depthStencil.DSV;
				RenderTargetView[] 	rtvs	=	renderTargets.Select( rt => rt.RTV ).ToArray();

				if (!rtvs.Any()) {
					deviceContext.OutputMerger.SetTargets( dsv, (RenderTargetView)null );
				} else {
					deviceContext.OutputMerger.SetTargets( dsv, rtvs );
				}

				/*if (w>0 && h>0) {
					SetViewport( 0, 0, w, h );
				} */
			}
		}



		/// <summary>
		/// Clear depth stencil surface
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="depth"></param>
		/// <param name="stencil"></param>
		public void Clear ( DepthStencilSurface surface, float depth = 1, byte stencil = 0 )
		{
			if (true) { // lock (deviceContext) {
				deviceContext.ClearDepthStencilView( surface.DSV, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, depth, stencil );
			}
		}



		/// <summary>
		/// Clears render target using given color
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="color"></param>
		public void Clear ( RenderTargetSurface surface, Color4 color )
		{
			if (true) { // lock (deviceContext) {
				deviceContext.ClearRenderTargetView( surface.RTV, SharpDXHelper.Convert( color ) );
			}
		}



		/// <summary>
		/// Fills structured buffer with given values
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="values"></param>
		public void Clear ( StructuredBuffer buffer, Int4 values )
		{
			if (true) { // lock (deviceContext) {
				deviceContext.ClearUnorderedAccessView( buffer.UAV, SharpDXHelper.Convert( values ) );
			}
		}



		/// <summary>
		/// Fills structured buffer with given values
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="values"></param>
		public void Clear ( ByteAddressBuffer buffer, Int4 values )
		{
			if (true) { // lock (deviceContext) {
				deviceContext.ClearUnorderedAccessView( buffer.UAV, SharpDXHelper.Convert( values ) );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		public void Resolve ( RenderTarget2D source, RenderTarget2D destination )
		{
			Resolve( source.Surface, destination.Surface );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		public void Resolve ( RenderTargetSurface source, RenderTargetSurface destination )
		{
			if ( source.Width != destination.Width || source.Height != destination.Height ) {
				throw new GraphicsException( "Could not resolve: source and destination are not the same size");
			}

			if ( source.SampleCount <= 1 ) {
				throw new GraphicsException( "Could not resolve: source surface is not multisampled");
			}

			if ( destination.SampleCount > 1 ) {
				throw new GraphicsException( "Could not resolve: destination surface is multisampled");
			}

			if (true) { // lock (deviceContext) {
				deviceContext.ResolveSubresource( source.Resource, source.Subresource, destination.Resource, destination.Subresource, Converter.Convert( source.Format ) );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public void SetViewport ( int x, int y, int w, int h )
		{
			SetViewport( new ViewportF(x,y,w,h) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public void SetViewport ( Rectangle bounds )
		{
			SetViewport( new ViewportF( bounds.X, bounds.Y, bounds.Width, bounds.Height ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public void SetViewport ( Viewport viewport )
		{
			SetViewport( new ViewportF( viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public void SetViewport ( ViewportF viewport )
		{
			if (true) { // lock (deviceContext) {
				deviceContext.Rasterizer.SetViewport( SharpDXHelper.Convert( viewport ) );
			}
		}


		public void SetScissorRect ( Rectangle rect )
		{
			deviceContext.Rasterizer.SetScissorRectangle( rect.Left, rect.Top, rect.Right, rect.Bottom );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="w"></param>
		/// <param name="h"></param>
		public void SetViewport ( RenderTargetSurface surface )
		{
			if (true) { // lock (deviceContext) {
				SetViewport( 0, 0, surface.Width, surface.Height );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="register"></param>
		/// <param name="buffer"></param>
		/// <param name="initialCount">An array of append and consume buffer offsets. 
		/// A value of -1 indicates to keep the current offset. 
		/// Any other values set the hidden counter for that appendable and consumable UAV. </param>
		public void SetComputeUnorderedAccess ( int register, UnorderedAccess unorderedAccess, int initialCount = -1 ) 
		{ 
			if (register>8) {
				throw new GraphicsException("Could not bind RW buffer at register " + register.ToString() + " (max 8)");
			}

			DeviceContext.ComputeShader.SetUnorderedAccessView( register, unorderedAccess?.Uav, initialCount ); 
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="register"></param>
		/// <param name="buffer"></param>
		/// <param name="initialCount">An array of append and consume buffer offsets. 
		/// A value of -1 indicates to keep the current offset. 
		/// Any other values set the hidden counter for that appendable and consumable UAV. </param>
		public void SetGfxUnorderedAccess ( int register, UnorderedAccess unorderedAccess, int initialCount = -1 ) 
		{ 
			if (register>8) {
				throw new GraphicsException("Could not bind RW buffer at register " + register.ToString() + " (max 8)");
			}

			DeviceContext.OutputMerger.SetUnorderedAccessView( register, unorderedAccess?.Uav, initialCount ); 
		}


		/// <summary>
		/// SetCSRWBuffer & SetCSRWTexture share same registers.
		/// </summary>
		/// <param name="register"></param>
		/// <param name="buffer"></param>
		/// <param name="initialCount">An array of append and consume buffer offsets. 
		/// A value of -1 indicates to keep the current offset. 
		/// Any other values set the hidden counter for that appendable and consumable UAV. </param>
		public void SetCSRWTexture ( int register, Texture3DCompute volume ) 
		{ 
			if (register>8) {
				throw new GraphicsException("Could not bind RW texture at register " + register.ToString() + " (max 8)");
			}

			if (true) { // lock (deviceContext) {
				DeviceContext.ComputeShader.SetUnorderedAccessView( register, volume?.Uav, -1 ); 
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="register"></param>
		/// <param name="volumeTexture"></param>
		public void SetPSRWTexture ( int register, Texture3DCompute tex3D )
		{
			if (register>8) {
				throw new GraphicsException("Could not bind RW texture at register " + register.ToString() + " (max 8)");
			}

			if (true) { // lock (deviceContext) {
				DeviceContext.OutputMerger.SetUnorderedAccessView ( register, tex3D==null?null:tex3D.Uav, -1 ); 
			}
		}
	}
}
