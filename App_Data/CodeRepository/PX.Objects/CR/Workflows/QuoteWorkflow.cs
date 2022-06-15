using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.EP;
using PX.Objects.SO;
using PX.Objects.AR;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.CR.Workflow.Quote
{
	using static CRQuote;
	using static BoundedTo<QuoteMaint, CRQuote>;
	using static PX.Objects.CR.QuoteMaint;
	using CreateSOOrder = QuoteMaint.CRCreateSalesOrderExt;
	using CreateInvoices = QuoteMaint.CRCreateInvoiceExt;

	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class QuoteWorkflow : PXGraphExtension<QuoteMaint>
	{
		public class Conditions : Condition.Pack
		{
			public Condition IsApprovalMapEnabled => 
				GetOrCreate(b => b.FromExpr(g => (
					g.IsSetupApprovalRequired == true
				)));
			public Condition IsPendingApprovalState => 
				GetOrCreate(b => b.FromExpr(g => (
					g.Status == CRQuoteStatusAttribute.PendingApproval
				)));
			public Condition IsRejectedState => 
				GetOrCreate(b => b.FromExpr(g => (
					g.Status == CRQuoteStatusAttribute.Rejected
				)));
			public Condition IsDraftState =>
				GetOrCreate(b => b.FromExpr(g => (
					g.Status == CRQuoteStatusAttribute.Draft
				)));
			public Condition IsPreparedState => 
				GetOrCreate(b => b.FromExpr(g => (
						g.Status == CRQuoteStatusAttribute.Approved
					||	g.Status == CRQuoteStatusAttribute.QuoteApproved
					||	g.Status == CRQuoteStatusAttribute.Sent
					||	g.Status == CRQuoteStatusAttribute.Accepted
				)));
			public Condition IsDeclinedState =>
				GetOrCreate(b => b.FromExpr(g => (
					g.Status == CRQuoteStatusAttribute.Declined
				)));
			public Condition IsSentState =>
				GetOrCreate(b => b.FromExpr(g => (
					g.Status == CRQuoteStatusAttribute.Sent
				)));
			public Condition IsApprovedState =>
				GetOrCreate(b => b.FromExpr(g => (
						g.Status == CRQuoteStatusAttribute.Approved
					||	g.Status == CRQuoteStatusAttribute.QuoteApproved
				)));
			public Condition IsConvertedState =>
				GetOrCreate(b => b.FromExpr(g => (
					g.Status == CRQuoteStatusAttribute.Converted
				)));
			public Condition IsAcceptedState =>
				GetOrCreate(b => b.FromExpr(g => (
					g.Status == CRQuoteStatusAttribute.Accepted
				)));
			public Condition IsPrimaryQuote =>
				GetOrCreate(b => b.FromExpr(g => (
					g.IsPrimary == true
				)));
			public Condition IsApproved => GetOrCreate(b => b.FromBql<
				approved.IsEqual<True>
			>());
			public Condition IsRejected => GetOrCreate(b => b.FromBql<
				rejected.IsEqual<True>
			>());
		}

		#region Consts

		public static class CategoryNames
		{
			public const string Processing = "Processing";
			public const string Approval = "Approval";
			public const string RecordCreation = "RecordCreation";
			public const string Activities = "Activities";
			public const string Validation = "Validation";
			public const string Other = "Ohter";
		}

		#endregion

		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<QuoteMaint, CRQuote>();
			var conditions = context.Conditions.GetPack<Conditions>();

			#region categories

			var categoryProcessing = context.Categories.CreateNew(CategoryNames.Processing,
				category => category.DisplayName("Processing"));
			var categoryApproval = context.Categories.CreateNew(CategoryNames.Approval,
				category => category.DisplayName("Approval"));
			var categoryRecordCreation = context.Categories.CreateNew(CategoryNames.RecordCreation,
				category => category.DisplayName("Record Creation"));
			var categoryActivities = context.Categories.CreateNew(CategoryNames.Activities,
				category => category.DisplayName("Activities"));
			var categoryValidation = context.Categories.CreateNew(CategoryNames.Validation,
				category => category.DisplayName("Validation"));
			var categoryOther = context.Categories.CreateNew(CategoryNames.Other,
				category => category.DisplayName("Other"));

			#endregion

			var actionSend = context.ActionDefinitions
				.CreateExisting(g => g.sendQuote, a => a
					.WithCategory(categoryProcessing)
					.IsHiddenWhen(
						!(
								(!conditions.IsApprovalMapEnabled && conditions.IsDraftState)
							|| conditions.IsApprovedState
						)
						&& !conditions.IsSentState
					)
					.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
					.IsExposedToMobile(true));

			var actionMarkAsAccepted = context.ActionDefinitions
				.CreateExisting(g => g.accept, a => a
					.WithCategory(categoryProcessing)
					.IsHiddenWhen(
						(conditions.IsApprovalMapEnabled && conditions.IsDraftState)
						|| conditions.IsPendingApprovalState
						|| conditions.IsAcceptedState
						|| conditions.IsRejectedState)
					.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
					.IsExposedToMobile(true));

			var actionEditQuote = context.ActionDefinitions
				.CreateExisting(g => g.editQuote, a => a
					.WithCategory(categoryProcessing)
					.IsHiddenWhen(!(conditions.IsPreparedState
									|| conditions.IsRejectedState
									|| conditions.IsPendingApprovalState
									|| conditions.IsDeclinedState
								))
					.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
					.IsExposedToMobile(true));

			var actionSetAsPrimary = context.ActionDefinitions
				.CreateExisting(g => g.primaryQuote, a => a
					.WithCategory(categoryOther)
					.IsDisabledWhen(conditions.IsPrimaryQuote)
					.IsExposedToMobile(true));

			var actionRequestApproval = context.ActionDefinitions
				.CreateExisting(g => g.requestApproval, a => a
					.WithCategory(categoryApproval)
					.PlaceAfter(actionSetAsPrimary)
					.IsHiddenWhen(
						!(conditions.IsApprovalMapEnabled
						&& conditions.IsDraftState)
					)
					.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
					.IsExposedToMobile(true));

			var actionApprove = context.ActionDefinitions
				.CreateExisting(g => g.approve, a => a
					.WithCategory(categoryApproval)
					.IsHiddenWhen(
						!(conditions.IsApprovalMapEnabled
						&& conditions.IsPendingApprovalState)
					)
					.WithFieldAssignments(fa => fa.Add<approved>(true))
					.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
					.IsExposedToMobile(true));

			var actionReject = context.ActionDefinitions
				.CreateExisting(g => g.reject, a => a
					.WithCategory(categoryApproval)
					.IsHiddenWhen(
						!(conditions.IsApprovalMapEnabled 
						&& conditions.IsPendingApprovalState)
					)
					.WithFieldAssignments(fa => fa.Add<rejected>(true))
					.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
					.IsExposedToMobile(true));

			var actionMarkAsConverted = context.ActionDefinitions
				.CreateExisting(g => g.markAsConverted, a => a
					.WithCategory(categoryProcessing)
					.IsHiddenWhen(
							conditions.IsApprovalMapEnabled && conditions.IsDraftState
						|| conditions.IsPendingApprovalState
						|| conditions.IsConvertedState
						|| conditions.IsRejectedState
					)
					.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
					.IsExposedToMobile(true));

			var actionMarkAsDeclined = context.ActionDefinitions
				.CreateExisting(g => g.decline, a => a
					.WithCategory(categoryProcessing)
					.IsHiddenWhen(
							conditions.IsDraftState 
						|| conditions.IsDeclinedState
						|| conditions.IsPendingApprovalState
						|| conditions.IsRejectedState
					)
					.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
					.IsExposedToMobile(true));

			var actionPrintQuote = context.ActionDefinitions
				.CreateExisting(g => g.printQuote, a => a
					.PlaceAfter(actionSetAsPrimary)
					.WithCategory(categoryOther)
					.IsDisabledWhen(
						conditions.IsApprovalMapEnabled 
						&& ( conditions.IsDraftState 
							|| conditions.IsPendingApprovalState
							|| conditions.IsRejectedState))
					.IsExposedToMobile(true));

			var actionCopyQuote = context.ActionDefinitions
				.CreateExisting(g => g.copyQuote, a => a
					.WithCategory(categoryOther)
					.IsExposedToMobile(true));

			var actionRecalculatePrices = context.ActionDefinitions
				.CreateExisting<Discount>(g => g.graphRecalculateDiscountsAction, a => a
					.PlaceAfter(actionCopyQuote)
					.WithCategory(categoryOther)
					.IsDisabledWhen(
						 conditions.IsPendingApprovalState
						|| conditions.IsRejectedState
						|| conditions.IsPreparedState
						|| conditions.IsConvertedState
						|| conditions.IsDeclinedState)
					.IsExposedToMobile(true));

			var actionValidateAddresses = context.ActionDefinitions
				.CreateExisting(g => g.validateAddresses, a => a
					.WithCategory(categoryOther)
					.PlaceAfter(actionRecalculatePrices)
					.IsExposedToMobile(true));

			var actionCreateSalesOrder = context.ActionDefinitions
				.CreateExisting<CreateSOOrder>(g => g.CreateSalesOrder, a => a
					.DisplayName(Messages.ConvertToSalesOrder)
					.WithCategory(categoryRecordCreation)
					.PlaceAfter(actionValidateAddresses)
					.IsExposedToMobile(true));

			var actionCreateInvoices = context.ActionDefinitions
				.CreateExisting<CreateInvoices>(g => g.CreateInvoice, a => a
					.DisplayName(Messages.ConvertToInvoice)
					.WithCategory(categoryRecordCreation)
					.PlaceAfter(actionCreateSalesOrder));

			var actionCreateTask = context.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWTASK_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));

			var actionCreateEmail = context.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWMAILACTIVITY_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));

			var actionCreatePhoneCall = context.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWACTIVITY_PHONECALL_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(DefaultSalesQuoteNoApprovalMapFlow)
					.WithActions(actions =>
					{
						actions.Add(actionSend);
						actions.Add(actionEditQuote);
						actions.Add(actionMarkAsAccepted);
						actions.Add(actionMarkAsConverted);
						actions.Add(actionMarkAsDeclined);

						actions.Add(actionRequestApproval);
						actions.Add(actionApprove);
						actions.Add(actionReject);

						actions.Add(actionCreateSalesOrder);
						actions.Add(actionCreateInvoices);

						actions.Add(actionCreateTask);
						actions.Add(actionCreateEmail);
						actions.Add(actionCreatePhoneCall);

						actions.Add(actionValidateAddresses);

						actions.Add(actionPrintQuote);
						actions.Add(actionCopyQuote);
						actions.Add(actionSetAsPrimary);
						actions.Add(actionRecalculatePrices);
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => (handler
							.WithTargetOf<SOOrder>()
								.WithParametersOf<CRQuote>()
								.OfEntityEvent<SOOrder.Events>(e => e.OrderCreatedFromQuote)
								.Is(g => g.OnSalesOrderCreatedFromQuote) as WorkflowEventHandlerDefinition.INeedEventPrimaryEntityGetter<SOOrder, CRQuote>)
							?.UsesParameterAsPrimaryEntity()
							);
						handlers.Add(handler => handler
							.WithTargetOf<SOOrder>()
								.OfEntityEvent<SOOrder.Events>(e => e.OrderDeleted)
								.Is(g => g.OnSalesOrderDeleted)
								.UsesPrimaryEntityGetter<
									SelectFrom<CRQuote>.
									InnerJoin<CRRelation>.
										On<CRRelation.targetNoteID.IsEqual<CRQuote.noteID>>.
									Where<
										CRRelation.refNoteID.IsEqual<SOOrder.noteID.FromCurrent>>
								>(allowSelectMultipleRecords: false));

						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
								.WithParametersOf<CRQuote>()
								.OfEntityEvent<ARInvoice.Events>(e => e.ARInvoiceCreatedFromQuote)
								.Is(g => g.OnARInvoiceCreatedFromQuote).UsesParameterAsPrimaryEntity()
							);

						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
								.OfEntityEvent<ARInvoice.Events>(e => e.ARInvoiceDeleted)
								.Is(g => g.OnARInvoiceDeleted)
								.UsesPrimaryEntityGetter<
									SelectFrom<CRQuote>.
									InnerJoin<CRRelation>.
										On<CRRelation.targetNoteID.IsEqual<CRQuote.noteID>>.
									Where<
										CRRelation.refNoteID.IsEqual<ARInvoice.noteID.FromCurrent>>
								>(allowSelectMultipleRecords: false));
					})
					.WithCategories(categories =>
					{
						categories.Add(categoryProcessing);
						categories.Add(categoryApproval);
						categories.Add(categoryActivities);
						categories.Add(categoryRecordCreation);
						categories.Add(categoryValidation);
						categories.Add(categoryOther);
					});
			});

			Workflow.IConfigured DefaultSalesQuoteNoApprovalMapFlow(Workflow.INeedStatesFlow flow)
			{
				#region Disable Fields helpers
				void DisableQuoteMain(FieldState.IContainerFillerFields fields)
				{
					fields.AddField<documentDate>(f => f.IsDisabled());
					fields.AddField<expirationDate>(f => f.IsDisabled());
					fields.AddField<locationID>(f => f.IsDisabled());
					fields.AddField<curyID>(f => f.IsDisabled());
					fields.AddField<manualTotalEntry>(f => f.IsDisabled());
					fields.AddField<curyAmount>(f => f.IsDisabled());
					fields.AddField<curyDiscTot>(f => f.IsDisabled());
					fields.AddField<branchID>(f => f.IsDisabled());
					fields.AddField<termsID>(f => f.IsDisabled());
					fields.AddField<taxZoneID>(f => f.IsDisabled());
					fields.AddField<taxCalcMode>(f => f.IsDisabled());
					fields.AddField<taxRegistrationID>(f => f.IsDisabled());
					fields.AddField<externalTaxExemptionNumber>(f => f.IsDisabled());
					fields.AddField<avalaraCustomerUsageType>(f => f.IsDisabled());

					fields.AddField<siteID>(f => f.IsDisabled());
					fields.AddField<carrierID>(f => f.IsDisabled());
					fields.AddField<shipTermsID>(f => f.IsDisabled());
					fields.AddField<shipZoneID>(f => f.IsDisabled());
					fields.AddField<fOBPointID>(f => f.IsDisabled());
					fields.AddField<resedential>(f => f.IsDisabled());
					fields.AddField<saturdayDelivery>(f => f.IsDisabled());
					fields.AddField<insurance>(f => f.IsDisabled());
					fields.AddField<shipComplete>(f => f.IsDisabled());

					fields.AddTable<CRTaxTran>(f => f.IsDisabled());
					fields.AddTable<CROpportunityDiscountDetail>(f => f.IsDisabled());
					fields.AddTable<CROpportunityProducts>(f => f.IsDisabled());
				}
				void DisableContact(FieldState.IContainerFillerFields fields)
				{
					fields.AddField<contactID>(f => f.IsDisabled());
					fields.AddField<allowOverrideContactAddress>(f => f.IsDisabled());
					fields.AddTable<CRContact>(f => f.IsDisabled());
				}
				void DisableBilling(FieldState.IContainerFillerFields fields)
				{
					fields.AddField<allowOverrideBillingContactAddress>(f => f.IsDisabled());
					fields.AddTable<CRBillingContact>(f => f.IsDisabled());
					fields.AddTable<CRBillingAddress>(f => f.IsDisabled());
				}
				void DisableShipping(FieldState.IContainerFillerFields fields)
				{
					fields.AddField<allowOverrideShippingContactAddress>(f => f.IsDisabled());
					fields.AddTable<CRShippingContact>(f => f.IsDisabled());
					fields.AddTable<CRShippingAddress>(f => f.IsDisabled());
				}
				#endregion Disable Fields helpers

				#region Assignment fields helper
				void ResetQuoteAprroveRejectStatus(Assignment.IContainerFillerFields fields)
				{
					fields.Add<approved>(e => e.SetFromValue(false));
					fields.Add<rejected>(e => e.SetFromValue(false));
				}
				#endregion

				void AddCommonActionState(ActionState.IContainerFillerActions actions)
				{
					actions.Add(actionSetAsPrimary);
					actions.Add(actionPrintQuote);
					actions.Add(actionCopyQuote);
					actions.Add(actionRecalculatePrices);
					actions.Add(actionValidateAddresses);
				}

				#region States
				var stateDraft = context.FlowStates.Create<CRQuoteStatusAttribute.draft>(state => state
					.IsInitial()
					.WithActions(actions =>
					{
						actions.Add(actionSend);
						actions.Add(actionRequestApproval, a => a.IsDuplicatedInToolbar());
						actions.Add(actionMarkAsAccepted);
						actions.Add(actionMarkAsConverted);
						actions.Add(actionCreateSalesOrder);
						actions.Add(actionCreateInvoices);
						AddCommonActionState(actions);
					})
					.WithEventHandlers(handlers =>
					{
						handlers.Add(g => g.OnSalesOrderCreatedFromQuote);
						handlers.Add(g => g.OnARInvoiceCreatedFromQuote);
					}));

				var stateSent = context.FlowStates
					.Create<CRQuoteStatusAttribute.sent>(state => state
						.WithActions(actions =>
						{
							actions.Add(actionEditQuote);
							actions.Add(actionSend);
							actions.Add(actionMarkAsAccepted);
							actions.Add(actionMarkAsConverted);
							actions.Add(actionMarkAsDeclined);
							actions.Add(actionCreateSalesOrder);
							actions.Add(actionCreateInvoices);
							AddCommonActionState(actions);
						})
						.WithFieldStates(fields =>
						{
							DisableQuoteMain(fields);
							DisableShipping(fields);
						}));

				var stateAccepted = context.FlowStates
					.Create<CRQuoteStatusAttribute.accepted>(state => state
						.WithActions(actions =>
						{
							actions.Add(actionEditQuote);
							actions.Add(actionMarkAsConverted);
							actions.Add(actionMarkAsDeclined);
							actions.Add(actionCreateSalesOrder);
							actions.Add(actionCreateInvoices);
							AddCommonActionState(actions);
						})
						.WithEventHandlers(handlers =>
						{
							handlers.Add(g => g.OnSalesOrderCreatedFromQuote);
							handlers.Add(g => g.OnARInvoiceCreatedFromQuote);
						})
						.WithFieldStates(fields =>
						{
							DisableQuoteMain(fields);
							DisableShipping(fields);
						}));

				var stateConverted = context.FlowStates
					.Create<CRQuoteStatusAttribute.converted>(state => state
						.WithActions(actions =>
						{
							actions.Add(actionMarkAsAccepted);
							AddCommonActionState(actions);
						})
						.WithEventHandlers(handlers =>
						{
							handlers.Add(g => g.OnSalesOrderDeleted);
							handlers.Add(g => g.OnARInvoiceDeleted);
						})
						.WithFieldStates(fields =>
						{
							DisableQuoteMain(fields);
							DisableContact(fields);
							DisableBilling(fields);
							DisableShipping(fields);
						}));

				var stateDeclined = context.FlowStates
					.Create<CRQuoteStatusAttribute.declined>(state => state
						.WithActions(actions =>
						{
							actions.Add(actionEditQuote);
							actions.Add(actionMarkAsAccepted);
							AddCommonActionState(actions);
						})
						.WithFieldStates(fields =>
						{
							DisableQuoteMain(fields);
							DisableContact(fields);
							DisableBilling(fields);
							DisableShipping(fields);
						}));

				var statePendingApproval = context.FlowStates
					.Create<CRQuoteStatusAttribute.pendingApproval>(state => state
						.WithActions(actions =>
						{
							actions.Add(actionEditQuote);
							actions.Add(actionApprove, a => a.IsDuplicatedInToolbar());
							actions.Add(actionReject, a => a.IsDuplicatedInToolbar());
							AddCommonActionState(actions);
						})
						.WithFieldStates(fields =>
						{
							DisableQuoteMain(fields);
							DisableShipping(fields);
						}));

				var stateRejected = context.FlowStates
					.Create<CRQuoteStatusAttribute.rejected>(state => state
						.WithActions(actions =>
						{
							actions.Add(actionEditQuote, a => a.IsDuplicatedInToolbar());
							AddCommonActionState(actions);
						})
						.WithFieldStates(fields =>
						{
							DisableQuoteMain(fields);
							DisableShipping(fields);
						}));

				var stateApproved = context.FlowStates
					.Create<CRQuoteStatusAttribute.quoteApproved>(state => state
						.WithActions(actions =>
						{
							actions.Add(actionEditQuote);
							actions.Add(actionSend);
							actions.Add(actionMarkAsAccepted);
							actions.Add(actionMarkAsConverted);
							actions.Add(actionMarkAsDeclined);
							actions.Add(actionCreateSalesOrder);
							actions.Add(actionCreateInvoices);
							AddCommonActionState(actions);
						})
						.WithEventHandlers(handlers =>
						{
							handlers.Add(g => g.OnSalesOrderCreatedFromQuote);
							handlers.Add(g => g.OnARInvoiceCreatedFromQuote);
						})
						.WithFieldStates(fields =>
						{
							DisableQuoteMain(fields);
							DisableShipping(fields);
						}));

				var statePrepared = context.FlowStates
					.Create<CRQuoteStatusAttribute.approved>(state => state
						.WithActions(actions =>
						{
							actions.Add(actionEditQuote);
							actions.Add(actionSend);
							actions.Add(actionMarkAsAccepted);
							actions.Add(actionMarkAsConverted);
							actions.Add(actionMarkAsDeclined);
							actions.Add(actionCreateSalesOrder);
							actions.Add(actionCreateInvoices);
							AddCommonActionState(actions);
						})
						.WithEventHandlers(handlers =>
						{
							handlers.Add(g => g.OnSalesOrderCreatedFromQuote);
						})
						.WithFieldStates(fields =>
						{
							DisableQuoteMain(fields);
							DisableShipping(fields);
						}));
				#endregion

				return flow
					.WithFlowStates(states =>
					{
						states.Add(stateDraft);
						states.Add(stateSent);
						states.Add(stateAccepted);
						states.Add(stateConverted);
						states.Add(stateDeclined);
						states.Add(stateApproved);
						states.Add(stateRejected);
						states.Add(statePendingApproval);
						states.Add(statePrepared);
					})
					.WithTransitions(transitions =>
					{
						//Draft transitions
						transitions.AddGroupFrom(stateDraft, ts =>
						{
							ts.Add(t => t
								.To(stateSent)
								.IsTriggeredOn(actionSend));
							ts.Add(t => t
								.To(stateAccepted)
								.IsTriggeredOn(actionMarkAsAccepted));
							ts.Add(t => t
								.To(stateConverted)
								.IsTriggeredOn(actionMarkAsConverted));
							ts.Add(t => t
								.To(stateConverted)
								.IsTriggeredOn(g => g.OnSalesOrderCreatedFromQuote));
							ts.Add(t => t
								.To(stateApproved)
								.IsTriggeredOn(actionRequestApproval)
								.When(conditions.IsApproved));
							ts.Add(t => t
								.To(stateRejected)
								.IsTriggeredOn(actionRequestApproval)
								.When(conditions.IsRejected));
							ts.Add(t => t
								.To(statePendingApproval)
								.IsTriggeredOn(actionRequestApproval)
								.When(!conditions.IsRejected && !conditions.IsApproved));
						});

						// PendingApproval
						transitions.AddGroupFrom(statePendingApproval, ts =>
						{
							ts.Add(t => t
								.To(stateDraft)
								.IsTriggeredOn(actionEditQuote)
								.WithFieldAssignments(fas =>
								{
									ResetQuoteAprroveRejectStatus(fas);
								}));
							ts.Add(t => t
								.To(stateApproved)
								.IsTriggeredOn(actionApprove)
								.When(conditions.IsApproved));
							ts.Add(t => t
								.To(stateRejected)
								.IsTriggeredOn(actionReject)
								.When(conditions.IsRejected));
						});

						// Approved
						transitions.AddGroupFrom(stateApproved, ts =>
						{
							ts.Add(t => t
								.To(stateDraft)
								.IsTriggeredOn(actionEditQuote)
								.WithFieldAssignments(fas =>
								{
									ResetQuoteAprroveRejectStatus(fas);
								}));
							ts.Add(t => t
								.To(stateSent)
								.IsTriggeredOn(actionSend));
							ts.Add(t => t
								.To(stateAccepted)
								.IsTriggeredOn(actionMarkAsAccepted));
							ts.Add(t => t
								.To(stateConverted)
								.IsTriggeredOn(actionMarkAsConverted));
							ts.Add(t => t
								.To(stateDeclined)
								.IsTriggeredOn(actionMarkAsDeclined));
						});

						// Prepared - same as Approved 
						transitions.AddGroupFrom(statePrepared, ts =>
						{
							ts.Add(t => t
								.To(stateDraft)
								.IsTriggeredOn(actionEditQuote)
								.WithFieldAssignments(fas =>
								{
									ResetQuoteAprroveRejectStatus(fas);
								}));
							ts.Add(t => t
								.To(stateSent)
								.IsTriggeredOn(actionSend));
							ts.Add(t => t
								.To(stateAccepted)
								.IsTriggeredOn(actionMarkAsAccepted));
							ts.Add(t => t
								.To(stateConverted)
								.IsTriggeredOn(actionMarkAsConverted));
							ts.Add(t => t
								.To(stateDeclined)
								.IsTriggeredOn(actionMarkAsDeclined));
						});

						// Rejected
						transitions.Add(ts => ts
							.From(stateRejected)
							.To(stateDraft)
							.IsTriggeredOn(actionEditQuote)
							.WithFieldAssignments(fas =>
							{
								ResetQuoteAprroveRejectStatus(fas);
							}));

						//Sent
						transitions.AddGroupFrom(stateSent, ts =>
						{
							ts.Add(t => t
								.To(stateAccepted)
								.IsTriggeredOn(actionMarkAsAccepted));
							ts.Add(t => t
								.To(stateDraft)
								.IsTriggeredOn(actionEditQuote)
								.WithFieldAssignments(fas =>
								{
									ResetQuoteAprroveRejectStatus(fas);
								}));
							ts.Add(t => t
								.To(stateConverted)
								.IsTriggeredOn(actionMarkAsConverted));
							ts.Add(t => t
								.To(stateDeclined)
								.IsTriggeredOn(actionMarkAsDeclined));
						});

						//Accepted
						transitions.AddGroupFrom(stateAccepted, ts =>
						{
							ts.Add(t => t
								.To(stateDraft)
								.IsTriggeredOn(actionEditQuote)
								.WithFieldAssignments(fas =>
								{
									ResetQuoteAprroveRejectStatus(fas);
								}));
							ts.Add(t => t
								.To(stateConverted)
								.IsTriggeredOn(actionMarkAsConverted));
							ts.Add(t => t
								.To(stateDeclined)
								.IsTriggeredOn(actionMarkAsDeclined));

							ts.Add(t => t
								.To(stateConverted)
								.IsTriggeredOn(g => g.OnARInvoiceCreatedFromQuote));
							ts.Add(t => t
								.To(stateConverted)
								.IsTriggeredOn(g => g.OnSalesOrderCreatedFromQuote));
						});

						//Converted
						transitions.AddGroupFrom(stateConverted, ts =>
						{
							ts.Add(t => t
								.To(stateAccepted)
								.IsTriggeredOn(actionMarkAsAccepted));

							ts.Add(t => t
								.To(stateAccepted)
								.IsTriggeredOn(g => g.OnSalesOrderDeleted));

							ts.Add(t => t
								.To(stateAccepted)
								.IsTriggeredOn(g => g.OnARInvoiceDeleted));
						});

						//Declined
						transitions.AddGroupFrom(stateDeclined, ts =>
						{
							ts.Add(t => t
								.To(stateDraft)
								.IsTriggeredOn(actionEditQuote)
								.WithFieldAssignments(fas =>
								{
									ResetQuoteAprroveRejectStatus(fas);
								}));
							ts.Add(t => t
								.To(stateAccepted)
								.IsTriggeredOn(actionMarkAsAccepted));
						});
					});
			}
		}
	}
}
