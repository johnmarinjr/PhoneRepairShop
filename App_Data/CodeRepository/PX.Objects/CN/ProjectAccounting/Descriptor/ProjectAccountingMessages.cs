using PX.Common;

namespace PX.Objects.CN.ProjectAccounting.Descriptor
{
    [PXLocalizable]
    public class ProjectAccountingMessages
    {
        public const string TaskTypeIsNotAvailable = "Task Type is not valid";

        public const string TaskTypeCannotBeChanged =
            "Task type cannot be changed. The Task is already used in at least one {0} related document.";

        public const string TaskCannotBeDeleted =
            "Cannot delete Task since it already has at least one Document associated with it.";

        public const string CostTaskTypeIsNotValid =
            "Project Task Type is not valid. Only Tasks of 'Cost Task' and 'Cost and Revenue Task' types are allowed.";

        public const string RevenueTaskTypeIsNotValid =
            "Project Task Type is not valid. Only Tasks of 'Revenue Task' and 'Cost and Revenue Task' types are allowed.";

        public const string CostProjectionClassIsNotValid = "The class is used and cannot be changed.";

        public const string CannotReclassifiedMigratedDoc = "The bill cannot be reclassified because it has been created in migration mode.";
        public const string CannotReclassifiedWithBilledTransactions = "The bill cannot be reclassified because the related project transaction has been billed.";
        public const string CannotReclassifiedWithAllocatedTransactions = "The bill cannot be reclassified because the related project transaction has been allocated.";
        public const string CannotReclassifiedWithConsolidatedBatch = "The bill cannot be reclassified because the general ledger transaction created on release of the bill was included to the {0} consolidated batch.";
        public const string CannotReclassifiedWithWithReleasedRetainage = "The bill cannot be reclassified because it includes the released retainage.";
        public const string CannotReclassifiedWithReclassifiedBatch = "The bill cannot be reclassified because the corresponding GL transaction has been reclassified.";
        public const string CannotReclassifiedWithSummaryPosted = "The bill cannot be reclassified because the corresponding GL transaction has been generated with the Post Summary on Updating GL setting selected on the Accounts Payable Preferences (AP101000) form.";
        public const string CannotReclassifiedWithDeferredCode = "The bill line cannot be reclassified because the deferral code is specified in this line.";
        public const string CannotReclassifiedWithExpenseClaim = "The bill cannot be reclassified because it is linked to an expense claim.";
        public const string CannotReclassifiedWithServiceDoc = "The bill line cannot be reclassified because it is linked to a service document.";
        public const string CannotReclassifiedWithMultipleTerms = "The bill line cannot be reclassified because the multiple installment credit terms are specified in the bill.";
        public const string CannotReleasedWithLinkedReceipts = "The bill cannot be released because the purchase receipt {0} is linked to the purchase order selected in the {1} bill line.";
        public const string EmptyInventoryItemCannotBeChanged = "The inventory item cannot be changed on reclassification. Select a commitment line with empty inventory item.";
        public const string InventoryItemCannotBeChanged = "The inventory item cannot be changed on reclassification. Select a commitment line with the {0} inventory item.";
        public const string CannotAdjustedWithNonServiceLine = "The line cannot be adjusted because it is linked to a commitment line with the line type different from the Service type.";
        public const string CommitmentLineCannotBeSelected = "The commitment line cannot be selected because it is already linked to the {0} bill line.";
        public const string CannotClearReclassifyBill = "To be able to clear the Allow Bill Reclassification check box, release the bills that are assigned the Under Reclassification status first.";
    }
}