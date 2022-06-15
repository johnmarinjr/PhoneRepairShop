using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using PX.Data;
using PX.CCProcessingBase.Interfaces.V2;
using PX.Objects.AR;
using PX.Objects.SO;
using PX.Commerce.Core;
using PX.Commerce.Shopify.API.REST;
using static PX.Commerce.Shopify.ShopifyPayments.ShopifyPluginHelper;
using PX.Commerce.Shopify;

namespace PX.Commerce.Shopify.ShopifyPayments
{
	public abstract class ShopifyProcessor : OrderRestDataProvider
	{
		protected BCSyncStatus OriginalBCSyncStatus { get; set; } = null;
		protected OrderData OriginalOrderData { get; set; } = null;

		protected ShopifyProcessor(IEnumerable<SettingsValue> settingValues)
			: base(GetRestClient(settingValues))
		{
		}

		public static IShopifyRestClient GetRestClient(IEnumerable<SettingsValue> settingValues)
		{
			if (CommerceFeaturesHelper.ShopifyConnector == false)
			{
				throw new PXException(ShopifyPluginMessages.TheXPluginRequiresTheXFeatureEnabled,
													ShopifyPluginMessages.APIPluginDisplayName,
													ShopifyPluginMessages.ShopifyConnectorFeatureDisplayName);
			}

			string errors = ShopifyValidator.Validate(settingValues);
			if (!String.IsNullOrEmpty(errors))
			{
				throw new CCProcessingException(errors);
			}

			string shopifyStoreName = settingValues?.FirstOrDefault(x => x.DetailID == SettingsKeys.Key_StoreName)?.Value;
			BCBindingShopify shopifyStore = PXSelectJoin<BCBindingShopify,
				InnerJoin<BCBinding, On<BCBinding.bindingID, Equal<BCBindingShopify.bindingID>>>,
				Where<BCBindingShopify.bindingID, Equal<Required<BCBindingShopify.bindingID>>,
					Or<BCBinding.bindingName, Equal<Required<BCBinding.bindingName>>>>>.Select(PXGraph.CreateInstance<PXGraph>(), shopifyStoreName, shopifyStoreName);
			if (shopifyStore == null) throw new CCProcessingException(ShopifyPluginMessages.StoreName_CannotBeFoundWithHint);

			return SPConnector.GetRestClient(shopifyStore);
		}

		protected virtual void ErrorHandler(Exception e)
		{
			if (e is System.Net.WebException)
			{
				var webException = (System.Net.WebException)e;

				var webResponse = webException.Response;
				if (webResponse != null)
				{
					using (var reader = new StreamReader(webResponse.GetResponseStream()))
					{
						string result = reader.ReadToEnd();
						PXTrace.WriteError("Response received: {result}.", result);
					}
				}
				else
				{
					PXTrace.WriteError("Received Response Is Empty.");
				}

				throw new CCProcessingException(PX.CCProcessingBase.Messages.CannotProcessRequest, webException);
			}

			if (e is CCProcessingException)
			{
				PXTrace.WriteError(e);
				throw PXException.PreserveStack(e);
			}

			PXTrace.WriteError(e);
			throw new CCProcessingException(e.Message, e);
		}

