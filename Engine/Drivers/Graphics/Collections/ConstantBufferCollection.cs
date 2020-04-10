using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace Fusion.Drivers.Graphics {

	/// <summary>
	/// The constant buffer collection.
	/// </summary>
	public sealed class ConstantBufferCollection : GraphicsObject {

		readonly CommonShaderStage[] stages;


		/// <summary>
		/// Creates instance of sampler state collection.
		/// </summary>
		/// <param name="device"></param>
		internal ConstantBufferCollection ( GraphicsDevice device, params CommonShaderStage[] stages ) : base(device)
		{
			this.stages	=	stages;
		}


		
		/// <summary>
		/// Total count of sampler states that can be simultaniously bound to pipeline.
		/// </summary>
		public int Count { 
			get { 
				return CommonShaderStage.ConstantBufferApiSlotCount;
			}
		}

		

		/// <summary>
		/// Sets and gets sampler state to given shader stage.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ConstantBuffer this[int index] 
		{
			set 
			{
				for (int i=0; i<stages.Length; i++)
				{
					stages[i].SetConstantBuffer( index, (value==null) ? null : value.buffer );
				}
			}
		}
	}
}
