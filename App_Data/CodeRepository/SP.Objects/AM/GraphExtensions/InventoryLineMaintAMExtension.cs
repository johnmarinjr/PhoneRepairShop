using System;
using PX.Data;
using SP.Objects.IN;

namespace SP.Objects.AM.GraphExtensions
{
	[Serializable]
	public class InventoryLineMaintAMExtension : PortalCardLinesConfigurationBase<InventoryLineMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<PX.Objects.CS.FeaturesSet.manufacturingProductConfigurator>();
		}

		public delegate void PersistCardLinesDelegate(bool inserted, bool updated);

		[PXOverride]
		public virtual void PersistCardLines(bool inserted, bool updated, PersistCardLinesDelegate del)
		{
			del?.Invoke(inserted, updated);

			SetPersistedConfigurations();
		}

		[PXOverride]
		public virtual void PersistCardLinesInTranScope(bool inserted, bool updated, PersistCardLinesDelegate del)
		{
			del?.Invoke(inserted, updated);

			PersistConfigurations();
		}

		protected override void InsertConfigurationResult(PXCache sender, PortalCardLines row)
		{
			InsertConfigurationResult(sender, row, Base.GetCurrencyInfo(), Base.currentCustomer.BAccountID, Base.currentCustomer.DefLocationID);
		}
	}
}
