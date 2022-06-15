using System;
using System.Collections.Generic;
using Autofac;
using PX.Data;
using PX.Objects.CA;
using PX.Objects.CS;

namespace PX.Objects.Localizations.GB
{
	internal class ServiceRegistration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
			.RegisterType<HSBCDirectDepositType>()
				.As<IDirectDepositType>();
		}
	}

	public class HSBCDirectDepositType : IDirectDepositType
	{
		#region Constants
		public const string Code = "BACSHSBC";
		public const string Decsription = "BACS HSBC payment";
		private const string DefaultExportScenarioGuid = "5DDA162B-92F9-4548-8FA9-17CCA4432787";

		private static class BACSHSBCDefaults
		{
			public enum AttributeName
			{
				// AP
				DestinationSortingCodeNumber,
				DestinationAccountNumber,
				DestinationAccountName,
				DestinationPaymentRef,
				// Remittance
				OriginatingSortingCodeNumber,
				OriginatingAccountNumber,
				OriginatingAccountName,
				SUN
			}

			// AP
			public const string DestinationSortingCodeNumber = "Destination sorting code number";
			public const string DestinationSortingCodeNumberMask = "000000";
			public const string DestinationSortingCodeNumberValExp = @"^\d{6,6}$";

			public const string DestinationAccountNumber = "Destination Account Number";
			public const string DestinationAccountNumberMask = "00000000";
			public const string DestinationAccountNumberValExp = @"^\d{8,8}$";

			public const string DestinationAccountName = "Destination Account Name";
			public const string DestinationAccountNameMask = "CCCCCCCCCCCCCCCCCC";

			public const string DestinationPaymentRef = "Destination Payment Ref";
			public const string DestinationPaymentRefMask = "CCCCCCCCCCCCCCCCCC";

			// Remittance
			public const string OriginatingSortingCodeNumber = "Originating sorting code number";
			public const string OriginatingSortingCodeNumberMask = "000000";
			public const string OriginatingSortingCodeNumberValExp = @"^\d{6,6}$";

			public const string OriginatingAccountNumber = "Originating account number";
			public const string OriginatingAccountNumberMask = "00000000";
			public const string OriginatingAccountNumberValExp = @"^\d{8,8}$";

			public const string OriginatingAccountName = "Originating Account Name";
			public const string OriginatingAccountNameMask = "CCCCCCCCCCCCCCCCCC";

			public const string SUN = "Service User Number (SUN)";
			public const string SUNMask = "000000";
			public const string SUNValExp = @"^\d{6,6}$";
		}
		#endregion

		#region IsActive
		public bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.uKLocalization>();
		}
		#endregion
		#region GetDirectDepositType
		public DirectDepositType GetDirectDepositType() => new DirectDepositType() { Code = Code, Description = Decsription };
		#endregion
		#region GetDefaults
		public IEnumerable<PaymentMethodDetail> GetDefaults()
		{
			return new List<PaymentMethodDetail>()
			{
				// AP
				new PaymentMethodDetail() { DetailID = "1", OrderIndex = 1, Descr = BACSHSBCDefaults.DestinationSortingCodeNumber, IsRequired = true, EntryMask = BACSHSBCDefaults.DestinationSortingCodeNumberMask,
					ValidRegexp = BACSHSBCDefaults.DestinationSortingCodeNumberValExp, UseFor = PaymentMethodDetailUsage.UseForVendor.ToString() },
				new PaymentMethodDetail() { DetailID = "2", OrderIndex = 2, Descr = BACSHSBCDefaults.DestinationAccountNumber, IsRequired = true, EntryMask = BACSHSBCDefaults.DestinationAccountNumberMask,
					ValidRegexp = BACSHSBCDefaults.DestinationAccountNumberValExp, UseFor = PaymentMethodDetailUsage.UseForVendor.ToString() },
				new PaymentMethodDetail() { DetailID = "3", OrderIndex = 3, Descr = BACSHSBCDefaults.DestinationAccountName, IsRequired = true, EntryMask = BACSHSBCDefaults.DestinationAccountNameMask,
					ValidRegexp = string.Empty, UseFor = PaymentMethodDetailUsage.UseForVendor.ToString() },
				new PaymentMethodDetail() { DetailID = "4", OrderIndex = 4, Descr = BACSHSBCDefaults.DestinationPaymentRef, IsRequired = true, EntryMask = BACSHSBCDefaults.DestinationPaymentRefMask,
					ValidRegexp = string.Empty, UseFor = PaymentMethodDetailUsage.UseForVendor.ToString() },
				// Remittance
				new PaymentMethodDetail() { DetailID = "1", OrderIndex = 1, Descr = BACSHSBCDefaults.OriginatingSortingCodeNumber, IsRequired = true, EntryMask = BACSHSBCDefaults.OriginatingSortingCodeNumberMask,
					ValidRegexp = BACSHSBCDefaults.OriginatingSortingCodeNumberValExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "2", OrderIndex = 2, Descr = BACSHSBCDefaults.OriginatingAccountNumber, IsRequired = true, EntryMask = BACSHSBCDefaults.OriginatingAccountNumberMask,
					ValidRegexp = BACSHSBCDefaults.OriginatingAccountNumberValExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "3", OrderIndex = 3, Descr = BACSHSBCDefaults.OriginatingAccountName, IsRequired = true, EntryMask = BACSHSBCDefaults.OriginatingAccountNameMask,
					ValidRegexp = string.Empty, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "4", OrderIndex = 4, Descr = BACSHSBCDefaults.SUN, IsRequired = false, EntryMask = BACSHSBCDefaults.SUNMask,
					ValidRegexp = BACSHSBCDefaults.SUNValExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() }
			};
		}	
		#endregion
		#region SetPaymentMethodDefaults
		public void SetPaymentMethodDefaults(PXCache cache)
		{
			PaymentMethod paymentMethod = (PaymentMethod)cache.Current;

			if (paymentMethod.DirectDepositFileFormat == Code)
			{
				cache.SetValueExt<PaymentMethod.useForAP>(cache.Current, true);
				cache.SetValueExt<PaymentMethod.useForAR>(cache.Current, false);
				cache.SetValueExt<PaymentMethod.useForCA>(cache.Current, true);
				cache.SetValueExt<PaymentMethod.aPAdditionalProcessing>(cache.Current, PaymentMethod.aPAdditionalProcessing.CreateBatchPayment);
				cache.SetValueExt<PaymentMethod.requireBatchSeqNum>(cache.Current, true);
				cache.SetValueExt<PaymentMethod.aPBatchExportSYMappingID>(cache.Current, Guid.Parse(DefaultExportScenarioGuid));
			}
		}
		#endregion
	}
}
