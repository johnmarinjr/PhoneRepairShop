using PX.Common;

namespace PX.Commerce.Shopify.ShopifyPayments
{
    [PXLocalizable]
    public static class ShopifyPluginMessages
    {
		public const string APIPluginDisplayName = "Shopify Payments API plug-in";
		public const string ShopifyConnectorFeatureDisplayName = "Shopify Connector";

		public const string APIPluginParameter_StoreName = "Shopify Store Name";

		public const string StoreName_CannotBeFound = "The Shopify store {0} has not been found. Make sure that the store name is correct.";
		public const string StoreName_CannotBeEmpty = "Enter the name of your Shopify store to configure the processing center for Shopify Payments.";
		public const string StoreName_CannotBeFoundWithHint = "The Shopify store {0} specified in the plug-in parameters on the Processing Centers (CA205000) form has not been found.";
		public const string StoreName_CannotBeEmptyWithHint = "The Shopify store has not been specified in the plug-in parameters on the Processing Centers (CA205000) form.";

		public const string SettingsEmpty = "Make sure that all the required settings have been specified.";

		public const string TheExternalOrderIDCouldNotBeFound = "The external order ID could not be found.";
		public const string TransactionTypeXIsNotImplemented = "The {0} transaction type is not supported by the Shopify Payments API plug-in.";

		public const string ShopifyPaymentPluginExpectsOneAndOnlyOneSalesOrderRelatedToThePrepayment = "This operation cannot be performed. The prepayment must be applied to one sales order.";
		public const string ShopifyPaymentPluginExpectsOneAndOnlyOnePrepaymentRelatedToTheCustomerRefund = "This operation cannot be performed. The customer refund must be linked to one prepayment or return order.";
				
		public const string TheOriginalTransactionXInTheCustomerRefundDocumentDoesNotBelongToTheRelatedPrepaymentDocument = "The original transaction in the Customer Refund document ({0}) does not belong to the related Prepayment document.";
		public const string TheExternalOrderIDCouldNotBeCalculatedBecauseTheARPaymentKeysAreMissingInTheMethodX = "The external order ID, which is required for the Shopify API, could not be calculated because the ARPayment keys are missing in the {0} method.";
		public const string TheExternalCardTransactionWithIdXCouldNotBeFound = "The external card transaction with ID {0} could not be found.";
		public const string DoNotUseAcceptPaymentFormWarning = "The check box was cleared automatically because this processing center does not allow accepting payments from new cards.";
		public const string TheXPluginRequiresTheXFeatureEnabled = "The {0} requires that the {1} feature be enabled.";
		public const string TheMethodXIsNotImplementedInTheX = "The {0} method is not implemented in the {1}.";
		public const string InvalidProcessingCenterTransactionNumberXTheXExpectsItToBeAnIntegerNumber = "The {0} processing center transaction is invalid. The {1} expects an integer of the long data type.";
	}
}
