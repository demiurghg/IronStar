using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Fusion.Core.Extensions {
	public class XmlImporter {

		XmlSerializer serializer;
		XmlDeserializationEvents events;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <param name="extraTypes"></param>
		public XmlImporter( Type type, Type[] extraTypes )
		{
			serializer	=	new XmlSerializer(type, extraTypes ?? new Type[0]);

			serializer.UnknownAttribute		+=	Serializer_UnknownAttribute;
			serializer.UnknownElement		+=	Serializer_UnknownElement;
			serializer.UnknownNode			+=	Serializer_UnknownNode;
			serializer.UnreferencedObject	+=	Serializer_UnreferencedObject;
		}


		private void Serializer_UnreferencedObject( object sender, UnreferencedObjectEventArgs e )
		{
			Log.Warning("XML: unreferenced object '{0}'", e.UnreferencedId);
		}

		private void Serializer_UnknownNode( object sender, XmlNodeEventArgs e )
		{
			Log.Warning("XML: unknown node: {0} at [{1}, {2}]", e.LocalName, e.LineNumber, e.LinePosition);
		}

		private void Serializer_UnknownElement( object sender, XmlElementEventArgs e )
		{
			Log.Warning("XML: unknown element: {0} at [{1}, {2}]", e.Element.Name, e.LineNumber, e.LinePosition);
		}

		private void Serializer_UnknownAttribute( object sender, XmlAttributeEventArgs e )
		{
			Log.Warning("XML: unknown attribute: {0} at [{1}, {2}]", e.Attr.Name, e.LineNumber, e.LinePosition);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		public object Import ( Stream stream )
		{
			using ( TextReader textReader = new StreamReader( stream ) ) {
				object obj = serializer.Deserialize( textReader );
				return obj;
			}
		}
	}
}
