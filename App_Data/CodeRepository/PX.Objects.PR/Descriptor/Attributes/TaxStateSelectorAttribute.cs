using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Payroll.Data;
using System;

namespace PX.Objects.PR
{
	public class TaxStateSelectorAttribute : PXSelectorAttribute
	{
		private Type _CountryIDField;

		public TaxStateSelectorAttribute(Type countryIDField)
			: base(BqlTemplate.OfCommand<
				  SearchFor<State.stateID>
					.Where<State.countryID.IsEqual<BqlPlaceholder.A.AsField.FromCurrent>>>
				.Replace<BqlPlaceholder.A>(countryIDField)
				.ToType())
		{
			DescriptionField = typeof(State.name);
			Filterable = true;
			_CountryIDField = countryIDField;
		}

		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.FieldSelecting(sender, e);

			if (e.ReturnValue == null)
			{
				if (sender.GetValue(e.Row, _CountryIDField.Name)?.Equals(LocationConstants.CanadaCountryCode) == true)
				{
					e.ReturnValue = LocationConstants.CanadaFederalStateCode;
				}
				else
				{
					e.ReturnValue = LocationConstants.USFederalStateCode;
				}
			}
		}
	}
}
