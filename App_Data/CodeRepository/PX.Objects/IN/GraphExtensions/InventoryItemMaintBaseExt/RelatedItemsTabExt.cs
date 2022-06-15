using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN.RelatedItems;
using System;

namespace PX.Objects.IN.GraphExtensions.InventoryItemMaintBaseExt
{
	public class RelatedItemsTabExt: RelatedItemsTab<InventoryItemMaintBase>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.relatedItems>() || PXAccess.FeatureInstalled<FeaturesSet.commerceIntegration>();

		/// <summary>
		/// Overrides <see cref="InventoryItemMaintBase.Persist"/>
		/// </summary>
		/// <param name="baseImpl"></param>
		[PXOverride]
		public virtual void Persist(Action baseImpl)
        {
			CheckForDuplicates();

			baseImpl();
        }
	}
}
