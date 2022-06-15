using PX.Data;

namespace PX.Objects.AM
{
    /// <summary>
    /// Graph for Manufacturing MRP Preferences
    /// </summary>
    
    public class MRPSetupMaint : PXGraph<MRPSetupMaint>
    {
        public PXSelect<AMRPSetup> setup;
        public PXSave<AMRPSetup> Save;
        public PXCancel<AMRPSetup> Cancel;

        protected virtual void AMRPSetup_ExceptionDaysAfter_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
        {
            var amrpSetup = (AMRPSetup)e.Row;
            if ( (int)e.NewValue > amrpSetup.GracePeriod)
            {
                throw new PXSetPropertyException(Messages.GetLocal(AM.Messages.MrpSetupExceptionWindow,
                    PXUIFieldAttribute.GetDisplayName<AMRPSetup.exceptionDaysAfter>(setup.Cache),
                    PXUIFieldAttribute.GetDisplayName<AMRPSetup.gracePeriod>(setup.Cache)));
            }
        }

        protected virtual void AMRPSetup_GracePeriod_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
        {
            var amrpSetup = (AMRPSetup)e.Row;
            if ((int)e.NewValue < amrpSetup.ExceptionDaysAfter)
            {
                throw new PXSetPropertyException(Messages.GetLocal(AM.Messages.MrpSetupExceptionWindow,
                    PXUIFieldAttribute.GetDisplayName<AMRPSetup.exceptionDaysAfter>(setup.Cache),
                    PXUIFieldAttribute.GetDisplayName<AMRPSetup.gracePeriod>(setup.Cache)));
            }
        }

        protected virtual void AMRPSetup_UseFixMfgLeadTime_FieldVerifying(PXCache cache, PXFieldVerifyingEventArgs e)
        {
            var amrpSetup = (AMRPSetup)e.Row;
            if (amrpSetup == null || (bool?) e.NewValue != true)
            {
                return;
            }

            var ampSetup = (AMPSetup)PXSelect<AMPSetup>.Select(this);
            if (ampSetup?.FixMfgCalendarID != null)
            {
                return;
            }

            throw new PXSetPropertyException(Messages.GetLocal(Messages.MrpFixedLeadTimeRequiresProdPreferencesCalendar));
        }

		protected virtual void _(Events.FieldUpdated<AMRPSetup, AMRPSetup.useDaysSupplytoConsolidateOrders> e)
		{
			if ((bool?)e.NewValue == false && e.Row.UseLongTermConsolidationBucket == true)
			{
				e.Cache.SetValueExt<AMRPSetup.useLongTermConsolidationBucket>(e.Row, false);
			}
		}

		protected virtual void _(Events.RowSelected<AMRPSetup> e)
		{
			bool enableFields = e.Row != null && e.Row.UseDaysSupplytoConsolidateOrders == true;
			bool hideFields = e.Row != null && e.Row.UseLongTermConsolidationBucket == true;

			PXUIFieldAttribute.SetEnabled<AMRPSetup.useLongTermConsolidationBucket>(e.Cache, e.Row, enableFields);
			PXUIFieldAttribute.SetVisible<AMRPSetup.consolidateAfterDays>(e.Cache, e.Row, hideFields);
			PXUIFieldAttribute.SetVisible<AMRPSetup.bucketDays>(e.Cache, e.Row, hideFields);
		}
	}

}
