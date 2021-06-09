using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Fusion.Build.Processors;
using Fusion.Core.IniParser;
using Fusion.Core.IniParser.Model;
using Fusion;
using Fusion.Core.Shell;
using Fusion.Core.Content;
using Fusion.Core.Extensions;
using Fusion.Engine.Graphics;
using System.Net;
using Fusion.Build.Mapping;
using Fusion.Engine.Graphics.Ubershaders;
using Fusion.Core;

namespace Fusion.Build 
{
	public class Builder 
	{

		readonly List<string> ignorePatterns = new List<string>();
		readonly List<string> inputDirs = new List<string>();
		readonly List<string> toolsDirs = new List<string>();
		readonly List<BuildRule> buildRules = new List<BuildRule>();
		readonly List<AssetGenerator> generators = new List<AssetGenerator>();
		string tempDirectory;
		string targetDirectory;

		DirectoryInfo targetDirInfo;

		/// <summary>
		/// Creates instance of Builder
		/// </summary>
		/// <param name="inputPath"></param>
		/// <param name="outputPath"></param>
		public Builder( string targetDir )
		{
			targetDirectory = targetDir;
			targetDirInfo	= new DirectoryInfo( targetDir );
		}


		/// <summary>
		/// Adds ignore pattern to the list.
		/// Only one pattern at once.
		/// </summary>
		/// <param name="ignorePattern"></param>
		/// <returns>This builder instance</returns>
		public Builder Ignore( string ignorePattern )
		{
			ignorePatterns.Insert( 0, ignorePattern );
			return this;
		}


		/// <summary>
		/// Sets temporary directory path
		/// </summary>
		/// <param name="tempPath"></param>
		/// <returns></returns>
		public Builder TempDirectory ( string tempPath )
		{
			tempDirectory = tempPath;
			return this;
		}


		/// <summary>
		/// Adds additional content directory
		/// </summary>
		/// <param name="directoryPath"></param>
		/// <returns>This builder instance</returns>
		public Builder InputDirectory( string contentPath )
		{
			inputDirs.Add( contentPath );
			return this;
		}


		/// <summary>
		/// Adds additional content directory
		/// </summary>
		/// <param name="directoryPath"></param>
		/// <returns>This builder instance</returns>
		public Builder TargetDirectory( string targetPath )
		{
			targetDirectory = targetPath;
			return this;
		}



		/// <summary>
		/// Adds directory to search tools.
		/// </summary>
		/// <param name="directoryPath"></param>
		/// <returns>This builder instance</returns>
		public Builder ToolsDirectory( string toolsPath )
		{
			toolsDirs.Insert( 0, toolsPath );
			return this;
		}


		/// <summary>
		/// Adds content generator
		/// </summary>
		/// <param name="generator"></param>
		/// <returns>This builder instance</returns>
		public Builder Generate( AssetGenerator generator )
		{
			generators.Add( generator );
			return this;
		}


		/// <summary>
		/// Adds content processor and pattern to match files for processing.
		/// Newer patterns has higher priority.
		/// </summary>
		/// <param name="pattern"></param>
		/// <param name="processor"></param>
		/// <returns></returns>
		public Builder Process ( string pattern, AssetProcessor processor )
		{
			buildRules.Add( new BuildRule( pattern, processor ) );
			return this;
		}


		/// <summary>
		/// Copy raw files as target type
		/// </summary>
		/// <typeparam name="TTarget">Target object type</typeparam>
		/// <param name="pattern"></param>
		/// <returns></returns>
		public Builder Copy<TTarget> ( string pattern )
		{
			buildRules.Add( new BuildRule( pattern, new CopyProcessor(typeof(TTarget)) ) );
			return this;
		}


		/// <summary>
		/// Copy raw files as target type. Files are specified by base directory and extension
		/// </summary>
		/// <typeparam name="TTarget"></typeparam>
		/// <param name="directory">Base directory for content type</param>
		/// <param name="extension">Extension with leading dot</param>
		/// <returns></returns>
		public Builder Copy<TTarget> ( string directory, string extension )
		{
			buildRules.Add( new BuildRule( directory + "/*" + extension, new CopyProcessor(typeof(TTarget)) ) );
			return this;
		}


		/// <summary>
		/// Builds content
		/// </summary>
		/// <param name="rebuild"></param>
		/// <param name=""></param>
		/// <returns></returns>
		public BuildResult Build ()
		{
			return BuildInternal( false, null );
		}


