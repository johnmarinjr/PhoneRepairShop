using PX.Data;
using PX.Objects.AP;
using System;
using System.Collections.Generic;

namespace PX.Objects.Extensions.MultiCurrency.AP
{
	public abstract class APMultiCurrencyGraph<TGraph, TPrimary> : FinDocMultiCurrencyGraph<TGraph, TPrimary>
		where TGraph : PXGraph
		where TPrimary : class, IBqlTable, new()
	{
		protected override string Module => GL.BatchModule.AP;
		protected override IEnumerable<Type> FieldWhichShouldBeRecalculatedAnyway
		{
			get
			{
				yield return typeof(APInvoice.curyDocBal);
				yield return typeof(APInvoice.curyDiscBal);
			}
		}

		protected override CurySourceMapping GetCurySourceMapping()
		{
			return new CurySourceMapping(typeof(Vendor));
		}

		protected override bool ShouldBeDisabledDueToDocStatus()
		{
			switch (DocumentStatus)
			{
				case APDocStatus.Open:
				case APDocStatus.Closed:
				case APDocStatus.Prebooked:
				case APDocStatus.PendingApproval:
					return true;
				default: return false;
			}
		}
	}
}
