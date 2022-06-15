using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.CS.DAC;
using PX.Objects.GL.DAC;

namespace PX.Objects.Localizations.GB.HMRC.DAC
{
	public class OrganizationBAccountExt : PXCacheExtension<OrganizationBAccount>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.uKLocalization>();
		#region MTDApplicationID
		public abstract class mTDApplicationID : BqlInt.Field<mTDApplicationID> { }
		[PXInt]
		[PXUIField(DisplayName = "MTD External Application")]
		[PXSelector(typeof(SearchFor<HMRCOAuthApplication.applicationID>.In<
				SelectFrom<HMRCOAuthApplication>.
				LeftJoin<BAccountMTDApplication>.On<HMRCOAuthApplication.applicationID.IsEqual<BAccountMTDApplication.applicationID>>.
				Where<HMRCOAuthApplication.type.IsEqual<HMRCOAuthApplication.HMRCApplicationType>.
					And<
						Brackets
						<BAccountMTDApplication.applicationID.IsNull>.
						Or<BAccountMTDApplication.bAccountID.IsEqual<OrganizationBAccount.bAccountID.FromCurrent>>>>>),
			typeof(HMRCOAuthApplication.applicationID),
			typeof(HMRCOAuthApplication.applicationName),
			SubstituteKey = typeof(HMRCOAuthApplication.applicationID),
			DescriptionField = typeof(HMRCOAuthApplication.applicationName))]
		[PXDBScalar(typeof(SearchFor<HMRCOAuthApplication.applicationID>.In<
			SelectFrom<HMRCOAuthApplication>.InnerJoin<BAccountMTDApplication>.On<
				HMRCOAuthApplication.applicationID.IsEqual<BAccountMTDApplication.applicationID>>.Where<
				BAccountMTDApplication.bAccountID.IsEqual<Organization.bAccountID>
			>>))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public int? MTDApplicationID
		{
			get;
			set;
		}
		#endregion
	}
}
