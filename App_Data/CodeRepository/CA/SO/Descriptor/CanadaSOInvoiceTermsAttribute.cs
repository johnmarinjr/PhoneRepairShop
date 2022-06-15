using System;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA.SO
{
    /// <summary>
    /// This class is based on code from Acumatica : SOInvoiceTermsAttribute
    /// </summary>
    /// MIGRATION_CHECK : Verify PX.Objects.SO.SOInvoiceTermsAttribute and adapt the changes
    public class CanadaSOInvoiceTermsAttribute : TermsAttribute
    {
        protected Type _SOInvDocType;
        protected Type _SOInvCuryDocBal;
        protected Type _SOInvCuryDocBalCSL;
        protected Type _SOInvCuryDiscBal;

        public CanadaSOInvoiceTermsAttribute(Type DocDate, Type DueDate, Type DiscDate, Type SOInvDocType, Type SOInvCuryDocBal, Type SOInvCuryDocBalCSL, Type SOInvCuryDiscBal)
            : base(DocDate, DueDate, DiscDate, null, null)
        {
            _SOInvDocType = SOInvDocType;
            _SOInvCuryDocBal = SOInvCuryDocBal;
            _SOInvCuryDocBalCSL = SOInvCuryDocBalCSL;
            _SOInvCuryDiscBal = SOInvCuryDiscBal;
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);

            if (_SOInvCuryDocBal == null || _SOInvCuryDocBalCSL == null || _SOInvCuryDiscBal == null)
            {
                return;
            }
            
            SubscribeCalcDisc(sender);
            sender.Graph.FieldVerifying.AddHandler(BqlCommand.GetItemType(_SOInvCuryDiscBal), _SOInvCuryDiscBal.Name, VerifyDiscountHandler);

            _CuryDiscBal = _SOInvCuryDiscBal;
        }

        public override void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            base.FieldUpdated(sender, e);
            CalcDiscHandler(sender, e);
            CalcDiscHandlerCSL(sender, e);
        }

        protected override void SubscribeCalcDisc(PXCache sender)
        {
            sender.Graph.FieldUpdated.AddHandler(BqlCommand.GetItemType(_SOInvCuryDocBal), _SOInvCuryDocBal.Name, CalcDiscHandler);
            sender.Graph.FieldUpdated.AddHandler(BqlCommand.GetItemType(_SOInvCuryDocBalCSL), _SOInvCuryDocBalCSL.Name, CalcDiscHandlerCSL);
        }

        protected override void UnsubscribeCalcDisc(PXCache sender)
        {
            sender.Graph.FieldUpdated.RemoveHandler(BqlCommand.GetItemType(_SOInvCuryDocBal), _SOInvCuryDocBal.Name, CalcDiscHandler);
            sender.Graph.FieldUpdated.RemoveHandler(BqlCommand.GetItemType(_SOInvCuryDocBalCSL), _SOInvCuryDocBalCSL.Name, CalcDiscHandlerCSL);
        }

        /// <summary>
        /// Discount calculation for non cash sales.  This handler is used to detect changes to the
        /// document total amount of non cash sales (typically curyOrigDocAmt).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalcDiscHandler(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            if (_SOInvDocType == null)
            {
                return;
            }

            string docType = (string) sender.GetValue(e.Row, _SOInvDocType.Name);

            if (docType != ARDocType.CashSale && docType != ARDocType.CashReturn)
            {
                // As the document type is dynamic, we only allow the execution of the base class's real 
                // calculation when the document type is not a cash sale nor a cash return.
                _CuryDocBal = _SOInvCuryDocBal;
            }

            try
            {
                CalcDisc(sender, e);
            }
            finally
            {
                _CuryDocBal = null;
            }
        }

        /// <summary>
        /// Discount calculation for cash sales.  This handler is used to detect changes to the
        /// document total amount of cash sales (typically curyDocBal).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CalcDiscHandlerCSL(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            if (_SOInvDocType == null)
            {
                return;
            }

            string docType = (string)sender.GetValue(e.Row, _SOInvDocType.Name);

            if (docType == ARDocType.CashSale || docType == ARDocType.CashReturn)
            {
                // As the document type is dynamic, we only call the base class's real calculation when
                // the document type is a cash sale or cash return.
                _CuryDocBal = _SOInvCuryDocBalCSL;
            }

            try
            {
                CalcDisc(sender, e);
            }
            finally
            {
                _CuryDocBal = null;
            }
        }

        private void VerifyDiscountHandler(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            if (_SOInvDocType == null)
            {
                return;
            }

            string docType = (string)sender.GetValue(e.Row, _SOInvDocType.Name);

            _CuryDocBal = (docType == ARDocType.CashSale || docType == ARDocType.CashReturn
                               ? _SOInvCuryDocBalCSL
                               : _SOInvCuryDocBal);

            try
            {
                VerifyDiscount(sender, e);
            }
            finally
            {
                _CuryDocBal = null;
            }
        }
    }
}
