using System;
using System.Collections.Generic;
using Autofac;
using PX.Data;
using PX.Objects.CA;
using PX.Objects.CS;

namespace PX.Objects.Localizations.CA
{
	internal class ServiceRegistration : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder
			.RegisterType<EFTDirectDepositType>()
				.As<IDirectDepositType>();
		}
	}
	public class EFTDirectDepositType : IDirectDepositType
	{
		#region Constants
		public const string Code = "CPA005";
		public const string Decsription = "CPA005";
		private const string DefaultExportScenarioGuid = "8C8402E8-AD01-4D8F-AA80-877F4EDC3B96";

		private static class CPA005Defaults
		{
			public enum AttributeName
			{
				// AP
				Bank,
				Branch,
				Account,
				BeneficiaryName,
				// Remittance
				Name,
				ShortName,
				BankRemittance,
				BranchRemittance,
				AccountRemittance,
				OriginatorID,
				DataCenter,
				CompresstoZIPformat,
				FileName
			}
			// Descriptions
			// AP
			public const string Bank = "Bank";
			public const string Branch = "Branch";
			public const string Account = "Account";
			public const string BeneficiaryName = "Beneficiary Name";
			// Remittance
			public const string Name = "Name";
			public const string ShortName = "Short Name";
			public const string BankRemittance = "Bank";
			public const string BranchRemittance = "Branch";
			public const string AccountRemittance = "Account";
			public const string OriginatorID = "Originator ID";
			public const string DataCenter = "Data Center";
			public const string CompresstoZIPformat = "Compress to ZIP format";
			public const string FileName = "File Name";
			// Masks
			// AP
			public const string BankMask = "0000";
			public const string BranchMask = "00000";
			public const string AccountMask = "AAAAAAAAAAAA";
			public const string BeneficiaryNameMask = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			// Remittance
			public const string NameMask = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
			public const string ShortNameMask = "AAAAAAAAAAAAAAA";
			public const string BankRemittanceMask = "0000";
			public const string BranchRemittanceMask = "00000";
			public const string AccountRemittanceMask = "AAAAAAAAAAAA";
			public const string OriginatorIDMask = "AAAAAAAAAA";
			public const string CompresstoZIPformatMask = "0";
			public const string FileNameMask = "AAAAAAAAAAAAAAAAAAAAAAAAA";
			// Validation expressions
			// AP
			public const string BankValidationExp = @"^\d{3,3}$";
			public const string BranchValidationExp = @"^\d{5,5}$";
			public const string AccountValidationExp = @"^([\w]|\s){1,12}$";
			public const string BeneficiaryNameValidationExp = @"^([\w]|\s){1,30}$";
			// Remittance
			public const string NameValidationExp = @"^([\w]|\s){1,30}$";
			public const string ShortNameValidationExp = @"^([\w]|\s){1,15}$";
			public const string BankRemittanceValidationExp = @"^\d{3,3}$";
			public const string BranchRemittanceValidationExp = @"^\d{5,5}$";
			public const string AccountRemittanceValidationExp = @"^([\w]|\s){1,12}$";
			public const string OriginatorIDValidationExp = @"^([\w]|\s){1,10}$";
			public const string DataCenterValidationExp = @"^\d{5,5}$";
			public const string CompresstoZIPformatValidationExp = @"^(1|0)$";
			public const string FileNameValidationExp = @"^(?!\.)([\w\s-.](?!\.\.)){1,25}(?<!\.)$";
		}
		#endregion

		#region IsActive
		public bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.canadianLocalization>();
		}
		#endregion
		#region GetDirectDepositType
		public DirectDepositType GetDirectDepositType()
		{
			return new DirectDepositType() { Code = Code, Description = Decsription };
		}
		#endregion
		#region GetDefaults
		public IEnumerable<PaymentMethodDetail> GetDefaults()
		{
			return new List<PaymentMethodDetail>()
			{
				// AP
				new PaymentMethodDetail() { DetailID = "1", OrderIndex = 1, Descr = CPA005Defaults.Bank, IsRequired = true, EntryMask = CPA005Defaults.BankMask,
					ValidRegexp = CPA005Defaults.BankValidationExp, UseFor = PaymentMethodDetailUsage.UseForVendor.ToString() },
				new PaymentMethodDetail() { DetailID = "2", OrderIndex = 2, Descr = CPA005Defaults.Branch, IsRequired = false, EntryMask = CPA005Defaults.BranchMask,
					ValidRegexp = CPA005Defaults.BranchValidationExp, UseFor = PaymentMethodDetailUsage.UseForVendor.ToString() },
				new PaymentMethodDetail() { DetailID = "3", OrderIndex = 3, Descr = CPA005Defaults.Account, IsRequired = true, EntryMask = CPA005Defaults.AccountMask,
					ValidRegexp = CPA005Defaults.AccountValidationExp, UseFor = PaymentMethodDetailUsage.UseForVendor.ToString() },
				new PaymentMethodDetail() { DetailID = "4", OrderIndex = 4, Descr = CPA005Defaults.BeneficiaryName, IsRequired = true, EntryMask = CPA005Defaults.BeneficiaryNameMask,
					ValidRegexp = CPA005Defaults.BeneficiaryNameValidationExp, UseFor = PaymentMethodDetailUsage.UseForVendor.ToString() },
				// Remittance
				new PaymentMethodDetail() { DetailID = "1", OrderIndex = 1, Descr = CPA005Defaults.Name, IsRequired = true, EntryMask = CPA005Defaults.NameMask,
					ValidRegexp = CPA005Defaults.NameValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "2", OrderIndex = 2, Descr = CPA005Defaults.ShortName, IsRequired = true, EntryMask = CPA005Defaults.ShortNameMask,
					ValidRegexp = CPA005Defaults.ShortNameValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "3", OrderIndex = 3, Descr = CPA005Defaults.BankRemittance, IsRequired = true, EntryMask = CPA005Defaults.BankRemittanceMask,
					ValidRegexp = CPA005Defaults.BankRemittanceValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "4", OrderIndex = 4, Descr = CPA005Defaults.BranchRemittance, IsRequired = false, EntryMask = CPA005Defaults.BranchRemittanceMask,
					ValidRegexp = CPA005Defaults.BranchRemittanceValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "5", OrderIndex = 5, Descr = CPA005Defaults.AccountRemittance, IsRequired = true, EntryMask = CPA005Defaults.AccountRemittanceMask,
					ValidRegexp = CPA005Defaults.AccountRemittanceValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "6", OrderIndex = 6, Descr = CPA005Defaults.OriginatorID, IsRequired = true, EntryMask = CPA005Defaults.OriginatorIDMask,
					ValidRegexp = CPA005Defaults.OriginatorIDValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "7", OrderIndex = 7, Descr = CPA005Defaults.DataCenter, IsRequired = true, EntryMask = string.Empty,
					ValidRegexp = CPA005Defaults.DataCenterValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "8", OrderIndex = 8, Descr = CPA005Defaults.CompresstoZIPformat, IsRequired = false, EntryMask = CPA005Defaults.CompresstoZIPformatMask,
					ValidRegexp = CPA005Defaults.CompresstoZIPformatValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
				new PaymentMethodDetail() { DetailID = "9", OrderIndex = 9, Descr = CPA005Defaults.FileName, IsRequired = false, EntryMask = CPA005Defaults.FileNameMask,
					ValidRegexp = CPA005Defaults.FileNameValidationExp, UseFor = PaymentMethodDetailUsage.UseForCashAccount.ToString() },
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
