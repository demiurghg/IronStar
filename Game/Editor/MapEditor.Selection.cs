using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core.Content;
using Fusion.Engine.Server;
using Fusion.Engine.Client;
using Fusion.Core.Extensions;
using IronStar.SFX;
using Fusion.Core.IniParser.Model;
using Fusion.Engine.Graphics;
using IronStar.Mapping;
using Fusion.Build;
using BEPUphysics;
using IronStar.ECS;
using IronStar.Editor.Commands;

namespace IronStar.Editor 
{

	/// <summary>
	/// World represents entire game state.
	/// </summary>
	public partial class MapEditor 
	{
		LayerState GetLayerStateForNode ( MapNode node )
		{
			if (node is MapEntity)			return LayerEntities;

			if (node is MapModel)			return LayerGeometry;
			if (node is MapDecal)			return LayerDecals;

			if (node is MapLightProbeBox)		return LayerLightProbes;
			if (node is MapOmniLight)		return LayerLightSet;
			if (node is MapSpotLight)		return LayerLightSet;

			return LayerState.Default;
		}


		bool IsVisible ( MapNode node )
		{
			return (GetLayerStateForNode(node)==LayerState.Frozen || GetLayerStateForNode(node)==LayerState.Default) && node.Visible; 
		}


		bool IsSelectable ( MapNode node )
		{
			return (GetLayerStateForNode(node)==LayerState.Default) && !node.Frozen; 
		}


		public bool IsSelected( Entity entity )
		{
			return selection.Any( node => node.EcsEntity == entity );
		}


		public bool GetRenderProperties( Entity entity, out Color color, out bool selected )
		{
			var node	=	Map.Nodes.FirstOrDefault( n => n.EcsEntity == entity );

			if (node!=null)
			{
				selected	=	selection.Contains(node);
				color		=	selected ? Utils.WireColorSelected : ( IsSelectable(node) ? Utils.WireColor : Utils.GridColor );
				color		=	(selection.LastOrDefault()==node) ? Color.White : color;

				return IsVisible(node);
			}
			else
			{
				color		=	Color.Black;
				selected	=	false;
				return false;
			}
		}
	}
}
