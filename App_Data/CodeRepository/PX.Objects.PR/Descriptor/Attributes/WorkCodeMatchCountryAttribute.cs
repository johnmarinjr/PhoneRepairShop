using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR
{
	public class WorkCodeMatchCountryAttribute : PMWorkCodeAttribute
	{
		public WorkCodeMatchCountryAttribute(Type countryIDField)
		{
			if (_SelAttrIndex != -1)
			{
				_Attributes.RemoveAt(_SelAttrIndex);
				_SelAttrIndex = -1;
			}

			Type search = BqlTemplate.OfCommand<SearchFor<PMWorkCode.workCodeID>
				.Where<PRxPMWorkCode.countryID.IsEqual<BqlPlaceholder.A.AsField.FromCurrent>>>
				.Replace<BqlPlaceholder.A>(countryIDField)
				.ToType();

			PXSelectorAttribute select = new PXSelectorAttribute(search, DescriptionField = typeof(PMWorkCode.description));
			_Attributes.Add(select);
			_SelAttrIndex = _Attributes.Count - 1;
		}
	}
}
