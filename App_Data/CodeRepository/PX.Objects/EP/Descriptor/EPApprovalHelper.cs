using PX.Data;
using PX.Objects.CA;
using PX.Objects.CM.Extensions;
using PX.Objects.Common.Interfaces;
using System;
using System.Linq;

namespace PX.Objects.EP
{
	/// <summary>
	/// A helper for the approval mechanism.
	/// </summary>
	public static class EPApprovalHelper
	{
		public static string BuildEPApprovalDetailsString(PXCache sender, IApprovalDescription currentDocument)
		{
			CashAccount ca = PXSelect<CashAccount>.Search<CashAccount.cashAccountID>(sender.Graph, currentDocument.CashAccountID).First();
			PaymentMethod pm = PXSelect<PaymentMethod>.Search<PaymentMethod.paymentMethodID>(sender.Graph, currentDocument.PaymentMethodID).First();
			CurrencyInfo ci = PXSelect<CurrencyInfo>.Search<CurrencyInfo.curyInfoID>(sender.Graph, currentDocument.CuryInfoID).First();

			return string.Concat(ca?.Descr, " (", pm?.Descr, "; ", GetChargeString(currentDocument, ci), ")");
		}

		private static string GetChargeString(IApprovalDescription currentDocument, CurrencyInfo ci)
		{
			if (currentDocument.CuryChargeAmt == null || currentDocument.CuryChargeAmt == 0.0m)
				return PXLocalizer.Localize(Common.Messages.NoCharges);
			else
			{
				int precision = ci.BasePrecision ?? 4;
				return string.Join("=",
					PXLocalizer.Localize(Common.Messages.Charges),
					Math.Round(currentDocument.CuryChargeAmt.Value, precision, MidpointRounding.AwayFromZero).ToString("N" + precision)
					);
			}
		}
	}
}