		/// <summary>
		/// Rebuilds entire content
		/// </summary>
		/// <param name="rebuild"></param>
		/// <param name=""></param>
		/// <returns></returns>
		public BuildResult RebuildAll ()
		{
			return BuildInternal( true, null );
		}


		/// <summary>
		/// Rebuilds content that matches specified pattern
		/// </summary>
		/// <param name="rebuild"></param>
		/// <param name=""></param>
		/// <returns></returns>
		public BuildResult Rebuild ( string pattern )
		{
			return BuildInternal( false, pattern );
		}


		/// <summary>
		/// Gets full path to first input directory
		/// </summary>
		/// <returns></returns>
		public string GetBaseInputDirectory ()
		{
			return Path.GetFullPath( inputDirs.FirstOrDefault() );
		}


		/// <summary>
		/// Creates and replace existing source file for writing
		/// </summary>
		/// <param name="dir"></param>
		/// <param name="nameExt"></param>
		/// <returns></returns>
		public Stream CreateSourceFile( string dir, string nameExt )
		{
			var basePath	=	GetBaseInputDirectory();
			var dirPath		=	Path.Combine( basePath, dir );
			var filePath	=	Path.Combine( basePath, dir, nameExt );

			if (!Directory.Exists(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}

			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}

			return File.OpenWrite(filePath);
		}


		public Stream CreateSourceFile( string nameExt )
		{
			var basePath	=	GetBaseInputDirectory();
			var filePath	=	Path.Combine( basePath, nameExt );
			var dirPath		=	Path.GetDirectoryName( filePath );

			if (!Directory.Exists(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}

			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}

			return File.OpenWrite(filePath);
		}


		public Stream OpenSourceFile( string nameExt )
		{
			var basePath	=	GetBaseInputDirectory();
			var filePath	=	Path.Combine( basePath, nameExt );

			return File.OpenRead(filePath);
		}


		public void CreateJsonFile( Type type, string dir, string nameExt )
		{
			using ( var file = CreateSourceFile( dir, nameExt ) )
			{
				JsonUtils.ExportJson( file, Activator.CreateInstance( type ) );
			}
		}


		public void CreateJsonFile<T>( string dir, string nameExt )
		{
			CreateJsonFile( typeof(T), dir, nameExt );
		}

		/*-----------------------------------------------------------------------------------------
		 * 
		 * Content building
		 * 
		-----------------------------------------------------------------------------------------*/

		class LocalFile
		{
			public bool Handled;
			public readonly string KeyPath;
			public readonly string BaseDir;
			public readonly string FullPath;

			public LocalFile( string baseDir, string fullPath )
			{
				this.Handled    =   false;
				this.BaseDir    =   baseDir;
				this.FullPath   =   fullPath;
				this.KeyPath    =   ContentUtils.BackslashesToSlashes( ContentUtils.MakeRelativePath( baseDir+"/", fullPath ) );
			}
		}


		class BuildRule 
		{
			public readonly string Pattern;
			public readonly AssetProcessor Processor;

			public BuildRule ( string pattern, AssetProcessor processor )
			{
				Pattern	=	pattern;
				Processor	=	processor;
			}
		}



