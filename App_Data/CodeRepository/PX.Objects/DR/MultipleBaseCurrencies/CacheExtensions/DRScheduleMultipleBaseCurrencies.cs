using System;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.CM;

namespace PX.Objects.DR
{
	public sealed class DRScheduleMultipleBaseCurrencies : PXCacheExtension<DRSchedule>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BaseCuryIDASC606
		public abstract class baseCuryIDASC606 : PX.Data.BQL.BqlString.Field<baseCuryIDASC606> { }

		[PXString]
		[PXSelector(typeof(Search<CurrencyList.curyID>))]
		[PXRestrictor(typeof(Where<CurrencyList.curyID, IsBaseCurrency>), Messages.CurrencyIsNotBaseCurrency)]
		[PXUIField(DisplayName = "Currency")]
		// Acuminator disable once PX1030 PXDefaultIncorrectUse used for unbound field linked to a hidden bound.
		[PXDefault(typeof(Current<AccessInfo.baseCuryID>))]
		[PXDBCalced(typeof(DRSchedule.baseCuryID), typeof(string))]
		public string BaseCuryIDASC606 { get; set; }
		#endregion
	}
}
