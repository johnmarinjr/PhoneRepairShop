using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL.DAC;
using PX.Objects.TX;
using PX.Objects.Localizations.CA.TX;

namespace PX.Objects.Localizations.CA.CS
{
	public class BranchMaintExt : PXGraphExtension<BranchMaint>
    {
        public PXSelectJoin<TaxRegistration,
                InnerJoin<Tax, On<Tax.taxID, Equal<TaxRegistration.taxID>>>,
                Where<TaxRegistration.bAccountID, Equal<Current<BAccount.bAccountID>>>>
            Taxes;

        public PXSelect<Organization,
                Where<Organization.organizationID, Equal<Current<BranchMaint.BranchBAccount.organizationID>>>>
            Organization;

        #region IsActive

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }

        #endregion

        protected virtual void BranchBAccount_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            BranchMaint.BranchBAccount row = e.Row as BranchMaint.BranchBAccount;

            if (row == null)
            {
                return;
            }

            if (Organization.Current != null && Organization.Current.FileTaxesByBranches != null)
            {
                Taxes.Cache.AllowSelect = Organization.Current.FileTaxesByBranches.Value;
            }
            else
            {
                Taxes.Cache.AllowSelect = false;
            }

        }

        protected virtual void TaxRegistration_TaxID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            TaxRegistration row = e.Row as TaxRegistration;
            if (row == null)
            {
                return;
            }

            string taxID = e.NewValue as string;
            if (string.IsNullOrWhiteSpace(taxID))
            {
                throw new PXSetPropertyException<TaxRegistration.taxID>(Messages.Common.CannotBeEmpty);
            }

            // Do not use PXSelectorAttribute.Select, there's currently an issue on Acumatica's side
            // (https://jira.acumatica.com/browse/AC-57782) and the data of an extension field is
            // accessible only to the first user session that reads it.
            //
            // Technically, no problem is caused here, but used PXSelect<> anyway to be consistent throughout
            // the project.
            //
            //Tax tax = PXSelectorAttribute.Select<TaxRegistration.taxID>(sender, row, taxID) as Tax;

            Tax tax = PXSelect<Tax,
                    Where<Tax.taxID,
                        Equal<Required<Tax.taxID>>>>
                .Select(sender.Graph, taxID);

            if (tax == null)
            {
                throw new PXSetPropertyException<TaxRegistration.taxID>(Messages.Common.CannotBeFound, taxID);
            }

        }

    }

}