		public virtual BCSyncStatus GetOrderBCSyncStatus(PXGraph graph, string docType, string docRefNbr, out ARPayment arPayment)
		{
			arPayment = null;
			BCSyncStatus bcSyncStatus = null;
			ARPayment actionARPayment = ARPayment.PK.Find(graph, docType, docRefNbr);
			if (actionARPayment != null)
			{
				if (actionARPayment.DocType == ARDocType.Refund)
				{
					IEnumerable<ARAdjust> arAdjustRows = ARAdjust.FK.Payment.SelectChildren(graph, actionARPayment);

					if (arAdjustRows.Count() == 0 || arAdjustRows.Count() > 1)
					{
						IEnumerable<SOAdjust> rcAdjustrows = SOAdjust.FK.AdjustingPayment.SelectChildren(graph, actionARPayment);// CR payment can be linked to either RC order or Prepayment
						if (rcAdjustrows.Count() == 0 || rcAdjustrows.Count() > 1)
						{
							throw new PXException(ShopifyPluginMessages.ShopifyPaymentPluginExpectsOneAndOnlyOnePrepaymentRelatedToTheCustomerRefund);
						}
						SOAdjust rcAdjust = rcAdjustrows.FirstOrDefault();

						bcSyncStatus = PXSelectJoin<BCSyncStatus, InnerJoin<BCSyncDetail, On<BCSyncDetail.syncID, Equal<BCSyncStatus.syncID>>,
												InnerJoin<SOOrder, On<BCSyncDetail.entityType, Equal<Required<BCSyncDetail.entityType>>, And<SOOrder.noteID, Equal<BCSyncDetail.localID>>>>>,
												Where<SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
												And<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>.
												Select(graph, BCEntitiesAttribute.CustomerRefundOrder, rcAdjust.AdjdOrderType, rcAdjust.AdjdOrderNbr);
						if (bcSyncStatus == null && !string.IsNullOrEmpty(actionARPayment.RefTranExtNbr))// if order syncstaus not found , try to search with orig transaction number from CR Payment
						{
							ExternalTransaction externalTransaction = PXSelect<ExternalTransaction, Where<ExternalTransaction.processingCenterID, Equal<Required<ExternalTransaction.processingCenterID>>,
								And<ExternalTransaction.tranNumber, Equal<Required<ExternalTransaction.tranNumber>>>>>.Select(graph, actionARPayment.ProcessingCenterID, actionARPayment.RefTranExtNbr);
							arPayment = ARPayment.PK.Find(graph, externalTransaction.DocType, externalTransaction.RefNbr);

						}
						else
							return bcSyncStatus;
					}
					else
					{
						ARAdjust arAdjust = arAdjustRows.FirstOrDefault();
						arPayment = ARPayment.PK.Find(graph, arAdjust.AdjdDocType, arAdjust.AdjdRefNbr);
					}

					CCProcTran ccProcTran = GetProcCenterTransaction(graph, arPayment.DocType, arPayment.RefNbr, actionARPayment.RefTranExtNbr, null);

					if (ccProcTran == null)
					{
						throw new PXException(ShopifyPluginMessages.TheOriginalTransactionXInTheCustomerRefundDocumentDoesNotBelongToTheRelatedPrepaymentDocument, actionARPayment.RefTranExtNbr);
					}
				}
				else
				{
					arPayment = actionARPayment;
				}

				IEnumerable<SOAdjust> soAdjustRows = SOAdjust.FK.AdjustingPayment.SelectChildren(graph, arPayment);

				if (soAdjustRows.Count() == 0 || soAdjustRows.Count() > 1)
				{
					throw new PXException(ShopifyPluginMessages.ShopifyPaymentPluginExpectsOneAndOnlyOneSalesOrderRelatedToThePrepayment);
				}

				SOAdjust soAdjust = soAdjustRows.FirstOrDefault();

				bcSyncStatus = PXSelectJoin<BCSyncStatus,
											   InnerJoin<SOOrder,
												   On<SOOrder.noteID, Equal<BCSyncStatus.localID>>>,
										   Where<SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
											   And<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>.
										   Select(graph, soAdjust.AdjdOrderType, soAdjust.AdjdOrderNbr);

				return bcSyncStatus;
			}

			return null;
		}

		public virtual CCProcTran GetProcCenterTransaction(PXGraph graph, string docType, string refNbr, string refTranExtNbr, string tranType)
		{
			return PXSelect<CCProcTran,
					Where<CCProcTran.docType, Equal<Required<CCProcTran.docType>>,
						And<CCProcTran.refNbr, Equal<Required<CCProcTran.refNbr>>,
						And<CCProcTran.pCTranNumber, Equal<Required<CCProcTran.pCTranNumber>>,
						And<CCProcTran.tranStatus, Equal<CCTranStatusCode.approved>,
						And<Where<Required<CCProcTran.tranType>, IsNull,
								Or<CCProcTran.tranType, Equal<Required<CCProcTran.tranType>>>>>>>>>>.
				Select(graph, docType, refNbr, refTranExtNbr, tranType, tranType);
		}

