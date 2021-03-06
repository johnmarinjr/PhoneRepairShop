using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AP;
using PX.Objects.CN.Common.DAC;
using PX.Objects.CN.JointChecks.AP.Descriptor.Attributes;
using PX.Objects.CN.JointChecks.Descriptor;
using PX.Objects.CS;

namespace PX.Objects.CN.JointChecks.AP.DAC
{
	[Serializable]
	[PXCacheName("Joint Payee")]
	public class JointPayee : BaseCache, IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<JointPayee>.By<jointPayeeId>
		{
			public static JointPayee Find(PXGraph graph, int? jointPayeeId) => FindBy(graph, jointPayeeId);
		}
		#endregion

		[PXDBIdentity(IsKey = true)]
		[PXReferentialIntegrityCheck]
		public virtual int? JointPayeeId
		{
			get;
			set;
		}

		[Vendor(DisplayName = "Joint Payee (Vendor)")]
		[JointPayeeRequired]
		public virtual int? JointPayeeInternalId
		{
			get;
			set;
		}

		[PXDBString(30)]
		[JointPayeeRequired]
		[PXUIField(DisplayName = "Joint Payee")]
		public virtual string JointPayeeExternalName
		{
			get;
			set;
		}

		[PXDBDecimal]
		[PXUIField(DisplayName = "Joint Amount Owed")]
		public virtual decimal? JointAmountOwed
		{
			get;
			set;
		}

		[PXDBDecimal]
		[PXUIField(DisplayName = "Joint Amount Paid", IsReadOnly = true)]
		public virtual decimal? JointAmountPaid
		{
			get;
			set;
		}

		[PXDBDecimal]
		[PXFormula(typeof(Sub<jointAmountOwed, jointAmountPaid>))]
		[PXUIField(DisplayName = "Joint Balance", IsReadOnly = true)]
		public virtual decimal? JointBalance
		{
			get;
			set;
		}

		[PXDBGuid]
		[PXSelector(typeof(Search<APInvoice.noteID, Where<APInvoice.docType, Equal<APDocType.invoice>>>),
			SubstituteKey = typeof(APInvoice.refNbr))]
		[PXUIField(DisplayName = JointCheckLabels.ApBillNbr)]
		public virtual Guid? BillId
		{
			get;
			set;
		}

		[PXDBInt]
		[PXDefault]
		[PXSelector(typeof(Search<APTran.lineNbr,
				Where<APTran.tranType, Equal<Current<APInvoice.docType>>,
					And<APTran.refNbr, Equal<Current<APInvoice.refNbr>>>>>),
			typeof(APTran.lineNbr),
			typeof(APTran.inventoryID),
			typeof(APTran.tranDesc),
			typeof(APTran.projectID),
			typeof(APTran.taskID),
			typeof(APTran.costCodeID),
			typeof(APTran.accountID),
			typeof(APTran.curyTranAmt), DirtyRead = true)]
		[ChangeBillLineNumberHeaders(typeof(PXSelectorAttribute))]
		[PXUIField(DisplayName = JointCheckLabels.BillLineNumber)]
		public virtual int? BillLineNumber
		{
			get;
			set;
		}

		[PXDecimal]
		[PXFormula(typeof(Default<billLineNumber>))]
		[PXUIField(DisplayName = "Bill Line Amount", Visibility = PXUIVisibility.Invisible,
			Enabled = false)]
		public virtual decimal? BillLineAmount
		{
			get;
			set;
		}

		#region LinkedToPayment
		public abstract class linkedToPayment : BqlBool.Field<linkedToPayment> { }

		[PXDefault(false)]
		[PXDBBool]
		public virtual bool? LinkedToPayment { get; set; }
		#endregion

		#region CanDelete
		public abstract class canDelete : PX.Data.BQL.BqlBool.Field<canDelete> { }

		[PXBool]
		public virtual bool? CanDelete { get; set; }
		#endregion

		public abstract class billId : BqlGuid.Field<billId>
		{
		}

		public abstract class jointPayeeInternalId : BqlInt.Field<jointPayeeInternalId>
		{
		}

		public abstract class jointPayeeExternalName : BqlString.Field<jointPayeeExternalName>
		{
		}

		public abstract class jointAmountOwed : BqlDecimal.Field<jointAmountOwed>
		{
		}

		public abstract class jointAmountPaid : BqlDecimal.Field<jointAmountPaid>
		{
		}

		public abstract class jointBalance : BqlDecimal.Field<jointBalance>
		{
		}

		public abstract class jointPayeeId : BqlInt.Field<jointPayeeId>
		{
		}

		public abstract class billLineNumber : BqlInt.Field<billLineNumber>
		{
		}

		public abstract class billLineAmount : BqlDecimal.Field<billLineAmount>
		{
		}
	}
}