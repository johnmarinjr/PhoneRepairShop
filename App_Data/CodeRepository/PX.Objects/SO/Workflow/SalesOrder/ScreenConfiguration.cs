using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.Common;

namespace PX.Objects.SO.Workflow.SalesOrder
{
	using State = SOOrderStatus;
	using CreatePaymentExt = GraphExtensions.SOOrderEntryExt.CreatePaymentExt;
	using DropshipReturn = GraphExtensions.SOOrderEntryExt.DropshipReturn;
	using Blanket = GraphExtensions.SOOrderEntryExt.Blanket;
	using static SOOrder;
	using static BoundedTo<SOOrderEntry, SOOrder>;

	public class ScreenConfiguration : PXGraphExtension<SOOrderEntry>
	{
		public class Conditions : Condition.Pack
		{
			public Condition CanNotBeCompleted => GetOrCreate(b => b.FromBql<
				completed.IsEqual<True>.
				Or<shipmentCntr.IsEqual<Zero>>.
				Or<openShipmentCntr.IsGreater<Zero>>
			>());

			public Condition AllowQuickProcess => GetOrCreate(b => b.FromBql<
				Where<Selector<orderType, SOOrderType.allowQuickProcess>, Equal<True>>
			>());
		}

		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<SOOrderEntry, SOOrder>());

