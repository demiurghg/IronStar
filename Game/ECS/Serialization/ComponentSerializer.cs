using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Fusion.Core.Extensions;

namespace IronStar.ECS.Serialization
{
	public class ComponentSerializer
	{
		readonly Type componentType;

		enum FieldFlag
		{
			DoNotSave,
			Data,
			DataArray,
			Entity,
			String,
		}

		struct FieldData
		{
			public FieldData( FieldInfo fi ) 
			{ 
				Field	=	fi; 

				if (fi.HasAttribute<DoNotSaveAttribute>())
				{		
					Flag = FieldFlag.DoNotSave;
				}
				else if (fi.FieldType==typeof(Entity)) 
				{
					Flag = FieldFlag.Entity;
				}
				else if (fi.FieldType==typeof(string)) 
				{
					Flag = FieldFlag.String;
				}
				else if (fi.FieldType.IsValueType)
				{
					Flag = FieldFlag.Data;
				}
				else if (fi.FieldType.IsArray && fi.FieldType.GetElementType().IsValueType)
				{	
					Flag = FieldFlag.DataArray;
				}
				else
				{
					throw new ArgumentException(string.Format("Component field {0} is not serializable", fi));
				}
			}
			public readonly FieldInfo	Field;
			public readonly FieldFlag	Flag;
		}

		readonly FieldData[]	fields;

		public ComponentSerializer( Type componentType )
		{
			this.componentType	=	componentType;
			this.fields			=	componentType
									.GetFields()
									.Select( fi => new FieldData(fi) )
									.ToArray();
		}


		public virtual void Save( IGameState gs, IComponent component, BinaryWriter writer )
		{
			CheckComponent(component);

			for (int i=0; i<fields.Length; i++)
			{
				var field = fields[i];
				switch (field.Flag)
				{
					case FieldFlag.Entity:		WriteEntity		( gs, writer, field.Field, field.Field.GetValue(component) );	continue;
					case FieldFlag.String:		WriteString		( gs, writer, field.Field, field.Field.GetValue(component) );	continue;
					case FieldFlag.Data:		WriteData		( gs, writer, field.Field, field.Field.GetValue(component) );	continue;
					case FieldFlag.DataArray:	WriteDataArray	( gs, writer, field.Field, field.Field.GetValue(component) );	continue;
					case FieldFlag.DoNotSave:	continue;
					default: continue;
				}
			}
		}

		public virtual void Load( IGameState gs, IComponent component, BinaryReader reader )
		{
			CheckComponent(component);

		}


		void WriteEntity( IGameState gs, BinaryWriter writer, FieldInfo fi, object value )
		{
			var entity = (Entity)value;
			writer.Write( entity.ID );
		}


		void WriteString( IGameState gs, BinaryWriter writer, FieldInfo fi, object value )
		{
			var str = (string)value;
			writer.Write( str );
		}


		void WriteData( IGameState gs, BinaryWriter writer, FieldInfo fi, object value )
		{
			var type = fi.FieldType.IsEnum ? Enum.GetUnderlyingType(fi.FieldType) : fi.FieldType;
			writer.WriteArray( value, type, 1 );
		}


		void WriteDataArray( IGameState gs, BinaryWriter writer, FieldInfo fi, object value )
		{
			var array = (Array)value;
			writer.Write( array.Length );
			writer.WriteArray( value, fi.FieldType.GetElementType(), array.Length );
		}


		void CheckComponent( IComponent component )
		{
			if (component==null) new ArgumentNullException("component");
			if (componentType!=component.GetType()) 
			{
				throw new ArgumentException(string.Format("Bad component type: expected {0}, got {1}", componentType, component.GetType()));
			}
		}
	}
}