		public static CcvVerificationStatus GetCcvVerificationStatusFromErrorCode(String errorCode)
		{
			if (errorCode == null)
				return CcvVerificationStatus.RelyOnPreviousVerification;

			switch (errorCode)
			{
				case "incorrect_cvc":
					return CcvVerificationStatus.NotMatch;
				case "invalid_cvc":
					return CcvVerificationStatus.ShouldHaveBeenPresent;
				case "call_issuer":
					return CcvVerificationStatus.IssuerUnableToProcessRequest;
				default:
					return CcvVerificationStatus.Unknown;
			}
		}

		public static CCTranStatus GetCCTranStatus(OrderTransaction transaction)
		{
			CCTranStatus retStatus = CCTranStatus.Unknown;

			switch (transaction.Status)
			{
				case TransactionStatus.Success:
					retStatus = CCTranStatus.Approved;
					break;
				case TransactionStatus.Failure:
					retStatus = CCTranStatus.Declined;
					break;
				case TransactionStatus.Error:
					retStatus = CCTranStatus.Error;
					break;
				case TransactionStatus.Pending:
					retStatus = CCTranStatus.HeldForReview;
					break;
			}

			if (transaction.Kind == TransactionType.Authorization
				&& transaction.ProcessedAt?.AddDays(AuthorizationValidPeriodDays) < DateTime.Now)
			{
				retStatus = CCTranStatus.Expired;
			}

			return retStatus;
		}

		protected void SetTranType(TransactionData tranData, TransactionType PCtranType, TransactionStatus? PCtranStatus)
		{
			PX.CCProcessingBase.Interfaces.V2.CCTranType ccTranType = 0;
			GetCCTranTypeByAuthTran(PCtranType, out ccTranType);
			tranData.TranType = ccTranType;
		}

		public static bool GetCCTranTypeByAuthTran(TransactionType input, out CCTranType output)
		{
			bool res = true;
			output = 0;
			switch (input)
			{
				case TransactionType.Authorization: output = CCTranType.AuthorizeOnly; break;
				case TransactionType.Sale: output = CCTranType.AuthorizeAndCapture; break;
				case TransactionType.Capture: output = CCTranType.PriorAuthorizedCapture; break;
				//case TransactionType.captureOnlyTransaction: output = CCTranType.CaptureOnly; break;
				case TransactionType.Refund: output = CCTranType.Credit; break;
				case TransactionType.Void: output = CCTranType.Void; break;
				default: res = false; break;
			}
			return res;
		}

		public void PrepareBCSyncStatusUpdate(BCSyncStatus bcSyncStatus)
		{
			OriginalBCSyncStatus = bcSyncStatus;

			if (bcSyncStatus != null && bcSyncStatus.ExternID != null)
			{
				OriginalOrderData = GetByID(bcSyncStatus.ExternID);
			}
			else
			{
				OriginalOrderData = null;
			}
		}

		public void UpdateBCSyncStatus(PXGraph graph)
		{
			BCSyncStatus bcSyncStatus = OriginalBCSyncStatus;
			OrderData orderData = OriginalOrderData;

			OriginalBCSyncStatus = null;
			OriginalOrderData = null;

			if (bcSyncStatus == null || orderData == null)
			{
				return;
			}

			if (bcSyncStatus.ExternTS != orderData.DateModifiedAt.ToDate(false)
				|| bcSyncStatus.PendingSync == true)
			{
				return;
			}

			OrderData newOrderData = GetByID(bcSyncStatus.ExternID);
			bcSyncStatus.ExternTS = newOrderData.DateModifiedAt.ToDate(false);

			PXUpdate<
				Set<BCSyncStatus.externTS, Required<BCSyncStatus.externTS>>,
			BCSyncStatus,
			Where<BCSyncStatus.syncID, Equal<Required<BCSyncStatus.syncID>>>>
			.Update(graph, bcSyncStatus.ExternTS, bcSyncStatus.SyncID);
		}
	}
}
