using PX.Data;
using PX.Objects.AR;
using PX.Objects.GL;
using System;
using System.Collections.Generic;
using PX.Objects.CM.Extensions;

namespace PX.Objects.CT
{
	public class GetItemPriceValue<ContractID, ContractItemID, ItemType, ItemPriceType, ItemID, FixedPrice, SetupPrice, Qty, PriceDate> : BqlFormulaEvaluator<ContractID, ContractItemID, ItemType, ItemPriceType, ItemID, FixedPrice, SetupPrice, Qty, PriceDate>
		where ContractID : IBqlOperand
		where ContractItemID : IBqlOperand
		where ItemType : IBqlOperand
		where ItemPriceType : IBqlOperand
		where ItemID : IBqlOperand
		where FixedPrice : IBqlOperand
		where SetupPrice : IBqlOperand
		where Qty : IBqlOperand
		where PriceDate : IBqlOperand
	{
		public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> pars)
		{
			int? contractID = (int?)pars[typeof(ContractID)];
			string priceOption = (string)pars[typeof(ItemPriceType)];
			string itemType = (string)pars[typeof(ItemType)];
			int? contractItemID = (int?)pars[typeof(ContractItemID)];
			int? itemID = (int?)pars[typeof(ItemID)];
			decimal? fixedPrice = (decimal?)pars[typeof(FixedPrice)];
			decimal? setupPrice = (decimal?)pars[typeof(SetupPrice)];
			decimal? qty = (decimal?)pars[typeof(Qty)];
			DateTime? date = (DateTime?)pars[typeof(PriceDate)];

			PXResult<Contract, ContractBillingSchedule> customerContract = (PXResult<Contract, ContractBillingSchedule>)PXSelectJoin<
				Contract,
					LeftJoin<ContractBillingSchedule,
						On<ContractBillingSchedule.contractID, Equal<Contract.contractID>>>,
				Where<
					Contract.contractID, Equal<Required<Contract.contractID>>>>
				.Select(cache.Graph, contractID);

			if (customerContract == null) return null;

			Contract contract = customerContract;
			ContractBillingSchedule billingSchedule = customerContract;

			return GetItemPrice(cache,
				contract.CuryID,
				billingSchedule.AccountID ?? contract.CustomerID,
				billingSchedule.LocationID ?? contract.LocationID,
				contract.Status,
				contractItemID,
				itemID,
				itemType,
				priceOption,
				fixedPrice,
				setupPrice,
				qty,
				date);
		}

		public virtual decimal GetItemPrice(PXCache sender, string curyID, int? customerID, int? locationID, string contractStatus, int? contractItemID, int? itemID, string itemType, string priceOption, decimal? fixedPrice, decimal? setupPrice, decimal? qty, DateTime? date)
		{
			ContractItem item = PXSelect<ContractItem, Where<ContractItem.contractItemID, Equal<Required<ContractItem.contractItemID>>>>.Select(sender.Graph, contractItemID);
			if (item == null) return 0m;
			else
			{
				IN.InventoryItem nonstock = PXSelect<IN.InventoryItem, Where<IN.InventoryItem.inventoryID, Equal<Required<IN.InventoryItem.inventoryID>>>>.Select(sender.Graph, itemID);

				CR.Location customerLocation = PXSelect<
					CR.Location,
					Where<
						CR.Location.bAccountID, Equal<Required<Contract.customerID>>,
						And<CR.Location.locationID, Equal<Required<Contract.locationID>>>>>
					.Select(sender.Graph, customerID, locationID);

				string customerPriceClass = string.IsNullOrEmpty(customerLocation?.CPriceClassID)
					? ARPriceClass.EmptyPriceClass
					: customerLocation.CPriceClassID;

				string taxCalcMode = customerLocation?.CTaxCalcMode ?? TX.TaxCalculationMode.TaxSetting;

				CurrencyInfo currencyInfo = new CurrencyInfo
				{
					BaseCuryID = new PXSetup<Company>(sender.Graph).Current.BaseCuryID,
					CuryID = curyID,
					CuryEffDate = date
				};
				Customer customer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.Select(sender.Graph, customerID);
				if (customer != null && customer.CuryRateTypeID != null)
					currencyInfo.CuryRateTypeID = customer.CuryRateTypeID;

				currencyInfo.SearchForNewRate(sender.Graph)?.Populate(currencyInfo);

				if (nonstock != null && currencyInfo != null)
				{
					switch (priceOption ?? GetPriceOptionFromItem(itemType, item))
					{
						case PriceOption.ItemPrice:
							return ARSalesPriceMaint.CalculateSalesPrice(sender, customerPriceClass, customerID, itemID, currencyInfo.GetCM(), qty, nonstock.BaseUnit, date ?? DateTime.Now, false, taxCalcMode) ?? 0m;
						case PriceOption.ItemPercent:
							return (ARSalesPriceMaint.CalculateSalesPrice(sender, customerPriceClass, customerID, itemID, currencyInfo.GetCM(), qty, nonstock.BaseUnit, date ?? DateTime.Now, false, taxCalcMode) ?? 0m) / 100m * (fixedPrice ?? 0m);
						case PriceOption.BasePercent:
							return (setupPrice ?? 0m) / 100m * (fixedPrice ?? 0m);
						case PriceOption.Manually:
							return fixedPrice ?? 0m;
						default: throw new InvalidOperationException("Unexpected Price Option: " + priceOption);
					}
				}
				else return 0m;
			}
		}

		private static string GetPriceOptionFromItem(string itemType, ContractItem item)
		{
			switch (itemType)
			{
				case ContractDetailType.Setup:
					return item.BasePriceOption;
				case ContractDetailType.Renewal:
					return item.RenewalPriceOption;
				case ContractDetailType.Billing:
					return item.FixedRecurringPriceOption;
				case ContractDetailType.UsagePrice:
					return item.UsagePriceOption;
				default: throw new InvalidOperationException("Unexpected Item Type: " + itemType);
			}
		}
	}
}
