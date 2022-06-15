using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.Common.Exceptions;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.RelatedItems;

using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	[PXProtectedAccess]
	public abstract class SOOrderItemAvailabilityRelatedItemsExtension : PXGraphExtension<SOOrderItemAvailabilityExtension, SOOrderEntry>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.relatedItems>();

		/// <summary>
		/// Overrides <see cref="IN.GraphExtensions.ItemAvailabilityExtension{TGraph, TLine, TSplit}.GetCheckErrors(ILSMaster, IStatus)"/>
		/// </summary>
		[PXOverride]
		public virtual IEnumerable<PXExceptionInfo> GetCheckErrors(ILSMaster row, IStatus availability,
			Func<ILSMaster, IStatus, IEnumerable<PXExceptionInfo>> base_GetCheckErrors)
		{
			if (row is SOLine soLine && availability is SiteStatus && !IsAvailableQty(row, availability))
			{
				var substitutableLine = soLine.GetExtension<SubstitutableSOLine>();
				if (substitutableLine?.SuggestRelatedItems == true && substitutableLine.RelatedItemsRelation > 0)
				{
					PXCache<SOLine> lineCache = Base1.LineCache;
					var relatedItemsAttribute = lineCache.GetAttributesOfType<RelatedItemsAttribute>(soLine, nameof(SubstitutableSOLine.RelatedItems)).FirstOrDefault();
					if (relatedItemsAttribute != null)
					{
						var msgInfo = relatedItemsAttribute.QtyMessage(
							lineCache.GetStateExt<SOLine.inventoryID>(soLine),
							lineCache.GetStateExt<SOLine.subItemID>(soLine),
							lineCache.GetStateExt<SOLine.siteID>(soLine),
							(InventoryRelation.RelationType)substitutableLine.RelatedItemsRelation);

						if (msgInfo != null)
							return new[] { new PXExceptionInfo(PXErrorLevel.Warning, msgInfo.MessageFormat, msgInfo.MessageArguments) };
					}
				}
			}

			return base_GetCheckErrors(row, availability);
		}

		/// <summary>
		/// Overrides <see cref="SOOrderItemAvailabilityExtension.RaiseQtyExceptionHandling(SOLine, PXExceptionInfo, decimal?)"/>
		/// </summary>
		[PXOverride]
		public virtual void RaiseQtyExceptionHandling(SOLine line, PXExceptionInfo ei, decimal? newValue,
			Action<SOLine, PXExceptionInfo, decimal?> base_RaiseQtyExceptionHandling)
		{
			if (ei.MessageArguments.Length != 0)
				Base1.LineCache.RaiseExceptionHandling<SOLine.orderQty>(line, newValue,
					new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning, ei.MessageArguments));
			else
				base_RaiseQtyExceptionHandling(line, ei, newValue);
		}

		[PXProtectedAccess(typeof(SOOrderItemAvailabilityExtension))]
		protected abstract bool IsAvailableQty(ILSMaster row, IStatus availability);
	}
}
