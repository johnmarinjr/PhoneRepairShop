using PX.Data;
using System;

namespace PX.Objects.PM.TaxZoneExtension
{
	public abstract class ProjectRevenueTaxZoneExtension<T> : PXGraphExtension<T>
		where T : PXGraph
	{
		#region Mappings

		protected abstract DocumentMapping GetDocumentMapping();

		public PXSelectExtension<Document> Document;

		#endregion

		protected virtual void _(Events.FieldUpdated<Document, Document.projectID> e)
		{
			SetDefaultShipToAddress(e.Cache, e.Row);
		}

		protected abstract void SetDefaultShipToAddress(PXCache sender, Document row);
	}

	public class Document : PXMappedCacheExtension
	{
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		public virtual int? ProjectID
		{
			get;
			set;
		}
		#endregion
	}

	public class DocumentMapping : IBqlMapping
	{
		public Type Extension => typeof(Document);

		protected Type _table;
		public Type Table => _table;
		public DocumentMapping(Type table)
		{
			_table = table;
		}

		public Type ProjectID = typeof(Document.projectID);
	}
}
