using PX.Data;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CM;
using System;
using System.Web.Caching;
using PX.Objects.AP.Standalone;


namespace PX.Objects.DR
{
	public class DraftScheduleMaintASC606MultiplebaseCurrencies : PXGraphExtension<DraftScheduleMaintASC606, DraftScheduleMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>() &&
			       PXAccess.FeatureInstalled<FeaturesSet.aSC606>();
		}
		protected virtual void _(Events.FieldUpdated<DRScheduleMultipleBaseCurrencies.baseCuryIDASC606> e)
		{
			e.Cache.SetValueExt<DRSchedule.baseCuryID>(e.Row, e.NewValue);
		}
		protected virtual void _(Events.FieldUpdated<DRSchedule.baseCuryID> e)
		{
			var pending = e.Cache.GetValuePending<DRScheduleMultipleBaseCurrencies.baseCuryIDASC606>(e.Row);
			if((pending == PXCache.NotSetValue || pending == null))
			e.Cache.SetValue<DRScheduleMultipleBaseCurrencies.baseCuryIDASC606>(e.Row, e.NewValue);
		}

		protected virtual void _(Events.RowSelected<DRSchedule> e)
		{
			if (e.Row.BaseCuryID == null)
			{
				Exception ex = new PXSetPropertyException(CR.Messages.EmptyValueErrorFormat,
					PXUIFieldAttribute.GetDisplayName<DRScheduleMultipleBaseCurrencies.baseCuryIDASC606>(e.Cache));
				e.Cache.RaiseExceptionHandling<DRScheduleMultipleBaseCurrencies.baseCuryIDASC606>(e.Row, null, ex);
			}
			else
			{
				e.Cache.RaiseExceptionHandling<DRScheduleMultipleBaseCurrencies.baseCuryIDASC606>(e.Row, null, null);
			}
		}
	}
}
