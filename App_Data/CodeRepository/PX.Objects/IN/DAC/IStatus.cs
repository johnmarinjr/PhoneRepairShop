using System;
using PX.Data;

namespace PX.Objects.IN
{
	public interface ICostStatus
	{
		decimal? QtyOnHand { get; set; }
		decimal? TotalCost { get; set; }
	}

	public interface IStatus
	{
		decimal? QtyOnHand { get; set; }
		decimal? QtyAvail { get; set; }
		decimal? QtyNotAvail { get; set; }
		decimal? QtyExpired { get; set; }
		decimal? QtyHardAvail { get; set; }
		decimal? QtyActual { get; set; }
		decimal? QtyINIssues { get; set; }
		decimal? QtyINReceipts { get; set; }
		decimal? QtyInTransit { get; set; }
		decimal? QtyPOPrepared { get; set; }
		decimal? QtyPOOrders { get; set; }
		decimal? QtyPOReceipts { get; set; }

		decimal? QtyFSSrvOrdPrepared { get; set; }
		decimal? QtyFSSrvOrdBooked { get; set; }
		decimal? QtyFSSrvOrdAllocated { get; set; }


		decimal? QtySOBackOrdered { get; set; }
		decimal? QtySOPrepared { get; set; }
		decimal? QtySOBooked { get; set; }
		decimal? QtySOShipped { get; set; }
		decimal? QtySOShipping { get; set; }
		decimal? QtyINAssemblySupply { get; set; }
		decimal? QtyINAssemblyDemand { get; set; }
		decimal? QtyInTransitToProduction { get; set; }
		decimal? QtyProductionSupplyPrepared { get; set; }
		decimal? QtyProductionSupply { get; set; }
		decimal? QtyPOFixedProductionPrepared { get; set; }
		decimal? QtyPOFixedProductionOrders { get; set; }
		decimal? QtyProductionDemandPrepared { get; set; }
		decimal? QtyProductionDemand { get; set; }
		decimal? QtyProductionAllocated { get; set; }
		decimal? QtySOFixedProduction { get; set; }
		decimal? QtyProdFixedPurchase { get; set; }
		decimal? QtyProdFixedProduction { get; set; }
		decimal? QtyProdFixedProdOrdersPrepared { get; set; }
		decimal? QtyProdFixedProdOrders { get; set; }
		decimal? QtyProdFixedSalesOrdersPrepared { get; set; }
		decimal? QtyProdFixedSalesOrders { get; set; }

		decimal? QtyFixedFSSrvOrd { get; set; }
		decimal? QtyPOFixedFSSrvOrd { get; set; }
		decimal? QtyPOFixedFSSrvOrdPrepared { get; set; }
		decimal? QtyPOFixedFSSrvOrdReceipts { get; set; }

		decimal? QtySOFixed { get; set; }
		decimal? QtyPOFixedOrders { get; set; }
		decimal? QtyPOFixedPrepared { get; set; }
		decimal? QtyPOFixedReceipts { get; set; }
		decimal? QtySODropShip { get; set; }
		decimal? QtyPODropShipOrders { get; set; }
		decimal? QtyPODropShipPrepared { get; set; }
		decimal? QtyPODropShipReceipts { get; set; }
		decimal? QtyInTransitToSO { get; set; }
		decimal? QtyINReplaned { get; set; }
	}

	public static class INStatusMethods
	{
		public static T Add<T>(this T it, IStatus other)
			where T : IStatus
		{
			if (it == null || other == null)
				return it;

			return ApplyChange(it, it, other, (l, r) => l + r);
		}

		public static T AddToNew<T>(this IStatus it, IStatus other)
			where T : class, IBqlTable, IStatus, new()
		{
			T result = new T();
			if (typeof(T).IsAssignableFrom(it.GetType()))
			{
				PXCache<T>.RestoreCopy(result, (T)it);
				return result.Add(other);
			}

			return ApplyChange(result, it, other, (l, r) => l + r);
		}

