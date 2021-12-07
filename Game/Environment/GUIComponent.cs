using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Mathematics;
using IronStar.ECS;

namespace IronStar.Environment
{
	public enum UIEffect
	{
		None,
		Glitches,
	}


	public enum UIClass
	{
		SimpleButton,
		DoorButton,
	}


	public class GUIComponent : IComponent
	{
		public bool Interactive = false;
		public string Text;
		public string Target;
		public UIClass UIClass = UIClass.SimpleButton;


		public GUIComponent()
		{
		}

		public GUIComponent(bool interacrtive, string text, string target)
		{
			this.Text			=	text;
			this.Interactive	=	interacrtive;
			this.Target			=	target;
		}


		public IComponent Clone()
		{
			return (IComponent)MemberwiseClone();
		}

		public IComponent Interpolate( IComponent previous, float dt, float factor )
		{
			return Clone();
		}

		public void Save( GameState gs, BinaryWriter writer )
		{
			writer.Write( Interactive );
			writer.Write( Text );
			writer.Write( Target );
			writer.Write( (int)UIClass );
		}

		public void Load( GameState gs, BinaryReader reader )
		{
			Interactive =	reader.ReadBoolean();
			Text		=	reader.ReadString();
			Target		=	reader.ReadString();
			UIClass		=	(UIClass)reader.ReadInt32();
		}
	}
}
