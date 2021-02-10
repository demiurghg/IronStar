using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using Fusion.Engine.Graphics.Scenes;
using Fusion.Core;

namespace IronStar
{
	/// <summary>
	/// Represent particular scene representation, like physical, rendering etc.
	/// </summary>
	/// <typeparam name="TMesh"></typeparam>
	public class SceneView<TMesh> where TMesh: class
	{
		public readonly Scene		scene;
		public readonly Matrix[]	transforms;
		public readonly TMesh[]		meshes;



		public SceneView( Scene scene, Func<Node,Mesh,TMesh> meshSelector, Func<Node,bool> nodeFilter )
		{
			this.scene	=	scene;
			transforms	=	new Matrix[ scene.Nodes.Count ];
			meshes		=	new TMesh[ scene.Nodes.Count ];

			scene.ComputeAbsoluteTransforms( transforms );

			for ( int i=0; i<scene.Nodes.Count; i++ ) 
			{
				if (nodeFilter(scene.Nodes[i]))
				{
					var node		=	scene.Nodes[i];
					var meshIndex	=	node.MeshIndex;
					meshes[i]		=	(meshIndex < 0) ? null : meshSelector( node, scene.Meshes[ meshIndex ] );
				}
			}
		}

		public SceneView( Scene scene, Func<Mesh,TMesh> meshSelector, Func<Node,bool> nodeFilter )
		:this( scene, (node,mesh) => meshSelector(mesh), nodeFilter )
		{
			
		}

		public Matrix GetAbsoluteTransform(int nodeIndex)
		{
			return transforms[nodeIndex];
		}


		public void SetTransform( Action<TMesh,Matrix> transformAction, Matrix worldMatrix )
		{
			for ( int i=0; i<meshes.Length; i++ )
			{
				if (meshes[i]!=null)
				{
					transformAction( meshes[i], transforms[i] * worldMatrix );
				}
			}
		}


		public void ForEachMesh( Action<TMesh> action )
		{
			foreach ( var mesh in meshes )
			{
				if (mesh!=null)
				{
					action( mesh );
				}
			}
		}
	}
}
