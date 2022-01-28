using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Fusion.Build.Processors;
using Fusion.Core.IniParser;
using Fusion.Core.IniParser.Model;
using Fusion;
using Fusion.Core;
using Fusion.Core.Content;
using Microsoft.Win32;
using Fusion.Engine.Graphics;

namespace Fusion.Build 
{
	public class BuildContext : IBuildContext 
	{
		readonly List<string> contentDirs = new List<string>();
		readonly List<string> toolsDirs = new List<string>();
		string tempDir;
		string targetDir;

		public IEnumerable<string> ContentDirectories { get { return contentDirs; } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="options"></param>
		public BuildContext ( string targetDir, IEnumerable<string> contentDirs, IEnumerable<string> toolsDirs, string tempDir )
		{
			Log.Message("Current dir : {0}", Directory.GetCurrentDirectory() );

			this.contentDirs	=	contentDirs.Select( dir => ResolveDirectory( dir ) ).ToList();
			this.toolsDirs		=	toolsDirs.Select( dir => ResolveDirectory( dir ) ).ToList();
			this.targetDir		=	ResolveDirectory( targetDir, true );
			this.tempDir		=	ResolveDirectory( tempDir, true );

			Log.Message("Source directories:");

				foreach ( var dir in this.contentDirs ) 
				{
					Log.Message("  {0}", dir );
				}

			Log.Message("");


			Log.Message("Tools directories:");

				foreach ( var dir in this.toolsDirs ) 
				{
					Log.Message("  {0}", dir );
				}

			Log.Message("");


			Log.Message("Target directory:");
			Log.Message("  {0}", this.targetDir );
			Log.Message("");

																	  
			Log.Message("Temp directory:");
			Log.Message("  {0}", this.tempDir );

			Log.Message("");
		}


		public string FullOutputDirectory 
		{
			get 
			{
				return targetDir;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public VTStorage GetVTStorage ()
		{
			var path = Path.Combine( targetDir, ".vtstorage" );
			return new VTStorage( path, false );
		}

		enum ResolveResult
		{
			Success,
			RegistryValueNotFound,
			RegistryValueIsNotAString,
			EnvironmentVariableNotFound,
			DirectoryNotFound,
		}

		ResolveResult TryResolveDirectory( string dir, out string resolvedDir )
		{
			resolvedDir = null;

			if ( dir.StartsWith("%") && dir.EndsWith("%") ) 
			{
				resolvedDir = Environment.GetEnvironmentVariable( dir.Substring(1, dir.Length-2) );
				if (resolvedDir==null) 
				{
					return ResolveResult.EnvironmentVariableNotFound;
				}
				if (!Directory.Exists( resolvedDir )) 
				{
					return ResolveResult.DirectoryNotFound;
				}
			}

			if ( dir.StartsWith("HKEY_") ) 
			{
				var keyValue	=	dir.Split(new[]{'@'}, 2);
				var key			=	keyValue[0];
				var value		=	keyValue.Length == 2 ? keyValue[1] : "";

				var regValue		=	Registry.GetValue(key, value, null);
				resolvedDir			=	regValue as string;

				if (regValue==null) 
				{
					return ResolveResult.RegistryValueNotFound;
				}
				if (resolvedDir==null) 
				{
					return ResolveResult.RegistryValueIsNotAString;
				}

				if (!Directory.Exists( resolvedDir )) 
				{
					return ResolveResult.DirectoryNotFound;
				}

				return ResolveResult.Success;
			}

			resolvedDir = Path.IsPathRooted( dir ) ? dir : Path.GetFullPath( dir );

			if (Directory.Exists( resolvedDir )) 
			{
				return ResolveResult.Success;
			}
			else
			{
				return ResolveResult.DirectoryNotFound;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		string ResolveDirectory ( string dir, bool createIfMissing = false )
		{
			if (dir==null) throw new ArgumentNullException("dir");

			string resolvedDir;
			var result = TryResolveDirectory( dir, out resolvedDir );

			switch (result)
			{
				case ResolveResult.Success:	return resolvedDir;
				case ResolveResult.RegistryValueNotFound:		throw new ContentException("Registry value not found: " + dir);
				case ResolveResult.RegistryValueIsNotAString:	throw new ContentException("Registry value is not a string: " + dir);
				case ResolveResult.EnvironmentVariableNotFound:	throw new ContentException("Environment variable not found: " + dir);
				case ResolveResult.DirectoryNotFound:
					if (createIfMissing)
					{
						Log.Message("Create missing directory : {0}", resolvedDir );
						var di = Directory.CreateDirectory(dir);
						return di.FullName;
					}
					else
					{
						throw new ContentException("Directory does not exist: " + resolvedDir);
					}
			}

			return resolvedDir;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string ResolveContentPath ( string path )
		{
			string dummy;
			return ResolveContentPath( path, out dummy );
		}

		

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string ResolveContentPath ( string path, out string baseDirectory )
		{
			var resolvedPath = ResolvePath( path, contentDirs, out baseDirectory );

			if (resolvedPath==null) {
				throw new BuildException(string.Format("Path '{0}' not resolved", path));
			}

			return resolvedPath;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="dirs"></param>
		/// <returns></returns>
		string ResolvePath ( string path, IEnumerable<string> dirs )
		{
			string dummy;
			return ResolvePath( path, dirs, out dummy );
		}

		

		/// <summary>
		/// Resolves content file path
		/// </summary>
		/// <param name="path">Key path</param>
		/// <param name="dirs">Possible directories</param>
		/// <param name="baseDirectory">Directory where given key path was resolved</param>
		/// <returns></returns>
		string ResolvePath ( string path, IEnumerable<string> dirs, out string baseDirectory )
		{
			baseDirectory	=	null;

			if (path==null) {
				throw new ArgumentNullException("path");
			}

			if ( Path.IsPathRooted( path ) ) {
				if (File.Exists( path )) {
					return path;
				} else {
					return null;
				}
			}

			//
			//	make search list :
			//
			foreach ( var dir in dirs ) {
				//Log.Message("...{0}", dir );
				var fullPath = Path.GetFullPath( Path.Combine( dir, path ) );
				if ( File.Exists( fullPath ) ) {
					baseDirectory = dir;
					return fullPath;
				}
			}

			return null;
		}



		/// <summary>
		/// Try to resolve source file path.
		/// If succeded returns true. False otherwice.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool ContentFileExists ( string path )
		{
			string dummy;
			return ResolvePath( path, contentDirs, out dummy ) != null;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public string GetRelativePath ( string path )
		{
			var baseDir = contentDirs.FirstOrDefault();
			return ContentUtils.MakeRelativePath( baseDir + @"\", path );
		}


		/// <summary>
		/// Generates temporary file name for given key with given extension
		/// and return full path to this file.
		/// </summary>
		/// <param name="key">unique key string value</param>
		/// <param name="ext">Desired extension with leading dot</param>
		/// <returns>Full path for generated file name.</returns>
		public string GetTempFileFullPath ( string key, string ext )
		{
			//var fileName	=	ContentUtils.GetHashedFileName( key, ext );
			var fileName	=	Path.GetFileName( key );

			var relDir		=	Path.GetDirectoryName( key );

			var fullDir		=	Path.Combine( tempDir, relDir );

			if (!Directory.Exists(fullDir)) {
				Directory.CreateDirectory(fullDir);
			}

			return Path.Combine( fullDir, fileName + ext );

			//return Path.Combine( Options.FullTempDirectory, fileName );
		}



		/// <summary>
		/// Writes report
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public string WriteReport( AssetSource assetFile, string textContent, string subfile = null )
		{
			string fileName		=	assetFile.KeyPath + (subfile??"") + ".html";
			string fullPath		=	Path.Combine( tempDir, fileName );
			string dirPath		=	Path.GetDirectoryName( fullPath );
			
			Directory.CreateDirectory( dirPath );
			File.WriteAllText( fullPath, textContent );

			return fileName;
		}



		/// <summary>
		/// Copies file to stream.
		/// </summary>
		/// <param name="fullSourceFileName"></param>
		/// <param name="targetStream"></param>
		public void CopyFileTo( string fullSourceFileName, Stream targetStream )
		{
			using ( var source = File.OpenRead( fullSourceFileName ) ) { 
				source.CopyTo( targetStream );
			}
		}



		/// <summary>
		/// Copies file file to stream.
		/// </summary>
		/// <param name="fullSourceFileName">Resolved source file name</param>
		/// <param name="targetStream"></param>
		public void CopyFileTo( string fullSourceFileName, BinaryWriter writer )
		{
			var data = File.ReadAllBytes( fullSourceFileName ); 
			writer.Write( data );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceFile"></param>
		/// <param name="targetFile"></param>
		/// <returns></returns>
		public bool IsUpToDate ( string sourceFile, string targetFile )
		{
			if (string.IsNullOrEmpty(sourceFile)) {
				throw new ArgumentNullException("sourceFile");
			}

			if (string.IsNullOrEmpty(targetFile)) {
				throw new ArgumentNullException("targetFile");
			}

			if (!Path.IsPathRooted(sourceFile)) {
				throw new ArgumentException("sourceFile", "Path must be rooted");
			}

			if (!Path.IsPathRooted(targetFile)) {
				throw new ArgumentException("sourceFile", "Path must be rooted");
			}


			if (!File.Exists(targetFile)) {
				return false;
			}
			
			var targetTime	=	File.GetLastWriteTime( targetFile );
			var sourceTime	=	File.GetLastWriteTime( sourceFile );

			if (targetTime < sourceTime) {
				return false;
			}

			return true;
		}
							  


		public int RunTool ( string exePath, string commandLine, out string stdout, out string stderr )
		{
			Log.Debug("...exec: {0} {1}", exePath, commandLine );

			ProcessStartInfo psi = new ProcessStartInfo();
			psi.RedirectStandardInput	=	true;
			psi.RedirectStandardOutput	=	true;
			psi.RedirectStandardError	=	true;
			psi.FileName				=	ResolvePath( exePath, toolsDirs );
			psi.Arguments				=	commandLine;
			psi.UseShellExecute			=	false;
			psi.CreateNoWindow			=	true;

			int exitCode = 0;

			using ( Process proc = Process.Start( psi ) ) {
				stdout = proc.StandardOutput.ReadToEnd().Trim(new[]{'\r', '\n'});
				stderr = proc.StandardError.ReadToEnd().Trim(new[]{'\r', '\n'});
				proc.WaitForExit();
				exitCode = proc.ExitCode;
			}

			return exitCode;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="exePath"></param>
		/// <param name="commandLine"></param>
		public void RunTool ( string exePath, string commandLine )
		{
			//commandLine = FixEncoding(commandLine);
			Log.Debug("...exec: {0} {1}", exePath, commandLine );

			ProcessStartInfo psi = new ProcessStartInfo();
			psi.RedirectStandardInput	=	true;
			psi.RedirectStandardOutput	=	true;
			psi.RedirectStandardError	=	true;
			psi.FileName				=	ResolvePath( exePath, toolsDirs );
			psi.Arguments				=	commandLine;
			psi.UseShellExecute			=	false;
			psi.CreateNoWindow			=	true;

			int exitCode = 0;

			string stdout = "";
			string stderr = "";


			using ( Process proc = Process.Start( psi ) ) {
				stdout = proc.StandardOutput.ReadToEnd().Trim(new[]{'\r', '\n'});
				stderr = proc.StandardError.ReadToEnd().Trim(new[]{'\r', '\n'});
				proc.WaitForExit();
				exitCode = proc.ExitCode;
			}

			Log.Trace( "{0}", stdout ); //*/
				
			if ( exitCode != 0 ) {
				//File.WriteAllText( @"C:\GITHUB\stderr.txt", commandLine);
				throw new ToolException( string.Format("Failed to launch tool:\r\n{0} {1}\r\n{2}", exePath, commandLine, stderr ) );
			} else {
				if (!string.IsNullOrWhiteSpace(stderr)) {				
					Log.Warning( "{0}", stderr.Trim(new[]{'\r', '\n'}) );
				} 
			}
		}
	}
}
