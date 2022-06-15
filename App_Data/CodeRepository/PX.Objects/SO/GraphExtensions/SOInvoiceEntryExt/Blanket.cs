using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.SO.Attributes;
using PX.Objects.SO.DAC.Projections;

namespace PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt
{
	public class Blanket : PXGraphExtension<SOInvoiceEntry>
	{
		public override void Initialize()
		{
			base.Initialize();

			Base.EnsureCachePersistence<BlanketSOOrder>();
			Base.EnsureCachePersistence<BlanketSOLine>();
			Base.EnsureCachePersistence<BlanketSOLineSplit>();
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXParent(typeof(Select<BlanketSOLine,
			Where<BlanketSOLine.orderType, Equal<Current<ARTran.blanketType>>,
				And<BlanketSOLine.orderNbr, Equal<Current<ARTran.blanketNbr>>,
				And<BlanketSOLine.lineNbr, Equal<Current<ARTran.blanketLineNbr>>>>>>), LeaveChildren = true)]
		public virtual void _(Events.CacheAttached<ARTran.blanketLineNbr> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUnboundFormula(typeof(BaseBilledQtyFormula), typeof(AddCalc<BlanketSOLine.baseBilledQty>))]
		[PXUnboundFormula(typeof(BaseBilledQtyFormula), typeof(SubCalc<BlanketSOLine.baseUnbilledQty>))]
		public virtual void _(Events.CacheAttached<ARTran.baseQty> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXParent(typeof(Select<SOOrder,
			Where<SOOrder.orderType, Equal<Current<BlanketSOLine.orderType>>,
				And<SOOrder.orderNbr, Equal<Current<BlanketSOLine.orderNbr>>>>>))]
		public virtual void _(Events.CacheAttached<BlanketSOLine.orderNbr> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFormula(null, typeof(SumCalc<SOOrder.unbilledOrderQty>))]
		public virtual void _(Events.CacheAttached<BlanketSOLine.unbilledQty> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[BlanketSOUnbilledTax(typeof(SOOrder), typeof(SOTax), typeof(SOTaxTran),
			Inventory = typeof(BlanketSOLine.inventoryID), UOM = typeof(BlanketSOLine.uOM), LineQty = typeof(BlanketSOLine.unbilledQty))]
		public virtual void _(Events.CacheAttached<BlanketSOLine.taxCategoryID> e)
		{
		}
	}
}
