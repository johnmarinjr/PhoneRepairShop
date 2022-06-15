// Decompiled

using PX.Data;
using PX.Data.BQL;
using PX.Objects.CR;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA
{
	public sealed class T5018BAccountExt : PXCacheExtension<BAccount>
    {
        #region IsActive

        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
        }

        #endregion

        public abstract class vendorT5018 : BqlType<IBqlBool, bool>.Field<vendorT5018>
        {
        }
        [PXDBBool]
        [PXUIField(DisplayName = "T5018 Vendor")]
        public bool? VendorT5018
        {
            get;
            set;
        }

        public abstract class boxT5018 : BqlType<IBqlInt, int>.Field<boxT5018>
        {
        }
        [PXDBInt]
        [PXIntList(new int[] {1, 2, 3}, new string[] {"Corporation", "Partnership", "Individual"})]
        [PXUIField(DisplayName = "T5018 Box")]
        public int? BoxT5018
        {
            get;
            set;
        }

        public abstract class businessNum : BqlType<IBqlString, string>.Field<businessNum>
        {
        }
        [PXDBString(70)]
        [PXUIField(DisplayName = "Program Account Number")]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public string BusinessNum
        {
            get;
            set;
        }

        public abstract class socialInsNum : BqlType<IBqlString, string>.Field<socialInsNum>
        {
        }
        [PXDBString(9, InputMask = "#########")]
        [PXUIField(DisplayName = "SIN")]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public string SocialInsNum
        {
            get;
            set;
        }
    }
}
