using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Fusion.Core;
using Fusion.Core.Mathematics;
using Fusion.Core.Content;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics;


namespace Fusion.Core.Content {
	
	public class ContentManager : DisposableBase {

		class Item {
			public DateTime	LoadTime;
			public object	Object;
		}

		object lockObject =  new object();

		public readonly Game Game;
		Dictionary<string, Item> content;
		List<object> toDispose = new List<object>();
		List<ContentLoader> loaders;
		readonly string contentDirectory;

		DirectoryStorage	vtStorage;


		/// <summary>
		/// Gets virtual texture page storage
		/// </summary>
		public IStorage VTStorage {
			get { return vtStorage; }
		}



		/// <summary>
		/// Overloaded. Initializes a new instance of ContentManager. 
		/// </summary>
		/// <param name="game"></param>
		public ContentManager ( Game game, string contentDirectory = "Content" )
		{
			this.Game = game;

			this.contentDirectory = contentDirectory;

			content	=	new Dictionary<string,Item>();
			loaders	=	ContentLoader.GatherContentLoaders()
						.Select( clt => (ContentLoader)Activator.CreateInstance( clt ) )
						.ToList();

			vtStorage	=	new DirectoryStorage(Path.Combine(contentDirectory, ".vtstorage"));
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose ( bool disposing )
		{
			if (disposing) {
				Unload();
				SafeDispose( ref vtStorage );
			}
			base.Dispose(disposing);
		}



		/// <summary>
		/// Gets path to cpecified content item. 
		/// Returns null if given object was not loaded by this content manager.
		/// </summary>
		/// <param name="contentItem"></param>
		/// <returns></returns>
		public string GetPathTo ( object contentItem )
		{
			return content
				.Where( c1 => c1.Value.Object == contentItem )
				.Select( c2 => c2.Key )
				.FirstOrDefault();
		}



		/// <summary>
		/// Determines whether the specified asset exists.
		/// </summary>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		public bool Exists ( string assetPath )
		{
			if ( string.IsNullOrWhiteSpace(assetPath) ) {
				throw new ArgumentException("Asset path can not be null, empty or whitespace.");
			}

			return File.Exists( GetRealAssetFileName( assetPath ) );
		}



		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		string GetRealAssetFileName ( string assetPath )
		{
			//	special case for megatexture :
			if (assetPath=="*megatexture") {	
				return Path.Combine(contentDirectory, @".vtstorage\.megatexture");
			}

			if ( string.IsNullOrWhiteSpace(assetPath) ) {
				throw new ArgumentException("Asset path can not be null, empty or whitespace.");
			}

			//	take characters until dash '|' :
			assetPath		= new string( assetPath.TakeWhile( ch => ch!='|' ).ToArray() );
			var assetExt	= Path.GetExtension( assetPath );

			//	special case for FMOD banks
			if (assetExt==".strings") {
				assetPath = assetPath + ".asset";
			} else {
				assetPath = Path.ChangeExtension( assetPath, ".asset" );
			}

			return Path.Combine( contentDirectory, assetPath );
		}



		/// <summary>
		/// Return list of asset name in given directory
		/// </summary>
		/// <param name="directory"></param>
		/// <returns></returns>
		public IEnumerable<string> EnumerateAssets ( string directory )
		{
			var dirName = Path.Combine(contentDirectory, directory);

			if (!Directory.Exists(dirName)) 
			{
				Log.Warning("EnumerateAssets: directory {0} does not exist", directory );
				return new string[] {};
			}

			return Directory
				.EnumerateFiles( dirName, "*.asset")
				.Select( path => Path.GetFileNameWithoutExtension(path) )
				.ToArray();
		}


		/// <summary>
		/// Gets asset information by given name
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public AssetInfo PeekAsset( string assetPath )
		{
			if (!Exists(assetPath)) 
			{
				return AssetInfo.NonExisting;
			}

			using ( var stream = OpenStream( assetPath ) ) 
			{
				return new AssetInfo( assetPath, stream.ContentType );
			}
		}


		/// <summary>
		/// Precache asset.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="assetPath"></param>
		/// <returns>Indicate whether precaching was successful</returns>
		public bool Precache<T>( string assetPath )
		{
			if (string.IsNullOrWhiteSpace(assetPath)) {
				return false;
			}

			if (!Exists(assetPath)) {
				return false;
			}

			var asset = Load<T>( assetPath );

			(asset as IPrecachable)?.Precache(this);

			return true;
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		public bool PrecacheSafe<T>( string assetPath )
		{
			try {
				return Precache<T>(assetPath);
			} catch ( Exception e ) {
				Log.Warning("Precache failed : {0}", e.Message);
				return false;
			}
		}



		/// <summary>
		/// Opens a stream for reading the specified asset.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public AssetStream OpenStream ( string assetPath )
		{
			var realName =  GetRealAssetFileName( assetPath );

			try {

				var stream	 =	AssetStream.OpenRead( realName );
				return stream;

			} catch ( IOException ioex ) {
				throw new IOException( string.Format("Could not open file: '{0}'\r\nHashed file name: '({1})'", assetPath, realName ), ioex );
			}
		}



		/// <summary>
		/// Gets loader for given type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		ContentLoader GetLoader( Type type )
		{
			// try get loader that absolutly meets desired type :
			foreach ( var loader in loaders ) {
				if (loader.TargetType==type) {
					return loader;
				}
			}

			//	try to get subclass loader :
			foreach ( var loader in loaders ) {
				if (type.IsSubclassOf( loader.TargetType ) ) {
					return loader;
				}
			}

			throw new ContentException( string.Format("Loader for type {0} not found", type ) );
		}



		/// <summary>
		/// Loads an asset that has been processed by the Content Pipeline.
		/// ContentManager.Unload will dispose all objects loaded by this method.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <returns></returns>
		public T Load<T> ( string assetPath )
		{
			if ( string.IsNullOrWhiteSpace(assetPath) ) 
			{
				throw new ArgumentException("Asset path can not be null, empty or whitespace.");
			}

			var item = new Item();

			//
			//	search for already loaded object, otherwice continue loading in non-blocking mode.
			//
			lock (lockObject) 
			{
				if (TryGetExisting<T>(assetPath, ref item)) {
					return (T)item.Object;
				}
			}


			//
			//	load content :
			//
			Log.Message("Loading : {0}", assetPath );
			using (var stream = OpenStream(assetPath) ) 
			{
				var loader	=	GetLoader( stream.ContentType );

				if (!typeof(T).IsAssignableFrom( stream.ContentType )) 
				{
					//throw new ContentException(string.Format("Requested type {0} is not assignable from {1} (content)", typeof(T), stream.ContentType));
					Log.Error("Requested type {0} is not assignable from {1} (content)", typeof(T), stream.ContentType);
				}

				item = new Item() 
				{
					Object		= loader.Load( this, stream, typeof(T), assetPath, GetAssetStorage(assetPath) ),
					LoadTime	= File.GetLastWriteTime( GetRealAssetFileName( assetPath ) ),
				};
			}


			//
			//	check for content again and add it.
			//
			lock (lockObject) 
			{
				Item anotherItem = new Item();

				if ( TryGetExisting<T>( assetPath, ref anotherItem ) ) 
				{
					(item.Object as IDisposable)?.Dispose();
					return (T)anotherItem.Object;
				}
				else
				{
					//	put object to content- and dispose lists :
					toDispose.Add( item.Object );
					content.Add( assetPath, item );
					return (T)item.Object;
				}
			}
		}



		/// <summary>
		/// Gets existing object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		bool TryGetExisting<T>( string assetPath, ref Item item )
		{
			//	try to find object in dictionary :
			if ( content.TryGetValue( assetPath, out item ) ) {
				
				if (item.Object is T) 
				{
					var time = File.GetLastWriteTime( GetRealAssetFileName( assetPath ) );

					if ( time > item.LoadTime ) 
					{
						//	content file was updates since last load
						//	need to load it again, so remove it for a while
						//	indeed, old object will be kept until Unload() called
						content.Remove(	assetPath );
						return false;
					} 
					else 
					{
						return true;
					}
				} 
				else 
				{
					throw new ContentException( string.Format("'{0}' is not '{1}'", assetPath, typeof(T) ) );
				}
			}

			return false;
		}



		/// <summary>
		/// This function resolves asset path using baseAssetPath and localAssetPath.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="baseAssetPath"></param>
		/// <param name="assetPath"></param>
		/// <returns></returns>
		public T Load<T>( string baseAssetPath, string localAssetPath )
		{
			var assetPath	=	Path.Combine( Path.GetDirectoryName( baseAssetPath ), localAssetPath );
			return Load<T>( assetPath );
		}


		/// <summary>
		/// Safe version of ContentManager.Load. If any exception occurs default object will be returned.
		/// ContentManager.Unload will not dispose default object.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <param name="fallbackObject"></param>
		/// <returns></returns>
		[Obsolete("Use TryLoad instead")]
		public T Load<T>( string path, T defaultObject )
		{
			if ( string.IsNullOrWhiteSpace(path) ) {
				throw new ArgumentException("Asset path can not be null, empty or whitespace.");
			}

			try {
				return Load<T>(path);
			} catch ( Exception e ) {
				Log.Warning("Could not load {0} '{1}' : {2}", typeof(T).Name, path, e.Message);
				return defaultObject;
			}
		}



		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="path"></param>
		/// <param name="defaultObject"></param>
		/// <returns></returns>
		public bool TryLoad<T>( string path, out T obj )
		{
			try {
				obj = Load<T>(path);
				return true;
			} catch ( Exception e ) {
				obj = default(T);
				Log.Warning("Could not load {0} '{1}' : {2}", typeof(T).Name, path, e.Message);
				return false;
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		IStorage GetAssetStorage ( string assetPath )
		{
			var path = Path.Combine(contentDirectory, ContentUtils.GetHashedFileName( assetPath, ".storage" ) );
			if (Directory.Exists(path)) {
				return new DirectoryStorage( path );
			} else {
				return null;
			}
		}


		/// <summary>
		/// Disposes all data that was loaded by this ContentManager.
		/// </summary>
		public void Unload()
		{
			lock (lockObject) {

				if (!toDispose.Any()) {
					return;
				}

				Log.Message("Unloading content");

				foreach ( var item in toDispose ) {
					if ( item is DisposableBase ) {
						if ( (item as DisposableBase).IsDisposed ) {
							Log.Error("Item {0} has been disposed!", item.ToString() );
						}
					}
					if ( item is IDisposable ) {
						((IDisposable)item).Dispose();
					}
				}

				toDispose.Clear();
				content.Clear();
			}
		}
	}
}
