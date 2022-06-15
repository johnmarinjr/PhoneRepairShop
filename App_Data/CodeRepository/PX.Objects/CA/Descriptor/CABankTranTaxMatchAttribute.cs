using System;
using System.Linq;
using System.Collections.Generic;
using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.TX;
using PX.Objects.Common;
using PX.Objects.CM;

namespace PX.Objects.CA
{
	public class CABankTranTaxMatchAttribute : CABankTranTaxAttribute
	{
		public CABankTranTaxMatchAttribute(Type parentType, Type taxType, Type taxSumType, Type calcMode = null, Type parentBranchIDField = null)
			: base(parentType, taxType, taxSumType, calcMode, parentBranchIDField) { }

		protected override object GetCurrent(PXGraph graph)
		{
			return ((CABankMatchingProcess)graph).CABankTran.Current;
		}

		public override void CacheAttached(PXCache sender)
		{
			if (sender.Graph is CABankMatchingProcess)
			{
				base.CacheAttached(sender);
			}
			else
			{
				this.TaxCalc = TaxCalc.NoCalc;
			}
		}
	}
}
