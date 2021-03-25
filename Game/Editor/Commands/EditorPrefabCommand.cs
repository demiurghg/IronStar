using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core.Mathematics;
using Fusion.Core.Shell;
using IronStar.ECS;
using IronStar.Mapping;
using Fusion;
using Fusion.Core;
using Fusion.Core.Extensions;
using Fusion.Build;

namespace IronStar.Editor.Commands
{
	public class EditorPrefabCommand : BaseCommand, ICommand
	{
		[CommandLineParser.Required]
		[CommandLineParser.Name("name")]
		public string Name { get; set; }

		public EditorPrefabCommand( MapEditor editor ) : base( editor )
		{
			
		}

		public object Execute()
		{
			if (Selection.Any())
			{
				var baseTrans	=	Selection.Last().Translation;
				var prefab		=	new MapNodeCollection( Selection.Select( node => node.DuplicateNode() ) );

				foreach ( var node in prefab )
				{
					node.Translation -= baseTrans;
				}

				using ( var stream = editor.Game.GetService<Builder>().CreateSourceFile( "prefabs", Name + ".pfb" ) )
				{
					JsonUtils.ExportJson( stream, prefab );
				}
			}
			else
			{
				throw new Exception("Selection is empty");
			}

			return null;
		}
	}
}
