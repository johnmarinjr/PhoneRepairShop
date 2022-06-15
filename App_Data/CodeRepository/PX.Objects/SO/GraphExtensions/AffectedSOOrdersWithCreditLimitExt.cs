using PX.Data;
using PX.Objects.Extensions;

namespace PX.Objects.SO.GraphExtensions
{
	public class AffectedSOOrdersWithCreditLimitExtBase<TGraph> : ProcessAffectedEntitiesInPrimaryGraphBase<AffectedSOOrdersWithCreditLimitExtBase<TGraph>, TGraph, SOOrder, SOOrderEntry>
		where TGraph : PXGraph
	{
		private PXCache<SOOrder> orders => Base.Caches<SOOrder>();
		protected override bool PersistInSameTransaction => false;

		protected override bool EntityIsAffected(SOOrder order)
		{
			bool? originalFullyPaid = (bool?)orders.GetValueOriginal<SOOrder.isFullyPaid>(order);

			return (originalFullyPaid != order.IsFullyPaid);
		}

		protected override void ProcessAffectedEntity(SOOrderEntry orderEntry, SOOrder order)
		{
			if (order.CreditHold == true && order.IsFullyPaid == true)
			{
				order.SatisfyCreditLimitByPayment(orderEntry);
			}
			else if (order.CreditHold != true && order.IsFullyPaid != true)
			{
				RunCreditLimitVerification(orderEntry, order);
			}
		}

		protected virtual void RunCreditLimitVerification(SOOrderEntry orderEntry, SOOrder order)
		{
			orderEntry.Document.Update(order);
		}

		protected override SOOrder ActualizeEntity(SOOrderEntry orderEntry, SOOrder order)
			=> orderEntry.Document.Search<SOOrder.orderNbr>(order.OrderNbr, order.OrderType);
	}
}