		public static T Subtract<T>(this T it, IStatus other)
			where T : IStatus
		{
			if (it == null || other == null)
				return it;

			return ApplyChange(it, it, other, (l, r) => l - r);
		}

		public static T SubtractToNew<T>(this IStatus it, IStatus other)
			where T : class, IBqlTable, IStatus, new()
		{
			T result = new T();
			if (typeof(T).IsAssignableFrom(it.GetType()))
			{
				PXCache<T>.RestoreCopy(result, (T)it);
				return result.Subtract(other);
			}

			return ApplyChange(result, it, other, (l, r) => l - r);
		}

		public static T Multiply<T>(this T it, decimal unitRate)
			where T : IStatus
		{
			if (it == null)
				return it;

			return ApplyChange(it, it, it, (l, r) => l * unitRate);
		}

		public static T OverrideBy<T>(this T it, IStatus other)
			where T : IStatus
		{
			if (it == null || other == null)
				return it;

			return ApplyChange(it, other, other, (l, r) => l);
		}

		public static T CopyToNew<T>(this IStatus source)
			where T : class, IBqlTable, IStatus, new()
		{
			return new T().OverrideBy(source);
		}

		private static T ApplyChange<T>(T result, IStatus left, IStatus right, Func<decimal, decimal, decimal> operation)
			where T : IStatus
		{
			decimal opSafe(decimal? l, decimal? r)
				=> Math.Round(operation(l ?? 0m, r ?? 0m), CommonSetupDecPl.Qty, MidpointRounding.AwayFromZero);

			result.QtyOnHand = opSafe(left.QtyOnHand, right.QtyOnHand);
			result.QtyAvail = opSafe(left.QtyAvail, right.QtyAvail);
			result.QtyNotAvail = opSafe(left.QtyNotAvail, right.QtyNotAvail);
			result.QtyExpired = opSafe(left.QtyExpired, right.QtyExpired);
			result.QtyHardAvail = opSafe(left.QtyHardAvail, right.QtyHardAvail);
			result.QtyActual = opSafe(left.QtyActual, right.QtyActual);
			result.QtyINIssues = opSafe(left.QtyINIssues, right.QtyINIssues);
			result.QtyINReceipts = opSafe(left.QtyINReceipts, right.QtyINReceipts);
			result.QtyInTransit = opSafe(left.QtyInTransit, right.QtyInTransit);
			result.QtyPOPrepared = opSafe(left.QtyPOPrepared, right.QtyPOPrepared);
			result.QtyPOOrders = opSafe(left.QtyPOOrders, right.QtyPOOrders);
			result.QtyPOReceipts = opSafe(left.QtyPOReceipts, right.QtyPOReceipts);
			result.QtyFSSrvOrdPrepared = opSafe(left.QtyFSSrvOrdPrepared, right.QtyFSSrvOrdPrepared);
			result.QtyFSSrvOrdBooked = opSafe(left.QtyFSSrvOrdBooked, right.QtyFSSrvOrdBooked);
			result.QtyFSSrvOrdAllocated = opSafe(left.QtyFSSrvOrdAllocated, right.QtyFSSrvOrdAllocated);
			result.QtySOBackOrdered = opSafe(left.QtySOBackOrdered, right.QtySOBackOrdered);
			result.QtySOPrepared = opSafe(left.QtySOPrepared, right.QtySOPrepared);
			result.QtySOBooked = opSafe(left.QtySOBooked, right.QtySOBooked);
			result.QtySOShipped = opSafe(left.QtySOShipped, right.QtySOShipped);
			result.QtySOShipping = opSafe(left.QtySOShipping, right.QtySOShipping);
			result.QtyINAssemblySupply = opSafe(left.QtyINAssemblySupply, right.QtyINAssemblySupply);
			result.QtyINAssemblyDemand = opSafe(left.QtyINAssemblyDemand, right.QtyINAssemblyDemand);
			result.QtyInTransitToProduction = opSafe(left.QtyInTransitToProduction, right.QtyInTransitToProduction);
			result.QtyProductionSupplyPrepared = opSafe(left.QtyProductionSupplyPrepared, right.QtyProductionSupplyPrepared);
			result.QtyProductionSupply = opSafe(left.QtyProductionSupply, right.QtyProductionSupply);
			result.QtyPOFixedProductionPrepared = opSafe(left.QtyPOFixedProductionPrepared, right.QtyPOFixedProductionPrepared);
			result.QtyPOFixedProductionOrders = opSafe(left.QtyPOFixedProductionOrders, right.QtyPOFixedProductionOrders);
			result.QtyProductionDemandPrepared = opSafe(left.QtyProductionDemandPrepared, right.QtyProductionDemandPrepared);
			result.QtyProductionDemand = opSafe(left.QtyProductionDemand, right.QtyProductionDemand);
			result.QtyProductionAllocated = opSafe(left.QtyProductionAllocated, right.QtyProductionAllocated);
			result.QtySOFixedProduction = opSafe(left.QtySOFixedProduction, right.QtySOFixedProduction);
			result.QtyProdFixedPurchase = opSafe(left.QtyProdFixedPurchase, right.QtyProdFixedPurchase);
			result.QtyProdFixedProduction = opSafe(left.QtyProdFixedProduction, right.QtyProdFixedProduction);
			result.QtyProdFixedProdOrdersPrepared = opSafe(left.QtyProdFixedProdOrdersPrepared, right.QtyProdFixedProdOrdersPrepared);
			result.QtyProdFixedProdOrders = opSafe(left.QtyProdFixedProdOrders, right.QtyProdFixedProdOrders);
			result.QtyProdFixedSalesOrdersPrepared = opSafe(left.QtyProdFixedSalesOrdersPrepared, right.QtyProdFixedSalesOrdersPrepared);
			result.QtyProdFixedSalesOrders = opSafe(left.QtyProdFixedSalesOrders, right.QtyProdFixedSalesOrders);
			result.QtyFixedFSSrvOrd = opSafe(left.QtyFixedFSSrvOrd, right.QtyFixedFSSrvOrd);
			result.QtyPOFixedFSSrvOrd = opSafe(left.QtyPOFixedFSSrvOrd, right.QtyPOFixedFSSrvOrd);
			result.QtyPOFixedFSSrvOrdPrepared = opSafe(left.QtyPOFixedFSSrvOrdPrepared, right.QtyPOFixedFSSrvOrdPrepared);
			result.QtyPOFixedFSSrvOrdReceipts = opSafe(left.QtyPOFixedFSSrvOrdReceipts, right.QtyPOFixedFSSrvOrdReceipts);
			result.QtySOFixed = opSafe(left.QtySOFixed, right.QtySOFixed);
			result.QtyPOFixedOrders = opSafe(left.QtyPOFixedOrders, right.QtyPOFixedOrders);
			result.QtyPOFixedPrepared = opSafe(left.QtyPOFixedPrepared, right.QtyPOFixedPrepared);
			result.QtyPOFixedReceipts = opSafe(left.QtyPOFixedReceipts, right.QtyPOFixedReceipts);
			result.QtySODropShip = opSafe(left.QtySODropShip, right.QtySODropShip);
			result.QtyPODropShipOrders = opSafe(left.QtyPODropShipOrders, right.QtyPODropShipOrders);
			result.QtyPODropShipPrepared = opSafe(left.QtyPODropShipPrepared, right.QtyPODropShipPrepared);
			result.QtyPODropShipReceipts = opSafe(left.QtyPODropShipReceipts, right.QtyPODropShipReceipts);
			result.QtyInTransitToSO = opSafe(left.QtyInTransitToSO, right.QtyInTransitToSO);
			result.QtyINReplaned = opSafe(left.QtyINReplaned, right.QtyINReplaned);

			if (result is ICostStatus resultCostStatus && left is ICostStatus leftCostStatus && right is ICostStatus rightCostStatus)
				resultCostStatus.TotalCost = opSafe(leftCostStatus.TotalCost, rightCostStatus.TotalCost);

			return result;
		}
	}
}
