using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;

namespace PX.Objects.DR
{
	public class InventoryItemMaintBaseASC606 : PXGraphExtension<InventoryItemMaintBase>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.aSC606>();
		}

		protected virtual void _(Events.FieldUpdated<INComponent, INComponent.deferredCode> e)
		{
			if (e.Row == null)
				return;

			DRDeferredCode code = (DRDeferredCode)PXSelectorAttribute.Select(e.Cache, e.Row, typeof(INComponent.deferredCode).Name);
			if (code != null)
			{
				e.Cache.SetValueExt< INComponent.overrideDefaultTerm>(e.Row, DeferredMethodType.RequiresTerms(code));
			}
			else
			{
				e.Cache.SetValueExt<INComponent.overrideDefaultTerm>(e.Row, false);
			}
		}

		protected virtual void _(Events.FieldUpdated<INComponent, INComponent.componentID> e)
		{
			if (e.Row == null)
				return;

			DRDeferredCode code = (DRDeferredCode)PXSelectorAttribute.Select(e.Cache, e.Row, typeof(INComponent.deferredCode).Name);
			if (code != null)
			{
				e.Cache.SetValueExt<INComponent.overrideDefaultTerm>(e.Row, DeferredMethodType.RequiresTerms(code));
			}
			else
			{
				e.Cache.SetValueExt<INComponent.overrideDefaultTerm>(e.Row, false);
			}
		}
	}
}
