using System;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.PM;

namespace PX.Objects.FS
{
	[Serializable]
	public partial class FSContractForecastDet : IBqlTable
	{
		#region ServiceContractID
		public abstract class serviceContractID : PX.Data.BQL.BqlInt.Field<serviceContractID> { }

		[PXDBInt(IsKey = true)]
		[PXDefault(typeof(FSContractForecast.serviceContractID))]
		public virtual int? ServiceContractID { get; set; }
		#endregion
		#region ForecastID
		public abstract class forecastID : PX.Data.BQL.BqlGuid.Field<forecastID> { }

		[PXDBGuid(IsKey = true)]
		[PXParent(typeof(Select<FSContractForecast,
							Where<FSContractForecast.forecastID, Equal<Current<FSContractForecastDet.forecastID>>,
							  And<FSContractForecast.serviceContractID, Equal<Current<FSContractForecastDet.serviceContractID>>>>>))]
		[PXDBDefault(typeof(FSContractForecast.forecastID))]
		public virtual Guid? ForecastID { get; set; }
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(FSContractForecast.lineCntr))]
		public virtual int? LineNbr { get; set; }
		#endregion
		#region ForecastDetType
		public abstract class forecastDetType : PX.Data.BQL.BqlString.Field<forecastDetType>
		{
			public abstract class Values : ListField_ForecastDet_Type { }
		}

		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Forecast Type")]
		public virtual string ForecastDetType { get; set; }
		#endregion
		#region LineType
		public abstract class lineType : PX.Data.BQL.BqlString.Field<lineType>
		{
		}

		[PXDBString(5, IsFixed = true)]
		[PXUIField(DisplayName = "Line Type")]
		[FSLineType.List]
		[PXDefault(ID.LineType_ALL.SERVICE)]
		public virtual string LineType { get; set; }
		#endregion
		#region ScheduleID
		public abstract class scheduleID : PX.Data.BQL.BqlInt.Field<scheduleID> { }

		[PXDBInt]
		public virtual int? ScheduleID { get; set; }
		#endregion
		#region ScheduleDetID
		public abstract class scheduleDetID : PX.Data.BQL.BqlInt.Field<scheduleDetID> { }

		[PXDBInt]
		public virtual int? ScheduleDetID { get; set; }
		#endregion
		#region ContractPeriodID
		public abstract class contractPeriodID : PX.Data.BQL.BqlInt.Field<contractPeriodID> { }

		[PXDBInt]
		public virtual int? ContractPeriodID { get; set; }
		#endregion
		#region ContractPeriodDetID
		public abstract class contractPeriodDetID : PX.Data.BQL.BqlInt.Field<contractPeriodDetID> { }

		[PXDBInt]
		public virtual int? ContractPeriodDetID { get; set; }
		#endregion
		#region BillingRule
		public abstract class billingRule : ListField_BillingRule
		{
		}

		[PXDBString(4, IsFixed = true)]
		[billingRule.List]
		[PXDefault(ID.BillingRule.NONE, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Billing Rule", Enabled = false)]
		public virtual string BillingRule { get; set; }
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<lineType>))]
		[InventoryIDByLineType(typeof(lineType), Filterable = true)]
		public virtual int? InventoryID { get; set; }
		#endregion
		#region ComponentID
		public abstract class componentID : PX.Data.BQL.BqlInt.Field<componentID> { }

		[PXDBInt]
		public virtual int? ComponentID { get; set; }
		#endregion
		#region Occurrences
		public abstract class occurrences : PX.Data.BQL.BqlInt.Field<occurrences> { }

		[PXDBInt]
		[PXDefault(TypeCode.Int32, "0")]
		public virtual int? Occurrences { get; set; }
		#endregion
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		[INUnit(typeof(inventoryID), DisplayName = "UOM")]
		[PXDefault(typeof(Search<InventoryItem.salesUnit, Where<InventoryItem.inventoryID, Equal<Current<inventoryID>>>>))]
		[PXFormula(typeof(Default<inventoryID>))]
		public virtual string UOM { get; set; }
		#endregion
		#region UnitPrice
		public abstract class unitPrice : PX.Data.BQL.BqlDecimal.Field<unitPrice> { }

		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Unit Price")]
		public virtual decimal? UnitPrice { get; set; }
		#endregion
		#region SMEquipmentID
		public abstract class SMequipmentID : PX.Data.BQL.BqlInt.Field<SMequipmentID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Target Equipment ID", FieldClass = FSSetup.EquipmentManagementFieldClass)]
		public virtual int? SMEquipmentID { get; set; }
		#endregion
		#region EquipmentAction
		public abstract class equipmentAction : ListField_EquipmentAction
		{
		}

		[PXDBString(2, IsFixed = true)]
		[equipmentAction.ListAtrribute]
		[PXDefault(ID.Equipment_Action.NONE)]
		[PXUIField(DisplayName = "Equipment Action", FieldClass = FSSetup.EquipmentManagementFieldClass)]
		public virtual string EquipmentAction { get; set; }
		#endregion
		#region EquipmentLineRef
		public abstract class equipmentLineRef : PX.Data.BQL.BqlInt.Field<equipmentLineRef> { }

		[PXDBInt]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Component Line Ref.", FieldClass = FSSetup.EquipmentManagementFieldClass)]
		[FSSelectorEquipmentLineRefServiceOrderAppointment(typeof(inventoryID), typeof(SMequipmentID), typeof(componentID), typeof(equipmentAction))]
		public virtual int? EquipmentLineRef { get; set; }
		#endregion
		#region ExtPrice
		public abstract class extPrice : PX.Data.BQL.BqlDecimal.Field<extPrice> { }

		[PXDBDecimal]
		[PXUIField(DisplayName = "Ext. Price")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? ExtPrice { get; set; }
		#endregion
		#region OveragePrice
		public abstract class overagePrice : PX.Data.BQL.BqlDecimal.Field<overagePrice> { }

		[PXDBDecimal]
		[PXUIField(DisplayName = "Overage Price")]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? OveragePrice { get; set; }
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

		[PXDefault(typeof(FSServiceContract.projectID), PersistingCheck = PXPersistingCheck.Nothing)]
		[ProjectBase(typeof(FSServiceContract.billCustomerID), Visible = false)]
		public virtual int? ProjectID { get; set; }
		#endregion
		#region ProjectTaskID
		public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Project Task", FieldClass = ProjectAttribute.DimensionName)]
		[FSSelectorActive_AR_SO_ProjectTask(typeof(Where<PMTask.projectID, Equal<Current<projectID>>>))]
		public virtual int? ProjectTaskID { get; set; }
		#endregion
		#region Qty
		public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }

		[PXDBQuantity(MinValue = 0.00)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity", Visible = false, Enabled = false)]
		public virtual Decimal? Qty { get; set; }
		#endregion
		#region TotalPrice
		public abstract class totalPrice : PX.Data.BQL.BqlDecimal.Field<totalPrice> { }

		[PXDBDecimal]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total Price per Duration")]
		[PXFormula(typeof(Mult<Mult<qty, unitPrice>, occurrences>))]
		public virtual decimal? TotalPrice { get; set; }
		#endregion
		#region TranDesc
		public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }

		[PXDBString(Common.Constants.TranDescLength, IsUnicode = true)]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Description")]
		public virtual string TranDesc { get; set; }
		#endregion
		#region RecurrenceDesc
		public abstract class recurrenceDesc : PX.Data.BQL.BqlString.Field<recurrenceDesc> { }
		[PXDBString(int.MaxValue, IsUnicode = true)]
		[PXUIField(DisplayName = "Recurrence Description")]
		public virtual string RecurrenceDesc { get; set; }
		#endregion
		#region TimeDuration
		public new abstract class timeDuration : PX.Data.BQL.BqlInt.Field<timeDuration> { }

		[FSDBTimeSpanLong]
		[PXUIField(DisplayName = "Duration")]
		public virtual int? TimeDuration { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID]
		[PXUIField(DisplayName = "CreatedByID")]
		public virtual Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID]
		[PXUIField(DisplayName = "CreatedByScreenID")]
		public virtual string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime]
		[PXUIField(DisplayName = "CreatedDateTime")]
		public virtual DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID]
		[PXUIField(DisplayName = "LastModifiedByID")]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID]
		[PXUIField(DisplayName = "LastModifiedByScreenID")]
		public virtual string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime]
		[PXUIField(DisplayName = "LastModifiedDateTime")]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
		#endregion
	}
}
