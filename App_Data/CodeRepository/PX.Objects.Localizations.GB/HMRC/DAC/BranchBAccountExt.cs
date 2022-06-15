using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.Localizations.GB.HMRC.DAC
{
	public class BranchBAccountExt : PXCacheExtension<BranchMaint.BranchBAccount>
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
						Or<BAccountMTDApplication.bAccountID.IsEqual<BranchMaint.BranchBAccount.bAccountID.FromCurrent>>>>>),
			typeof(HMRCOAuthApplication.applicationID),
			typeof(HMRCOAuthApplication.applicationName),
			SubstituteKey = typeof(HMRCOAuthApplication.applicationID),
			DescriptionField = typeof(HMRCOAuthApplication.applicationName))]
		[PXDBScalar(typeof(SearchFor<HMRCOAuthApplication.applicationID>.In<
			SelectFrom<HMRCOAuthApplication>.InnerJoin<BAccountMTDApplication>.On<
				HMRCOAuthApplication.applicationID.IsEqual<BAccountMTDApplication.applicationID>>.Where<
				BAccountMTDApplication.bAccountID.IsEqual<Branch.bAccountID>
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
