using PX.Data;
using PX.Objects.AR;
using System;
using System.Collections.Generic;

namespace PX.Objects.Extensions.MultiCurrency.AR
{
	public abstract class ARMultiCurrencyGraph<TGraph, TPrimary> : FinDocMultiCurrencyGraph<TGraph, TPrimary>
		where TGraph : PXGraph
		where TPrimary : class, IBqlTable, new()
	{
		protected override string Module => GL.BatchModule.AR;

		protected override IEnumerable<Type> FieldWhichShouldBeRecalculatedAnyway
		{
			get
			{
				yield return typeof(ARInvoice.curyDocBal);
				yield return typeof(ARInvoice.curyDiscBal);
			}
		}

		protected override CurySourceMapping GetCurySourceMapping()
		{
			return new CurySourceMapping(typeof(Customer));
		}

		protected override bool ShouldBeDisabledDueToDocStatus()
		{
			switch (DocumentStatus)
			{
				case ARDocStatus.Open:
				case ARDocStatus.Closed:
				case ARDocStatus.PendingApproval:
					return true;
				default: return false;
			}
		}
	}
}
