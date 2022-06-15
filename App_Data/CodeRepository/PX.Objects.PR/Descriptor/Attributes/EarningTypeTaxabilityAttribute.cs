using PX.Data;
using PX.Payroll.Data;
using PX.Payroll.Data.Vertex;
using System;

namespace PX.Objects.PR
{
	public class EarningTypeTaxabilityAttribute : PXIntListAttribute, IPXRowSelectedSubscriber, IPXFieldVerifyingSubscriber, IPXRowPersistingSubscriber
	{
		private Type _CountryIDField;
		private Type _TaxIDField;

		public EarningTypeTaxabilityAttribute(Type countryIDField, Type taxIDField) : base(
			new (int, string)[]
			{
				((int)CompensationType.CashSubjectTaxable, Messages.CashSubjectTaxable),
				((int)CompensationType.CashSubjectNonTaxable, Messages.CashSubjectNonTaxable),
				((int)CompensationType.CashNonSubjectNonTaxable, Messages.CashNonSubjectNonTaxable),
				((int)CompensationType.NonCashSubjectTaxable, Messages.NonCashSubjectTaxable),
				((int)CompensationType.NonCashSubjectNonTaxable, Messages.NonCashSubjectNonTaxable),
				((int)CompensationType.NonCashNonSubjectNonTaxable, Messages.NonCashNonSubjectNonTaxable),
			})
		{
			_CountryIDField = countryIDField;
			_TaxIDField = taxIDField;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			sender.Graph.RowSelected.AddHandler(_TaxIDField, (cache, e) =>
			{
				if (e.Row == null)
				{
					return;
				}

				PXUIFieldAttribute.SetEnabled(sender, e.Row, _TaxIDField.Name, UseTaxabilityField(sender, e.Row));
			});
		}

		public void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (e.Row == null || !UseTaxabilityField(sender, e.Row) || e.NewValue != null)
			{
				return;
			}

			throw new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(sender, FieldName));
		}

		public void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (e.Row == null || !UseTaxabilityField(sender, e.Row) || sender.GetValue(e.Row, FieldName) != null)
			{
				return;
			}

			throw new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(sender, FieldName));
		}

		public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetVisible(sender, e.Row, FieldName, UseTaxabilityField(sender, e.Row));
		}

		private bool UseTaxabilityField(PXCache sender, object row)
		{
			return Equals(sender.GetValue(row, _CountryIDField.Name), LocationConstants.CanadaCountryCode);
		}
	}
}
