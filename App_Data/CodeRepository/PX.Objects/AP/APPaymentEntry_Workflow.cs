using System;
using System.Collections.Generic;
using System.Data;
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
	using static APPayment;
	using static BoundedTo<APPaymentEntry, APPayment>;

	public partial class APPaymentEntry_Workflow : PXGraphExtension<APPaymentEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<APPaymentEntry, APPayment>());

		public class Conditions : Condition.Pack
		{
			public Condition IsNotOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<False>
			>());

			public Condition IsReserved => GetOrCreate(c => c.FromBql<
				hold.IsEqual<True>.And<released.IsEqual<True>>
			>());

			public Condition IsNotPrintable => GetOrCreate(c => c.FromBql<
				printCheck.IsEqual<False>.Or<printed.IsEqual<True>>
			>());

			public Condition IsSkipPrinted => GetOrCreate(c => c.FromBql<
				printCheck.IsEqual<False>.Or<printed.IsEqual<False>>
			>());

			public Condition IsOpen => GetOrCreate(c => c.FromBql<
				openDoc.IsEqual<True>.And<released.IsEqual<True>>
			>());

			public Condition IsClosed => GetOrCreate(c => c.FromBql<
				openDoc.IsEqual<False>.And<released.IsEqual<True>>
			>());
			
			public Condition IsVoidHidden => GetOrCreate(c => c.FromBql<
				docType.IsIn<APDocType.voidCheck, APDocType.voidRefund, APDocType.debitAdj>
			>());

			public Condition IsDebitAdj => GetOrCreate(c => c.FromBql<
				docType.IsEqual<APDocType.debitAdj>
			>());
		}

		protected virtual void Configure(WorkflowContext<APPaymentEntry, APPayment> context)
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
													})
													.WithEventHandlers(handlers => { handlers.Add(g => g.OnPrintCheck); });
											});
											sss.Add<State.printed>(flowState =>
											{
												return flowState
													.IsSkippedWhen(conditions.IsSkipPrinted)
													.WithActions(actions =>
									{
														actions.Add(g => g.release,
															a => a.IsDuplicatedInToolbar()
																.WithConnotation(ActionConnotation.Success));
													})
													.WithEventHandlers(handlers => { handlers.Add(g => g.OnCancelPrintCheck); });
										});
											sss.Add<State.balanced>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
														actions.Add(g => g.release,
															a => a.IsDuplicatedInToolbar()
																.WithConnotation(ActionConnotation.Success));
														actions.Add(g => g.putOnHold);
										});
							});
										})
									.WithActions(actions =>
									{
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.vendorDocuments);
											actions.Add(g => g.validateAddresses);
										})
										.WithEventHandlers(handlers =>
									{
											handlers.Add(g => g.OnUpdateStatus);
										handlers.Add(g => g.OnReleaseDocument);
										}));
							fss.Add<State.prebooked>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.release, a => a.IsDuplicatedInToolbar());
										actions.Add(g => g.printAPEdit);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnVoidDocument);
										handlers.Add(g => g.OnCloseDocument);
										});
										});

							fss.Add<State.open>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
											actions.Add(g => g.release,
												a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.putOnHold);
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.voidCheck);
										actions.Add(g => g.reverseApplication);
										actions.Add(g => g.initializeState, act => act.IsAutoAction());
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnReleaseDocument);
										handlers.Add(g => g.OnVoidDocument);
										handlers.Add(g => g.OnCloseDocument);
										});
										});
							fss.Add<State.reserved>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
											actions.Add(g => g.releaseFromHold,
												a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.voidCheck);
										}).WithEventHandlers(handlers => { handlers.Add(g => g.OnVoidDocument); });
							});
							fss.Add<State.closed>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printAPRegister);
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.vendorDocuments);
										actions.Add(g => g.reverseApplication);
										actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
									}).WithEventHandlers(handlers =>
									{
										handlers.Add(g => g.OnVoidDocument);
										handlers.Add(g => g.OnOpenDocument);
										});
							});
							fss.Add<State.voided>(flowState =>
							{
								return flowState
									.WithActions(actions =>
									{
										actions.Add(g => g.printAPPayment);
										actions.Add(g => g.printAPRegister);
									});
							});
						})
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
						transitions.AddGroupFrom<State.pendingPrint>(ts =>
						{
										/*
							ts.Add(t => t
								.To<State.printed>()
								.IsTriggeredOn(g => g.OnPrintCheck));
										*/	
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
							ts.Add(t => t
								.To<State.pendingPrint>()
											.IsTriggeredOn(g => g.OnCancelPrintCheck));
						});
						transitions.AddGroupFrom<State.balanced>(ts =>
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
						transitions.AddGroupFrom<State.prebooked>(ts =>
						{
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsOpen));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnCloseDocument));
						});
						transitions.AddGroupFrom<State.open>(ts =>
						{
							ts.Add(t => t
								.To<State.reserved>()
								.IsTriggeredOn(g => g.putOnHold));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnReleaseDocument)
								.When(conditions.IsClosed));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.OnCloseDocument));
							ts.Add(t => t
								.To<State.closed>()
								.IsTriggeredOn(g => g.initializeState)
								.When(conditions.IsClosed));
						});
						transitions.AddGroupFrom<State.reserved>(ts =>
						{
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.releaseFromHold));
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
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
							ts.Add(t => t
								.To<State.voided>()
								.IsTriggeredOn(g => g.OnVoidDocument));
							ts.Add(t => t
								.To<State.open>()
								.IsTriggeredOn(g => g.reverseApplication)
								.DoesNotPersist());
						});
					}
					))
					.WithActions(actions =>
					{
						actions.Add(g => g.initializeState, a => a
							.IsHiddenAlways());
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
							.IsHiddenWhen(conditions.IsNotPrintable));
						actions.Add(g => g.release, c => c
							.WithCategory(processingCategory));
						actions.Add(g => g.voidCheck, c => c
							.WithCategory(correctionsCategory)
							.IsHiddenWhen(conditions.IsVoidHidden));
						actions.Add(g => g.validateAddresses, g => g
							.WithCategory(customOtherCategory));
						actions.Add(g => g.reverseApplication, g=> g
							.WithPersistOptions(ActionPersistOptions.NoPersist)
						);
						actions.Add(g => g.vendorDocuments, c => c.WithCategory(PredefinedCategory.Inquiries));
						actions.Add(g => g.printAPEdit, c => c
							.WithCategory(PredefinedCategory.Reports)
							.IsHiddenWhen(conditions.IsDebitAdj));
						actions.Add(g => g.printAPRegister, c => c.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.printAPPayment, c => c
							.WithCategory(PredefinedCategory.Reports)
							.IsHiddenWhen(conditions.IsDebitAdj));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.PrintCheck)
							.Is(g => g.OnPrintCheck)
							.UsesTargetAsPrimaryEntity()
							.WithFieldAssignments(fas => fas.Add<printed>(f => f.SetFromValue(true))));
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.CancelPrintCheck)
							.Is(g => g.OnCancelPrintCheck)
							.UsesTargetAsPrimaryEntity()
							.WithFieldAssignments(fas =>
                     	{
                     		fas.Add<hold>(f => f.SetFromValue(false));
                     		fas.Add<printed>(f => f.SetFromValue(false));
                     		fas.Add<extRefNbr>(f => f.SetFromValue(null));
                     	}
                     ));
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.ReleaseDocument)
							.Is(g => g.OnReleaseDocument)
							.UsesTargetAsPrimaryEntity()
							.WithUpcastTo<APRegister>());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.VoidDocument)
							.Is(g => g.OnVoidDocument)
							.UsesTargetAsPrimaryEntity()
							.WithUpcastTo<APRegister>());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.OpenDocument)
							.Is(g => g.OnOpenDocument)
							.UsesTargetAsPrimaryEntity()
							.WithUpcastTo<APRegister>());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfEntityEvent<APPayment.Events>(e => e.CloseDocument)
							.Is(g => g.OnCloseDocument)
							.UsesTargetAsPrimaryEntity()
							.WithUpcastTo<APRegister>());
						handlers.Add(handler => handler
							.WithTargetOf<APPayment>()
							.OfFieldsUpdated<BqlFields.FilledWith<APPayment.hold, APPayment.printCheck,APPayment.printed>>()
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
			public const string CustomInquiries = "Inquiries";
		}

		public static class ActionCategory
		{
			public const string Processing = "Processing";
			public const string Approval = "Approval";
			public const string Corrections = "Corrections";
			public const string Other = "Other";
			public const string Inquiries = "Inquiries";
		}
	}
}