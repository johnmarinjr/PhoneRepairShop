using PX.Api.ContractBased.Models;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Commerce.Shopify.API.REST;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.Shopify
{
	public abstract class SPOrderBaseProcessor<TGraph, TEntityBucket, TPrimaryMapped> : BCProcessorSingleBase<TGraph, TEntityBucket, TPrimaryMapped>, IProcessor
		where TGraph : PXGraph
		where TEntityBucket : class, IEntityBucket, new()
		where TPrimaryMapped : class, IMappedEntity, new()
	{
		public SPHelper helper = PXGraph.CreateInstance<SPHelper>();

		protected InventoryItem refundItem;

		public override void Initialise(IConnector iconnector, ConnectorOperation operation)
		{
			base.Initialise(iconnector, operation);

			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			refundItem = bindingExt.RefundAmountItemID != null ? PX.Objects.IN.InventoryItem.PK.Find((PXGraph)this, bindingExt.RefundAmountItemID) : throw new PXException(ShopifyMessages.NoRefundItem);

			helper.Initialize(this);
		}

		#region Refunds
		public virtual SalesOrderDetail InsertRefundAmountItem(decimal amount, StringValue branch)
		{
			decimal quantity = 1;
			BCBindingExt bindingExt = GetBindingExt<BCBindingExt>();
			SalesOrderDetail detail = new SalesOrderDetail();
			detail.InventoryID = refundItem.InventoryCD?.TrimEnd().ValueField();
			detail.OrderQty = quantity.ValueField();
			detail.UOM = refundItem.BaseUnit.ValueField();
			detail.Branch = branch;
			detail.UnitPrice = amount.ValueField();
			detail.ManualPrice = true.ValueField();
			detail.ReasonCode = bindingExt?.ReasonCode?.ValueField();
			return detail;

		}

		#endregion

		protected string DetermineTaxName(OrderData data, OrderTaxLine tax)
		{
			string TaxName = tax.TaxName;
			OrderAddressData taxAddress = data.ShippingAddress ?? data.BillingAddress ?? new OrderAddressData();
			string taxNameWithLocation = TaxName + (taxAddress.CountryCode ?? String.Empty) + (taxAddress.ProvinceCode ?? String.Empty);
			string mappedTaxName = null;
			//Check substituion list for taxCodeWithLocation
			mappedTaxName = helper.GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxSubstitutionListID, taxNameWithLocation, null);
			if (mappedTaxName is null)
			{
				//If not found check taxCodes for taxCodeWithLocation
				helper.TaxCodes.TryGetValue(taxNameWithLocation, out mappedTaxName);
			}
			if (mappedTaxName is null)
			{
				//If not found check substitution list for taxName
				mappedTaxName = helper.GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxSubstitutionListID, TaxName, null);
			}
			if (mappedTaxName is null)
			{
				//if not found just use tax name
				mappedTaxName = TaxName;
			}
			//Trim found tax name
			mappedTaxName = helper.TrimAutomaticTaxNameForAvalara(mappedTaxName);
			//check substitution list for trimmed tax name, otherwise use trimmed tax name 
			mappedTaxName = helper.GetSubstituteLocalByExtern(GetBindingExt<BCBindingExt>().TaxSubstitutionListID, mappedTaxName, mappedTaxName);
			return mappedTaxName;
		}
	}
}
