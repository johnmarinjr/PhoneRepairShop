using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.PO.GraphExtensions;
using PX.Objects.Extensions;
using PX.Data;
using System;
using System.Linq;
using System.Collections.Generic;
using PX.Objects.CS;
using PX.Objects.CM.Extensions;
using PX.Objects.Extensions.MultiCurrency;

namespace PX.Objects.AP
{
	public class AffectedPOOrdersByAPRelease : AffectedPOOrdersByPOLineUOpen<AffectedPOOrdersByAPRelease, APReleaseProcess>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		private POOrder[] _affectedOrders;

		[PXOverride]
		public override void Persist(Action basePersist)
		{
			_affectedOrders = GetAffectedEntities().ToArray();

			basePersist();
		}

		/// <summary>
		/// Overrides <see cref="APReleaseProcess.ExtensionsPersist"/>
		/// and <see cref="PO.GraphExtensions.APReleaseProcessExt.UpdatePOOnRelease.ExtensionsPersist"/>
		/// </summary>
		[PXOverride]
		public virtual void ExtensionsPersist(Action baseImpl)
		{
			baseImpl();

			if (_affectedOrders != null)
			{
				ProcessAffectedEntities(_affectedOrders);
				_affectedOrders = null;
			}
		}

		protected override void ProcessAffectedEntity(POOrderEntry primaryGraph, POOrder entity)
		{
			primaryGraph.UpdateDocumentState(entity);
		}

		protected virtual void _(Events.RowUpdated<POLineUOpen> e)
        {
			if (e.Row?.POType != POOrderType.Blanket)
				return;

			if(!e.Cache.ObjectsEqual<POLineUOpen.billedQty, POLineUOpen.curyBilledAmt>(e.Row, e.OldRow))
            {
				UpdateBlanketRow(e.Cache, e.Row, e.OldRow);
			}
		}

		protected virtual void UpdateBlanketRow(PXCache cache, POLineUOpen normalRow, POLineUOpen normalOldRow)
        {
			var blanketRow = new POLineUOpen
			{
				OrderType = normalRow.POType,
				OrderNbr = normalRow.PONbr,
				LineNbr = normalRow.POLineNbr
			};
			blanketRow = (POLineUOpen)cache.Locate(blanketRow)
				?? POLineUOpen.FK.BlanketOrderLine.FindParent(Base, normalRow);

			var normalOrder = PXParentAttribute.SelectParent<POOrder>(cache, normalRow);
			var blanketOrder = PXParentAttribute.SelectParent<POOrder>(cache, blanketRow);

			var billedQtyDiff = normalRow.UOM == blanketRow.UOM
				? normalRow.BilledQty - normalOldRow.BilledQty
				: (blanketRow.InventoryID == null
					? INUnitAttribute.ConvertGlobalUnits(Base, normalRow.UOM, blanketRow.UOM, normalRow.BaseBilledQty - normalOldRow.BaseBilledQty ?? 0, INPrecision.QUANTITY)
					: INUnitAttribute.ConvertFromBase(cache, blanketRow.InventoryID, blanketRow.UOM, normalRow.BaseBilledQty - normalOldRow.BaseBilledQty ?? 0, INPrecision.QUANTITY));
			blanketRow.BilledQty += billedQtyDiff;

			CurrencyInfo blanketCuryInfo = Base.FindImplementation<IPXCurrencyHelper>().GetCurrencyInfo(blanketRow.CuryInfoID);
			if (normalOrder.CuryID == blanketOrder.CuryID)
				blanketRow.CuryBilledAmt += normalRow.CuryBilledAmt - normalOldRow.CuryBilledAmt ?? 0;
			else
				blanketRow.CuryBilledAmt += blanketCuryInfo.CuryConvCury(normalRow.BilledAmt - normalOldRow.BilledAmt ?? 0);

			bool closedBlanketRow;
			if (blanketRow.CompletePOLine == CompletePOLineTypes.Quantity)
			{
				closedBlanketRow = blanketRow.BilledQty >= blanketRow.OrderQty;
			}
			else
			{
				closedBlanketRow = blanketRow.CuryBilledAmt >= blanketRow.CuryExtCost + blanketRow.CuryRetainageAmt;
			}
			if(closedBlanketRow != blanketRow.Closed)
			{
				blanketRow.Closed = closedBlanketRow;
				if (closedBlanketRow)
					blanketRow.Completed = true;
			}
			cache.Update(blanketRow);
		}
	}
}
