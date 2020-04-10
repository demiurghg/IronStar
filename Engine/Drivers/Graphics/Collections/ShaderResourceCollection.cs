using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using D3D = SharpDX.Direct3D11;

namespace Fusion.Drivers.Graphics {

	/// <summary>
	/// The texture collection.
	/// </summary>
	public sealed class ShaderResourceCollection : GraphicsObject {

		readonly CommonShaderStage[] stages;
		readonly ShaderResourceView[] clearArray;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="device"></param>
		internal ShaderResourceCollection ( GraphicsDevice device, params CommonShaderStage[] stages ) : base(device)
		{
			this.stages	=	stages;

			clearArray	=	Enumerable
				.Range( 0, D3D.CommonShaderStage.InputResourceRegisterCount )
				.Select( i => (D3D.ShaderResourceView)null )
				.ToArray();
		}



		/// <summary>
		/// Total count of sampler states that can be simultaniously bound to pipeline.
		/// </summary>
		public int Count { 
			get { 
				return D3D.CommonShaderStage.InputResourceRegisterCount;
			}
		}



		/// <summary>
		/// Clears collection
		/// </summary>
		public void Clear ()
		{
			foreach ( var stage in stages )
			{
				stage.SetShaderResources( 0, clearArray );
			}
		}
	

		
		/// <summary>
		/// Sets and gets shader resources bound to given shader stage.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ShaderResource this[int index] 
		{
			set 
			{
				for (int i=0; i<stages.Length; i++)
				{
					stages[i].SetShaderResource( index, (value==null) ? null : value.SRV );
				}
			}
		}
	}
}
