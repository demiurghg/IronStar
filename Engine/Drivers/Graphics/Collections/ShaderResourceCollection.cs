using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace Fusion.Drivers.Graphics {

	/// <summary>
	/// The texture collection.
	/// </summary>
	public sealed class ShaderResourceCollection : GraphicsResource {

		readonly CommonShaderStage	stage;
		readonly DeviceContext	deviceContext;

		readonly ShaderResourceView[] clearArray;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		internal ShaderResourceCollection ( GraphicsDevice device, CommonShaderStage stage ) : base(device)
		{
			this.stage	=	stage;
			deviceContext	=	device.DeviceContext;

			clearArray	=	Enumerable
				.Range( 0, CommonShaderStage.InputResourceRegisterCount )
				.Select( i => (ShaderResourceView)null )
				.ToArray();
		}



		/// <summary>
		/// Total count of sampler states that can be simultaniously bound to pipeline.
		/// </summary>
		public int Count { 
			get { 
				return CommonShaderStage.InputResourceRegisterCount;
			}
		}



		/// <summary>
		/// Clears collection
		/// </summary>
		public void Clear ()
		{
			stage.SetShaderResources( 0, clearArray );
		}
	

		
		/// <summary>
		/// Sets and gets shader resources bound to given shader stage.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ShaderResource this[int index] {
			set {
				stage.SetShaderResource( index, (value==null) ? null : value.SRV );
			}
		}
	}
}
