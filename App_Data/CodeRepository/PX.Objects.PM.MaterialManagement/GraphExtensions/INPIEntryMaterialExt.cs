using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM.MaterialManagement
{
	public class INPIEntryMaterialExt : PXGraphExtension<INPIEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
		}


		[PXOverride]
		public virtual IEnumerable<INCostStatus> ReadCostLayers(INPIDetail detail, Func<INPIDetail, IEnumerable<INCostStatus>> baseMethod)
		{
			List<INCostStatus> list = new List<INCostStatus>();
			list.AddRange(baseMethod(detail));
			list.AddRange(ReadCostLayersFromProjectCostCenter(detail));

			return list;
		}

		[PXOverride]
		public virtual INPIEntry.ProjectedTranRec CreateProjectedTran(INCostStatus costLayer, INPIDetail line, INItemSiteSettings itemSiteSettings,
			Func<INCostStatus, INPIDetail, INItemSiteSettings, INPIEntry.ProjectedTranRec> baseMethod)
		{
			INPIEntry.ProjectedTranRec tran = baseMethod(costLayer, line, itemSiteSettings);
			PMCostCenter costCenter = PMCostCenter.PK.Find(Base, costLayer.CostSiteID);
			if (costCenter != null)
            {
				tran.ProjectID = costCenter.ProjectID;
				tran.TaskID = costCenter.TaskID;
				tran.CostCenterID = costCenter.CostCenterID;
			}

			INLocation location = INLocation.PK.Find(Base, line.LocationID);
			if (location != null && location.TaskID != null)
            {
				tran.ProjectID = location.ProjectID;
				tran.TaskID = location.TaskID;
            }

			return tran;
		}

		private IEnumerable<INCostStatus> ReadCostLayersFromProjectCostCenter(INPIDetail detail)
		{
			var select = new PXSelectJoin<INCostStatus,
				InnerJoin<PMCostCenter, On<INCostStatus.costSiteID, Equal<PMCostCenter.costCenterID>>,
				InnerJoin<INCostSubItemXRef, On<INCostSubItemXRef.costSubItemID, Equal<INCostStatus.costSubItemID>>>>,
				Where<INCostStatus.inventoryID, Equal<Required<INCostStatus.inventoryID>>,
				And<INCostStatus.qtyOnHand, Greater<decimal0>,
				And<INCostSubItemXRef.subItemID, Equal<Required<INCostSubItemXRef.subItemID>>,
				And<PMCostCenter.siteID, Equal<Required<PMCostCenter.siteID>>,
				And<PMCostCenter.locationID, Equal<Required<PMCostCenter.locationID>>,
				And<Where<INCostStatus.lotSerialNbr, Equal<Required<INCostStatus.lotSerialNbr>>,
					Or<INCostStatus.lotSerialNbr, IsNull,
					Or<INCostStatus.lotSerialNbr, Equal<Empty>>>>>>>>>>>(Base);

			return select.Select(detail.InventoryID, detail.SubItemID, detail.SiteID, detail.LocationID, detail.LotSerialNbr).RowCast<INCostStatus>();
		}
	}
}
