using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	using State = APDocStatus;
	using static APInvoice;
	using static BoundedTo<APInvoiceEntry, APInvoice>;

	public class APSetupDefinition : IPrefetchable
	{
		public bool? MigrationMode { get; private set; }

		void IPrefetchable.Prefetch()
		{
			using (PXDataRecord rec =
				PXDatabase.SelectSingle<APSetup>(
					new PXDataField("migrationMode")
					))
			{
				MigrationMode = rec != null ? rec.GetBoolean(0) : false;
			}
		}

		public static APSetupDefinition GetSlot()
		{
			return PXDatabase.GetSlot<APSetupDefinition>(typeof(APSetup).FullName, typeof(APSetup));
		}
	}

	public class APInvoiceEntry_Workflow : PXGraphExtension<APInvoiceEntry>
	{
		[PXWorkflowDependsOnType(typeof(APSetup))]
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<APInvoiceEntry, APInvoice>());

		public class Conditions : Condition.Pack
		{
			private readonly APSetupDefinition _Definition = APSetupDefinition.GetSlot();

			public Condition IsNotOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<False>
			>());

			public Condition IsReserved => GetOrCreate(c => c.FromBql<
				hold.IsEqual<True>.And<released.IsEqual<True>>
			>());

			public Condition IsOpen => GetOrCreate(c => c.FromBql<
				openDoc.IsEqual<True>.And<released.IsEqual<True>>
			>());

			public Condition IsClosed => GetOrCreate(c => c.FromBql<
				openDoc.IsEqual<False>.And<released.IsEqual<True>>
			>());

			public Condition IsZeroBalance => GetOrCreate(c => c.FromBql<
				docBal.IsEqual<decimal0>
			>());

			public Condition IsPrepayment => GetOrCreate(c => c.FromBql<
				docType.IsEqual<APDocType.prepayment>
			>());
				
			public Condition IsNotDebitAdjustment => GetOrCreate(c => c.FromBql<
				docType.IsNotEqual<APDocType.debitAdj>
			>());

			public Condition IsNotAllowRefund => GetOrCreate(c => c.FromBql<
				docType.IsNotEqual<APDocType.debitAdj>
						.Or<APRegister.curyRetainageTotal.IsGreater<decimal0>>
				.Or<isRetainageDocument.IsEqual<True>>
			>());

			public Condition IsNotAllowReclasify => GetOrCreate(c => c.FromBql<
				docType.IsEqual<APDocType.prepayment>
						.Or<docType.IsEqual<APDocType.debitAdj>
							.And<APRegister.curyRetainageTotal.IsGreater<decimal0>
						.Or<isRetainageDocument.IsEqual<True>>>>
			>());

			public Condition IsNotAllowReverce => GetOrCreate(c => c.FromBql<
				docType.IsIn<APDocType.prepayment, APDocType.debitAdj>
			>());

			public Condition IsNotAllowVoidPrepayment => GetOrCreate(c => c.FromBql<
				docType.IsNotEqual<APDocType.prepayment>
			>());

			public Condition IsNotAllowVoidInvoice => GetOrCreate(c => c.FromBql<
				docType.IsNotIn<APDocType.invoice, APDocType.creditAdj, APDocType.debitAdj>
			>());

			public Condition IsRetainage => GetOrCreate(c => c.FromBql<
				isRetainageDocument.IsEqual<True>.Or<retainageApply.IsEqual<True>>
			>());

			public Condition IsNotAllowRecalcPrice => GetOrCreate(c => c.FromBql<
				pendingPPD.IsEqual<True>
						.Or<docType.IsEqual<APDocType.debitAdj>
							.And<APRegister.curyRetainageTotal.IsGreater<decimal0>
						.Or<isRetainageDocument.IsEqual<True>>>>
			>());

			public Condition IsMigrationMode => GetOrCreate(c =>
					_Definition.MigrationMode == true
					? c.FromBql<True.IsEqual<True>>()
					: c.FromBql<True.IsEqual<False>>()
			);
		}

		protected virtual void Configure(WorkflowContext<APInvoiceEntry, APInvoice> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();		

			#region Categories

			var processingCategory = context.Categories.CreateNew(ActionCategoryNames.Processing,
				category => category.DisplayName(ActionCategory.Processing));
			var approvalCategory = context.Categories.CreateNew(ActionCategoryNames.Approval,
				category => category.DisplayName(ActionCategory.Approval));
			var correctionsCategory = context.Categories.CreateNew(ActionCategoryNames.Corrections,
				category => category.DisplayName(ActionCategory.Corrections));
			var customOtherCategory = context.Categories.CreateNew(ActionCategoryNames.CustomOther,
				category => category.DisplayName(ActionCategory.Other));

			#endregion

			const string initialState = "_";

			context.AddScreenConfigurationFor(screen =>
				screen
					.StateIdentifierIs<status>()
					.AddDefaultFlow(flow =>
						flow
						.WithFlowStates(fss =>
						{
							fss.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
									fss.AddSequence<State.HoldToBalance>(seq =>
										seq.WithStates(sss =>
											{
												sss.Add<State.hold>(flowState =>
							{
								return flowState
														.IsSkippedWhen(conditions.IsNotOnHold)
									.WithActions(actions =>
									{
															actions.Add(g => g.releaseFromHold,
																a => a.IsDuplicatedInToolbar()
																	.WithConnotation(ActionConnotation.Success));
									});
							});
												sss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
															actions.Add(g => g.release,
																a => a.IsDuplicatedInToolbar()
																	.WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.prebook, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.putOnHold);
															actions.Add(g => g.createSchedule);
														});
												});
											})
											.WithActions(actions =>
											{
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.recalculateDiscountsAction);
											})
											.WithEventHandlers(handlers =>
									{
												handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnReleaseDocument);
											})
									);
							fss.Add<State.scheduled>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.createSchedule, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
									})
									.WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnConfirmSchedule);
										handlers.Add(g => g.OnVoidSchedule);
									});
							});
							fss.Add<State.voided>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
									});
							});
							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
												actions.Add(g => g.payInvoice,
													a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.voidDocument);
										actions.Add(g => g.voidInvoice);
										actions.Add(g => g.vendorRefund);
										actions.Add(g => g.reverseInvoice);
										actions.Add(g => g.reclassifyBatch);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnCloseDocument);
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnVoidDocument);
									});
							});

							fss.Add<State.prebooked>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.payInvoice, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.reverseInvoice);
										actions.Add(g => g.voidInvoice);
											}).WithEventHandlers(handlers => { handlers.Add(g => g.OnReleaseDocument); });
							});
							fss.Add<State.printed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.payInvoice, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.voidInvoice);
											}).WithEventHandlers(handlers => { handlers.Add(g => g.OnReleaseDocument); });
							});
							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.reverseInvoice);
										actions.Add(g => g.reclassifyBatch);
									})
									.WithEventHandlers(handlers =>
										handlers.Add(g => g.OnOpenDocument));
							});
							fss.Add<State.reserved>();
						}
						)
					.WithTransitions(transitions =>
					{
						transitions.AddGroupFrom(initialState, ts =>
						{
									ts.Add(t => t.To<State.HoldToBalance>()
										.IsTriggeredOn(g => g.initializeState)); // To default sequence
							});
								transitions.AddGroupFrom<State.HoldToBalance>(ts =>
						{
							ts.Add(t => t
										.To<State.HoldToBalance>()
										.IsTriggeredOn(g => g.OnUpdateStatus));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
									ts.Add(t => t.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
								});
								transitions.AddGroupFrom<State.balanced>(ts =>
								{
							ts.Add(t => t
										.To<State.prebooked>()
										.IsTriggeredOn(g => g.prebook));
									
						});
						transitions.AddGroupFrom<State.prebooked>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.voidInvoice));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
						});
						transitions.AddGroupFrom<State.scheduled>(ts =>
						{
							ts.Add(t => t
								.To<State.scheduled>()
								.IsTriggeredOn(g => g.OnConfirmSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<scheduled>(e => e.SetFromValue(true));
									fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
								}));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidSchedule)
								.WithFieldAssignments(fas =>
								{
									fas.Add<voided>(e => e.SetFromValue(true));
									fas.Add<scheduled>(e => e.SetFromValue(false));
									fas.Add<scheduleID>(e => e.SetFromValue(null));
								}));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.voidInvoice));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.voidDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnCloseDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
						});
						transitions.AddGroupFrom<State.printed>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
						});
						transitions.AddGroupFrom<State.closed>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnOpenDocument));
						});
					}))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
						actions.Add(g => g.releaseFromHold, c => c
							.WithCategory(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
						actions.Add(g => g.putOnHold, c => c
							.WithCategory(processingCategory)
							.WithPersistOptions(ActionPersistOptions.NoPersist)
							.WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(true))));
						actions.Add(g => g.prebook, c => c
							.WithCategory(processingCategory)
							.IsHiddenWhen(conditions.IsPrepayment || conditions.IsMigrationMode)
							.IsDisabledWhen(conditions.IsRetainage));
						actions.Add(g => g.release, c => c
							.WithCategory(processingCategory));
						actions.Add(g => g.payInvoice, c => c
							.WithCategory(processingCategory)
							.IsHiddenWhen(conditions.IsMigrationMode)
							.IsDisabledWhen(conditions.IsClosed));
						actions.Add(g => g.vendorRefund, c => c
							.WithCategory(processingCategory)
							.IsDisabledWhen(conditions.IsNotAllowRefund)
							.IsHiddenWhen(conditions.IsNotDebitAdjustment || conditions.IsMigrationMode));
						actions.Add(g => g.reverseInvoice, c => c
							.WithCategory(correctionsCategory)
							.IsDisabledWhen(conditions.IsNotAllowReverce)
							.IsHiddenWhen(conditions.IsNotAllowReverce));
						//Complex dependency by PO link & Applications
						actions.Add(g => g.voidInvoice, c => c
							.WithCategory(correctionsCategory)
							.PlaceAfterInCategory(g => g.reverseInvoice)
							.IsHiddenWhen(conditions.IsNotAllowVoidInvoice || conditions.IsMigrationMode));
						actions.Add(g => g.reclassifyBatch, c => c
							.WithCategory(correctionsCategory)
							.PlaceAfterInCategory(g => g.voidInvoice)
							.IsDisabledWhen(conditions.IsNotAllowReclasify)
							.IsHiddenWhen(conditions.IsPrepayment || conditions.IsMigrationMode));
						actions.Add(g => g.voidDocument, c => c
							.WithCategory(correctionsCategory)
							.IsDisabledWhen(conditions.IsNotAllowVoidPrepayment)
							.IsHiddenWhen(conditions.IsNotAllowVoidPrepayment || conditions.IsMigrationMode));
						actions.Add(g => g.createSchedule, c => c
							.WithCategory(customOtherCategory)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.recalculateDiscountsAction, c => c
							.WithCategory(customOtherCategory)
							.IsHiddenWhen(conditions.IsMigrationMode)
							.IsDisabledWhen(conditions.IsNotAllowRecalcPrice));
						actions.Add(g => g.vendorDocuments, c => c.WithCategory(PredefinedCategory.Inquiries));
						actions.Add(g => g.printAPEdit, c => c.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.printAPRegister,
							c => c.WithCategory(PredefinedCategory.Reports).IsHiddenWhen(conditions.IsPrepayment));
					})
					.WithHandlers(handlers =>
						{
							handlers.Add(handler => handler
								.WithTargetOf<APRegister>()
								.WithParametersOf<GL.Schedule>()
								.OfEntityEvent<APRegister.Events>(e => e.ConfirmSchedule)
								.Is(g => g.OnConfirmSchedule)
								.UsesPrimaryEntityGetter<
									SelectFrom<APInvoice>.
									Where<APInvoice.docType.IsEqual<APRegister.docType.FromCurrent>.
										And<APInvoice.refNbr.IsEqual<APRegister.refNbr.FromCurrent>>>
								>());
							handlers.Add(handler => handler
								.WithTargetOf<APRegister>()
								.WithParametersOf<GL.Schedule>()
								.OfEntityEvent<APRegister.Events>(e => e.VoidSchedule)
								.Is(g => g.OnVoidSchedule)
								.UsesPrimaryEntityGetter<
									SelectFrom<APInvoice>.
									Where<APInvoice.docType.IsEqual<APRegister.docType.FromCurrent>.
										And<APInvoice.refNbr.IsEqual<APRegister.refNbr.FromCurrent>>>
								>());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.OpenDocument)
								.Is(g => g.OnOpenDocument)
								.UsesTargetAsPrimaryEntity()
								.WithUpcastTo<APRegister>());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.CloseDocument)
								.Is(g => g.OnCloseDocument)
								.UsesTargetAsPrimaryEntity()
								.WithUpcastTo<APRegister>());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.ReleaseDocument)
								.Is(g => g.OnReleaseDocument)
								.UsesTargetAsPrimaryEntity()
								.WithUpcastTo<APRegister>());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
								.OfEntityEvent<APInvoice.Events>(e => e.VoidDocument)
								.Is(g => g.OnVoidDocument)
								.UsesTargetAsPrimaryEntity()
								.WithUpcastTo<APRegister>());
							handlers.Add(handler => handler
								.WithTargetOf<APInvoice>()
							.OfFieldUpdated<APInvoice.hold>()
								.Is(g => g.OnUpdateStatus)
								.UsesTargetAsPrimaryEntity());
						})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(correctionsCategory);
						categories.Add(approvalCategory);
						categories.Add(customOtherCategory);
						categories.Update(FolderType.InquiriesFolder, category => category.PlaceAfter(customOtherCategory));
						categories.Update(FolderType.ReportsFolder,
							category => category.PlaceAfter(context.Categories.Get(FolderType.InquiriesFolder)));
					})
			);
		}

		public static class ActionCategoryNames
		{
			public const string Processing = "Processing";
			public const string Approval = "Approval";
			public const string Corrections = "Corrections";
			public const string CustomOther = "CustomOther";
		}

		public static class ActionCategory
		{
			public const string Processing = "Processing";
			public const string Approval = "Approval";
			public const string Corrections = "Corrections";
			public const string Other = "Other";
		}
	}	
}