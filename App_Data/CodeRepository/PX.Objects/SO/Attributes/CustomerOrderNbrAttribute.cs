using PX.Common;
using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using CreateSOOrderFilter = PX.Objects.PO.DAC.Unbound.CreateSOOrderFilter;

namespace PX.Objects.SO.Attributes
{
	public abstract class CustomerOrderNbrBaseAttribute : PXDefaultAttribute, IPXFieldVerifyingSubscriber, IPXRowUpdatedSubscriber, IPXRowSelectedSubscriber
	{
		protected readonly Type _orderTypeField;
		protected readonly Type _orderNbrField;
		protected readonly Type _customerIDField;

		public CustomerOrderNbrBaseAttribute(Type orderTypeField, Type orderNbrField, Type customerIDField)
		{
			PersistingCheck = PXPersistingCheck.Nothing;

			_orderTypeField = orderTypeField;
			_orderNbrField = orderNbrField;
			_customerIDField = customerIDField;
		}

		public virtual void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			var newValue = (string)e.NewValue;
			string orderType = (string)sender.GetValue(e.Row, _orderTypeField.Name);
			string orderNbr = (string)sender.GetValue(e.Row, _orderNbrField.Name);
			int? customerID = (int?)sender.GetValue(e.Row, _customerIDField.Name);
			if (orderType == null || customerID == null)
				return;

			var soOrderType = SOOrderType.PK.Find(sender.Graph, orderType);
			if (soOrderType.CustomerOrderIsRequired != true || string.IsNullOrWhiteSpace(newValue) ||
				soOrderType.CustomerOrderValidation.IsNotIn(CustomerOrderValidationType.Warn, CustomerOrderValidationType.Error))
				return;

			SOOrder duplicateOrder = FindCustomerOrder(sender, orderType, orderNbr, customerID, newValue, e.Row);
			if (duplicateOrder == null)
				return;

			if (soOrderType.CustomerOrderValidation == CustomerOrderValidationType.Error)
			{
				throw new PXSetPropertyException(Messages.CustomerOrderHasDuplicateError, PXErrorLevel.Error, duplicateOrder.OrderNbr);
			}

			sender.RaiseExceptionHandling(_FieldName, e.Row, newValue,
				new PXSetPropertyException(Messages.CustomerOrderHasDuplicateWarning, PXErrorLevel.Warning, duplicateOrder.OrderNbr));
		}

