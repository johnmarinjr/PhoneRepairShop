using System;
using System.Collections.Generic;

using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

namespace PX.Objects.IN
{
	[Serializable]
	public abstract class LSSelect // the class is moved from ../Descriptor/Attribute.cs as is
	{
		[Serializable]
		[PXHidden]
		public partial class LotSerOptions : IBqlTable
		{
			#region StartNumVal
			public abstract class startNumVal : PX.Data.BQL.BqlString.Field<startNumVal> { }
			protected string _StartNumVal;
			[PXDBString(30)]
			[PXUIField(DisplayName = "Start Lot/Serial Number", FieldClass = "LotSerial")]
			public virtual string StartNumVal
			{
				get
				{
					return _StartNumVal;
				}
				set
				{
					_StartNumVal = value;
				}
			}
			#endregion
			#region UnassignedQty
			public abstract class unassignedQty : PX.Data.BQL.BqlDecimal.Field<unassignedQty> { }
			protected decimal? _UnassignedQty;
			[PXDBDecimal]
			[PXUIField(DisplayName = "Unassigned Qty.", Enabled = false)]
			public virtual decimal? UnassignedQty
			{
				get
				{
					return _UnassignedQty;
				}
				set
				{
					_UnassignedQty = value;
				}
			}
			#endregion
			#region Qty
			public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }
			protected decimal? _Qty;
			[PXDBDecimal]
			[PXUIField(DisplayName = "Quantity to Generate", FieldClass = "LotSerial")]
			public virtual decimal? Qty
			{
				get
				{
					return _Qty;
				}
				set
				{
					_Qty = value;
				}
			}
			#endregion
			#region AllowGenerate
			public abstract class allowGenerate : PX.Data.BQL.BqlBool.Field<allowGenerate> { }
			protected bool? _AllowGenerate;
			[PXDBBool]
			public virtual bool? AllowGenerate
			{
				get
				{
					return _AllowGenerate;
				}
				set
				{
					_AllowGenerate = value;
				}
			}
			#endregion
			#region IsSerial
			public abstract class isSerial : PX.Data.BQL.BqlBool.Field<isSerial> { }
			protected bool? _IsSerial;
			[PXDBBool]
			public virtual bool? IsSerial
			{
				get
				{
					return _IsSerial;
				}
				set
				{
					_IsSerial = value;
				}
			}
			#endregion
		}

		public class Counters
		{
			public int RecordCount;
			public decimal BaseQty;
			public Dictionary<DateTime?, int> ExpireDates = new Dictionary<DateTime?, int>();
			public int ExpireDatesNull;
			public DateTime? ExpireDate;
			public Dictionary<int?, int> SubItems = new Dictionary<int?, int>();
			public int SubItemsNull;
			public int? SubItem;
			public Dictionary<int?, int> Locations = new Dictionary<int?, int>();
			public int LocationsNull;
			public int? Location;
			public Dictionary<string, int> LotSerNumbers = new Dictionary<string, int>();
			public int LotSerNumbersNull;
			public string LotSerNumber;
			public int UnassignedNumber;
			public Dictionary<KeyValuePair<int?, int?>, int> ProjectTasks = new Dictionary<KeyValuePair<int?, int?>, int>();
			public int ProjectTasksNull;
			public int? ProjectID;
			public int? TaskID;
		}

		public static DateTime? ExpireDateByLot(PXGraph graph, ILSMaster item, ILSMaster master)
		{
			if (master != null && master.ExpireDate != null && master.InvtMult > 0)
				return master.ExpireDate;

			var rec = (PXResult<INSite, InventoryItem, INLotSerClass, INItemRep, S.INItemSite, INItemLotSerial>)
				SelectFrom<INSite>
				.CrossJoin<InventoryItem>
				.LeftJoin<INLotSerClass>.On<
					INLotSerClass.lotSerClassID.IsEqual<InventoryItem.lotSerClassID>>
				.LeftJoin<INItemRep>.On<
					INItemRep.replenishmentClassID.IsEqual<INSite.replenishmentClassID>
					.And<INItemRep.inventoryID.IsEqual<InventoryItem.inventoryID>>
					.And<INItemRep.curyID.IsEqual<INSite.baseCuryID>>>
				.LeftJoin<S.INItemSite>.On<
					S.INItemSite.inventoryID.IsEqual<InventoryItem.inventoryID>
					.And<S.INItemSite.siteID.IsEqual<INSite.siteID>>>
				.LeftJoin<INItemLotSerial>.On<
					INItemLotSerial.inventoryID.IsEqual<InventoryItem.inventoryID>
					.And<INItemLotSerial.lotSerialNbr.IsEqual<@P.AsString>
					.And<INItemLotSerial.expireDate.IsNotNull>>>
				.Where<InventoryItem.inventoryID.IsEqual<@P.AsInt>
					.And<INSite.siteID.IsEqual<@P.AsInt>>>
				.View.SelectWindowed(graph, 0, 1, item.LotSerialNbr, item.InventoryID, item.SiteID);

			if (rec == null)
				return master?.ExpireDate ?? item.ExpireDate;

			DateTime? defaultExpireDate = null;

			if (((INLotSerClass)rec).LotSerClassID == null || ((INLotSerClass)rec).LotSerTrackExpiration == true)
			{
				int? shelfLife = ((S.INItemSite)rec).MaxShelfLife ?? ((INItemRep)rec).MaxShelfLife;

				defaultExpireDate = shelfLife > 0 && item.TranDate != null
				? item.TranDate.Value.AddDays(shelfLife.Value)
					: (DateTime?)null;
			}

			INItemLotSerial status = rec;
			return (status.InventoryID == null ? null : status.ExpireDate)
				?? master?.ExpireDate
				?? item.ExpireDate
				?? defaultExpireDate;
		}
	}

	[Flags, Obsolete]
	public enum AvailabilityFetchMode
	{
		None = 0,
		ExcludeCurrent = 1,
		TryOptimize = 2,
		Project = 4
	}
}
