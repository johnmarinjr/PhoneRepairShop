using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CN.Common.Services;
using PX.Objects.CN.JointChecks.AP.CacheExtensions;
using PX.Objects.CN.JointChecks.AP.DAC;
using PX.Objects.CN.JointChecks.AP.Extensions;
using PX.Objects.CN.JointChecks.AP.Services.ChecksAndPaymentsServices;
using PX.Objects.CN.JointChecks.AP.Services.DataProviders;
using PX.Objects.CN.JointChecks.Descriptor;
using PX.Objects.CS;

namespace PX.Objects.CN.JointChecks.AP.GraphExtensions.PaymentEntry
{
    public class ApPaymentEntryExt : JointPayeeGraphExtBase<APPaymentEntry>
    {
        public SelectFrom<JointPayeePayment>
            .InnerJoin<JointPayee>
            .On<JointPayee.jointPayeeId.IsEqual<JointPayeePayment.jointPayeeId>>.View JointPayeePayments;

        public PXSelect<JointPayee> JointPayees;

		private JointPayeePaymentService jointPayeePaymentService;
        private ComplianceDocumentsService complianceDocumentsService;

        public override PXSelectBase<JointPayee> JointPayeeViewAccessor => JointPayees;

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.construction>();
        }

        public IEnumerable jointPayeePayments()
        {
	        return SelectFrom<JointPayeePayment>
                .InnerJoin<JointPayee>.On<JointPayee.jointPayeeId.IsEqual<JointPayeePayment.jointPayeeId>>
                .Where<JointPayeePayment.paymentDocType.IsEqual<APPayment.docType.FromCurrent>
                    .And<JointPayeePayment.paymentRefNbr.IsEqual<APPayment.refNbr.FromCurrent>>>
                .View.Select(Base);
        }

        public override void Initialize()
        {
            jointPayeePaymentService = new JointPayeePaymentService(Base);
            complianceDocumentsService = new ComplianceDocumentsService(Base);
        }

        public PXAction<APPayment> reloadJointPayees;

