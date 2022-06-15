using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CS;
using PX.Payroll.Data;
using System.Collections;
using System.Collections.Generic;

namespace PX.Objects.PR
{
	public class PRAcaDedBenCodeMaint : PXGraphExtension<PRDedBenCodeMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollUS>();
		}

		protected virtual void _(Events.RowUpdating<PRDeductCode> e)
		{
			PRDeductCode row = e.NewRow;
			if (row == null)
			{
				return;
			}

			if (row.IsWorkersCompensation == true)
			{
				PRAcaDeductCode acaExt = PXCache<PRDeductCode>.GetExtension<PRAcaDeductCode>(row);
				acaExt.AcaApplicable = false;
			}
		}

		protected virtual void _(Events.RowSelected<PRDeductCode> e)
		{
			if (e.Row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetEnabled<PRAcaDeductCode.acaApplicable>(e.Cache, e.Row, e.Row.CountryID == LocationConstants.USCountryCode);
		}
	}
}