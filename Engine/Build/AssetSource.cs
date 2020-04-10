using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Fusion;
using Fusion.Core.Content;
using Fusion.Build.Processors;

namespace Fusion.Build {

	public class AssetSource {
			
		string fullPath;
		string keyPath;
		string baseDir;
		string outputDir;
		readonly AssetProcessor processor;
		readonly IBuildContext context;

		/// <summary>
		/// Logical relativee path to file to be built.
		/// </summary>
		public string KeyPath { 
			get { 
				return keyPath; 
			} 
		}

		/// <summary>
		/// Full actual path to asset file
		/// </summary>
		public string FullSourcePath { 
			get { 
				return fullPath; 
			} 
		}

		/// <summary>
		/// Base directory for this file
		/// </summary>
		public string BaseDirectory { 
			get { return baseDir; } 
		}



		/// <summary>
		/// Gets target file name.
		/// </summary>
		public string TargetName {
			get {
				return ContentUtils.SlashesToBackslashes( Path.ChangeExtension( KeyPath, ".asset" ) );
				//return ContentUtils.GetHashedFileName( KeyPath, ".asset" );
			}
		}


		public AssetProcessor Processor {
			get {
				return processor;
			}
		}


		/// <summary>
		/// Full target file path
		/// </summary>
		public string FullTargetPath {
			get {
				return Path.Combine( outputDir, TargetName );
			}
		}



		/// <summary>
		/// 
		/// </summary>
		public bool TargetFileExists {
			get {
				return File.Exists( FullTargetPath );
			}
		}



		/// <summary>
		/// Is asset up-to-date.
		/// </summary>
		public bool IsUpToDate {
			get {
				if ( !File.Exists( FullTargetPath ) ) {
					return false;
				}

				var targetTime	=	File.GetLastWriteTime( FullTargetPath );
				var buildArgs	=	Processor.GenerateParametersHash();

				using ( var assetStream = AssetStream.OpenRead( FullTargetPath ) ) {
					
					if (assetStream.BuildParameters!=buildArgs) {
						return false;
					}

					foreach ( var dependency in assetStream.Dependencies ) {
						if ( context.ContentFileExists(dependency) ) {
							var fullDependencyPath = context.ResolveContentPath(dependency);

							var sourceTime	=	File.GetLastWriteTime(fullDependencyPath);

							if (targetTime < sourceTime) {
								return false;
							}
						}
					}
				}

				return true;
			}
		}



		public IEnumerable<string> GetAllDependencies()
		{
			var removedDeps = new List<string>();

			using ( var assetStream = AssetStream.OpenRead( FullTargetPath ) ) {
				return assetStream.Dependencies;
			}
		}



		/// <summary>
		/// Return list of key path to changed content file.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetRemovedDependencies ()
		{
			var removedDeps = new List<string>();

			if (!TargetFileExists) {
				throw new InvalidOperationException("Target file does not exist");
			}

			using ( var assetStream = AssetStream.OpenRead( FullTargetPath ) ) {
					
				foreach ( var dependency in assetStream.Dependencies ) {

					if ( !context.ContentFileExists(dependency) ) {
						removedDeps.Add( dependency );
					}
				}
			}

			return removedDeps;
		}



		/// <summary>
		/// Return list of key path to changed content file.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetNewDependencies ( IEnumerable<string> currentDependencies )
		{
			if (!TargetFileExists) {
				throw new InvalidOperationException("Target file does not exist");
			}

			using ( var assetStream = AssetStream.OpenRead( FullTargetPath ) ) {
				
				var hashSet = new HashSet<string>(assetStream.Dependencies);

				return currentDependencies.Where( dep => !hashSet.Contains(dep) );
			}
		}



		/// <summary>
		/// Return list of key path to changed content file.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetChangedDependencies ()
		{
			var changedDeps = new List<string>();

			if (!TargetFileExists) {
				throw new InvalidOperationException("Target file does not exist");
			}

			var targetTime	=	File.GetLastWriteTime( FullTargetPath );

			using ( var assetStream = AssetStream.OpenRead( FullTargetPath ) ) {
					
				foreach ( var dependency in assetStream.Dependencies ) {

					if ( context.ContentFileExists(dependency) ) {

						var fullDependencyPath = context.ResolveContentPath(dependency);

						var sourceTime	=	File.GetLastWriteTime(fullDependencyPath);

						if (targetTime < sourceTime) {
							changedDeps.Add( dependency );
						}
					}
				}
			}

			return changedDeps;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyPath">Key path</param>
		/// <param name="baseDir">Base directory</param>
		/// <param name="buildParameters"></param>
		/// <param name="context"></param>
		public AssetSource ( string keyPath, string baseDir, string fullOutputDir, AssetProcessor processor, IBuildContext context )
		{
			this.processor	=	processor;
			this.outputDir	=	fullOutputDir;
			this.fullPath	=	Path.Combine( baseDir, keyPath );
			this.baseDir	=	baseDir;
			this.keyPath	=	keyPath;
			this.context	=	context;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[Obsolete]
		public AssetProcessor CreateProcessor()
		{
			return Processor;
		} 



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dependencies"></param>
		/// <returns></returns>
		public Stream OpenTargetStream ( IEnumerable<string> dependencies, Type targetType )
		{
			var dir = Path.GetDirectoryName( FullTargetPath );

			Directory.CreateDirectory( dir );

			var paramHash	=	processor.GenerateParametersHash();
			return AssetStream.OpenWrite( FullTargetPath, paramHash, dependencies.Concat( new[]{ KeyPath } ).Distinct().ToArray(), targetType );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ZipArchive OpenAssetArchive ()
		{
			return ZipFile.Open( Path.ChangeExtension(FullTargetPath, ".zip"), ZipArchiveMode.Update );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="dependencies"></param>
		/// <returns></returns>
		public Stream OpenTargetStream ( Type type )
		{
			return OpenTargetStream( new string[0], type );
		}


		/// <summary>
		/// Opens source stream file
		/// </summary>
		/// <returns></returns>
		public Stream OpenSourceStream ()
		{	
			return File.OpenRead( FullSourcePath );
		}
	}
}
