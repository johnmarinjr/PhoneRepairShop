using System;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.IN;
using static PX.Objects.AP.APVendorPriceMaint;

namespace PX.Objects.AP
{
	public class APVendorPriceMaintTemplateItemExtension : PXGraphExtension<APVendorPriceMaint>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<PX.Objects.CS.FeaturesSet.matrixItem>();

		[Obsolete]
		public delegate BqlCommand CreateUnitCostSelectCommandOrig(bool isBaseUOM);
		[PXOverride]
		[Obsolete]
		public virtual BqlCommand CreateUnitCostSelectCommand(bool isBaseUOM, CreateUnitCostSelectCommandOrig baseMethod)
		{
			return baseMethod(isBaseUOM).OrderByNew(typeof(OrderBy<Desc<APVendorPrice.isPromotionalPrice,
						Desc<APVendorPrice.siteID,
						Desc<APVendorPrice.vendorID,
						Asc<InventoryItem.isTemplate,
						Desc<APVendorPrice.breakQty>>>>>>));
		}

		public delegate BqlCommand CreateUnitCostSelectCommandParameterLessOrig();
		[PXOverride]
		public virtual BqlCommand CreateUnitCostSelectCommand(CreateUnitCostSelectCommandParameterLessOrig baseMethod)
		{
			return baseMethod().OrderByNew(typeof(OrderBy<Desc<APVendorPrice.isPromotionalPrice,
						Desc<APVendorPrice.siteID,
						Desc<APVendorPrice.vendorID,
						Asc<InventoryItem.isTemplate,
						Desc<APVendorPrice.breakQty>>>>>>));
		}

		public delegate int?[] GetInventoryIDsOrig(PXCache sender, int? inventoryID);
		[PXOverride]
		public virtual int?[] GetInventoryIDs(PXCache sender, int? inventoryID, GetInventoryIDsOrig baseMethod)
		{
			int? templateInventoryID = InventoryItem.PK.Find(sender.Graph, inventoryID)?.TemplateItemID;

			return templateInventoryID != null ? new int?[] { inventoryID, templateInventoryID } : baseMethod(sender, inventoryID);
		}
	}
}
