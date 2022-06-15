using System;
using PX.Commerce.Core;
using PX.Data;
using PX.Objects.IN;

namespace PX.Commerce.Objects.Availability
{
	[PXHidden] //TODO, Remove after merge
	public class InventoryItem : IBqlTable
	{
		#region InventoryID
		[PXDBInt(IsKey = true)]
        [PXUIField(DisplayName = "Inventory ID")]
		public virtual int? InventoryID { get; set; }
        public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		#endregion
		#region InventoryCD
		[PXDBString]
		[PXUIField(DisplayName = "Inventory CD")]
		public virtual string InventoryCD { get; set; }
		public abstract class inventoryCD : PX.Data.BQL.BqlString.Field<inventoryCD> { }
		#endregion
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		[PXDBGuid()]
		[PXUIField(DisplayName = "NoteID")]
		public virtual Guid? NoteID
		{
			get; set;
		}
		#endregion		
		#region TemplateItemID
		public abstract class templateItemID : PX.Data.BQL.BqlInt.Field<templateItemID> { }
		[PXDBInt()]
		[PXUIField(DisplayName = "Template Item ID")]
		public virtual int? TemplateItemID
		{
			get;
			set;
		}
		#endregion

		#region Availability
		[PXDBString(1, IsUnicode = false)]
		[PXUIField(DisplayName = "Availability")]
		[BCItemAvailabilities.ListDef]
		public virtual string Availability { get; set; }
		public abstract class availability : PX.Data.BQL.BqlString.Field<availability> { }
		#endregion
		#region ExportToExternal
		[PXDBBool()]
		[PXUIField(DisplayName = "Export to External System")]
		public virtual bool? ExportToExternal { get; set; }
		public abstract class exportToExternal : PX.Data.BQL.BqlBool.Field<exportToExternal> { }
		#endregion
	}

	[PXHidden]
	public class ChildInventoryItem : PX.Objects.IN.InventoryItem
	{
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		public new abstract class isTemplate : PX.Data.BQL.BqlBool.Field<isTemplate> { }
		public new abstract class templateItemID : PX.Data.BQL.BqlInt.Field<templateItemID> { }
		public new abstract class stkItem : PX.Data.BQL.BqlBool.Field<stkItem> { }
		public new abstract class availability : PX.Data.BQL.BqlString.Field<availability> { }
		public new abstract class exportToExternal : PX.Data.BQL.BqlBool.Field<exportToExternal> { }
		public new abstract class inventoryCD : PX.Data.BQL.BqlString.Field<inventoryCD> { }
		public new abstract class itemStatus : PX.Data.BQL.BqlString.Field<itemStatus> { }
		public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		public new abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
	}
}