        [PXButton]
		[PXUIField(DisplayName = "Reload Joint Payees", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
        public virtual IEnumerable ReloadJointPayees(PXAdapter adapter)
        {
	        APPayment payment = Base.Document.Current;

			if (payment == null)
				return adapter.Get();

	        HashSet<Tuple<int?, string, string>> usedPayeeIDByDoc =
		        JointPayeePayments.Select()
			        .FirstTableItems
			        .Select(row => new Tuple<int?, string, string>(row.JointPayeeId, row.InvoiceDocType, row.InvoiceRefNbr))
			        .ToHashSet();

	        PXResultset<APAdjust> rows =
		        PXSelectJoin<APAdjust,
			        InnerJoin<APInvoice,
				        On<APAdjust.adjdDocType, Equal<APInvoice.docType>,
					        And<APAdjust.adjdRefNbr, Equal<APInvoice.refNbr>>>,
					LeftJoin<APTran,
				        On<APInvoice.paymentsByLinesAllowed, Equal<True>,
							And<APAdjust.adjdDocType, Equal<APTran.tranType>,
					        And<APAdjust.adjdRefNbr, Equal<APTran.refNbr>,
							And<APAdjust.adjdLineNbr, Equal<APTran.lineNbr>>>>>,
					LeftJoin<APInvoice2,
						On<APInvoice.isRetainageDocument, Equal<True>,
							And<APInvoice.origDocType, Equal<APInvoice2.docType>,
							And<APInvoice.origRefNbr, Equal<APInvoice2.refNbr>>>>,
					LeftJoin<APTran2,
						On<APInvoice.isRetainageDocument, Equal<True>,
							And<APInvoice.paymentsByLinesAllowed, Equal<True>,
							And<APInvoice.origDocType, Equal<APTran2.tranType>,
							And<APInvoice.origRefNbr, Equal<APTran2.refNbr>,
							And<APTran.origLineNbr, Equal<APTran2.lineNbr>>>>>>,
					LeftJoin<JointPayee,
						On<IsNull<APInvoice2.noteID, APInvoice.noteID>, Equal<JointPayee.billId>,
							And<IsNull<APTran2.lineNbr, APAdjust.adjdLineNbr>, Equal<JointPayee.billLineNumber>>>>>>>>,
			        Where<APAdjust.adjgDocType, Equal<Required<APAdjust.adjgDocType>>,
				        And<APAdjust.adjgRefNbr, Equal<Required<APAdjust.adjgRefNbr>>,
						And<JointPayee.jointPayeeId, IsNotNull>>>>
					.Select(Base, 
							payment.DocType,
							payment.RefNbr);

			foreach (PXResult<APAdjust, APInvoice, APTran, APInvoice2, APTran2, JointPayee> row in rows)
			{
				JointPayee payee = row;
				APAdjust adjust = row;

				if (!usedPayeeIDByDoc.Contains(new Tuple<int?, string, string>(payee.JointPayeeId, adjust.AdjdDocType, adjust.AdjdRefNbr)))
				{
					jointPayeePaymentService.AddJointPayeePayment(payee, adjust);
				}
			}

			return adapter.Get();
        }

		#region Events

		public virtual void _(Events.RowSelected<APPayment> args)
		{
			APPayment payment = args.Row;

			if (payment == null)
				return;

			reloadJointPayees.SetEnabled(IsJointPayeeDataEditable(payment));

			if (isValidScreenToProcess)
            {
                UpdateJointCheckAvailability(args.Row);
                SetBillLineNumberVisibility();
                UpdateVendorAndJointPaymentAmounts(args.Row);
            }
        }

        public virtual void _(Events.RowSelected<JointPayeePayment> args)
        {
            var jointPayeePayment = args.Row;
            if (jointPayeePayment?.JointPayeeId != null && isValidScreenToProcess)
            {
                UpdateJointAmountToPayAvailability(jointPayeePayment);
                PXUIFieldAttribute.SetDisplayName<JointPayee.billLineNumber>(Base.Caches<JointPayee>(),
                    JointCheckLabels.ApBillLineNumber);
            }
        }

        public virtual void APAdjust_RowInserted(PXCache cache, PXRowInsertedEventArgs args)
        {
            var adjustment = args.Row as APAdjust;
            if (adjustment?.AdjdDocType == APDocType.Invoice && IsCheck() && isValidScreenToProcess)
            {
                jointPayeePaymentService.AddJointPayeePayments(adjustment);
            }
        }

        public virtual void _(Events.RowDeleted<APPayment> args)
        {
            jointPayeePaymentService.DeleteJointPayeePayments(args.Row);
        }

        public virtual void _(Events.RowDeleted<APAdjust> args)
        {
            if (args.Row.AdjdDocType == APDocType.Invoice && !IsCurrentPaymentDeleted())
            {
                jointPayeePaymentService.DeleteJointPayeePayments(args.Row);
            }
        }

        public virtual void _(Events.RowPersisting<JointPayeePayment> args)
        {
            if (args.Operation != PXDBOperation.Delete)
            {
                args.Row.PaymentRefNbr = Base.Document.Current.RefNbr;
            }
        }

        public virtual void APAdjust_RowPersisting(PXCache cache, PXRowPersistingEventArgs args)
        {
            if (args.Row is APAdjust adjustment)
            {
                switch (args.Operation)
                {
                    case PXDBOperation.Delete:
                        jointPayeePaymentService.ClosePaymentCycleWorkflowIfNeeded(adjustment);
                        break;
                    case PXDBOperation.Insert:
                        jointPayeePaymentService.InitializePaymentCycleWorkflowIfRequired(adjustment);
                        complianceDocumentsService.UpdateComplianceDocumentsIfRequired(adjustment);
                        break;
                }
            }
        }

		#endregion

		private bool IsCheck()
        {
            return Base.Document.Current?.DocType == APDocType.Check;
        }

        private bool IsVoidCheck()
        {
            return Base.Document.Current?.DocType == APDocType.VoidCheck;
        }

        private void UpdateJointAmountToPayAvailability(JointPayeePayment jointPayeePayment)
        {
            var jointPayee = JointPayeeDataProvider.GetJointPayee(Base, jointPayeePayment);
            var adjustment = AdjustmentDataProvider.GetAdjustment(Base, jointPayeePayment);
            var hasReversedAdjustments = DoesCheckContainReversedAdjustments(jointPayeePayment);
            var isZeroJointBalance = jointPayee.JointBalance == 0 && !hasReversedAdjustments;
            var isReleased = adjustment?.Released == true;
            var isVoidAdjustment = !IsVoidCheck() && adjustment?.Voided == true;
            var isReadOnly = isZeroJointBalance || isReleased || isVoidAdjustment;
            PXUIFieldAttribute.SetReadOnly<JointPayeePayment.jointAmountToPay>(
                JointPayeePayments.Cache, jointPayeePayment, isReadOnly);
        }

        private bool DoesCheckContainReversedAdjustments(JointPayeePayment jointPayeePayment)
        {
            return JointPayeePayments.SelectMain()
                .Any(jpp => jpp.JointAmountToPay < 0 && jpp.JointPayeeId == jointPayeePayment.JointPayeeId);
        }

        private void UpdateJointCheckAvailability(APPayment payment)
        {
            var extension = PXCache<APPayment>.GetExtension<ApPaymentExt>(payment);

			if (IsJointPayeeDataEditable(payment))
			{
				extension.IsJointCheck =
						SelectFrom<APAdjust>
							.InnerJoin<APInvoice>
								.On<APAdjust.adjdDocType.IsEqual<APInvoice.docType>
									.And<APAdjust.adjdRefNbr.IsEqual<APInvoice.refNbr>>>
							.Where<APAdjust.adjgDocType.IsEqual<P.AsString>
								.And<APAdjust.adjgRefNbr.IsEqual<P.AsString>
							.And<APInvoice.docType.IsEqual<APDocType.invoice>
							.And<APInvoice.status.IsNotEqual<APDocStatus.closed>
							.And<APInvoiceJCExt.isJointPayees.IsEqual<True>>>>>>
							.View
						.SelectSingleBound(Base, null, payment.DocType, payment.RefNbr)
						.FirstTableItems
						.Any();
			}
			else
			{
				extension.IsJointCheck = JointPayeePayments.SelectSingle() != null;
			}

			UpdateJointCheckVisibility(extension.IsJointCheck == true);
            JointPayeePayments.AllowUpdate = Base.Adjustments.AllowUpdate;
        }

        private void UpdateJointCheckVisibility(bool isJointCheck)
        {
            var isExpectedType = IsCheck() || IsVoidCheck();
            PXUIFieldAttribute.SetVisible<ApPaymentExt.isJointCheck>(Base.Document.Cache, null, isExpectedType);
            PXUIFieldAttribute.SetVisible(JointPayeePayments.Cache, null, isExpectedType);
            PXUIFieldAttribute.SetVisible<ApPaymentExt.jointPaymentAmount>(Base.Document.Cache, null,
                isExpectedType && isJointCheck);
            PXUIFieldAttribute.SetVisible<ApPaymentExt.vendorPaymentAmount>(Base.Document.Cache, null,
                isExpectedType && isJointCheck);
        }

        private void SetBillLineNumberVisibility()
        {
            var shouldShowBillLineNumber = JointPayeePayments.SelectMain().Any(jpp => jpp.IsPaymentByline());
            PXUIFieldAttribute.SetVisible<JointPayee.billLineNumber>(JointPayeePayments.Cache, null,
                shouldShowBillLineNumber);
        }

        private void UpdateVendorAndJointPaymentAmounts(APPayment payment)
        {
            var extension = PXCache<APPayment>.GetExtension<ApPaymentExt>(payment);
            extension.JointPaymentAmount = JointPayeePayments.SelectMain().Sum(jpp => jpp.JointAmountToPay);
            extension.VendorPaymentAmount = payment.CuryOrigDocAmt - extension.JointPaymentAmount;
        }

        private bool IsCurrentPaymentDeleted()
        {
            var status = Base.Document.Cache.GetStatus(Base.Document.Current);
            return status.IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted);
        }

        public bool IsJointPayeeDataEditable(APPayment payment)
        {
	        return payment.VendorID != null && payment.Released != true
	                                        && IsCheck() && isValidScreenToProcess
                                            && !Base.IsDocReallyPrinted(payment);
        }
        private bool isValidScreenToProcess => SiteMapExtension.IsChecksAndPaymentsScreenId() || SiteMapExtension.IsReleasePaymentsScreenId();
    }
}