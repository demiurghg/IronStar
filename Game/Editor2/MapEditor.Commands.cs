using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Core;
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
using IronStar.Core;
using IronStar.Editor2.Controls;
using IronStar.Editor2.Manipulators;
using Fusion.Engine.Frames;
using Fusion.Core.Shell;
using Fusion.Core.Configuration;

namespace IronStar.Editor2 {

	class EditorSave : ICommand
	{
		public object Execute()
		{
			throw new NotImplementedException();
		}

		public bool IsHistoryOn()
		{
			throw new NotImplementedException();
		}

		public void Rollback()
		{
			throw new NotImplementedException();
		}
	}


	class EditorQuit : ICommand
	{
		public object Execute()
		{
			throw new NotImplementedException();
		}

		public bool IsHistoryOn()
		{
			throw new NotImplementedException();
		}

		public void Rollback()
		{
			throw new NotImplementedException();
		}
	}
}
