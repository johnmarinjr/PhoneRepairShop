using PX.Data;
using PX.Objects.CS;
using static PX.Objects.FA.TransferProcess;

namespace PX.Objects.FA
{
	public class TransferProcessMultipleBaseCurrencies : PXGraphExtension<TransferProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		protected virtual void _(Events.FieldUpdating<TransferFilter, TransferFilter.branchTo> e)
		{
			if (e.Row == null) return;

			TransferFilter filter = e.Row;

			if (e.Cache.GetValuePending<TransferFilter.branchFrom>(filter) != PXCache.NotSetValue)
			{
				object branchFrom = PXAccess.GetBranchID((string)e.Cache.GetValuePending<TransferFilter.branchFrom>(filter));
				object branchTo = PXAccess.GetBranchID((string)e.NewValue);

				if (branchTo == null || PXAccess.GetBranch((int?)branchFrom)?.BaseCuryID == PXAccess.GetBranch((int?)branchTo)?.BaseCuryID)
				{
					try
					{
						e.Cache.SetValue<TransferFilter.branchTo>(filter, branchTo);
						e.Cache.SetValueExt<TransferFilter.branchFrom>(filter, branchFrom);
						e.Cache.RaiseExceptionHandling<TransferFilter.branchFrom>(filter, branchFrom, null);
						e.Cache.RaiseFieldVerifying<TransferFilter.branchFrom>(filter, ref branchFrom);
					}
					catch (PXSetPropertyException ex)
					{
						e.Cache.RaiseExceptionHandling<TransferFilter.branchFrom>(filter, branchFrom, ex);
					}
				}
			}
		}
	}
}


