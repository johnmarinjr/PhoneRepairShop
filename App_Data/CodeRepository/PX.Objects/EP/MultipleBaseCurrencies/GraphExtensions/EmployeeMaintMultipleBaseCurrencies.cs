using System.Collections;
using System.Linq;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.AP;
using PX.Objects.CS;
using CRLocation = PX.Objects.CR.Standalone.Location;
using APQuickCheck = PX.Objects.AP.Standalone.APQuickCheck;
using PX.Objects.EP.DAC;
using PX.Objects.CA;
using PX.Objects.CR;

namespace PX.Objects.EP
{
    public sealed class EmployeeMaintMultipleBaseCurrencies: PXGraphExtension<EmployeeMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
        }

        protected void _(Events.FieldUpdated<EPEmployee, EPEmployee.parentBAccountID> e)
        {
            e.Row.BaseCuryID = PXOrgAccess.GetBaseCuryID(e.Row.ParentBAccountID);
        }

        protected void _(Events.FieldUpdating<EPEmployee, EPEmployee.parentBAccountID> e)
		{
			if (SelectFrom<APHistory>
					.Where<APHistory.vendorID.IsEqual<EPEmployee.bAccountID.FromCurrent>>
					.View.SelectSingleBound(Base, new object[] {e.Row}, null).Any()
				&& e.Row.BaseCuryID != PXOrgAccess.GetBaseCuryID(e.NewValue.ToString()))
			{
				throw new PXSetPropertyException(Messages.BranchCannotBeAssociated, PXErrorLevel.Error,
					e.Row.BaseCuryID,
					e.Row.AcctCD);
			}

			bool hasAnyRelatedDocuments = HasAnyRelatedDocumentsInvolved(e, out string documentsNotReleased);
			if (hasAnyRelatedDocuments && e.Row.BaseCuryID != PXOrgAccess.GetBaseCuryID(e.NewValue.ToString()))
			{
				throw new PXSetPropertyException(Messages.BranchCannotBeAssociatedDueToDocumentsInvolved, PXErrorLevel.Error,
					e.Row.BaseCuryID,
					documentsNotReleased);
			}
		}

		private bool HasAnyRelatedDocumentsInvolved(Events.FieldUpdating<EPEmployee, EPEmployee.parentBAccountID> e, out string documentsNotReleased)
		{
			documentsNotReleased = "";
			var hasAnyExpenseReceipt = SelectFrom<EPExpenseClaimDetails>.Where<EPExpenseClaimDetails.employeeID.IsEqual<EPEmployee.bAccountID.FromCurrent>>.View.SelectSingleBound(Base, new object[] { e.Row }, null).Any();
			var hasAnyExpenseClaim = SelectFrom<EPExpenseClaim>.Where<EPExpenseClaim.employeeID.IsEqual<EPEmployee.bAccountID.FromCurrent>>.View.SelectSingleBound(Base, new object[] { e.Row }, null).Any();
			var hasAnyAPInvoice = SelectFrom<AP.APInvoice>.Where<AP.APInvoice.vendorID.IsEqual<EPEmployee.bAccountID.FromCurrent>>.View.SelectSingleBound(Base, new object[] { e.Row }, null).Any();
			var hasAnyAPQuickCheck = SelectFrom<APQuickCheck>.Where<APQuickCheck.vendorID.IsEqual<EPEmployee.bAccountID.FromCurrent>>.View.SelectSingleBound(Base, new object[] { e.Row }, null).Any();

			if (hasAnyExpenseReceipt)
			{
				documentsNotReleased += PXLocalizer.Localize(Messages.ExpenseReceipt);
			}
			if (hasAnyExpenseClaim)
			{
				documentsNotReleased += $", {PXLocalizer.Localize(Messages.ExpenseClaim)}";
			}
			if (hasAnyAPInvoice)
			{
				documentsNotReleased += $", {PXLocalizer.Localize(AP.Messages.APInvoice)}";
			}
			if (hasAnyAPQuickCheck)
			{
				documentsNotReleased += $", {PXLocalizer.Localize(AP.Messages.QuickCheck)}";
			}

			var hasAnyDocument = hasAnyExpenseReceipt || hasAnyExpenseClaim || hasAnyAPInvoice || hasAnyAPQuickCheck;
			return hasAnyDocument;
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
        [PXDBInt(IsKey = true)]
        [PXParent(typeof(Select<CACorpCard, Where<CACorpCard.corpCardID, Equal<Current<EPEmployeeCorpCardLink.corpCardID>>>>))]
        [PXSelector(typeof(Search2<CACorpCard.corpCardID,
                        InnerJoin<BAccount,
                            On<BAccount.bAccountID, Equal<Current<EPEmployeeCorpCardLink.employeeID>>>,
                        InnerJoin<CashAccount,
                            On<CashAccount.cashAccountID, Equal<CACorpCard.cashAccountID>>>>,
                        Where<BAccount.baseCuryID, Equal<CashAccount.baseCuryID>>>),
            typeof(CACorpCard.corpCardCD), typeof(CACorpCard.name), typeof(CACorpCard.cardNumber), typeof(CACorpCard.cashAccountID),
            SubstituteKey = typeof(CACorpCard.corpCardCD))]
        protected void _(Events.CacheAttached<EPEmployeeCorpCardLink.corpCardID> e)
        {
        }

		protected void _(Events.RowSelected<EPEmployee> e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetVisible<EPEmployee.baseCuryID>(e.Cache, e.Row, true);
			}
		}
	}
}
