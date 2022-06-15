using System;
using System.Collections.Generic;
using PX.Data;
using PX.CCProcessingBase.Interfaces.V2;
using PX.Objects.AR;
using PX.Commerce.Shopify.API.REST;
using PX.Commerce.Core;

namespace PX.Commerce.Shopify.ShopifyPayments
{
	public class ShopifyTransactionProcessor : ShopifyProcessor, ICCTransactionProcessor
	{
		public ShopifyTransactionProcessor(IEnumerable<SettingsValue> settingValues) : base(settingValues)
		{
		}

		public ProcessingResult DoTransaction(ProcessingInput aInputData)
		{
			try
			{
				PXTrace.WriteVerbose("Perform transaction, TranType: {TranType}, CustomerCD: {CustomerCD}, PaymentProfileID: {PaymentProfileID}, DocRefNbr: {DocRefNbr}, AuthCode: {AuthCode}",
					 aInputData?.TranType.ToString(), aInputData?.CustomerData?.CustomerCD,
					 aInputData?.CardData?.PaymentProfileID, aInputData?.DocumentData?.DocRefNbr,
					 aInputData?.AuthCode);

				var error = ShopifyValidator.ValidateForTransaction(aInputData);
				if (!string.IsNullOrEmpty(error))
				{
					throw new CCProcessingException(error);
				}

				string customerProfileId = aInputData.CustomerData.CustomerProfileID;
				string customerPaymentProfileId = aInputData.CardData.PaymentProfileID;
				bool storedProfile = customerProfileId != null && customerPaymentProfileId != null;
				decimal amount = aInputData.Amount;

				PXGraph graph = PXGraph.CreateInstance<PXGraph>();

				ARPayment arPayment = null;
				BCSyncStatus bcSyncStatus = GetOrderBCSyncStatus(graph, aInputData.DocumentData.DocType, aInputData.DocumentData.DocRefNbr, out arPayment);
				string orderID = bcSyncStatus?.ExternID;

				if (orderID == null)
				{
					throw new PXException(ShopifyPluginMessages.TheExternalOrderIDCouldNotBeFound);
				}

				PrepareBCSyncStatusUpdate(bcSyncStatus);

				long parentID;
				if (long.TryParse(aInputData.OrigTranID, out parentID) == false)
				{
					throw new PXException(ShopifyPluginMessages.InvalidProcessingCenterTransactionNumberXTheXExpectsItToBeAnIntegerNumber, aInputData.OrigTranID, ShopifyPluginMessages.APIPluginDisplayName);
				}

				var transactionRequest = new OrderTransaction
				{
					Currency = aInputData.CuryID,
					Amount = aInputData.Amount,
					ParentId = parentID,
				};

				switch (aInputData.TranType)
				{
					case CCTranType.PriorAuthorizedCapture:
						transactionRequest.Kind = TransactionType.Capture;

						break;
					case CCTranType.Credit:
						transactionRequest.Kind = TransactionType.Refund;

						break;
					case CCTranType.Void:
						transactionRequest.Kind = TransactionType.Void;

						break;
					default:
						throw new PXException(ShopifyPluginMessages.TransactionTypeXIsNotImplemented, aInputData.TranType.ToString());
				}

				var response = PostPaymentToCapture(transactionRequest, orderID);
				var result = ProcessTransactionResponse(response);

				UpdateBCSyncStatus(graph);

				//TODO: ExpireAfterDays should not be set here
				if (aInputData.TranType == CCTranType.AuthorizeOnly)
				{
					result.ExpireAfterDays = ShopifyPluginHelper.AuthorizationValidPeriodDays;
				}

				PXTrace.WriteVerbose("Processing center returns {TransactionNumber}.", result?.TransactionNumber);
				return result;
			}
			catch (Exception ex)
			{
				ErrorHandler(ex);
				throw;
			}
		}

		public static ProcessingResult ProcessTransactionResponse(OrderTransaction transactionResponse)
		{
			if (transactionResponse == null)
			{
				return null;
			}

			ProcessingResult result = new ProcessingResult();
			// Authorization or approval code. 6 characters.
			result.AuthorizationNbr = transactionResponse.Authorization;

			// Card code verification(CCV) response code. 
			// Indicates result of the CCV filter.
			result.CcvVerificatonStatus = GetCcvVerificationStatusFromErrorCode(transactionResponse.ErrorCode);

			//TODO: result.ExpireAfterDays

			//Overall status of the transaction.
			//1 = Approved
			//2 = Declined
			//3 = Error
			//4 = Held for Review
			result.ResponseCode = ((int)GetCCTranStatus(transactionResponse)).ToString();
			result.ResponseReasonText = transactionResponse.Message;
			//TODO: result.ResponseText = ;

			//	The payment gateway assigned identification number for transaction.
			// The transId value must be used for any follow-on transactions such as a credit, prior authorization and capture, or void.
			result.TransactionNumber = transactionResponse.Id.ToString();
			//If you got zero transId number try to use refTransID. 
			//This happens when you got successful response with message 'Transaction already voided' after calling void command.
			//if (result.TransactionNumber == "0")
			//{
			//	result.TransactionNumber = transactionResponse.refTransID;
			//}
			return result;
			//TODO: result.ResponseReasonCode
		}
    }
}
