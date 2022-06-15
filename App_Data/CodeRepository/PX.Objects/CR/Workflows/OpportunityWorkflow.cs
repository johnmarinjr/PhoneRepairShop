using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.WorkflowAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CR.Workflows
{
	using static PX.Data.WorkflowAPI.BoundedTo<OpportunityMaint, CROpportunity>;
	using CreateContactExt = OpportunityMaint.CreateContactFromOpportunityGraphExt;
	using CreateAccountExt = OpportunityMaint.CreateBothAccountAndContactFromOpportunityGraphExt;
	using CreateSOOrder = OpportunityMaint.CRCreateSalesOrderExt;
	using CreateInvoices = OpportunityMaint.CRCreateInvoiceExt;

	/// <summary>
	/// Extensions that used to configure Workflow for <see cref="OpportunityMaint"/> and <see cref="CROpportunity"/>.
	/// Use Extensions Chaining for this extension if you want customize workflow with code for this graph of DAC.
	/// </summary>
	public class OpportunityWorkflow : PX.Data.PXGraphExtension<OpportunityMaint>
	{
		// workflow works without checking active
		public static bool IsActive() => false;

		#region Consts

		/// <summary>
		/// Statuses for <see cref="CROpportunity.status"/> used by default in system workflow.
		/// Values could be changed and extended by workflow.
		/// Note, that <see cref="OpportunityStatus.Won"/> status used in Campaigns screen to count won opportunities: <see cref="DAC.Standalone.CRCampaign.closedOpportunities"/>.
		/// </summary>
		[Obsolete("Use OpportunityStatus")]
		public class States : OpportunityStatus { }

		private static readonly string[] NewReasons =
		{
			OpportunityReason.Created,
			OpportunityReason.ConvertedFromLead,
			OpportunityReason.Qualified,
		};

		private static readonly string[] OpenReasons =
		{
			OpportunityReason.InProcess,
			OpportunityReason.Qualified,
		};

		private static readonly string[] WonReasons =
		{
			OpportunityReason.OrderPlaced,
			OpportunityReason.Price,
			OpportunityReason.Relationship,
			OpportunityReason.Technology,
			OpportunityReason.Other,
		};

		private static readonly string[] LostReasons =
		{
			OpportunityReason.CompanyMaturity,
			OpportunityReason.Price,
			OpportunityReason.Relationship,
			OpportunityReason.Technology,
			OpportunityReason.Other,
		};

		private const string ReasonFormField = "Reason";
		private const string StageFormField = "Stage";

		public static class CategoryNames
		{
			public const string Processing = "Processing";
			public const string RecordCreation = "RecordCreation";
			public const string Services = "Services";
			public const string Activities = "Activities";
			public const string Validation = "Validation";
			public const string Other = "Other";
		}

		public static class CategoryDisplayNames
		{
			public const string Processing = "Processing";
			public const string RecordCreation = "Record Creation";
			public const string Services = "Services";
			public const string Activities = "Activities";
			public const string Validation = "Validation";
			public const string Other = "Other";
		}

		#endregion

		public override void Configure(PXScreenConfiguration config)
		{
			var context = config.GetScreenConfigurationContext<OpportunityMaint, CROpportunity>();


			var conditions = new
			{
				IsInNewState =
					context.Conditions.FromBql<CROpportunity.status.IsEqual<OpportunityStatus.@new>>(),
				
				IsNotInNewState =
					context.Conditions.FromBql<CROpportunity.status.IsNotEqual<OpportunityStatus.@new>>(),

				BAccountIDIsNull =
					context.Conditions.FromBql<CROpportunity.bAccountID.IsNull>(),
			}.AutoNameConditions();

			var formOpen = CreateForm("FormOpen", OpenReasons, OpportunityReason.Qualified);
			var formWon = CreateForm("FormWon", WonReasons);
			var formLost = CreateForm("FormLost", LostReasons);

			#region categories

			var categoryProcessing = context.Categories.CreateNew(CategoryNames.Processing,
				category => category.DisplayName(CategoryDisplayNames.Processing));
			var categoryRecordCreation = context.Categories.CreateNew(CategoryNames.RecordCreation,
				category => category.DisplayName(CategoryDisplayNames.RecordCreation));
			var categoryServices = context.Categories.CreateNew(CategoryNames.Services,
				category => category.DisplayName(CategoryDisplayNames.Services));
			var categoryActivities = context.Categories.CreateNew(CategoryNames.Activities,
				category => category.DisplayName(CategoryDisplayNames.Activities));
			var categoryValidation = context.Categories.CreateNew(CategoryNames.Validation,
				category => category.DisplayName(CategoryDisplayNames.Validation));
			var categoryOther = context.Categories.CreateNew(CategoryNames.Other,
				category => category.DisplayName(CategoryDisplayNames.Other));

			#endregion

			var actionCreateTask = context.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWTASK_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));
			var actionCreateNote = context.ActionDefinitions.CreateExisting(
				CRActivityListBase<CRPMTimeActivity>._NEWACTIVITY_NOTE_WORKFLOW_COMMAND,
				a => a.WithCategory(categoryActivities));

			context.AddScreenConfigurationFor(screen =>
			{
				return screen
					.StateIdentifierIs<CROpportunity.status>()
					.AddDefaultFlow(DefaultOpportunityFlow)
					.WithActions(actions =>
					{
						actions.Add(g => g.Open, c => c
							.WithFieldAssignments(fields =>
							{
								fields.Add<CROpportunity.resolution>(f => f.SetFromFormField(formOpen, ReasonFormField));
								fields.Add<CROpportunity.stageID>(f => f.SetFromFormField(formOpen, StageFormField));
								fields.Add<CROpportunity.isActive>(f => f.SetFromValue(true));
								fields.Add<CROpportunity.closingDate>(f => f.SetFromValue(null));
							})
							.WithForm(formOpen)
							.IsHiddenWhen(conditions.IsInNewState)
							.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
							.WithCategory(categoryProcessing)
							.IsExposedToMobile(true)
							.MassProcessingScreen<UpdateOpportunityMassProcess>());

						actions.Add(g => g.OpenFromNew, c => c
							.WithFieldAssignments(fields =>
							{
								fields.Add<CROpportunity.resolution>(f => f.SetFromFormField(formOpen, ReasonFormField));
								fields.Add<CROpportunity.stageID>(f => f.SetFromFormField(formOpen, StageFormField));
								fields.Add<CROpportunity.isActive>(f => f.SetFromValue(true));
								fields.Add<CROpportunity.closingDate>(f => f.SetFromValue(null));
							})
							.WithForm(formOpen)
							.IsHiddenWhen(conditions.IsNotInNewState)
							.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
							.WithCategory(categoryProcessing)
							.IsExposedToMobile(true)
							.MassProcessingScreen<UpdateOpportunityMassProcess>());

						actions.Add(g => g.CloseAsWon, c => c
							.WithFieldAssignments(fields =>
							{
								fields.Add<CROpportunity.resolution>(f => f.SetFromFormField(formWon, ReasonFormField));
								fields.Add<CROpportunity.stageID>(f => f.SetFromFormField(formWon, StageFormField));
								fields.Add<CROpportunity.isActive>(f => f.SetFromValue(false));
								fields.Add<CROpportunity.closingDate>(f => f.SetFromToday());
								fields.Add<CROpportunity.allowOverrideContactAddress>(f => f.SetFromValue(true));
								fields.Add<CROpportunity.allowOverrideShippingContactAddress>(f => f.SetFromValue(true));
								fields.Add<CROpportunity.allowOverrideBillingContactAddress>(f => f.SetFromValue(true));
							})
							.WithForm(formWon)
							.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
							.WithCategory(categoryProcessing)
							.IsDisabledWhen(conditions.BAccountIDIsNull)
							.IsExposedToMobile(true)
							.MassProcessingScreen<UpdateOpportunityMassProcess>());

						actions.Add(g => g.CloseAsLost, c => c
							.WithFieldAssignments(fields =>
							{
								fields.Add<CROpportunity.resolution>(f => f.SetFromFormField(formLost, ReasonFormField));
								fields.Add<CROpportunity.stageID>(f => f.SetFromFormField(formLost, StageFormField));
								fields.Add<CROpportunity.isActive>(f => f.SetFromValue(false));
								fields.Add<CROpportunity.closingDate>(f => f.SetFromToday());
								fields.Add<CROpportunity.allowOverrideContactAddress>(f => f.SetFromValue(true));
								fields.Add<CROpportunity.allowOverrideShippingContactAddress>(f => f.SetFromValue(true));
								fields.Add<CROpportunity.allowOverrideBillingContactAddress>(f => f.SetFromValue(true));
							})
							.WithForm(formLost)
							.WithPersistOptions(ActionPersistOptions.PersistBeforeAction)
							.WithCategory(categoryProcessing)
							.IsExposedToMobile(true)
							.MassProcessingScreen<UpdateOpportunityMassProcess>());

						actions.Add(g => g.createQuote, c => c.WithCategory(categoryRecordCreation));
						actions.Add<CreateSOOrder>(g => g.CreateSalesOrder, c => c.WithCategory(categoryRecordCreation).WithPersistOptions(ActionPersistOptions.PersistBeforeAction));
						actions.Add<CreateAccountExt>(e => e.CreateBothContactAndAccount, c => c.WithCategory(categoryRecordCreation).WithPersistOptions(ActionPersistOptions.PersistBeforeAction));
						actions.Add<CreateContactExt>(e => e.CreateContact, c => c.WithCategory(categoryRecordCreation).WithPersistOptions(ActionPersistOptions.PersistBeforeAction));
						actions.Add<CreateInvoices>(g => g.CreateInvoice, c => c.WithCategory(categoryRecordCreation).WithPersistOptions(ActionPersistOptions.PersistBeforeAction));

						actions.Add(actionCreateTask);
						actions.Add(actionCreateNote);

						actions.Add<OpportunityMaint.Discount>(e => e.recalculatePrices, c => c.WithCategory(categoryOther).WithPersistOptions(ActionPersistOptions.PersistBeforeAction));
						actions.Add(g => g.validateAddresses, c => c.WithCategory(categoryOther).WithPersistOptions(ActionPersistOptions.PersistBeforeAction));
					})
					.WithForms(forms =>
					{
						forms.Add(formOpen);
						forms.Add(formWon);
						forms.Add(formLost);
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<CROpportunity>()
							.OfEntityEvent<CROpportunity.Events>(e => e.OpportunityCreatedFromLead)
							.Is(g => g.OnOpportunityCreatedFromLead)
							.UsesTargetAsPrimaryEntity()
							.DisplayName("Opportunity Created from Lead"));
					})
					.WithCategories(categories =>
					{
						categories.Add(categoryProcessing);
						categories.Add(categoryRecordCreation);
						categories.Add(categoryActivities);
						categories.Add(categoryServices);
						categories.Add(categoryValidation);
						categories.Add(categoryOther);
					});
			});


			Workflow.IConfigured DefaultOpportunityFlow(Workflow.INeedStatesFlow flow)
			{
				return flow
					.WithFlowStates(states =>
					{
						states.Add(context.FlowStates.Create<OpportunityStatus.@new>(state => state
							.IsInitial()
							.WithFieldStates(fields =>
							{
								fields.AddField<CROpportunity.resolution>(field => field
									.DefaultValue(OpportunityReason.Created)
									.ComboBoxValues(NewReasons));
								fields.AddField<CROpportunity.isActive>(field => field.IsDisabled());
								fields.AddField<CROpportunity.source>(field => field.ComboBoxValues(CRMSourcesAttribute.Values));
							})
							.WithActions(actions =>
							{
								actions.Add(g => g.createQuote, a => a.IsDuplicatedInToolbar());
								actions.Add<CreateSOOrder>(g => g.CreateSalesOrder);
								actions.Add<CreateInvoices>(g => g.CreateInvoice);
								actions.Add<CreateContactExt>(e => e.CreateContact);
								actions.Add<CreateAccountExt>(e => e.CreateBothContactAndAccount);
								actions.Add(g => g.validateAddresses);
								actions.Add<OpportunityMaint.Discount>(e => e.recalculatePrices);
								actions.Add(g => g.OpenFromNew, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
								actions.Add(g => g.CloseAsWon);
								actions.Add(g => g.CloseAsLost);
							})
							.WithEventHandlers(handlers =>
							{
								handlers.Add(g => g.OnOpportunityCreatedFromLead);
							})));

						states.Add(context.FlowStates.Create<OpportunityStatus.open>(state => state
							.WithFieldStates(fields =>
							{
								fields.AddField<CROpportunity.resolution>(field => field
									.DefaultValue(OpportunityReason.Qualified)
									.ComboBoxValues(OpenReasons));
								fields.AddField<CROpportunity.isActive>(field => field.IsDisabled());
								fields.AddField<CROpportunity.source>(field => field.ComboBoxValues(CRMSourcesAttribute.Values));
							})
							.WithActions(actions =>
							{
								actions.Add(g => g.createQuote, a => a.IsDuplicatedInToolbar());
								actions.Add<CreateSOOrder>(g => g.CreateSalesOrder);
								actions.Add<CreateInvoices>(g => g.CreateInvoice);
								actions.Add<CreateContactExt>(e => e.CreateContact);
								actions.Add<CreateAccountExt>(e => e.CreateBothContactAndAccount);
								actions.Add(g => g.validateAddresses);
								actions.Add<OpportunityMaint.Discount>(e => e.recalculatePrices);
								actions.Add(g => g.CloseAsWon, g => g.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
								actions.Add(g => g.CloseAsLost);
							})));

						states.Add(context.FlowStates.Create<OpportunityStatus.won>(state => state
							.WithFieldStates(fields =>
							{
								fields.AddField<CROpportunity.resolution>(field => field
									.ComboBoxValues(WonReasons)
									.IsDisabled());
								DisableFieldsForFinalStates(fields);
							})
							.WithActions(actions =>
							{
								actions.Add(g => g.createQuote);
								actions.Add<CreateSOOrder>(g => g.CreateSalesOrder);
								actions.Add<CreateInvoices>(g => g.CreateInvoice);
								actions.Add(g => g.Open, g => g.IsDuplicatedInToolbar());
							})));

						states.Add(context.FlowStates.Create<OpportunityStatus.lost>(state => state
							.WithFieldStates(fields =>
							{
								fields.AddField<CROpportunity.resolution>(field => field
									.ComboBoxValues(LostReasons)
									.IsDisabled());
								DisableFieldsForFinalStates(fields);
							})
							.WithActions(actions =>
							{
								actions.Add(g => g.createQuote);
								actions.Add(g => g.Open, g => g.IsDuplicatedInToolbar());
							})));
					})
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom<OpportunityStatus.@new>(ts =>
						{
							ts.Add(t => t.To<OpportunityStatus.open>().IsTriggeredOn(g => g.OpenFromNew));
							ts.Add(t => t.To<OpportunityStatus.won>().IsTriggeredOn(g => g.CloseAsWon));
							ts.Add(t => t.To<OpportunityStatus.lost>().IsTriggeredOn(g => g.CloseAsLost));

							ts.Add(t => t
								.To<OpportunityStatus.@new>()
								.IsTriggeredOn(g => g.OnOpportunityCreatedFromLead)
								.WithFieldAssignments(f =>
								{
									f.Add<CROpportunity.resolution>(OpportunityReason.ConvertedFromLead);
								}));
						});

						transitions.AddGroupFrom<OpportunityStatus.open>(ts =>
						{
							ts.Add(t => t.To<OpportunityStatus.won>().IsTriggeredOn(g => g.CloseAsWon));
							ts.Add(t => t.To<OpportunityStatus.lost>().IsTriggeredOn(g => g.CloseAsLost));
						});

						transitions.Add(ts => ts.From<OpportunityStatus.won>().To<OpportunityStatus.open>().IsTriggeredOn(g => g.Open));
						transitions.Add(ts => ts.From<OpportunityStatus.lost>().To<OpportunityStatus.open>().IsTriggeredOn(g => g.Open));
					});

				void DisableFieldsForFinalStates(FieldState.IContainerFillerFields fields)
				{
					fields.AddTable<CROpportunity>(field => field.IsDisabled());
					fields.AddTable<CROpportunityProducts>(field => field.IsDisabled());
					fields.AddTable<CRTaxTran>(field => field.IsDisabled());
					fields.AddTable<CROpportunityDiscountDetail>(field => field.IsDisabled());
					fields.AddTable<CRContact>(field => field.IsDisabled());
					fields.AddTable<CRAddress>(field => field.IsDisabled());
					fields.AddTable<CROpportunityTax>(field => field.IsDisabled());
					fields.AddTable<CRShippingContact>(field => field.IsDisabled());
					fields.AddTable<CRShippingAddress>(field => field.IsDisabled());
					fields.AddTable<CRBillingContact>(field => field.IsDisabled());
					fields.AddTable<CRBillingAddress>(field => field.IsDisabled());
					fields.AddField<CROpportunity.opportunityID>();
					fields.AddField<CROpportunity.subject>();
					fields.AddField<CROpportunity.details>();
				}
			}

			Form.IConfigured CreateForm(string formID, string[] valueCollection, string defaultValue = null, Action<FormField.IContainerFillerFields> fieldsExt = null)
			{
				return context.Forms.Create(formID, form => form
					.Prompt("Details")
					.WithFields(fields =>
					{
						fields.Add(ReasonFormField, field =>
						{
							var res = field
								.WithSchemaOf<CROpportunity.resolution>()
								.IsRequired()
								.Prompt("Reason")
								.OnlyComboBoxValues(valueCollection);

							if (defaultValue != null)
								return res.DefaultValue(defaultValue);

							return res;
						});

						fields.Add(StageFormField, field => field
							.WithSchemaOf<CROpportunity.stageID>()
							.DefaultValueFromSchemaField()
							.IsRequired()
							.Prompt("Stage"));

						fieldsExt?.Invoke(fields);
					}));
			}
		}
	}
}
