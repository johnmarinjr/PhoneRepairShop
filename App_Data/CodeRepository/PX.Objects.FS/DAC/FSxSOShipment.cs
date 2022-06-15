using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;

namespace PX.Objects.FS
{
    [PXTable(IsOptional = true)]
    public class FSxSOShipment : PXCacheExtension<SOShipment>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

		#region IsFSRelated
		public abstract class isFSRelated : PX.Data.BQL.BqlBool.Field<isFSRelated> { }

		[PXBool]
		[PXUnboundDefault(true)]
		public virtual bool? IsFSRelated { get; set; }
		#endregion

		#region Installed
		public abstract class installed : PX.Data.BQL.BqlBool.Field<installed> { }

        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Installed", Enabled = false)]
		[PXUIVisible(typeof(Where<isFSRelated, Equal<True>>))]
		public virtual bool? Installed { get; set; }
		#endregion
	}
}
