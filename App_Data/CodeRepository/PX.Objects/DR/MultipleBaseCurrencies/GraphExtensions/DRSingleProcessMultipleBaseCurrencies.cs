using PX.Data;
using PX.Objects.Common;
using System.Collections.Generic;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.CM;


namespace PX.Objects.DR
{
	public class DRSingleProcessMultipleBaseCurrencies : PXGraphExtension<DRSingleProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		public delegate void SetFairValueSalesPriceDelegate(DRScheduleDetail scheduleDetail, Location location, CurrencyInfo currencyInfo);
		[PXOverride]
		public void SetFairValueSalesPrice(DRScheduleDetail scheduleDetail, Location location, CurrencyInfo currencyInfo, SetFairValueSalesPriceDelegate baseMethod)
		{
			var takeInBaseCurrency = currencyInfo.CuryID == currencyInfo.BaseCuryID || Base.Setup.Current.UseFairValuePricesInBaseCurrency.Value;
			DRSingleProcess.SetFairValueSalesPrice(Base.Schedule.Current, scheduleDetail, Base.ScheduleDetail, location, currencyInfo, takeInBaseCurrency);
		}
	}
}
