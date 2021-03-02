﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;
using Fusion.Engine.Graphics;
using RSOmniLight = Fusion.Engine.Graphics.OmniLight;
using Fusion.Core.Shell;
using System.IO;
using Fusion.Widgets.Advanced;

namespace IronStar.SFX2
{
	public class LightProbeBox : IComponent
	{
		public readonly Guid guid;

		[AECategory("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Width { get; set; } = 16;

		[AECategory("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Height { get; set; } = 16;

		[AECategory("Light probe")]
		[AESlider(0,256,8,0.25f)]
		public float Depth  { get; set; } = 16;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Width")]
		[AESlider(0.25f,32,1,0.25f)]
		public float ShellWidth  { get; set; } = 8f;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Height")]
		[AESlider(0.25f,32,1,0.25f)]
		public float ShellHeight  { get; set; } = 8f;

		[AECategory("Light probe")]
		[AEDisplayName("Transition Depth")]
		[AESlider(0.25f,32,1,0.25f)]
		public float ShellDepth  { get; set; } = 8f;

		public LightProbeBox ( Guid guid )
		{
			this.guid	=	guid;
		}

		public void Save( GameState gs, Stream stream ) {}
		public void Load( GameState gs, Stream stream ) {}
	}
}
