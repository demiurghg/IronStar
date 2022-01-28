using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Microsoft.Win32;
using System.IO;
using Fusion.Engine.Graphics;
using BEPUutilities.Threading;

namespace Fusion.Build 
{
	public interface IBuildContext 
	{
		VTStorage	GetVTStorage ();

		ParallelOptions	ParallelOptions { get; }

		IEnumerable<string>	ContentDirectories { get; }

		string		FullOutputDirectory { get; }

		string		ResolveContentPath ( string path );
		string		ResolveContentPath ( string path, out string baseDirectory );

		bool		ContentFileExists ( string path );
		string		GetRelativePath ( string path );

		string		GetTempFileFullPath ( string key, string ext );
		string		WriteReport( AssetSource assetFile, string textContent, string subfile = null );
	
		void		CopyFileTo( string fullSourceFileName, Stream targetStream );
		void		CopyFileTo( string fullSourceFileName, BinaryWriter writer );

		bool		IsUpToDate ( string sourceFile, string targetFile );
		int			RunTool ( string exePath, string commandLine, out string stdout, out string stderr );
		void		RunTool ( string exePath, string commandLine );
	}
}
