using PX.Data;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.Localizations.GB.HMRC.DAC;
using static PX.Objects.CS.BranchMaint;
using PX.Data.BQL.Fluent;
using PX.Objects.GL.DAC;
using System.Text.RegularExpressions;

namespace PX.Objects.Localizations.GB.HMRC
{
	public class BranchMaint_Extension : PXGraphExtension<BranchMaint>
	{
		public const string GreatBritainCountryID = "GB";

		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.uKLocalization>();

		#region Views

		public SelectFrom<BAccountMTDApplication>.
			Where<BAccountMTDApplication.bAccountID.IsEqual<BranchBAccount.bAccountID.FromCurrent>>.View
			CurrentBAccountMTDApplication;
		#endregion

		#region Event Handlers
		protected void _(Events.RowSelected<BranchBAccount> e)
		{
			Organization organization = Base.CurrentOrganizationView.Current;
			var addresses = Base.GetExtension<BranchMaint.DefContactAddressExt>().DefAddress.Select();
			Address address = addresses.Count > 0 ? addresses[0] : null;

			if (organization != null)
			{
				// Enable/Disable ApplicationID field based on if FileTaxesByBranch
				// is enabled for the parent org,
				// and branch is based out of GB
				bool enabledFields = (organization?.FileTaxesByBranches ?? false) &&
									address?.CountryID == GreatBritainCountryID;
				PXUIFieldAttribute.SetEnabled<BranchBAccountExt.mTDApplicationID>
						(e.Cache, e.Row, enabledFields);
				PXUIFieldAttribute.SetVisible<BranchBAccountExt.mTDApplicationID>
						(e.Cache, e.Row, enabledFields);
			}
		}

		protected void _(Events.FieldUpdated<BranchBAccountExt.mTDApplicationID> e)
		{
			BAccountMTDApplication bAccountApplication = CurrentBAccountMTDApplication.Select();
			if (e.NewValue != null)
			{
				if (bAccountApplication == null)
				{
					bAccountApplication = CurrentBAccountMTDApplication.Insert(new BAccountMTDApplication()
						{ BAccountID = Base.CurrentBAccount.Current.BAccountID });
				}
				CurrentBAccountMTDApplication.SetValueExt<BAccountMTDApplication.applicationID>(bAccountApplication,
					e.NewValue);
			}
			else if (bAccountApplication != null)
			{
				CurrentBAccountMTDApplication.Cache.Delete(bAccountApplication);
			}
		}

		protected void _(Events.FieldVerifying<BranchBAccount.taxRegistrationID> e)
		{
			Address address = Base.GetExtension<BranchMaint.DefContactAddressExt>().DefAddress.Select()[0];

			if (address?.CountryID == GreatBritainCountryID)
			{
				string TaxRegistrationIDPattern = @"^(\d{9}|\d{12})$";
				if (e.NewValue != null && !Regex.Match(e.NewValue as string, TaxRegistrationIDPattern).Success)
				{
					throw new PXSetPropertyException<BranchBAccount.taxRegistrationID>(
						Messages.TaxRegistrationIDInvalid,
						PXErrorLevel.Error,
						e.NewValue);
				}
			}
		}

		protected void _(Events.FieldUpdated<Address.countryID> e)
		{
			if (e.NewValue as string == GreatBritainCountryID)
				// Verifies the Tax Registration ID when the country ID is changed.
				Base.CurrentBAccount.SetValueExt<BranchBAccount.taxRegistrationID>(Base.CurrentBAccount.Current, Base.CurrentBAccount.Current.TaxRegistrationID);
			else
			{
				BAccountMTDApplication bAccountApplication = CurrentBAccountMTDApplication.Select();
				CurrentBAccountMTDApplication.Delete(bAccountApplication);
			}
		}

		protected void _(Events.RowDeleting<BranchBAccount> e)
		{
			BAccountMTDApplication bAccountApplication = CurrentBAccountMTDApplication.Select();
			if (bAccountApplication != null)
			{
				CurrentBAccountMTDApplication.Delete(bAccountApplication);
			}
		}

		protected void _(Events.RowPersisted<BranchBAccount> e)
		{
			//persist of primary view always happens first
			//updating BAccountID after identity field was obtained from db
			if (e.Operation == PXDBOperation.Insert && e.TranStatus == PXTranStatus.Open)
			{
				foreach (var row in CurrentBAccountMTDApplication.Cache.Inserted)
				{
					CurrentBAccountMTDApplication.Cache.SetValue<BAccountMTDApplication.bAccountID>(row, e.Row.BAccountID);
				}
			}
		}
		#endregion
	}
}
