using System;
using System.Linq;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.AM.GraphExtensions
{
	[Serializable]
	public class INIntegrityCheckAMExtension : PXGraphExtension<INIntegrityCheck>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>();
		}

		public delegate void DeleteOrphanedItemPlansDelegate(INItemSiteSummary itemsite);

		[PXOverride]
		public virtual void DeleteOrphanedItemPlans(INItemSiteSummary itemsite, DeleteOrphanedItemPlansDelegate del)
		{
			del?.Invoke(itemsite);

			foreach (INItemPlan p in PXSelectReadonly2<INItemPlan,
			InnerJoin<AMProdItem, On<AMProdItem.noteID, Equal<INItemPlan.refNoteID>>,
			LeftJoin<AMProdItemSplit, On<AMProdItemSplit.orderType, Equal<AMProdItem.orderType>,
				And<AMProdItemSplit.prodOrdID, Equal<AMProdItem.prodOrdID>,
				And<AMProdItemSplit.planID, Equal<INItemPlan.planID>>>>>>,
				Where<AMProdItemSplit.prodOrdID, IsNull,
				And<INItemPlan.inventoryID, Equal<Current<INItemSiteSummary.inventoryID>>,
					And<INItemPlan.siteID, Equal<Current<INItemSiteSummary.siteID>>>>>>.SelectMultiBound(Base, new object[] { itemsite }))
			{
				PXDatabase.Delete<INItemPlan>(new PXDataFieldRestrict("PlanID", PXDbType.BigInt, 8, p.PlanID, PXComp.EQ));
			}

			foreach (INItemPlan p in PXSelectReadonly2<INItemPlan,
			InnerJoin<AMProdOper, On<AMProdOper.noteID, Equal<INItemPlan.refNoteID>>,
			LeftJoin<AMProdMatlSplit, On<AMProdMatlSplit.orderType, Equal<AMProdOper.orderType>,
				And<AMProdMatlSplit.prodOrdID, Equal<AMProdOper.prodOrdID>,
				And<AMProdMatlSplit.operationID, Equal<AMProdOper.operationID>,
				And<AMProdMatlSplit.planID, Equal<INItemPlan.planID>>>>>>>,
				Where<AMProdMatlSplit.prodOrdID, IsNull,
				And<INItemPlan.inventoryID, Equal<Current<INItemSiteSummary.inventoryID>>,
					And<INItemPlan.siteID, Equal<Current<INItemSiteSummary.siteID>>>>>>.SelectMultiBound(Base, new object[] { itemsite }))
			{
				PXDatabase.Delete<INItemPlan>(new PXDataFieldRestrict("PlanID", PXDbType.BigInt, 8, p.PlanID, PXComp.EQ));
			}
		}

		[PXOverride]
		public virtual Type[] GetParentDocumentsNoteFields(Func<Type[]> del)
		{
			var ret = del?.Invoke();
			if(ret == null)
			{
				return ret;
			}

			// Cannot use the same for material because the plan record points to AMProdOper noteid but reftype still shows AMProdMatl
			return ret.Append(typeof(AMProdItem.noteID)).ToArray();
		}

	}
}
