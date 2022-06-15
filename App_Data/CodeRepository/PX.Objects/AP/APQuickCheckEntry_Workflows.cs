using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ProjectDefinition.Workflow;
using PX.Data.WorkflowAPI;
using PX.Objects.AP.Standalone;
using PX.Objects.Common.Extensions;
using PX.Objects.CR.Workflows;
using PX.Objects.CS;

namespace PX.Objects.AP
{
	using State = APDocStatus;
	using static APQuickCheck;
	using static BoundedTo<APQuickCheckEntry, APQuickCheck>;

	public class APQuickCheckEntry_Workflow : PXGraphExtension<APQuickCheckEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<APQuickCheckEntry, APQuickCheck>());

		public class Conditions : Condition.Pack
		{
			private readonly APSetupDefinition _Definition = APSetupDefinition.GetSlot();
			
			public Condition IsNotOnHold => GetOrCreate(b => b.FromBql<
				hold.IsEqual<False>
			>());
			
			public Condition IsNotQuickCheck => GetOrCreate(b => b.FromBql<
				docType.IsNotEqual<APDocType.quickCheck>
			>());

			public Condition IsNotPrintable => GetOrCreate(b => b.FromBql<
				printCheck.IsEqual<False>.Or<printed.IsEqual<True>>
			>());

			public Condition IsSkipPrinted => GetOrCreate(b => b.FromBql<
				printCheck.IsEqual<False>.Or<printed.IsEqual<False>>
			>());

			public Condition IsVoided => GetOrCreate(b => b.FromBql<
				docType.IsEqual<APDocType.voidQuickCheck>
			>());

			public Condition IsMigrationMode => GetOrCreate(c =>
				_Definition.MigrationMode == true
					? c.FromBql<True.IsEqual<True>>()
					: c.FromBql<True.IsEqual<False>>()
			);
		}

		protected virtual void Configure(WorkflowContext<APQuickCheckEntry, APQuickCheck> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();
			
			const string initialState = "_";

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
										actions.Add(g => g.validateAddresses);
									});
							});
												sss.Add<State.pendingPrint>(flowState =>
							{
								return flowState
														.IsSkippedWhen(conditions.IsNotPrintable)													
									.WithActions(actions =>
									{
															actions.Add(g => g.putOnHold, a => a.IsDuplicatedInToolbar());
															actions.Add(g => g.printCheck,
																a => a.IsDuplicatedInToolbar()
																	.WithConnotation(ActionConnotation.Success));
									});
							});
												sss.Add<State.printed>(flowState =>
							{
								return flowState
                                    	   .IsSkippedWhen(conditions.IsSkipPrinted)
									.WithActions(actions =>
									{
                                    			actions.Add(g => g.release,
                                    				a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
                                    			actions.Add(g => g.prebook, a => a.IsDuplicatedInToolbar());
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
															actions.Add(g => g.validateAddresses);
														});
												});
											})
											.WithActions(actions =>
											{
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
											})
											.WithEventHandlers(handlers => { handlers.Add(g => g.OnUpdateStatus); })
									);
							fss.Add<State.prebooked>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
												actions.Add(g => g.release,
													a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									});
							});
							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.reclassifyBatch);
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									});
							});
							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.reclassifyBatch);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									});
							});
							fss.Add<State.voided>();
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
						});
									transitions.AddGroupFrom<State.printed>(ts =>
						{
							ts.Add(t => t
								.To<State.prebooked>()
								.IsTriggeredOn(g => g.prebook));
						});
                           transitions.AddGroupFrom<State.balanced>(ts =>
						{
							ts.Add(t => t
								.To<State.prebooked>()
								.IsTriggeredOn(g => g.prebook));
						});
						
						
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.voidCheck));
						});
					}
					))
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
						actions.Add(g => g.printCheck, c => c
							.WithCategory(processingCategory)
							.IsHiddenWhen(conditions.IsNotQuickCheck));
						actions.Add(g => g.prebook, c => c
							.WithCategory(processingCategory)
							.IsHiddenWhen(conditions.IsNotQuickCheck || conditions.IsMigrationMode));
						actions.Add(g => g.release, c => c
							.WithCategory(processingCategory));
						actions.Add(g => g.voidCheck, c => c
							.WithCategory(correctionsCategory)
							.IsHiddenWhen(conditions.IsVoided));
						actions.Add(g => g.validateAddresses, c => c
							.WithCategory(customOtherCategory));
						actions.Add(g => g.reclassifyBatch, c => c
							.WithCategory(correctionsCategory)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.vendorDocuments, c => c.WithCategory(PredefinedCategory.Inquiries));
						actions.Add(g => g.printAPEdit, c => c.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.printAPRegister, c => c.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.printAPPayment, c => c.WithCategory(PredefinedCategory.Reports));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<APQuickCheck>()
							.OfFieldsUpdated<BqlFields.FilledWith<APQuickCheck.hold, APQuickCheck.printCheck>>()
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