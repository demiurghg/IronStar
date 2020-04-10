using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace Fusion.Drivers.Graphics {

	/// <summary>
	/// The sampler state collection.
	/// </summary>
	public sealed class SamplerStateCollection : GraphicsObject {

		readonly CommonShaderStage[] stages;


		/// <summary>
		/// Creates instance of sampler state collection.
		/// </summary>
		/// <param name="device"></param>
		internal SamplerStateCollection ( GraphicsDevice device, params CommonShaderStage[] stages ) : base(device)
		{
			this.stages	=	stages;
		}


		
		/// <summary>
		/// Total count of sampler states that can be simultaniously bound to pipeline.
		/// </summary>
		public int Count { 
			get { 
				return CommonShaderStage.SamplerRegisterCount;
			}
		}
		
		/// <summary>
		/// Sets and gets sampler state to given shader stage.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public SamplerState this[int index] 
		{
			set 
			{
				for (int i=0; i<stages.Length; i++)
				{
					stages[i].SetSampler( index, (value==null) ? null : value.Apply(device) );
				}
			}
		}
	}
}
