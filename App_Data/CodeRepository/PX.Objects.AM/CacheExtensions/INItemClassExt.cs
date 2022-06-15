using System;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.AM.CacheExtensions
{
    [Serializable]
    public sealed class INItemClassExt : PXCacheExtension<INItemClass>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>();
        }

        #region AMReplenishmentSource

        public abstract class aMReplenishmentSource : PX.Data.BQL.BqlString.Field<aMReplenishmentSource> { }
        protected string _AMReplenishmentSource;

        [PXDBString(1, IsFixed = true)]
        [PXUIField(DisplayName = "Replenishment Source")]
        [PXDefault(INReplenishmentSource.Purchased, PersistingCheck = PXPersistingCheck.Nothing)]
        [INReplenishmentSource.List]
        public string AMReplenishmentSource
        {
            get => this._AMReplenishmentSource ?? INReplenishmentSource.Purchased;
            set => this._AMReplenishmentSource = value;
        }
        #endregion
		#region AMDaysSupply
		public abstract class aMDaysSupply : PX.Data.BQL.BqlInt.Field<aMDaysSupply> { }
		[PXDBInt(MinValue=0)]
		[PXUIField(DisplayName="Days of Supply", FieldClass = Features.MRPFIELDCLASS)]
		[PXDefault(TypeCode.Int32, "0", PersistingCheck = PXPersistingCheck.Nothing)]
		public int? AMDaysSupply { get; set; }
		#endregion
    }
}
