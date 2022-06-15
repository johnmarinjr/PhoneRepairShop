using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.Extensions.MultiCurrency
{
	/// <summary>The generic graph extension that defines the multi-currency functionality extendend for AP/AR Entities.</summary>
	/// <typeparam name="TGraph">A <see cref="PX.Data.PXGraph" /> type.</typeparam>
	/// <typeparam name="TPrimary">A DAC (a <see cref="PX.Data.IBqlTable" /> type).</typeparam>
	public abstract class FinDocMultiCurrencyGraph<TGraph, TPrimary> : MultiCurrencyGraph<TGraph, TPrimary>, IPXCurrencyHelper
			where TGraph : PXGraph
			where TPrimary : class, IBqlTable, new()
	{
		/// <summary>
		/// Override to specify the way current document status should be obtained. Override with returning Balanced status code if it is processing screen
		/// </summary>
		protected abstract string DocumentStatus { get; }

		/// <summary>
		/// Some base values should be recalculated even their Cury fields are marked as BaseCalc = false: DocBal and DiscBal, for example.
		/// Override this property to list them. This emulates behavior of old PXDBCurrencyAttribute.SetBaseCalc<ARInvoice.curyDiscBal>(cache, doc, state.BalanceBaseCalc);
		/// </summary>
		protected abstract IEnumerable<Type> FieldWhichShouldBeRecalculatedAnyway { get; }

		protected abstract bool ShouldBeDisabledDueToDocStatus();

		protected override bool AllowOverrideCury()
		{
			return base.AllowOverrideCury() && !ShouldBeDisabledDueToDocStatus();
		}

		protected override void DateFieldUpdated<CuryInfoID, DocumentDate>(PXCache sender, IBqlTable row)
		{
			if (ShouldBeDisabledDueToDocStatus()) return;
			else base.DateFieldUpdated<CuryInfoID, DocumentDate>(sender, row);
		}

		protected override bool ShouldMainCurrencyInfoBeReadonly()
		{
			if (Base.IsContractBasedAPI) //If it is API, random entity appears in Current, so  ShouldBeDisabledDueToDocStatus cannot make sense
				return base.ShouldMainCurrencyInfoBeReadonly();
			else
				return base.ShouldMainCurrencyInfoBeReadonly() || ShouldBeDisabledDueToDocStatus();
		}

		protected virtual void _(Events.RowSelected<TPrimary> e)
		{
			foreach (Type dacType in TrackedItems.Keys)
			{
				if (!typeof(TPrimary).IsAssignableFrom(dacType))
					continue;

				var curyFields = TrackedItems[dacType];
				foreach (var fieldToRecalculate in FieldWhichShouldBeRecalculatedAnyway)
				{
					var curyField = curyFields.SingleOrDefault(f => f.CuryName.Equals(fieldToRecalculate.Name, StringComparison.OrdinalIgnoreCase));
					if (curyField != null)
					{
						curyField.BaseCalc = !ShouldBeDisabledDueToDocStatus();
					}
				}
			}
		}
	}
}
