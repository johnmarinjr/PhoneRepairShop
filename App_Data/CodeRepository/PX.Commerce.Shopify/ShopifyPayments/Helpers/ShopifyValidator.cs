using PX.CCProcessingBase;
using PX.CCProcessingBase.Interfaces.V2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PX.Commerce.Shopify.ShopifyPayments.ShopifyPluginHelper;
using CCTranType = PX.CCProcessingBase.Interfaces.V2.CCTranType;
using ProcessingInput = PX.CCProcessingBase.Interfaces.V2.ProcessingInput;

namespace PX.Commerce.Shopify.ShopifyPayments
{
    internal static class ShopifyValidator
	{
		public static string ValidateStoreName(string name)
		{
			if (String.IsNullOrWhiteSpace(name))
			{
				return ShopifyPluginMessages.StoreName_CannotBeEmptyWithHint;
			}

			return String.Empty;
		}

		public static string Validate(SettingsValue setting)
		{
			if (setting == null)
			{
				return ShopifyPluginMessages.SettingsEmpty;
			}

			string result = String.Empty;

			switch (setting.DetailID)
			{
				case ShopifyPluginHelper.SettingsKeys.Key_StoreName:
					result = ValidateStoreName(setting.Value);
					break;
				default:
					result = Messages.UnknownDetailID;
					break;
			}
			return result;	
		}

		public static string Validate(IEnumerable<SettingsValue> settingValues)
		{
			if (settingValues == null || settingValues.Any() == false)
			{
				return ShopifyPluginMessages.SettingsEmpty;
			}
			return settingValues.Aggregate(String.Empty, (current, sv) => current + Validate(sv));
		}

		public static string ValidateForTransaction(ProcessingInput processingInput)
		{
			if (processingInput == null)
			{
				return Messages.ProcessingInputEmpty;
			}

			StringBuilder stringBuilder = new StringBuilder();

			if (processingInput.Amount <= 0)
			{
				stringBuilder.AppendLine(Messages.AmountMustBePositive);
			}

			string errs = ValidateTranType(processingInput);
			if (!string.IsNullOrEmpty(errs))
			{
				stringBuilder.Append(errs);
			}

			if (stringBuilder.Length != 0)
			{
				return stringBuilder.ToString();
			}
			return String.Empty;
		}

		public static string ValidateTranType(ProcessingInput processingInput)
		{
			StringBuilder stringBuilder = new StringBuilder();
			switch (processingInput.TranType)
			{
				case CCTranType.PriorAuthorizedCapture:
					if (String.IsNullOrWhiteSpace(processingInput.OrigTranID))
					{
						stringBuilder.AppendLine(Messages.OrigTranIDEmpty);
					}
					break;
				case CCTranType.CaptureOnly:
					if (String.IsNullOrWhiteSpace(processingInput.AuthCode) || processingInput.AuthCode.Length != 6)
					{
						stringBuilder.AppendLine(Messages.AuthCodeMustContain6Symbols);
					}
					break;
				case CCTranType.Credit:
					if (String.IsNullOrWhiteSpace(processingInput.OrigTranID))
					{
						stringBuilder.AppendLine(Messages.OrigTranIDEmpty);
					}
					break;
				case CCTranType.Void:
					if (String.IsNullOrWhiteSpace(processingInput.OrigTranID))
					{
						stringBuilder.AppendLine(Messages.OrigTranIDEmpty);
					}
					break;
				case CCTranType.VoidOrCredit:
					stringBuilder.AppendLine(Messages.VoidOrCreditIsNotImplemented);
					break;
			}
			return stringBuilder.ToString();
		}
	}
}
