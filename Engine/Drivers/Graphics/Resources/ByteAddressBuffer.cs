using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Specialized;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;
using D3D11 = SharpDX.Direct3D11;
using DXGI = SharpDX.DXGI;
using System.Runtime.InteropServices;


namespace Fusion.Drivers.Graphics {

	public class ByteAddressBuffer : ShaderResource {

		internal UnorderedAccessView	UAV				{ get { return uav; } }
		internal Buffer					BufferGPU		{ get { return bufferGpu; } }
		internal Buffer					BufferStaging	{ get { return bufferStaging; } }

		public readonly int Size;
			
		UnorderedAccessView	uav;
		Buffer				bufferGpu;
		Buffer				bufferStaging;


		/// <summary>
		/// Gets unordered access 
		/// </summary>
		public UnorderedAccess UnorderedAccess { get { return unorderedAccess; } }
		UnorderedAccess unorderedAccess;

			
		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="elementCount"></param>
		/// <param name="elementType"></param>
		public ByteAddressBuffer ( GraphicsDevice device, int size ) : base(device)
		{
			Size		=	size;
			Width		=	size;
			Height		=	0;
			Depth		=	0;

			//	create staging buffer :
			var bufferDesc = new BufferDescription {
					BindFlags			= BindFlags.None,
					Usage				= ResourceUsage.Staging,
					CpuAccessFlags		= CpuAccessFlags.Write | CpuAccessFlags.Read,
					OptionFlags			= ResourceOptionFlags.None,
					SizeInBytes			= size * sizeof(uint),
				};

			bufferStaging		= new Buffer(device.Device, bufferDesc);


			//	create GPU buffer :
			bufferDesc = new BufferDescription {
					BindFlags			= BindFlags.UnorderedAccess | BindFlags.ShaderResource,
					Usage				= ResourceUsage.Default,
					CpuAccessFlags		= CpuAccessFlags.None,
					OptionFlags			= ResourceOptionFlags.BufferAllowRawViews,
					SizeInBytes			= size * sizeof(uint),
				};

			bufferGpu		= new Buffer(device.Device, bufferDesc);


			//	create UAV :
			var uavDesc = new UnorderedAccessViewDescription {
					Format		= DXGI.Format.R32_Typeless,
					Dimension	= UnorderedAccessViewDimension.Buffer,
					Buffer		= new UnorderedAccessViewDescription.BufferResource { 
						ElementCount = size, 
						FirstElement = 0, 
						Flags = UnorderedAccessViewBufferFlags.Raw,
					}
				};

			uav	= new UnorderedAccessView(device.Device, BufferGPU, uavDesc);

			unorderedAccess = new UnorderedAccess( device, uav );

			//	create SRV :
			var srvDesc = new ShaderResourceViewDescription {
					Format		= DXGI.Format.R32_UInt,
					Buffer		= {ElementCount = size, FirstElement = 0 },
					BufferEx	= { ElementCount = size, FirstElement = 0, Flags = ShaderResourceViewExtendedBufferFlags.Raw },
					Dimension	= ShaderResourceViewDimension.Buffer
				};

			SRV	=	new ShaderResourceView( device.Device, BufferGPU, srvDesc );
		}



		/// <summary>
		/// Disposes
		/// </summary>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				SafeDispose( ref SRV );
				SafeDispose( ref uav );
				SafeDispose( ref bufferGpu );
				SafeDispose( ref bufferStaging );
			} 
			
			base.Dispose(disposing);
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		public void UpdateData<T> ( T[] data ) where T: struct 
		{
			if (data==null) {
				throw new ArgumentNullException("data");
			}
			
			int inputBytes	=	data.Length * Marshal.SizeOf(typeof(T));
			int bufferBytes =	Size * sizeof(uint);

			if ( inputBytes != bufferBytes ) {
				throw new ArgumentException("Input buffer size (" + inputBytes.ToString() + " bytes) not equals structured buffer size (" + bufferBytes.ToString() + " bytes)"); 
			}

			lock (device.DeviceContext) {

				device.DeviceContext.UpdateSubresource( data, bufferGpu );

			}
		}



		/// <summary>
		/// Sets structured buffer data
		/// </summary>
		public void SetData<T> ( T[] data, int startIndex, int elementCount ) where T: struct
		{
			if (data==null) {
				throw new ArgumentNullException("data");
			}

			if (data.Length < startIndex + elementCount) {
				throw new ArgumentException("The data passed has a length of " + data.Length + " but " + elementCount + " elements have been requested."); 
			}

			int inputBytes	=	data.Length * Marshal.SizeOf(typeof(T));
			int bufferBytes =	Size * sizeof(uint);

			if ( inputBytes > bufferBytes ) {
				throw new ArgumentException("Output data (" + inputBytes.ToString() + " bytes) exceeded buffer size (" + bufferBytes.ToString() + " bytes)"); 
			}


			//
			//	Write data
			//
			lock (device.DeviceContext ) {

				var db = device.DeviceContext.MapSubresource( bufferStaging, 0, MapMode.Write, D3D11.MapFlags.None );

				SharpDX.Utilities.Write( db.DataPointer, data, startIndex, elementCount );

				device.DeviceContext.UnmapSubresource( bufferStaging, 0 );

				device.DeviceContext.CopyResource( bufferStaging, bufferGpu );
			}
		}



		/// <summary>
		/// Sets structured buffer data
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		public void SetData<T>( T[] data ) where T: struct 
		{
			SetData<T>( data, 0, data.Length );
		}



		/// <summary>
		/// Gets structured buffer data
		/// </summary>
		public void GetData<T> ( T[] data, int startIndex, int elementCount ) where T: struct
		{
			if (data==null) {
				throw new ArgumentNullException("data");
			}

			if (data.Length < startIndex + elementCount) {
				throw new ArgumentException("The data passed has a length of " + data.Length + " but " + elementCount + " elements have been requested."); 
			}

			int inputBytes	=	data.Length * Marshal.SizeOf(typeof(T));
			int bufferBytes =	Size * sizeof(uint);

			if ( inputBytes > bufferBytes ) {
				throw new ArgumentException("Input data (" + inputBytes.ToString() + " bytes) exceeded buffer size (" + bufferBytes.ToString() + " bytes)"); 
			}


			//
			//	Read data
			//	
			lock (device.DeviceContext) {
				device.DeviceContext.CopyResource( bufferGpu, bufferStaging );

				var db = device.DeviceContext.MapSubresource( bufferStaging, 0, MapMode.Read, D3D11.MapFlags.None );

				SharpDX.Utilities.Read( db.DataPointer, data, 0, data.Length );

				device.DeviceContext.UnmapSubresource( bufferStaging, 0 );
			}
		}



		/// <summary>
		/// Gets structured buffer data
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="data"></param>
		public void GetData<T> ( T[] data ) where T: struct 
		{
			GetData<T>( data, 0, data.Length );
		}
	}
}