		protected virtual void Configure(WorkflowContext<SOOrderEntry, SOOrder> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();

			#region Categories
			var commonCategories = CommonActionCategories.Get(context);
			var processingCategory = commonCategories.Processing;
			var approvalCategory = commonCategories.Approval;
			var printingEmailingCategory = commonCategories.PrintingAndEmailing;
			var replenishmentCategory = context.Categories.CreateNew(ActionCategories.ReplenishmentCategoryID,
					category => category.DisplayName(ActionCategories.DisplayNames.Replenishment));
			var otherCategory = commonCategories.Other;
			#endregion

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<status>()
					.FlowTypeIdentifierIs<behavior>()
					.WithFlows(flows =>
					{
						// To be defined separatelly in workflow extensions
					})
					.WithActions(actions =>
					{
						#region Processing
						actions.Add(g => g.releaseFromHold, c => c
							.WithCategory(processingCategory)
							.WithFieldAssignments(fas => fas.Add<hold>(false)));
						actions.Add(g => g.putOnHold, c => c
							.WithCategory(processingCategory, g => g.releaseFromHold)
							.PlaceAfter(g => g.createShipmentIssue)
							.WithFieldAssignments(fas =>
							{
								fas.Add<cancelled>(false);
								fas.Add<hold>(true);
							}));

						actions.Add(g => g.initializeState);
						actions.Add<SOOrderEntry.SOQuickProcess>(g => g.quickProcess, c => c
							.WithCategory(processingCategory)
							.IsHiddenWhen(!conditions.AllowQuickProcess));

						actions.Add(g => g.createShipmentReceipt, c => c
							.WithCategory(processingCategory));

						actions.Add(g => g.createShipmentIssue, c => c
							.WithCategory(processingCategory)
							.MassProcessingScreen<SOCreateShipment>() // +RM Open, SO (Back-Order, Open)
							.InBatchMode());

						actions.Add(g => g.prepareInvoice, c => c
							.WithCategory(processingCategory)
							.MassProcessingScreen<SOCreateShipment>() // +CM Open, +IN Open, +RM (Completed, Open), +SO (Back-Order, Completed, Open)
							.InBatchMode());

						actions.Add(g => g.placeOnBackOrder, c => c
							.WithCategory(processingCategory));

						actions.Add(g => g.openOrder, c => c
							.WithCategory(processingCategory)
							.MassProcessingScreen<SOCreateShipment>()); // +SO Back-Order
						actions.Add(g => g.completeOrder, c => c
							.WithCategory(processingCategory)
							.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
							.IsDisabledWhen(conditions.CanNotBeCompleted)
							.WithFieldAssignments(fas => fas.Add<forceCompleteOrder>(true)));
						actions.Add(g => g.cancelOrder, c => c
							.WithCategory(processingCategory)
							.MassProcessingScreen<SOCreateShipment>() // +QT (Hold, Open), +RM (Hold, Open), +SO (Hold, Open)
							.WithFieldAssignments(fas =>
							{
								fas.Add<cancelled>(true);
								fas.Add<hold>(false);
								fas.Add<creditHold>(false);
								fas.Add<inclCustOpenOrders>(false);
							}));
						actions.Add(g => g.reopenOrder, c => c
							.WithCategory(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas =>
							{
								fas.Add<cancelled>(false);
								fas.Add<completed>(false);
							}));
						#endregion

						#region Approval
						actions.Add(g => g.releaseFromCreditHold, c => c
							.WithCategory(approvalCategory)
							.MassProcessingScreen<SOCreateShipment>() // +IN Credit-Hold, +SO Credit-Hold
							.WithFieldAssignments(fas =>
							{
								fas.Add<approvedCredit>(true);
								fas.Add<approvedCreditByPayment>(false);
								fas.Add<approvedCreditAmt>(e => e.SetFromField<orderTotal>());
							}));
						#endregion

						#region Printing and Emailing
						actions.Add(g => g.printSalesOrder, c => c
							.WithCategory(printingEmailingCategory)
							.MassProcessingScreen<SOOrderProcess>()
							// +SO (Cancelled, Completed, Hold, Open, Credit-Hold),
							// +RM (Cancelled, Completed, Hold, Open),
							// +IN (Cancelled, Completed, Hold, Open, Invoiced, Credit-Hold),
							// +CM (Cancelled, Completed, Hold, Open, Invoiced)
							.InBatchMode());
						actions.Add(g => g.printQuote, c => c
							.WithCategory(printingEmailingCategory)
							.MassProcessingScreen<SOOrderProcess>()
							// +QT (Hold, Open),
							.InBatchMode());
						actions.Add<Blanket>(g => g.printBlanket, c => c
							.WithCategory(printingEmailingCategory)
							.MassProcessingScreen<SOOrderProcess>()
							// +BL (Cancelled, Completed, Hold, Open),
							.InBatchMode());
						actions.Add(g => g.emailSalesOrder, c => c
							.WithCategory(printingEmailingCategory)
							.WithFieldAssignments(fass => fass.Add<emailed>(e => e.SetFromValue(true)))
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.MassProcessingScreen<SOOrderProcess>(/* +IN (Completed, Invoiced, Open), +SO (Back-Order, Completed, Credit-Hold, Open, Shipping) */)
							.InBatchMode());
						actions.Add(g => g.emailQuote, c => c
							.WithCategory(printingEmailingCategory)
							.WithFieldAssignments(fass => fass.Add<emailed>(e => e.SetFromValue(true)))
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.MassProcessingScreen<SOOrderProcess>(/* +QT (Completed, Hold, Open) */)
							.InBatchMode());
						actions.Add<Blanket>(g => g.emailBlanket, c => c
							.WithCategory(printingEmailingCategory)
							.WithFieldAssignments(fass => fass.Add<emailed>(e => e.SetFromValue(true)))
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.MassProcessingScreen<SOOrderProcess>(/* +BL (Open) */)
							.InBatchMode());
						#endregion

						#region Replenishment
						actions.Add(g => g.createPurchaseOrder, c => c
							.WithCategory(replenishmentCategory));
						actions.Add(g => g.createTransferOrder, c => c
							.WithCategory(replenishmentCategory));
						actions.Add<DropshipReturn>(g => g.createVendorReturn, c => c
							.WithCategory(replenishmentCategory));
						#endregion

						#region Other
						actions.Add(g => g.recalculateDiscountsAction, c => c
							.WithCategory(otherCategory));
						actions.Add<SOOrderEntryExternalTax>(e => e.recalcExternalTax, c => c
							.WithCategory(otherCategory));
						actions.Add(g => g.validateAddresses, c => c
							.WithCategory(otherCategory));
						actions.Add(g => g.copyOrder, c => c
							.WithCategory(otherCategory));
						#endregion

						actions.Add<CreatePaymentExt>(e => e.createAndAuthorizePayment, c => c
							.IsHiddenAlways() // only for mass processing
							.MassProcessingScreen<SOCreateShipment>());
						actions.Add<CreatePaymentExt>(e => e.createAndCapturePayment, c => c
							.IsHiddenAlways() // only for mass processing
							.MassProcessingScreen<SOCreateShipment>());

						actions.Add<Blanket>(e => e.createChildOrders, c => c
							.WithCategory(processingCategory)
							.MassProcessingScreen<SOCreateShipment>());

						actions.Add<Blanket>(g => g.processExpiredOrder, c => c
							.WithCategory(processingCategory)
							.MassProcessingScreen<SOCreateShipment>());

					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.OrderDeleted)
								.Is(g => g.OnOrderDeleted_ReopenQuote)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<
										orderType.IsEqual<origOrderType.FromCurrent>.
										And<orderNbr.IsEqual<origOrderNbr.FromCurrent>>>
								>()
								.DisplayName("Reopen Quote when Order Deleted");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.ShipmentCreationFailed)
								.Is(g => g.OnShipmentCreationFailed)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Shipment Creation Failed");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.ObtainedPaymentInPendingProcessing)
								.Is(g => g.OnObtainedPaymentInPendingProcessing)
								.UsesTargetAsPrimaryEntity();
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.LostLastPaymentInPendingProcessing)
								.Is(g => g.OnLostLastPaymentInPendingProcessing)
								.UsesTargetAsPrimaryEntity();
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.PaymentRequirementsSatisfied)
								.Is(g => g.OnPaymentRequirementsSatisfied)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Payment Requirements Satisfied");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.PaymentRequirementsViolated)
								.Is(g => g.OnPaymentRequirementsViolated)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Payment Requirements Violated");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.BlanketCompleted)
								.Is(g => g.OnBlanketCompleted)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Blanket Order Completed");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.BlanketReopened)
								.Is(g => g.OnBlanketReopened)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Blanket Order Reopened");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.CreditLimitSatisfied)
								.Is(g => g.OnCreditLimitSatisfied)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Credit Limit Satisfied");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.CreditLimitViolated)
								.Is(g => g.OnCreditLimitViolated)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Credit Limit Violated");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrderShipment>()
								.WithParametersOf<SOShipment>()
								.OfEntityEvent<SOOrderShipment.Events>(e => e.ShipmentLinked)
								.Is(g => g.OnShipmentLinked)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<SOOrder.orderType.IsEqual<SOOrderShipment.orderType.FromCurrent>.
										And<SOOrder.orderNbr.IsEqual<SOOrderShipment.orderNbr.FromCurrent>>>
								>()
								.DisplayName("Shipment Linked");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrderShipment>()
								.WithParametersOf<SOShipment>()
								.OfEntityEvent<SOOrderShipment.Events>(e => e.ShipmentUnlinked)
								.Is(g => g.OnShipmentUnlinked)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<SOOrder.orderType.IsEqual<SOOrderShipment.orderType.FromCurrent>.
										And<SOOrder.orderNbr.IsEqual<SOOrderShipment.orderNbr.FromCurrent>>>
								>()
								.DisplayName("Shipment Unlinked");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.GotShipmentConfirmed)
								.Is(g => g.OnShipmentConfirmed)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Shipment Confirmed");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.GotShipmentCorrected)
								.Is(g => g.OnShipmentCorrected)
								.UsesTargetAsPrimaryEntity()
								.DisplayName("Shipment Corrected");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrderShipment>()
								.WithParametersOf<SOInvoice>()
								.OfEntityEvent<SOOrderShipment.Events>(e => e.InvoiceLinked)
								.Is(g => g.OnInvoiceLinked)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<SOOrder.orderType.IsEqual<SOOrderShipment.orderType.FromCurrent>.
										And<SOOrder.orderNbr.IsEqual<SOOrderShipment.orderNbr.FromCurrent>>>
								>()
								.DisplayName("Invoice Linked");
						});
						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOOrderShipment>()
								.WithParametersOf<SOInvoice>()
								.OfEntityEvent<SOOrderShipment.Events>(e => e.InvoiceUnlinked)
								.Is(g => g.OnInvoiceUnlinked)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									Where<SOOrder.orderType.IsEqual<SOOrderShipment.orderType.FromCurrent>.
										And<SOOrder.orderNbr.IsEqual<SOOrderShipment.orderNbr.FromCurrent>>>
								>()
								.DisplayName("Invoice Unlinked");
						});

						handlers.Add(handler =>
						{
							return handler
								.WithTargetOf<SOInvoice>()
								.OfEntityEvent<SOInvoice.Events>(e => e.InvoiceReleased)
								.Is(g => g.OnInvoiceReleased)
								.UsesPrimaryEntityGetter<
									SelectFrom<SOOrder>.
									InnerJoin<SOOrderShipment>.On<SOOrderShipment.FK.Order>.
									Where<SOOrderShipment.FK.Invoice.SameAsCurrent>
								>(allowSelectMultipleRecords: true)
								.DisplayName("Invoice Released");
						});
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(approvalCategory);
						categories.Add(printingEmailingCategory);
						categories.Add(replenishmentCategory);
						categories.Add(otherCategory);
						categories.Update(FolderType.InquiriesFolder, category => category.PlaceAfter(otherCategory));
					});
			});
		}

		public static class ActionCategories
		{
			public const string ReplenishmentCategoryID = "Replenishment Category";

			[PXLocalizable]
			public static class DisplayNames
			{
				public const string Replenishment = "Replenishment";
			}
		}
	}
}