		/// <summary>
		/// Internal all-in-one function
		/// </summary>
		/// <param name="rebuild"></param>
		/// <param name="pattern"></param>
		/// <returns></returns>
		private BuildResult BuildInternal( bool rebuild, string pattern )
		{
			Log.Message("");
			Log.Message("-------- Build started --------" );

			var result = new BuildResult();

			IBuildContext context = new BuildContext( targetDirectory, inputDirs, toolsDirs, tempDirectory );

			Log.Message("Gathering files...");
			var assetSources = GatherAssetFiles( context, ref result );

			Log.Message("Cleaning stale content up...");
			CleanStaleContent( targetDirInfo.FullName, assetSources );			
			Log.Message("");

			Log.Message("Building assets...");
			foreach ( var assetFile in assetSources ) 
			{
				BuildAsset( context, assetFile, rebuild, pattern, ref result );
			}

			Log.Message("-------- {5} total, {0} succeeded, {1} failed, {2} up-to-date, {3} ignored, {4} skipped --------", 
				result.Succeded,
				result.Failed,
				result.UpToDate,
				result.Ignored,
				result.Skipped,
				result.Total );

			Log.Message("");

			return result;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="iniData"></param>
		/// <param name="context"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		List<AssetSource> GatherAssetFiles ( IBuildContext context, ref BuildResult result )
		{
			var assetSources = new List<AssetSource>();

			//	key contain key path
			//	value containt full path
			var files	=	new List<LocalFile>();

			Log.Message("...searching content directories");

			//	gather files from all directories 
			//	and then distinct them by key path.
			foreach ( var contentDir in context.ContentDirectories ) 
			{
				var localFiles	=	Directory.EnumerateFiles( contentDir, "*", SearchOption.AllDirectories );
				files.AddRange( localFiles.Select( fullpath => new LocalFile( contentDir, fullpath ) ) );
			}

			files			=	files.DistinctBy( file => file.KeyPath ).ToList();
			result.Total	=	files.Count;

			Log.Message("...filtering");

			//	ignore files by ignore pattern
			//	and count ignored files :
			result.Ignored = files.RemoveAll( file =>
			{
				foreach ( var pattern in ignorePatterns )
				{
					if ( Wildcard.Match( file.KeyPath, pattern ) )
					{
						return true;
					}
				}
				return false;
			} );


			Log.Message("...generating content");

			//	generate content using provided generators :
			foreach ( var generator in generators )
			{
				assetSources.AddRange( generator.Generate( context, result ) );
			}


			//  build associate files with processors according to 
			//	build rules :	
			Log.Message("...building workload");

			foreach ( var buildRule in buildRules.Reverse<BuildRule>() )
			{
				foreach ( var file in files )
				{
					if ( file.Handled )
					{
						continue;
					}

					if ( Wildcard.Match( file.KeyPath, buildRule.Pattern, true ) )
					{
						file.Handled = true;
						assetSources.Add( new AssetSource( file.KeyPath, file.BaseDir, targetDirInfo.FullName, buildRule.Processor, context ) );
					}
				}
			}

			//	count non-handled files :
			result.Skipped = files.Count( f => !f.Handled );

			//	remove dulicates :
			assetSources = assetSources.DistinctBy( f => f.KeyPath ).ToList();

			return assetSources;
		}


		/// <summary>
		/// Removes all content thar do not match given files.
		/// </summary>
		/// <param name="outputFolder"></param>
		/// <param name="files"></param>
		void CleanStaleContent ( string outputFolder, IEnumerable<AssetSource> inputFiles )
		{
			var dictinary	=	inputFiles.ToDictionary( file => file.FullTargetPath, StringComparer.CurrentCultureIgnoreCase );
			var outputFiles =	Directory.EnumerateFiles( outputFolder, "*.asset", SearchOption.AllDirectories ).ToArray();

			int totalOutput	=	outputFiles.Length;
			
			var staleFiles	=	outputFiles.Where( file => !dictinary.ContainsKey(file) ).ToArray();

			int totalStale	=	staleFiles.Length;

			foreach ( var name in staleFiles ) 
			{
				File.Delete( name );
			}

			Log.Message("{0} stale files from {1} are removed", totalStale, totalOutput );
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="processor"></param>
		/// <param name="fileName"></param>
		void BuildAsset ( IBuildContext context, AssetSource assetFile, bool rebuild, string pattern, ref BuildResult buildResult )
		{					
			try {

				//	Is up-to-date?
				if ( !rebuild )
				{
					if ( !Wildcard.Match( assetFile.KeyPath, pattern, true ) )
					{
						if ( assetFile.IsUpToDate )
						{
							buildResult.UpToDate++;
							return;
						}
					}
				}

				//	Build :
				Log.Message("...{0}", assetFile.KeyPath);

				assetFile.Processor.Process( assetFile, context );

				buildResult.Succeded ++;

			}
			catch ( BuildException be )
			{

				Log.Error( be.Message );
				buildResult.Failed++;

			}
			catch ( AggregateException ae )
			{

				ae = ae.Flatten();

				foreach ( var e in ae.InnerExceptions )
				{
					Log.Error( e.Message );
				}
				buildResult.Failed++;

			}
			catch ( Exception e )
			{

				Log.Error( "-------- Unhandled Exception --------" );
				Log.Error( "Asset:{0}", assetFile.KeyPath );
				Log.Error( "{0}", e );

				if ( e.InnerException!=null )
				{
					Log.Error( "" );
					Log.Error( "{0}", e.InnerException );
				}

				Log.Error( "-------------------------------------" );
				buildResult.Failed++;
			}
		}
	}
}