		public virtual void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			string orderType = (string)sender.GetValue(e.Row, _orderTypeField.Name);
			bool customerOrderNbrIsRequired = SOOrderType.PK.Find(sender.Graph, orderType)?.CustomerOrderIsRequired == true;
			sender.Adjust<PXUIFieldAttribute>(e.Row).For(_FieldName, a => a.Required = customerOrderNbrIsRequired);
			PersistingCheck = customerOrderNbrIsRequired ? PXPersistingCheck.Null : PXPersistingCheck.Nothing;
		}

		public virtual void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			string orderType = (string)sender.GetValue(e.Row, _orderTypeField.Name);
			string orderNbr = (string)sender.GetValue(e.Row, _orderNbrField.Name);
			int? customerID = (int?)sender.GetValue(e.Row, _customerIDField.Name);
			int? oldCustomerID = (int?)sender.GetValue(e.OldRow, _customerIDField.Name);
			string customerOrderNbr = (string)sender.GetValue(e.Row, _FieldName);
			if (orderType == null || customerID == null || oldCustomerID == customerID)
				return;

			var soOrderType = SOOrderType.PK.Find(sender.Graph, orderType);
			if (soOrderType.CustomerOrderIsRequired != true || string.IsNullOrWhiteSpace(customerOrderNbr) ||
				soOrderType.CustomerOrderValidation.IsNotIn(CustomerOrderValidationType.Warn, CustomerOrderValidationType.Error))
				return;

			SOOrder duplicateOrder = FindCustomerOrder(sender, orderType, orderNbr, customerID, customerOrderNbr, e.Row);
			if (duplicateOrder == null)
				return;

			var exception = soOrderType.CustomerOrderValidation == CustomerOrderValidationType.Error
				? new PXSetPropertyException(Messages.CustomerOrderHasDuplicateError, PXErrorLevel.Error, duplicateOrder.OrderNbr)
				: new PXSetPropertyException(Messages.CustomerOrderHasDuplicateWarning, PXErrorLevel.Warning, duplicateOrder.OrderNbr);
			sender.RaiseExceptionHandling(_FieldName, e.Row, customerOrderNbr, exception);
		}

		protected virtual SOOrder FindCustomerOrder(PXCache orderCache, string orderType, string orderNbr, int? customerID, string customerOrderNbr, object row)
		{
			return PXSelectReadonly<SOOrder,
				Where<SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
					And<SOOrder.customerID, Equal<Required<SOOrder.customerID>>,
					And<SOOrder.customerOrderNbr, Equal<Required<SOOrder.customerOrderNbr>>,
					And<SOOrder.orderNbr, NotEqual<Required<SOOrder.orderNbr>>>>>>>
				.SelectWindowed(orderCache.Graph, 0, 1, orderType, customerID, customerOrderNbr, orderNbr ?? string.Empty);
		}
	}

	public class CustomerOrderNbrLightAttribute : CustomerOrderNbrBaseAttribute
	{
		public CustomerOrderNbrLightAttribute()
			: base(typeof(CreateSOOrderFilter.orderType), typeof(CreateSOOrderFilter.orderNbr), typeof(CreateSOOrderFilter.customerID))
		{
		}

		public override void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.RowSelected(sender, e);

			var filter = (CreateSOOrderFilter)e.Row;
			var soOrderType = SOOrderType.PK.Find(sender.Graph, filter.OrderType);
			bool manualNumbering = Numbering.PK.Find(sender.Graph, soOrderType?.OrderNumberingID)?.UserNumbering == true;

			PXUIFieldAttribute.SetVisible<CreateSOOrderFilter.orderNbr>(sender, e.Row, manualNumbering);
			PXDefaultAttribute.SetPersistingCheck<CreateSOOrderFilter.orderNbr>(sender, e.Row, manualNumbering ? PXPersistingCheck.Null : PXPersistingCheck.Nothing);
		}
	}

	public class CustomerOrderNbrAttribute : CustomerOrderNbrBaseAttribute
	{
		public CustomerOrderNbrAttribute()
			: base(typeof(SOOrder.orderType), typeof(SOOrder.orderNbr), typeof(SOOrder.customerID))
		{
		}

		public override void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
		}

		public override void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			var order = (SOOrder)e.Row;
			if (order?.OrderType == null || order.CustomerID == null || e.Operation.Command().IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			var orderType = SOOrderType.PK.Find(sender.Graph, order.OrderType);
			if (orderType.CustomerOrderIsRequired != true)
				return;

			if (string.IsNullOrWhiteSpace(order.CustomerOrderNbr))
			{
				bool showOrderNbr = (e.Operation.Command() != PXDBOperation.Insert && sender.Graph.UnattendedMode);

				if (!showOrderNbr && sender.RaiseExceptionHandling<SOOrder.customerOrderNbr>(
					order,
					null,
					new PXSetPropertyKeepPreviousException(Messages.CustomerOrderIsEmpty)))
				{
					throw new PXRowPersistingException(nameof(SOOrder.customerOrderNbr), null, Messages.CustomerOrderIsEmpty);
				}

				if (showOrderNbr && sender.RaiseExceptionHandling<SOOrder.customerOrderNbr>(
					order,
					null,
					new PXSetPropertyKeepPreviousException(Messages.CustomerOrderIsEmptyOrderNbr, PXErrorLevel.Error, order.OrderNbr)))
				{
					throw new PXRowPersistingException(nameof(SOOrder.customerOrderNbr), null, Messages.CustomerOrderIsEmptyOrderNbr, order.OrderNbr);
				}
			}

			if (orderType.CustomerOrderValidation != CustomerOrderValidationType.Error)
				return;

			SOOrder duplicateOrder = FindCustomerOrderDuplicate(sender, order.CustomerOrderNbr, order);
			if (duplicateOrder != null &&
				sender.RaiseExceptionHandling<SOOrder.customerOrderNbr>(
					order,
					order.CustomerOrderNbr,
					new PXSetPropertyException(Messages.CustomerOrderHasDuplicateError, PXErrorLevel.Error, duplicateOrder.OrderNbr)))
			{
				throw new PXRowPersistingException(
					nameof(SOOrder.customerOrderNbr),
					null,
					Messages.CustomerOrderHasDuplicateError,
					duplicateOrder.OrderNbr);
			}
		}

		protected virtual SOOrder FindCustomerOrderDuplicate(PXCache orderCache, string customerOrderNbr, SOOrder order)
			=> FindCustomerOrder(orderCache, order.OrderType, order.OrderNbr, order.CustomerID, customerOrderNbr, order);
	}
}
