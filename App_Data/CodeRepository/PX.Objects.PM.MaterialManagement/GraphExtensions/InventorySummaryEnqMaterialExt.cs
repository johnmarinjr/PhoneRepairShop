using PX.Data;
using PX.Objects.IN;
using System;
using System.Collections.Generic;

namespace PX.Objects.PM.MaterialManagement
{
    public class InventorySummaryEnqMaterialExt : PXGraphExtension<InventorySummaryEnq>
	{		
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
		}

		[PXOverride]
		public virtual void AppendCostLocationLayerJoin(PXSelectBase<INLocationStatus> cmd, Action<PXSelectBase<INLocationStatus>> baseMethod)
		{
			cmd.Join<LeftJoin<PMLocationCostStatus,
					On<PMLocationCostStatus.inventoryID, Equal<INLocationStatus.inventoryID>,
						And<PMLocationCostStatus.subItemID, Equal<INLocationStatus.subItemID>,
						And<PMLocationCostStatus.locationID, Equal<INLocationStatus.locationID>>>>>>();
		}

		[PXOverride]
		public virtual List<Type> GetCostTables(Func<List<Type>> baseMethod)
		{
			List<Type> result = baseMethod();
			result.Add(typeof(PMLocationCostStatus));
			return result;
		}

		[PXOverride]
		public virtual ICostStatus GetCostStatus(PXResult res, Func<PXResult, ICostStatus> baseMethod)
		{
			ICostStatus result = baseMethod(res);

			PMLocationCostStatus plcs = PXResult.Unwrap<PMLocationCostStatus>(res);
			if (plcs != null && plcs.InventoryID != null)
			{
				if (result != null)
				{
					result.QtyOnHand += plcs.QtyOnHand.GetValueOrDefault();
					result.TotalCost += plcs.TotalCost.GetValueOrDefault();
				}
                else
                {
					result = plcs;
				}
			}

			return result;
		}
	}

	[PXProjection(typeof(Select5<INCostStatus,
		InnerJoin<PMCostCenter,
			On<PMCostCenter.costCenterID, Equal<INCostStatus.costSiteID>>,
		InnerJoin<INCostSubItemXRef, On<INCostSubItemXRef.costSubItemID, Equal<INCostStatus.costSubItemID>>>>,
		Aggregate<GroupBy<INCostStatus.inventoryID, 
			GroupBy<INCostSubItemXRef.subItemID, 
			GroupBy<PMCostCenter.locationID, 
			Sum<INCostStatus.qtyOnHand, 
			Sum<INCostStatus.totalCost>>>>>>>))]
	[Serializable]
	public partial class PMLocationCostStatus : IBqlTable, ICostStatus
	{
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		[PXDBInt(IsKey = true, BqlField = typeof(INCostStatus.inventoryID))]
		[PXDefault()]
		public virtual Int32? InventoryID
		{
			get;
			set;
		}
		#endregion
		#region SubItemID
		public abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		[PXDBInt(IsKey = true, BqlField = typeof(INCostSubItemXRef.subItemID))]
		public virtual Int32? SubItemID
		{
			get;
			set;
		}
		#endregion
		#region LocationID
		public abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		[PXDBInt(IsKey = true, BqlField = typeof(PMCostCenter.locationID))]
		public virtual Int32? LocationID
		{
			get;
			set;
		}
		#endregion
		#region QtyOnHand
		public abstract class qtyOnHand : PX.Data.BQL.BqlDecimal.Field<qtyOnHand> { }
		[PXDBQuantity(BqlField = typeof(INCostStatus.qtyOnHand))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? QtyOnHand
		{
			get;
			set;
		}
		#endregion
		#region TotalCost
		public abstract class totalCost : PX.Data.BQL.BqlDecimal.Field<totalCost> { }
		[CM.PXDBBaseCury(BqlField = typeof(INCostStatus.totalCost))]
		public virtual Decimal? TotalCost
		{
			get;
			set;
		}
		#endregion
		
	}
}
