using PX.Api.ContractBased.Models;
using PX.Commerce.BigCommerce.API.REST;
using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.GL;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PX.Commerce.BigCommerce
{
	public class BCHelper : CommerceHelper
	{
		protected List<Currency> _currencies;
		public List<Currency> Currencies
		{
			get
			{
				if (_currencies == null)
				{
					StoreCurrencyDataProvider storeCurrencyDataProvider = new StoreCurrencyDataProvider(BCConnector.GetRestClient(_processor.GetBindingExt<BCBindingBigCommerce>()));
					_currencies = storeCurrencyDataProvider.Get();
				}
				return _currencies;
			}
		}
		#region Inventory
		public virtual string GetInventoryCDByExternID(string productID, string variantID, string sku, OrdersProductsType type, out string uom, out string alternateID)
		{
			alternateID = null;
			if (type == OrdersProductsType.GiftCertificate)
			{
				BCBindingExt bindingExt = _processor.GetBindingExt<BCBindingExt>();
				PX.Objects.IN.InventoryItem inventory = bindingExt?.GiftCertificateItemID != null ? PX.Objects.IN.InventoryItem.PK.Find(this, bindingExt?.GiftCertificateItemID) : null;
				if (inventory?.InventoryCD == null)
					throw new PXException(BigCommerceMessages.NoGiftCertificateItem);

				uom = inventory.SalesUnit?.Trim();
				return inventory.InventoryCD.Trim();
			}

			String key = variantID != null ? new Object[] { productID, variantID }.KeyCombine() : productID;
			String priorityUOM = null;
			PX.Objects.IN.InventoryItem item = null;
			if (variantID != null)
			{
				item = PXSelectJoin<PX.Objects.IN.InventoryItem,
						InnerJoin<BCSyncDetail, On<PX.Objects.IN.InventoryItem.noteID, Equal<BCSyncDetail.localID>>,
						InnerJoin<BCSyncStatus, On<BCSyncStatus.syncID, Equal<BCSyncDetail.syncID>>>>,
							Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
								And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
								And<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
								And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>,
								And<InventoryItem.inventoryCD, Equal<Required<InventoryItem.inventoryCD>>>>>>>>
						.Select(this, BCEntitiesAttribute.ProductWithVariant, productID, sku);
			}
			else
			{
				item = PXSelectJoin<PX.Objects.IN.InventoryItem,
					   LeftJoin<BCSyncStatus, On<PX.Objects.IN.InventoryItem.noteID, Equal<BCSyncStatus.localID>>>,
					   Where<BCSyncStatus.connectorType, Equal<Current<BCEntity.connectorType>>,
						   And<BCSyncStatus.bindingID, Equal<Current<BCEntity.bindingID>>,
						   And2<Where<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>,
							   Or<BCSyncStatus.entityType, Equal<Required<BCEntity.entityType>>>>,
						   And<BCSyncStatus.externID, Equal<Required<BCSyncStatus.externID>>>>>>>
					   .Select(this, BCEntitiesAttribute.StockItem, BCEntitiesAttribute.NonStockItem, key);
			}
			if (item == null) //Serch by SKU
			{
				item = PXSelect<PX.Objects.IN.InventoryItem,
							Where<InventoryItem.inventoryCD, Equal<Required<InventoryItem.inventoryCD>>>>
							.Select(this, sku);
			}
			if (item == null && sku != null) //Search by cross references
			{
				PX.Objects.IN.InventoryItem itemCandidate = null;
				PX.Objects.IN.INItemXRef crossrefCandidate = null;
				foreach (PXResult<PX.Objects.IN.INItemXRef, PX.Objects.IN.InventoryItem> result in PXSelectJoin<PX.Objects.IN.INItemXRef,
					InnerJoin<PX.Objects.IN.InventoryItem, On<PX.Objects.IN.INItemXRef.inventoryID, Equal<PX.Objects.IN.InventoryItem.inventoryID>>>,
					Where<PX.Objects.IN.INItemXRef.alternateType, Equal<INAlternateType.global>,
						And<PX.Objects.IN.INItemXRef.alternateID, Equal<Required<PX.Objects.IN.INItemXRef.alternateID>>>>>.Select(this, sku))
				{
					if (itemCandidate != null && itemCandidate.InventoryID != result.GetItem<PX.Objects.IN.InventoryItem>().InventoryID)
						throw new PXException(BCMessages.InventoryMultipleAlternates, sku, key);

					itemCandidate = result.GetItem<PX.Objects.IN.InventoryItem>();
					crossrefCandidate = result.GetItem<PX.Objects.IN.INItemXRef>();
				}
				item = itemCandidate;
				priorityUOM = crossrefCandidate?.UOM;
				alternateID = crossrefCandidate?.AlternateID;
			}

			if (item == null)
				throw new PXException(BCMessages.InvenotryNotFound, sku, key);
			if (item.ItemStatus == PX.Objects.IN.INItemStatus.Inactive)
				throw new PXException(BCMessages.InvenotryInactive, item.InventoryCD);

			uom = priorityUOM ?? item?.SalesUnit?.Trim();
			return item?.InventoryCD?.Trim();
		}
		#endregion

		#region Payment 
		public virtual BCPaymentMethods GetPaymentMethodMapping(string transactionMethod, string orderMethod, string currency, out string cashAccount, bool throwError = true)
		{
			cashAccount = null;
			BCPaymentMethods result = null;
			//if order method(example in case of braintree payment method) is passed than check if found matching record, else just check with just payment method
			var results = PaymentMethods().Where(x =>
				string.Equals(currency, x.StoreCurrency, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.StorePaymentMethod, transactionMethod, StringComparison.OrdinalIgnoreCase)
				&& (!string.IsNullOrEmpty(orderMethod) && string.Equals(orderMethod, x.StoreOrderPaymentMethod, StringComparison.OrdinalIgnoreCase)));
			if (results != null && results.Any())
			{
				result = results.FirstOrDefault(x => x.Active == true);
			}
			else if (PaymentMethods().Any(x =>
				string.Equals(currency, x.StoreCurrency, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(x.StorePaymentMethod, transactionMethod, StringComparison.OrdinalIgnoreCase)))
			{
				result = PaymentMethods().FirstOrDefault(x =>
					string.Equals(currency, x.StoreCurrency, StringComparison.OrdinalIgnoreCase)
					&& string.Equals(x.StorePaymentMethod, transactionMethod, StringComparison.OrdinalIgnoreCase) && x.Active == true);
			}
			else
			{
				// if not found create entry and throw exception
				PXCache cache = base.Caches[typeof(BCPaymentMethods)];
				BCPaymentMethods entry = new BCPaymentMethods()
				{
					StorePaymentMethod = transactionMethod.ToUpper(),
					StoreCurrency = currency,
					BindingID = _processor.Operation.Binding,
					Active = true
				};
				cache.Insert(entry);
				cache.Persist(PXDBOperation.Insert);

				if (throwError)
					throw new PXException(BCMessages.OrderPaymentMethodIsMissing, transactionMethod, orderMethod?.ToUpper(), currency);
			}

			if (result != null)
			{
				CashAccount ca = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(this, result.CashAccountID);
				cashAccount = ca?.CashAccountCD;

				if (cashAccount == null || result?.PaymentMethodID == null)
				{
					throw new PXException(BCMessages.OrderPaymentMethodIsMissing, transactionMethod, orderMethod?.ToUpper(), currency);
				}
			}
			else if (throwError)
			{
				// in case if payment is filetered and forced synced but paymentmethod mapping is not active or not mapped
				//Note in case of refunds passed as false we donot throw error 

				throw new PXException(BCMessages.OrderPaymentMethodIsMissing, transactionMethod, orderMethod?.ToUpper(), currency);
			}

			return result;
		}

		public virtual string ParseTransactionNumber(OrdersTransactionData tran, out bool isCreditCardTran)
		{
			String paymentRef = tran?.GatewayTransactionId;
			isCreditCardTran = tran?.GatewayTransactionId != null;
			if (tran == null) return paymentRef;

			if (!String.IsNullOrWhiteSpace(paymentRef) && paymentRef.IndexOf("#") >= 0)
				paymentRef = paymentRef.Substring(0, paymentRef.IndexOf("#"));

			if (String.IsNullOrEmpty(paymentRef))
				paymentRef = tran.Id.ToString();

			return paymentRef;
		}

		public virtual string GetPaymentMethodName(OrdersTransactionData data)
		{
			if (data.PaymentMethod == BCConstants.Emulated)
				return data.Gateway?.ToUpper();
			return string.Format("{0} ({1})", data.Gateway, data.PaymentMethod ?? string.Empty)?.ToUpper();

		}
		public virtual bool CreatePaymentfromOrder(string method)
		{
			var paymentMethod = PaymentMethods().FirstOrDefault(x =>
				String.Equals(x.StorePaymentMethod, method, StringComparison.InvariantCultureIgnoreCase)
				&& x.CreatePaymentFromOrder == true && x.Active == true);
			return (paymentMethod != null);
		}

		public virtual void AddCreditCardProcessingInfo(BCPaymentMethods methodMapping, Payment payment, OrderPaymentEvent orderPaymentEvent, string paymentInstrumentToken, CreditCard cc)
		{
			payment.IsNewCard = true.ValueField();
			payment.SaveCard = (!String.IsNullOrWhiteSpace(paymentInstrumentToken)).ValueField();
			payment.ProcessingCenterID = methodMapping?.ProcessingCenterID?.ValueField();

			CreditCardTransactionDetail detail = new CreditCardTransactionDetail();
			detail.TranNbr = payment.PaymentRef;
			detail.TranDate = payment.ApplicationDate;
			detail.ExtProfileId = paymentInstrumentToken.ValueField();
			detail.TranType = GetTransactionType(orderPaymentEvent);
			detail.CardType = GetCardType(cc.CardType);

			payment.CreditCardTransactionInfo = new List<CreditCardTransactionDetail>(new[] { detail });
		}
		public virtual StringValue GetTransactionType(OrderPaymentEvent orderPaymentEvent)
		{
			switch (orderPaymentEvent)
			{
				case OrderPaymentEvent.Authorization:
					return CCTranTypeCode.Authorize.ValueField();
				case OrderPaymentEvent.Capture:
					return CCTranTypeCode.PriorAuthorizedCapture.ValueField();
				case OrderPaymentEvent.Purchase:
					return CCTranTypeCode.AuthorizeAndCapture.ValueField();
				case OrderPaymentEvent.Refund:
					return CCTranTypeCode.Credit.ValueField();
				default:
					return CCTranTypeCode.Unknown.ValueField();
			}
		}

		public virtual StringValue GetCardType(string externalCardType)
		{
			string cardTypeCode = ConvertBCCardTypeToCardTypeCode(externalCardType);
			return cardTypeCode == CardType.OtherCode ?
				externalCardType.ValueField() : cardTypeCode.ValueField();
		}

		public virtual string ConvertBCCardTypeToCardTypeCode(string externalCardType)
		{
			switch (externalCardType)
			{
				case BigCommerceCardTypes.Alelo:
					return CardType.AleloCode;
				case BigCommerceCardTypes.Alia:
					return CardType.MasterCardCode;
				case BigCommerceCardTypes.AmericanExpress:
					return CardType.AmericanExpressCode;
				case BigCommerceCardTypes.Cabal:
					return CardType.CabalCode;
				case BigCommerceCardTypes.Carnet:
					return CardType.CarnetCode;
				case BigCommerceCardTypes.Dankort:
					return CardType.DankortCode;
				case BigCommerceCardTypes.DinersClub:
					return CardType.DinersClubCode;
				case BigCommerceCardTypes.Discover:
					return CardType.DiscoverCode;
				case BigCommerceCardTypes.Elo:
					return CardType.EloCode;
				case BigCommerceCardTypes.Forbrugsforeningen:
					return CardType.ForbrugsforeningenCode;
				case BigCommerceCardTypes.Jcb:
					return CardType.JCBCode;
				case BigCommerceCardTypes.Maestro:
					return CardType.MaestroCode;
				case BigCommerceCardTypes.Master:
					return CardType.MasterCardCode;
				case BigCommerceCardTypes.Naranja:
					return CardType.NaranjaCode;
				case BigCommerceCardTypes.Sodexo:
					return CardType.SodexoCode;
				case BigCommerceCardTypes.Unionpay:
					return CardType.UnionPayCode;
				case BigCommerceCardTypes.Visa:
					return CardType.VisaCode;
				case BigCommerceCardTypes.Vr:
					return CardType.VrCode;
				default:
					return CardType.OtherCode;
			}
		}

		public virtual OrderPaymentEvent PopulateAction(IList<OrdersTransactionData> transactions, OrdersTransactionData data)
		{
			var lastTrans = transactions.LastOrDefault(x => x.Gateway == data.Gateway && x.Status == BCConstants.BCPaymentStatusOk && data.Event != x.Event
										  && (x.Event == OrderPaymentEvent.Authorization || x.Event == OrderPaymentEvent.Capture || x.Event == OrderPaymentEvent.Purchase));
			data.Amount = lastTrans?.Amount ?? data.Amount;
			var lastEvent= lastTrans?.Event ?? data.Event;
			data.Action = lastEvent;
			//void Payment if payement was authorized only and voided in external system
			var voidTrans = transactions.FirstOrDefault(x => x.Gateway == data.Gateway && x.Status == BCConstants.BCPaymentStatusOk && x.Event == OrderPaymentEvent.Void);
			if (voidTrans != null && lastEvent == OrderPaymentEvent.Authorization)
			{
				data.Action = voidTrans.Event;
			}

			return lastEvent;
		}

		#endregion

		public virtual string CleanAddress(string strIn)
		{
			if (String.IsNullOrWhiteSpace(strIn))
			{
				return String.Empty;
			}
			// Replace invalid characters with empty strings.
			try
			{
				//Removes unprintable characters from the string.
				return Regex.Replace(strIn, @"\p{C}+", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(1.5));
			}
			// If we timeout when replacing invalid characters,
			// we should return Empty.
			catch (RegexMatchTimeoutException)
			{
				return String.Empty;
			}
		}

		#region Utilities
		public override decimal? RoundToStoreSetting(decimal? price)
		{
			string curryId = PX.Objects.GL.Branch.PK.Find(this, _processor.GetBinding().BranchID)?.BaseCuryID;
			if (curryId != null)
			{
				price = price != null ? Decimal.Round(price.Value, Currencies?.FirstOrDefault(c => c.CurrencyCode == curryId).DecimalPlaces ?? CommonSetupDecPl.PrcCst, MidpointRounding.AwayFromZero) : 0;
			}
			return price;

		} 
		#endregion

		#region Filter
		public virtual void SetFilterMinDate(FilterOrders filter, DateTime? minDateTime, DateTime? syncOrdersFrom)
		{
			if (minDateTime != null && (syncOrdersFrom == null || minDateTime > syncOrdersFrom))
			{
				filter.MinDateModified = minDateTime;
			}
			else if (syncOrdersFrom != null)
			{
				filter.MinDateCreated = syncOrdersFrom;
			}
		}
		#endregion
	}
}
