﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Extensions;

namespace Fusion.Engine.Graphics.Ubershaders {

	/// <summary>
	/// Marks class as requiring the list of shaders.
	/// </summary>
	internal sealed class RequireShaderAttribute : Attribute {

		/// <summary>
		/// Gets collection of required shaders.
		/// </summary>
		public string RequiredShader {
			get; private set;
		}
		

		/// <summary>
		/// 
		/// </summary>
		public bool AutoGenerateHeader {
			get; private set;
		}


		public string IfDefined {
			get; private set;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="shaders"></param>
		public RequireShaderAttribute ( string shader, bool auto = false, string ifdef = null )
		{
			RequiredShader		=	shader;
			AutoGenerateHeader	=	auto;
			IfDefined			=	ifdef;
		}
	}
}
