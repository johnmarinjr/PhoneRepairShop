using System;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Specific;
using PX.Objects.CA;
using V2 = PX.CCProcessingBase.Interfaces.V2;
using PX.Data;
using System.Linq;

namespace PX.Objects.AR.CCPaymentProcessing.Helpers
{
	public class CCProcessingFeatureHelper
	{
		public static bool IsFeatureSupported(Type pluginType, CCProcessingFeature feature)
		{
			bool result = false;
			if (typeof(V2.ICCProcessingPlugin).IsAssignableFrom(pluginType))
			{
				V2.ICCProcessingPlugin plugin = (V2.ICCProcessingPlugin)Activator.CreateInstance(pluginType);
				Func<object>[] checkFuncArr = null;
				switch (feature)
				{
					case CCProcessingFeature.ProfileManagement:
						checkFuncArr = new Func<object>[] { () => plugin.CreateProcessor<V2.ICCProfileProcessor>(null) };
						break;
					case CCProcessingFeature.HostedForm:
						checkFuncArr = new Func<object>[] { () => plugin.CreateProcessor<V2.ICCHostedFormProcessor>(null) };
						break;
					case CCProcessingFeature.ExtendedProfileManagement:
						checkFuncArr = new Func<object>[] { () => plugin.CreateProcessor<V2.ICCProfileProcessor>(null) };
						break;
					case CCProcessingFeature.PaymentHostedForm:
						checkFuncArr = new Func<object>[] {
							() => plugin.CreateProcessor<V2.ICCHostedPaymentFormProcessor>(null),
							() => plugin.CreateProcessor<V2.ICCTransactionGetter>(null),
							() => plugin.CreateProcessor<V2.ICCProfileCreator>(null),
							() => plugin.CreateProcessor<V2.ICCHostedPaymentFormResponseParser>(null)
						};
						break;
					case CCProcessingFeature.WebhookManagement:
						checkFuncArr = new Func<object>[] {
							() => plugin.CreateProcessor<V2.ICCWebhookProcessor>(null),
							() => plugin.CreateProcessor<V2.ICCWebhookResolver>(null)
						};
						break;
					case CCProcessingFeature.TransactionGetter:
						checkFuncArr = new Func<object>[] { () => plugin.CreateProcessor<V2.ICCTransactionGetter>(null) };
						break;
					case CCProcessingFeature.PaymentForm:
						checkFuncArr = new Func<object>[] {
							() => plugin.CreateProcessor<V2.ICCPaymentFormProcessor>(null),
							() => plugin.CreateProcessor<V2.ICCTransactionGetter>(null),
							() => plugin.CreateProcessor<V2.ICCProfileCreator>(null)
						};
						break;
					case CCProcessingFeature.ProfileForm:
						checkFuncArr = new Func<object>[] {
							() => plugin.CreateProcessor<V2.ICCProfileFormProcessor>(null)
						};
						break;
					case CCProcessingFeature.CapturePreauthorization:
						return IsFeatureSupportedByPlugin(plugin, V2.PluginFeature.CaptureByAuthorizationCode) ?? true;
					case CCProcessingFeature.ProfileEditForm:
						return IsFeatureSupportedByPlugin(plugin, V2.PluginFeature.ProfileEditForm) ?? true;
				}

				if (checkFuncArr != null)
				{
					result = checkFuncArr.All(f => CheckV2TypeWrapper(f));
				}
			}
	
			return result;
		}

		private static bool CheckV2TypeWrapper(Func<object> check)
		{
			bool ret = true;
			try
			{
				object res = check();
				ret = res != null;
			}
			catch
			{}
			return ret;
		}

		public static bool IsFeatureSupported(CCProcessingCenter ccProcessingCenter, CCProcessingFeature feature)
		{
			return IsFeatureSupported(ccProcessingCenter, feature, false);
		}

		public static bool IsFeatureSupported(CCProcessingCenter ccProcessingCenter, CCProcessingFeature feature, bool throwOnError)
		{
			if (string.IsNullOrEmpty(ccProcessingCenter?.ProcessingTypeName))
			{
				if (throwOnError)
					throw new PXException(Messages.ERR_PluginTypeIsNotSelectedForProcessingCenter, ccProcessingCenter?.ProcessingCenterID);
				else
					return false;
			}

			try
			{
				Type procType = CCPluginTypeHelper.GetPluginTypeWithCheck(ccProcessingCenter);
				bool ret = IsFeatureSupported(procType, feature);
				return ret;
			}
			catch
			{
				if (throwOnError)
					throw;
				else
					return false;
			}
		}

		public static void CheckProcessing(CCProcessingCenter processingCenter, CCProcessingFeature feature, CCProcessingContext newContext)
		{
			CheckProcessingCenter(processingCenter);
			newContext.processingCenter = processingCenter;
			if (feature != CCProcessingFeature.Base && !CCProcessingFeatureHelper.IsFeatureSupported(processingCenter, feature, true))
			{
				throw new PXException(Messages.FeatureNotSupportedByProcessing, feature.ToString());
			}
		}

		public static bool IsPaymentHostedFormSupported(CCProcessingCenter procCenter)
		{
			return IsFeatureSupported(procCenter, CCProcessingFeature.PaymentHostedForm, false)
				|| IsFeatureSupported(procCenter, CCProcessingFeature.PaymentForm, false);
		}

		private static void CheckProcessingCenter(CCProcessingCenter processingCenter)
		{
			if (processingCenter == null)
			{
				throw new PXException(Messages.ERR_CCProcessingCenterNotFound);
			}
			if (processingCenter.IsActive != true)
			{
				throw new PXException(Messages.ERR_CCProcessingCenterIsNotActive);
			}
			if (string.IsNullOrEmpty(processingCenter.ProcessingTypeName))
			{
				throw new PXException(Messages.ERR_ProcessingCenterForCardNotConfigured);
			}
		}

		private static bool? IsFeatureSupportedByPlugin(V2.ICCProcessingPlugin plugin, V2.PluginFeature pluginFeature)
		{
			var optionsGetter = plugin.CreateProcessor<V2.ICCFeatureManager>(null);
			return optionsGetter?.IsFeatureSupported(pluginFeature);
		}
	}
}
