using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
    [Serializable]
    [PXCacheName(Messages.POAccrualDetail)]
    public class POAccrualDetail: IBqlTable
    {
		#region Keys
		public class PK : PrimaryKeyOf<POAccrualDetail>.By<documentNoteID, lineNbr>
		{
			public static POAccrualDetail Find(PXGraph graph, Guid? documentNoteID, int? lineNbr) => FindBy(graph, documentNoteID, lineNbr);
		}
		public static class FK
		{
			public class AccrualStatus : POAccrualStatus.PK.ForeignKeyOf<POAccrualDetail>.By<pOAccrualRefNoteID, pOAccrualLineNbr, pOAccrualType> { }
			
			public class APInvoice : AP.APInvoice.PK.ForeignKeyOf<POAccrualDetail>.By<aPDocType, aPRefNbr> { }
			public class APTran : AP.APTran.PK.ForeignKeyOf<POAccrualDetail>.By<aPDocType, aPRefNbr, lineNbr> { }
			
			public class Receipt : POReceipt.PK.ForeignKeyOf<POAccrualDetail>.By<pOReceiptType, pOReceiptNbr> { }
			public class ReceiptLine : POReceiptLine.PK.ForeignKeyOf<POAccrualDetail>.By<pOReceiptType, pOReceiptNbr, lineNbr> { }

			public class Branch : GL.Branch.PK.ForeignKeyOf<POReceiptLine>.By<branchID> { }

			//public class PPVAdjustment : INRegister.PK.ForeignKeyOf<POAccrualDetail>.By<INDocType.adjustment, pPVAdjRefNbr> { }
			//public class TaxAdjustment : INRegister.PK.ForeignKeyOf<POAccrualDetail>.By<INDocType.adjustment, taxAdjRefNbr> { }
		}
		#endregion

		#region DocumentNoteID
		[PXDBGuid(IsKey = true)]
		[PXDefault]
		public virtual Guid? DocumentNoteID { get; set; }
		public abstract class documentNoteID : BqlGuid.Field<documentNoteID> { }
		#endregion
		#region LineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		public virtual int? LineNbr { get; set; }
		public abstract class lineNbr : BqlInt.Field<lineNbr> { }
		#endregion

		#region POAccrualRefNoteID
		[PXDBGuid]
		[PXDefault]
		public virtual Guid? POAccrualRefNoteID { get; set; }
		public abstract class pOAccrualRefNoteID : BqlGuid.Field<pOAccrualRefNoteID> { }
		#endregion
		#region POAccrualLineNbr
		[PXDBInt]
		[PXDefault]
		public virtual int? POAccrualLineNbr { get; set; }
		public abstract class pOAccrualLineNbr : BqlInt.Field<pOAccrualLineNbr> { }
		#endregion
		#region POAccrualType
		[PXDBString(1, IsFixed = true)]
		[PXDefault]
		[POAccrualType.List]
		public virtual string POAccrualType { get; set; }
		public abstract class pOAccrualType : BqlString.Field<pOAccrualType> { }
		#endregion

		#region APDocType
		[APDocType.List]
		[PXDBString(3, IsFixed = true)]
		public virtual string APDocType { get; set; }
		public abstract class aPDocType : BqlString.Field<aPDocType> { }
		#endregion
		#region APRefNbr
		[PXDBString(15, IsUnicode = true)]
		public virtual string APRefNbr { get; set; }
		public abstract class aPRefNbr : BqlString.Field<aPRefNbr> { }
		#endregion

		#region POReceiptType
		[PXDBString(2, IsFixed = true)]
		public virtual string POReceiptType { get; set; }
		public abstract class pOReceiptType : BqlString.Field<pOReceiptType> { }
		#endregion
		#region POReceiptNbr
		[PXDBString(15, IsUnicode = true)]
		public virtual string POReceiptNbr { get; set; }
		public abstract class pOReceiptNbr : BqlString.Field<pOReceiptNbr> { }
		#endregion

		#region VendorID
		[Vendor]
		[PXDefault]
		public virtual int? VendorID { get; set; }
		public abstract class vendorID : BqlInt.Field<vendorID> { }
		#endregion
		#region Posted
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? Posted { get; set; }
		public abstract class posted : BqlBool.Field<posted> { }
		#endregion
		#region IsDropShip
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? IsDropShip { get; set; }
		public abstract class isDropShip : BqlBool.Field<isDropShip> { }
		#endregion
		#region IsReversed
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? IsReversed { get; set; }
		public abstract class isReversed : BqlBool.Field<isReversed> { }
		#endregion
		#region IsReversing
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? IsReversing { get; set; }
		public abstract class isReversing : BqlBool.Field<isReversing> { }
		#endregion
		#region BranchID
		[Branch]
		public virtual int? BranchID { get; set; }
		public abstract class branchID : BqlInt.Field<branchID> { }
		#endregion
		#region DocDate
		[PXDBDate]
		[PXDefault]
		public virtual DateTime? DocDate { get; set; }
		public abstract class docDate : BqlDateTime.Field<docDate> { }
		#endregion
		#region FinPeriodID
		// Acuminator disable once PX1030 PXDefaultIncorrectUse [FinPeriodIDAttribute appends PXDBStringAttribute]
		[FinPeriodID]
		[PXDefault]
		public virtual string FinPeriodID { get; set; }
		public abstract class finPeriodID : BqlString.Field<finPeriodID> { }
		#endregion
		#region TranDesc
		[PXDBString(256, IsUnicode = true)]
		public virtual string TranDesc { get; set; }
		public abstract class tranDesc : BqlString.Field<tranDesc> { }
		#endregion
		#region UOM
		[PXDBString(6, IsUnicode = true, InputMask = ">aaaaaa")]
		[PXDefault]
		public virtual string UOM { get; set; }
		public abstract class uOM : BqlString.Field<uOM> { }
		#endregion

		#region AccruedQty
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? AccruedQty { get; set; }
		public abstract class accruedQty : BqlDecimal.Field<accruedQty> { }
		#endregion
		#region BaseAccruedQty
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? BaseAccruedQty { get; set; }
		public abstract class baseAccruedQty : BqlDecimal.Field<baseAccruedQty> { }
		#endregion
		#region AccruedCost
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? AccruedCost { get; set; }
		public abstract class accruedCost : BqlDecimal.Field<accruedCost> { }
		#endregion
		#region PPVAmt
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? PPVAmt { get; set; }
		public abstract class pPVAmt : BqlDecimal.Field<pPVAmt> { }
		#endregion
		#region TaxAccruedCost
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? TaxAccruedCost { get; set; }
		public abstract class taxAccruedCost : BqlDecimal.Field<taxAccruedCost> { }
		#endregion

		#region TaxAdjAmt
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? TaxAdjAmt { get; set; }
		public abstract class taxAdjAmt : BqlDecimal.Field<taxAdjAmt> { }
		#endregion

		#region AccruedCostTotal
		[PXDecimal(4)]
		[PXDBCalced(typeof(accruedCost.Add<pPVAmt>.Add<taxAccruedCost>), typeof(decimal))]
		public virtual decimal? AccruedCostTotal { get; set; }
		public abstract class accruedCostTotal : BqlDecimal.Field<accruedCostTotal> { }
		#endregion

		#region PPVAdjRefNbr
		[PXDBString(15, IsUnicode = true)]
		public virtual string PPVAdjRefNbr { get; set; }
		public abstract class pPVAdjRefNbr : BqlString.Field<pPVAdjRefNbr> { }
		#endregion
		#region PPVAdjPosted
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? PPVAdjPosted { get; set; }
		public abstract class pPVAdjPosted : BqlBool.Field<pPVAdjPosted> { }
		#endregion

		#region TaxAdjRefNbr
		[PXDBString(15, IsUnicode = true)]
		public virtual string TaxAdjRefNbr { get; set; }
		public abstract class taxAdjRefNbr : BqlString.Field<taxAdjRefNbr> { }
		#endregion
		#region TaxAdjPosted
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? TaxAdjPosted { get; set; }
		public abstract class taxAdjPosted : BqlBool.Field<taxAdjPosted> { }
		#endregion

		#region CreatedDateTime
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region CreatedByID
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region tstamp
		[PXDBTimestamp(RecordComesFirst = true)]
		public virtual byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion
	}

	[POAccrualDetailPostedUpdate.Accumulator(BqlTable = typeof(POAccrualDetail))]
	[PXHidden]
	[Serializable]
	public partial class POAccrualDetailPostedUpdate : IBqlTable
	{
		#region POReceiptType
		[PXDBString(2, IsFixed = true, IsKey = true)]
		[PXDefault]
		public virtual string POReceiptType { get; set; }
		public abstract class pOReceiptType : BqlString.Field<pOReceiptType> { }
		#endregion
		#region POReceiptNbr
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDefault]
		public virtual string POReceiptNbr { get; set; }
		public abstract class pOReceiptNbr : BqlString.Field<pOReceiptNbr> { }
		#endregion
		#region LineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		public virtual int? LineNbr { get; set; }
		public abstract class lineNbr : BqlInt.Field<lineNbr> { }
		#endregion

		#region Posted
		[PXDBBool]
		public virtual bool? Posted { get; set; }
		public abstract class posted : BqlBool.Field<posted> { }
		#endregion
		#region FinPeriodID
		[FinPeriodID]
		public virtual string FinPeriodID { get; set; }
		public abstract class finPeriodID : BqlString.Field<finPeriodID> { }
		#endregion
		#region AccruedCost
		[PXDBDecimal(4)]
		public virtual decimal? AccruedCost { get; set; }
		public abstract class accruedCost : BqlDecimal.Field<accruedCost> { }
		#endregion
		#region PreviousCost
		[PXDecimal(4)]
		public virtual decimal? PreviousCost { get; set; }
		public abstract class previousCost : BqlDecimal.Field<previousCost> { }
		#endregion

		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region tstamp
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
		#endregion

		public class AccumulatorAttribute : PXAccumulatorAttribute
		{
			protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
			{
				if (!base.PrepareInsert(sender, row, columns))
					return false;

				var detail = (POAccrualDetailPostedUpdate)row;

				columns.UpdateOnly = true;

				if(detail.Posted != null)
					columns.Update<posted>(detail.Posted, PXDataFieldAssign.AssignBehavior.Replace);

				if (detail.FinPeriodID != null)
					columns.Update<finPeriodID>(detail.FinPeriodID, PXDataFieldAssign.AssignBehavior.Replace);

				if(detail.AccruedCost != null)
					columns.Update<accruedCost>(detail.AccruedCost, PXDataFieldAssign.AssignBehavior.Replace);

				columns.Update<lastModifiedByID>(detail.LastModifiedByID, PXDataFieldAssign.AssignBehavior.Replace);
				columns.Update<lastModifiedDateTime>(detail.LastModifiedDateTime, PXDataFieldAssign.AssignBehavior.Replace);
				columns.Update<lastModifiedByScreenID>(detail.LastModifiedByScreenID, PXDataFieldAssign.AssignBehavior.Replace);

				return true;
			}
		}
	}

	[POAccrualDetailPPVAdjPostedUpdate.Accumulator(BqlTable = typeof(POAccrualDetail))]
	[PXHidden]
	[Serializable]
	public partial class POAccrualDetailPPVAdjPostedUpdate : IBqlTable
	{
		#region POAccrualRefNoteID
		[PXDBGuid(IsKey = true)]
		[PXDefault]
		public virtual Guid? POAccrualRefNoteID { get; set; }
		public abstract class pOAccrualRefNoteID : BqlGuid.Field<pOAccrualRefNoteID> { }
		#endregion
		#region POAccrualLineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		public virtual int? POAccrualLineNbr { get; set; }
		public abstract class pOAccrualLineNbr : BqlInt.Field<pOAccrualLineNbr> { }
		#endregion
		#region POAccrualType
		[PXDBString(1, IsFixed = true, IsKey = true)]
		[PXDefault]
		[POAccrualType.List]
		public virtual string POAccrualType { get; set; }
		public abstract class pOAccrualType : BqlString.Field<pOAccrualType> { }
		#endregion

		#region PPVAdjRefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		public virtual string PPVAdjRefNbr { get; set; }
		public abstract class pPVAdjRefNbr : BqlString.Field<pPVAdjRefNbr> { }
		#endregion

		#region PPVAdjPosted
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? PPVAdjPosted { get; set; }
		public abstract class pPVAdjPosted : BqlBool.Field<pPVAdjPosted> { }
		#endregion

		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region tstamp
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
		#endregion

		public class AccumulatorAttribute : PXAccumulatorAttribute
		{
			protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
			{
				if (!base.PrepareInsert(sender, row, columns))
					return false;

				var detail = (POAccrualDetailPPVAdjPostedUpdate)row;

				columns.UpdateOnly = true;
				columns.Update<pPVAdjPosted>(detail.PPVAdjPosted, PXDataFieldAssign.AssignBehavior.Replace);

				columns.Update<lastModifiedByID>(detail.LastModifiedByID, PXDataFieldAssign.AssignBehavior.Replace);
				columns.Update<lastModifiedDateTime>(detail.LastModifiedDateTime, PXDataFieldAssign.AssignBehavior.Replace);
				columns.Update<lastModifiedByScreenID>(detail.LastModifiedByScreenID, PXDataFieldAssign.AssignBehavior.Replace);

				return true;
			}
		}
	}

	[POAccrualDetailTaxAdjPostedUpdate.Accumulator(BqlTable = typeof(POAccrualDetail))]
	[PXHidden]
	[Serializable]
	public partial class POAccrualDetailTaxAdjPostedUpdate : IBqlTable
	{
		#region POAccrualRefNoteID
		[PXDBGuid(IsKey = true)]
		[PXDefault]
		public virtual Guid? POAccrualRefNoteID { get; set; }
		public abstract class pOAccrualRefNoteID : BqlGuid.Field<pOAccrualRefNoteID> { }
		#endregion
		#region POAccrualLineNbr
		[PXDBInt(IsKey = true)]
		[PXDefault]
		public virtual int? POAccrualLineNbr { get; set; }
		public abstract class pOAccrualLineNbr : BqlInt.Field<pOAccrualLineNbr> { }
		#endregion
		#region POAccrualType
		[PXDBString(1, IsFixed = true, IsKey = true)]
		[PXDefault]
		[POAccrualType.List]
		public virtual string POAccrualType { get; set; }
		public abstract class pOAccrualType : BqlString.Field<pOAccrualType> { }
		#endregion

		#region TaxAdjRefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		public virtual string TaxAdjRefNbr { get; set; }
		public abstract class taxAdjRefNbr : BqlString.Field<taxAdjRefNbr> { }
		#endregion

		#region TaxAdjPosted
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? TaxAdjPosted { get; set; }
		public abstract class taxAdjPosted : BqlBool.Field<taxAdjPosted> { }
		#endregion

		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region tstamp
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
		#endregion

		public class AccumulatorAttribute : PXAccumulatorAttribute
		{
			protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
			{
				if (!base.PrepareInsert(sender, row, columns))
					return false;

				var detail = (POAccrualDetailTaxAdjPostedUpdate)row;

				columns.UpdateOnly = true;
				columns.Update<taxAdjPosted>(detail.TaxAdjPosted, PXDataFieldAssign.AssignBehavior.Replace);

				columns.Update<lastModifiedByID>(detail.LastModifiedByID, PXDataFieldAssign.AssignBehavior.Replace);
				columns.Update<lastModifiedDateTime>(detail.LastModifiedDateTime, PXDataFieldAssign.AssignBehavior.Replace);
				columns.Update<lastModifiedByScreenID>(detail.LastModifiedByScreenID, PXDataFieldAssign.AssignBehavior.Replace);

				return true;
			}
		}
	}
}
