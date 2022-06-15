using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using System;

namespace PX.Objects.PR
{
	public class DeductionActiveSelectorAttribute : PXSelectorAttribute
	{
		public DeductionActiveSelectorAttribute(Type where, Type matchCountryIDField) : 
			base(BqlTemplate.OfCommand<SearchFor<PRDeductCode.codeID>
				.Where<PRDeductCode.isActive.IsEqual<True>
					.And<PRDeductCode.countryID.IsEqual<BqlPlaceholder.A.AsField.FromCurrent>>
					.And<BqlPlaceholder.B>>>
				.Replace<BqlPlaceholder.A>(matchCountryIDField)
				.Replace<BqlPlaceholder.B>(where == null ? typeof(Where<True.IsEqual<True>>) : where)
				.ToType())
		{
			SubstituteKey = typeof(PRDeductCode.codeCD);
			DescriptionField = typeof(PRDeductCode.description);
		}

		public DeductionActiveSelectorAttribute(Type where) :
			base(BqlTemplate.OfCommand<SearchFor<PRDeductCode.codeID>
				.Where<PRDeductCode.isActive.IsEqual<True>
					.And<MatchPRCountry<PRDeductCode.countryID>>
					.And<BqlPlaceholder.A>>>
				.Replace<BqlPlaceholder.A>(where == null ? typeof(Where<True.IsEqual<True>>) : where)
				.ToType())
		{
			SubstituteKey = typeof(PRDeductCode.codeCD);
			DescriptionField = typeof(PRDeductCode.description);
		}
	}
}
