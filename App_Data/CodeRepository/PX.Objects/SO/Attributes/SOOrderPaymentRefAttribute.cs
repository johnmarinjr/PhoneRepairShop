using PX.Common;
using PX.Data;
using PX.Objects.AR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO.Attributes
{
	/// <summary>
	/// This attribute implements auto-generation of the next check sequential number for Sales Order document<br/>
	/// according to the settings in the CashAccount and PaymentMethod. <br/>
	/// </summary>
	public class SOOrderPaymentRefAttribute : PaymentRefAttribute
	{
		public SOOrderPaymentRefAttribute(Type cashAccountID, Type paymentTypeID, Type updateNextNumber)
			: base(cashAccountID, paymentTypeID, updateNextNumber)
		{
		}

		protected virtual bool IsEnabled(SOOrder order)
			=> (order?.ARDocType?.IsIn(ARDocType.CashSale, ARDocType.CashReturn) == true);

		public override void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (!IsEnabled(e.Row as SOOrder))
				return;

			// Acuminator disable once PX1075 RaiseExceptionHandlingInEventHandlers Legacy code - PaymentRefAttribute
			base.FieldDefaulting(sender, e);
		}

		public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (!IsEnabled(e.Row as SOOrder))
				return;

			base.FieldVerifying(sender, e);
		}

		public override void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if (!IsEnabled(e.Row as SOOrder))
				return;

			// Acuminator disable once PX1043 SavingChangesInEventHandlers Legacy code - PaymentRefAttribute
			base.RowPersisting(sender, e);
		}

		protected override void DefaultRef(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (!IsEnabled(e.Row as SOOrder))
				return;

			base.DefaultRef(sender, e);
		}
	}
}
