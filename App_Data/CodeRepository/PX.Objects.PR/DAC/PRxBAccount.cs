using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using System;

namespace PX.Objects.PR
{
	public class PRxBAccount : PXCacheExtension<BAccount>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>();
		}

		#region CRAPayrollAccountNumber
		public abstract class craPayrollAccountNumber : PX.Data.BQL.BqlString.Field<craPayrollAccountNumber> { }
		/// <summary>
		/// CRA (Canada Revenue Agency) Payroll Account Number that is needed for Record of Employment.
		/// </summary>
		[PXDBString(15, InputMask = ">000000000LL0000")]
		[PXUIField(DisplayName = "CRA Payroll Account Number")]
		public virtual String CRAPayrollAccountNumber { get; set; }
		#endregion
	}
}
