using System;
using System.Linq;
using System.Collections.Generic;
using PX.CCProcessingBase.Interfaces.V2;
using PX.Data;
using PX.Objects.AR;
using PX.Commerce.Shopify.ShopifyPayments.Extensions;
using PX.Commerce.Shopify.API.REST;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.ShopifyPayments
{
	public class ShopifyTransactionGetter : ShopifyProcessor, ICCTransactionGetter
	{
		public ShopifyTransactionGetter(IEnumerable<SettingsValue> settings) : base(settings)
		{
		}

		public TransactionData GetTransaction(string transactionId)
		{
			try
			{
				PXTrace.WriteVerbose("Get transaction details by TransactionId: {TransactionId}.", transactionId);

				if (string.IsNullOrEmpty(transactionId))
				{
					throw new CCProcessingException(PX.CCProcessingBase.Messages.PaymentTransactionIDEmpty);
				}

				string docType;
				string docRefNbr;
				SlotARPaymentKeys.GetKeys(out docType, out docRefNbr, true);

				if (docType == null || docRefNbr == null)
                {
					throw new PXException(ShopifyPluginMessages.TheExternalOrderIDCouldNotBeCalculatedBecauseTheARPaymentKeysAreMissingInTheMethodX, nameof(ShopifyTransactionGetter.GetTransaction));
				}

				PXGraph graph = PXGraph.CreateInstance<PXGraph>();

				ARPayment arPayment = null;
				BCSyncStatus bcSyncStatus = GetOrderBCSyncStatus(graph, docType, docRefNbr, out arPayment);
				string orderID = bcSyncStatus?.ExternID;

				if (orderID == null)
				{
					throw new PXException(ShopifyPluginMessages.TheExternalOrderIDCouldNotBeFound);
				}

				long longTransactionID;
				if (long.TryParse(transactionId, out longTransactionID) == false)
				{
					throw new PXException(ShopifyPluginMessages.InvalidProcessingCenterTransactionNumberXTheXExpectsItToBeAnIntegerNumber, transactionId, ShopifyPluginMessages.APIPluginDisplayName);
				}

				OrderTransaction transaction = null;
				OrderTransaction newestTransaction = null;

				foreach (OrderTransaction tran in GetOrderTransactions(orderID).OrderByDescending(t => t.DateModifiedAt))
				{
					if (docType == ARPaymentType.Prepayment
						&& tran.Kind == TransactionType.Refund
						&& tran.Id != longTransactionID)
                    {
						continue;
                    }

					if (newestTransaction == null
						&& (tran.Id == longTransactionID || tran.ParentId == longTransactionID))
					{
						newestTransaction = tran;
					}

					if (tran.Id == longTransactionID)
					{
						transaction = tran;
						break;
					}
				}

				if (transaction == null)
				{
					throw new PXException(ShopifyPluginMessages.TheExternalCardTransactionWithIdXCouldNotBeFound, transactionId);
				}

				transaction = newestTransaction;

				TransactionData result = new TransactionData()
				{
					Amount = transaction.Amount ?? 0,
					CustomerId = null, //tranDetails.profile?.customerProfileId,
					PaymentId = null, //tranDetails.profile?.customerPaymentProfileId,
					CardNumber = null, //((creditCardMaskedType)tranDetails.payment?.Item)?.cardNumber,
					DocNum = transaction.OrderId.ToString(),
					TranID = transaction.Id.ToString(),
					AuthCode = transaction.Authorization,
					SubmitTime = (DateTime)transaction.DateModifiedAt.Value.ToUniversalTime(),
					CcvVerificationStatus = GetCcvVerificationStatusFromErrorCode(transaction.ErrorCode),
					TranStatus = GetCCTranStatus(transaction),
					ResponseReasonText = transaction.Message,
					ResponseReasonCode = 0,
				};

				SetTranType(result, transaction.Kind, transaction.Status);

                //if (result.TranType == PX.CCProcessingBase.Interfaces.V2.CCTranType.Credit 
                //	&& tranDetails.refTransId != null && result.CustomerId == null)
                //{
                //	var refResult = GetTransaction(tranDetails.refTransId);
                //	if (refResult.CustomerId != null && refResult.PaymentId != null)
                //	{
                //		result.PaymentId = refResult.PaymentId;
                //		result.CustomerId = refResult.CustomerId;
                //	}
                //}

                if (result.TranType == CCTranType.Credit
					|| result.TranType == CCTranType.Void
					|| result.TranType == CCTranType.PriorAuthorizedCapture)
                {
					result.RefTranID = transaction.ParentId.ToString();

					if (transaction.Kind == TransactionType.Refund)
                    {
						// This is because the Shopify connector doesn't create a CCProcTran record with the Capture transactionId
						// for the case when the payment is captured in Shopify before being imported in Acumatica;
						// it only creates a CCProcTran record with the Tran. Type "Capture Authorized"
						// but with the Authorization transactionId.

						ExternalTransaction externalTransaction = PXSelect<ExternalTransaction,
							Where<ExternalTransaction.docType, Equal<Required<ExternalTransaction.docType>>,
							And<ExternalTransaction.refNbr, Equal<Required<ExternalTransaction.refNbr>>,
							And<ExternalTransaction.tranNumber, Equal<Required<ExternalTransaction.tranNumber>>,
							And<ExternalTransaction.active, Equal<True>>>>>>
							.Select(graph, docType, docRefNbr, result.RefTranID);

						if (externalTransaction == null)
                        {
							OrderTransaction capture = GetOrderSingleTransaction(orderID, transaction.ParentId.ToString());
							if (capture != null)
							{
								// This would match the Authorization transactionId of the record created by the Shopify connector.
								result.RefTranID = capture.ParentId.ToString();
							}
						}
					}
				}

                if (result.TranType == CCTranType.AuthorizeOnly 
					&& result.TranStatus != CCTranStatus.Expired)
				{
					result.ExpireAfterDays = ShopifyPluginHelper.AuthorizationValidPeriodDays;
				}

				PXTrace.WriteVerbose("Processing center returns CustomerId: {CustomerId}, Amount: {Amount}.", result.CustomerId, result.Amount);
				return result;
			}
			catch (Exception ex)
			{
				ErrorHandler(ex);
				throw;
			}
		}

		public IEnumerable<TransactionData> GetTransactionsByCustomer(string customerProfileId, TransactionSearchParams searchParams = null)
        {
            throw new PXException(ShopifyPluginMessages.TheMethodXIsNotImplementedInTheX, nameof(ICCTransactionGetter) + "." + nameof(GetTransactionsByCustomer), ShopifyPluginMessages.APIPluginDisplayName);
        }

		public IEnumerable<TransactionData> GetUnsettledTransactions(TransactionSearchParams searchParams = null)
		{
			try
            {
				PXTrace.WriteVerbose("Get unsettled transactions from Processing center.");

				string docType;
				string docRefNbr;
				SlotARPaymentKeys.GetKeys(out docType, out docRefNbr, true);

				if (docType == null || docRefNbr == null)
				{
					throw new PXException(ShopifyPluginMessages.TheExternalOrderIDCouldNotBeCalculatedBecauseTheARPaymentKeysAreMissingInTheMethodX, nameof(ShopifyTransactionGetter.GetUnsettledTransactions));
				}

				PXGraph graph = PXGraph.CreateInstance<PXGraph>();

				ARPayment arPayment = null;
				BCSyncStatus bcSyncStatus = GetOrderBCSyncStatus(graph, docType, docRefNbr, out arPayment);
				string orderID = bcSyncStatus?.ExternID;

				if (orderID == null)
				{
					throw new PXException(ShopifyPluginMessages.TheExternalOrderIDCouldNotBeFound);
				}

				IOrderedEnumerable<OrderTransaction> transactions = GetOrderTransactions(orderID).OrderByDescending(t => t.DateModifiedAt);

				IEnumerable<TransactionData> output = GetTransactionList(transactions);

				int cnt = transactions != null ? transactions.ToList().Count : 0;
				PXTrace.WriteVerbose("Processing center returns {TotalRows} records.", cnt);
				return output;
			}
			catch (Exception ex)
			{
				ErrorHandler(ex);
				throw;
			}
		}

		private IEnumerable<TransactionData> GetTransactionList(IOrderedEnumerable<OrderTransaction> transactions)
		{
			if (transactions != null)
			{
				foreach (OrderTransaction transaction in transactions)
				{
					TransactionData data = new TransactionData();
					data.Amount = transaction.Amount ?? 0;
					data.DocNum = transaction.OrderId.ToString();
					data.TranID = transaction.Id.ToString();
					data.CustomerId = null;
					data.PaymentId = null;
					data.SubmitTime = (DateTime)transaction.DateModifiedAt.Value.ToUniversalTime();
					yield return data;
				}
			}
		}
	}
}
