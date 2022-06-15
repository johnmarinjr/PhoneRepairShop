using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.IN;
using PX.Objects.PO;
using PX.Objects.SO;
using PX.Objects.CR;
using PX.Objects.CM;
using System;
using System.Collections.Generic;
using PX.Common;

namespace PX.Objects.Common.GraphExtensions.Abstract
{
	public abstract class PreventEditBAccountRestrictToBase<TField, TGraph, TDocument, TSelect> : PreventEditOf<TField>.On<TGraph>.IfExists<TSelect>
		where TField : class, IBqlField
		where TGraph : PXGraph
		where TDocument : class, IBqlTable
		where TSelect : BqlCommand, new()
	{
		protected override string CreateEditPreventingReason(GetEditPreventingReasonArgs arg,
			object firstPreventingEntity, string fieldName, string currentTableName, string foreignTableName)
		{
			BAccount baccount = arg.Row as BAccount;
			var branch = PXAccess.GetBranchByBAccountID(baccount?.BAccountID);
			if (branch != null && arg.NewValue.IsIn(null, 0))
				return null;

			var newBAccountID = arg.NewValue as int?;
			var newBaseCuryID = PXAccess.GetBranchByBAccountID(newBAccountID)?.BaseCuryID ??
				PXAccess.GetOrganizationByBAccountID(newBAccountID)?.BaseCuryID;

			var document = firstPreventingEntity as TDocument;
			var documentBaseCurrency = GetBaseCurrency(document);

			if (branch != null)
			{
				var documentWithDifferentBaseCurrency = FindDocumentWithDifferentBaseCurrency(baccount.BAccountID, documentBaseCurrency);
				if (documentWithDifferentBaseCurrency != null)
					return PXMessages.LocalizeFormatNoPrefix(Messages.CannotChangeRestricToIfDocumetnsWithDifferentBaseCurrencyExist,
						GetDocumentDescription(document), GetDocumentDescription(documentWithDifferentBaseCurrency), baccount.AcctCD?.TrimEnd());
			}

			if (newBaseCuryID == documentBaseCurrency)
				return null;

			return GetErrorMessage(baccount, document, documentBaseCurrency);
		}

		protected virtual string GetBaseCurrency(TDocument document)
		{
			var documentCache = Base.Caches[typeof(TDocument)];
			var curyInfoID = (long?)documentCache.GetValue(document, nameof(CurrencyInfo.curyInfoID));
			var currency = CurrencyInfo.PK.Find(Base, curyInfoID);
			if (currency == null)
				throw new Common.Exceptions.RowNotFoundException(Base.Caches<CurrencyInfo>(), curyInfoID);

			return currency.BaseCuryID;
		}

		protected abstract string GetErrorMessage(BAccount baccount, TDocument document, string documentBaseCurrency);

		protected virtual IBqlTable FindDocumentWithDifferentBaseCurrency(int? baccountID, string currency)
		{
			INItemSite itemSite = SelectFrom<INItemSite>
				.InnerJoin<INSite>.On<INItemSite.FK.Site>
				.Where<INItemSite.preferredVendorOverride.IsEqual<True>.
					And<INItemSite.preferredVendorID.IsEqual<BAccount.bAccountID.AsOptional>>.
					And<INSite.baseCuryID.IsNotEqual<CurrencyInfo.baseCuryID.AsOptional>>>
				.View.Select(Base, baccountID, currency);

			if (itemSite != null)
				return itemSite;

			POReceipt receipt = SelectFrom<POReceipt>
				.InnerJoin<CurrencyInfo>.On<POReceipt.curyInfoID.IsEqual<CurrencyInfo.curyInfoID>>
				.Where<POReceipt.receiptType.IsNotEqual<POReceiptType.transferreceipt>.
					And<POReceipt.vendorID.IsEqual<BAccount.bAccountID.AsOptional>>.
					And<CurrencyInfo.baseCuryID.IsNotEqual<CurrencyInfo.baseCuryID.AsOptional>>>
				.View.Select(Base, baccountID, currency);

			if (receipt != null)
				return receipt;

			POLandedCostDoc landedCost = SelectFrom<POLandedCostDoc>
				.InnerJoin<CurrencyInfo>.On<POLandedCostDoc.curyInfoID.IsEqual<CurrencyInfo.curyInfoID>>
				.Where<POLandedCostDoc.vendorID.IsEqual<BAccount.bAccountID.AsOptional>.
					And<CurrencyInfo.baseCuryID.IsNotEqual<CurrencyInfo.baseCuryID.AsOptional>>>
				.View.Select(Base, baccountID, currency);

			if (landedCost != null)
				return landedCost;

			SOShipment shipment = SelectFrom<SOShipment>
				.InnerJoin<CurrencyInfo>.On<SOShipment.curyInfoID.IsEqual<CurrencyInfo.curyInfoID>>
			.Where<SOShipment.shipmentType.IsNotEqual<SOShipmentType.transfer>.
				And<SOShipment.customerID.IsEqual<BAccount.bAccountID.AsOptional>>.
				And<CurrencyInfo.baseCuryID.IsNotEqual<CurrencyInfo.baseCuryID.AsOptional>>>
			.View.Select(Base, baccountID, currency);

			if (shipment != null)
				return shipment;

			return null;
		}

		protected virtual string GetDocumentDescription(IBqlTable document)
		{
			switch(document)
			{
				case POReceipt receipt:
					return $"{receipt.ReceiptNbr} {Base.Caches<POReceipt>().DisplayName}";
				case POLandedCostDoc landedCost:
					return $"{landedCost.RefNbr} {Base.Caches<POLandedCostDoc>().DisplayName}";
				case SOShipment shipemnt:
					return $"{shipemnt.ShipmentNbr} {Base.Caches<SOShipment>().DisplayName}";
				case INItemSite itemSite:
					var item = InventoryItem.PK.Find(Base, itemSite.InventoryID);
					var site = INSite.PK.Find(Base, itemSite.SiteID);
					return $"{item.InventoryCD} {site.SiteCD} {Base.Caches<INItemSite>().DisplayName}";
				default:
					throw new NotImplementedException();
			}
		}
	}
}
