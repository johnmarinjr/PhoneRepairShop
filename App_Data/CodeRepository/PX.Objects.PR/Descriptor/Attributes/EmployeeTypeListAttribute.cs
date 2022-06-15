using PX.Data;
using PX.Data.BQL;
using PX.Objects.EP;

namespace PX.Objects.PR
{
	public class EmployeeType
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { SalariedExempt, SalariedNonExempt, Hourly, Other },
				new string[] { Messages.SalariedExempt, Messages.SalariedNonExempt, Messages.Hourly, Messages.Other })
			{ }
		}

		public class salariedExempt : BqlString.Constant<salariedExempt>
		{
			public salariedExempt() : base(SalariedExempt)
			{
			}
		}

		public class salariedNonExempt : BqlString.Constant<salariedNonExempt>
		{
			public salariedNonExempt() : base(SalariedNonExempt)
			{
			}
		}

		public const string SalariedExempt = "SAL";
		public const string SalariedNonExempt = "SLN";
		public const string Hourly = "HOR";
		public const string Other = "OTH";

		public static bool IsSalaried(string empType)
		{
			return empType == SalariedExempt || empType == SalariedNonExempt;
		}

		public static bool IsOvertimeEarningForSalariedExempt<ParentType>(PXCache cache, PREarningDetail row)
			 where ParentType : IEmployeeType
		{
			var parent = (IEmployeeType)PXParentAttribute.SelectParent(cache, row, typeof(ParentType));
			var earningCode = (EPEarningType)PXSelectorAttribute.Select<PREarningDetail.typeCD>(cache, row);
			return parent?.EmpType == EmployeeType.SalariedExempt && earningCode?.IsOvertime == true;
		}
	}
}
