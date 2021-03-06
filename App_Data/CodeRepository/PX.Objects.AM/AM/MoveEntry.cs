using PX.Data;
using PX.Objects.CS;
using PX.Objects.AM.Attributes;

namespace PX.Objects.AM
{
    public class MoveEntry : MoveEntryBase<Where<AMBatch.docType, Equal<AMDocType.move>>>
    {
        #region LineSplitting
        // Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
        public class LineSplittingExtension : AMMoveLineSplittingExtension<MoveEntry> { }
        public LineSplittingExtension LineSplittingExt => FindImplementation<LineSplittingExtension>();
        public override PXSelectBase<AMMTran> LSSelectDataMember => LineSplittingExt.lsselect; 

        // Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
        public class ItemAvailabilityExtension : AMBatchItemAvailabilityExtension<MoveEntry> { }
        #endregion

        public MoveEntry()
        {
            PXVerifySelectorAttribute.SetVerifyField<AMMTran.receiptNbr>(transactions.Cache, null, true);
            PXUIFieldAttribute.SetVisible<AMMTran.receiptNbr>(transactions.Cache, null, true);
        }

        protected override void AMMTran_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
        {
            var row = (AMMTran)e.Row;
            if (row == null || sender.GetStatus(row) == PXEntryStatus.InsertedDeleted)
            {
                return;
            }

            //Only prompt when a non referenced batch
            if (batch.Current != null
                && string.IsNullOrWhiteSpace(batch.Current.OrigBatNbr)
                && row.DocType == batch.Current.DocType && row.BatNbr == batch.Current.BatNbr
                && !_skipReleasedReferenceDocsCheck
                && ReferenceDeleteGraph.HasReleasedReferenceDocs(this, row, true))
            {
                throw new PXException(Messages.ReleasedTransactionExist);
            }
        }

        [PXRestrictor(typeof(Where<AMOrderType.function, NotEqual<OrderTypeFunction.disassemble>>), Messages.IncorrectOrderTypeFunction)]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<AMMTran.orderType> e) { }

        protected override void AMMTran_OperationID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            base.AMMTran_OperationID_FieldUpdated(sender, e);

            var row = (AMMTran)e.Row;
            if (row?.OperationID == null || IsImport || IsContractBasedAPI || row.Qty.GetValueOrDefault() < 0)
            {
                return;
            }

            var amOrderType = (AMOrderType)PXSelectorAttribute.Select<AMMTran.orderType>(sender, row);
            if (amOrderType == null || !amOrderType.DefaultOperationMoveQty.GetValueOrDefault())
            {
                return;
            }

            var amProdOper = (AMProdOper)PXSelectorAttribute.Select<AMMTran.operationID>(sender, row);
            if (amProdOper != null)
            {
                sender.SetValueExt<AMMTran.qty>(row, amProdOper.QtyRemaining.GetValueOrDefault());
            }
        }
    }
}
