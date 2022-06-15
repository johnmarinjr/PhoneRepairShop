using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AR.GraphExtensions;
using PX.Objects.AR.Standalone;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	using State = ARDocStatus;
	using static ARCashSale;
	using static BoundedTo<ARCashSaleEntry, ARCashSale>;

	public class ARCashSaleEntry_Workflow : PXGraphExtension<ARCashSaleEntry>
	{
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<ARCashSaleEntry, ARCashSale>());

		public class Conditions : Condition.Pack
		{
			private readonly ARSetupDefinition _Definition = ARSetupDefinition.GetSlot();

			public Condition IsNotOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<False>.And<released.IsEqual<False>>
			>());

			public Condition IsVoid => GetOrCreate(c => c.FromBql<
				docType.IsIn<ARDocType.voidPayment, ARDocType.voidRefund>
			>());

			public Condition IsCCProcessed => GetOrCreate(c => c.FromBql<
				ARRegister.pendingProcessing.IsEqual<False>
			>());

			public Condition IsCCIntegrated => GetOrCreate(c =>
				PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>() &&
				_Definition.IntegratedCCProcessing == true && _Definition.MigrationMode != true
					? c.FromBql<status.IsEqual<ARDocStatus.cCHold>>()
					: c.FromBql<True.IsEqual<False>>()
			);

			public Condition IsNotCapturable => GetOrCreate(c => c.FromBql<
				docType.IsEqual<ARDocType.cashReturn>
			>());

			public Condition IsMigrationMode => GetOrCreate(c =>
				_Definition.MigrationMode == true
					? c.FromBql<True.IsEqual<True>>()
					: c.FromBql<True.IsEqual<False>>()
			);
		}

		[PXWorkflowDependsOnType(typeof(ARSetup))]
		protected virtual void Configure(WorkflowContext<ARCashSaleEntry, ARCashSale> context)
		{
			var _Definition = ARSetupDefinition.GetSlot();
			var conditions = context.Conditions.GetPack<Conditions>();
			#region Categories

			var processingCategory = context.Categories.CreateNew(CategoryID.Processing,
				category => category.DisplayName(CategoryNames.Processing));
			var cardProcessingCategory = context.Categories.CreateNew(CategoryID.CardProcessing,
				category => category.DisplayName(CategoryNames.CardProcessing));
			var correctionsCategory = context.Categories.CreateNew(CategoryID.Corrections,
				category => category.DisplayName(CategoryNames.Corrections));
			var printingAndEmailingCategory = context.Categories.CreateNew(CategoryID.PrintingAndEmailing,
				category => category.DisplayName(CategoryNames.PrintingAndEmailing));
			var approvalCategory = context.Categories.CreateNew(CategoryID.Approval,
				category => category.DisplayName(CategoryNames.Approval));
			var otherCategory = context.Categories.CreateNew(CategoryID.Other,
				category => category.DisplayName(CategoryNames.Other));

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
															actions.Add(g => g.sendEmail);
															actions.Add(g => g.printAREdit);
															actions.Add(g => g.customerDocuments);
															actions.Add(g => g.printInvoice);
														});
												});
												sss.Add<State.cCHold>(flowState =>
												{
													return flowState
														.IsSkippedWhen(conditions.IsCCProcessed)
														.WithActions(actions =>
														{
															actions.Add(a => a.putOnHold);
															actions.Add<ARCashSaleEntryPaymentTransaction>(
																a => a.captureCCPayment,
																a => a.WithConnotation(ActionConnotation.Success));
															actions.Add<ARCashSaleEntryPaymentTransaction>(a =>
																a.authorizeCCPayment);
															actions.Add<ARCashSaleEntryPaymentTransaction>(a =>
																a.voidCCPayment);
															actions.Add<ARCashSaleEntryPaymentTransaction>(a =>
																a.creditCCPayment);
															actions.Add<ARCashSaleEntryPaymentTransaction>(a =>
																a.recordCCPayment);
															actions.Add<ARCashSaleEntryPaymentTransaction>(a =>
																a.captureOnlyCCPayment);
															actions.Add<ARCashSaleEntryPaymentTransaction>(a =>
																a.validateCCPayment);
															actions.Add(g => g.release);
															actions.Add(a => a.voidCheck);
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
															actions.Add(g => g.sendEmail);
															actions.Add(g => g.emailInvoice);
															actions.Add(g => g.putOnHold);
															actions.Add(g => g.printAREdit);
															actions.Add(g => g.printInvoice);
															actions.Add(g => g.customerDocuments);
														});
												});
											})
											.WithEventHandlers(handlers => { handlers.Add(g => g.OnUpdateStatus); }));

									fss.Add<State.closed>(flowState =>
									{
										return flowState
											.WithActions(actions =>
											{
												actions.Add(g => g.sendEmail);
												actions.Add(g => g.emailInvoice);
												actions.Add(g => g.printInvoice);
												actions.Add(g => g.printARRegister);
												actions.Add(g => g.customerDocuments);
												actions.Add(g => g.voidCheck, a => a.IsDuplicatedInToolbar());
												actions.Add(g => g.reclassifyBatch);
												actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.captureCCPayment);
												actions.Add<ARCashSaleEntryPaymentTransaction>(
													a => a.authorizeCCPayment);
												actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.voidCCPayment);
												actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.creditCCPayment);
												actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.recordCCPayment);
												actions.Add<ARCashSaleEntryPaymentTransaction>(a =>
													a.captureOnlyCCPayment);
												actions.Add<ARCashSaleEntryPaymentTransaction>(a =>
													a.validateCCPayment);
											});
									});
									fss.Add<State.open>();
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
									
									transitions.AddGroupFrom<State.cCHold>(ts =>
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
						actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.captureCCPayment, a => a
							.WithCategory(cardProcessingCategory)
							.IsHiddenWhen(conditions.IsNotCapturable));
						actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.authorizeCCPayment, a => a
							.WithCategory(cardProcessingCategory));
						actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.voidCCPayment, a => a
							.WithCategory(cardProcessingCategory));
						actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.creditCCPayment, a => a
							.WithCategory(cardProcessingCategory));
						actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.recordCCPayment, a => a
							.WithCategory(cardProcessingCategory));
						actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.captureOnlyCCPayment, a => a
							.WithCategory(cardProcessingCategory));
						actions.Add<ARCashSaleEntryPaymentTransaction>(a => a.validateCCPayment, a => a
							.WithCategory(cardProcessingCategory));
						actions.Add(g => g.printInvoice, c => c.WithCategory(printingAndEmailingCategory));
						actions.Add(g => g.emailInvoice, c => c
							.WithCategory(printingAndEmailingCategory)
							.WithFieldAssignments(fass => fass.Add<emailed>(v => v.SetFromValue(true))));
						actions.Add(g => g.release, c => c
							.WithCategory(processingCategory)
							.IsDisabledWhen(conditions.IsCCIntegrated));
						actions.Add(g => g.voidCheck, c => c
							.WithCategory(correctionsCategory)
							.IsHiddenWhen(conditions.IsVoid));
						actions.Add(g => g.reclassifyBatch, c => c
							.IsHiddenWhen(conditions.IsMigrationMode)
							.WithCategory(correctionsCategory));
						actions.Add(g => g.sendEmail, c => c
							.WithCategory(otherCategory));
						actions.Add(g => g.customerDocuments, c => c.WithCategory(PredefinedCategory.Inquiries));
						actions.Add(g => g.printAREdit, c => c.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.printARRegister, c => c.WithCategory(PredefinedCategory.Reports));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<ARCashSale>()
							.OfFieldsUpdated<BqlFields.FilledWith<ARCashSale.hold, ARCashSale.pendingProcessing>>()
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(cardProcessingCategory);
						categories.Add(correctionsCategory);
						categories.Add(approvalCategory);
						categories.Add(printingAndEmailingCategory);
						categories.Add(otherCategory);
						categories.Update(FolderType.InquiriesFolder, category => category.PlaceAfter(otherCategory));
						categories.Update(FolderType.ReportsFolder,
							category => category.PlaceAfter(FolderType.InquiriesFolder));
					})
			);
		}
		
		public static class CategoryNames
		{
			public const string Processing = "Processing";
			public const string CardProcessing = "Card Processing";
			public const string Corrections = "Corrections";
			public const string PrintingAndEmailing = "Printing and Emailing";
			public const string Approval = "Approval";
			public const string Other = "Other";
		}

		public static class CategoryID
		{
			public const string Processing = "ProcessingID";
			public const string CardProcessing = "CardProcessingID";
			public const string Corrections = "CorrectionsID";
			public const string PrintingAndEmailing = "PrintingAndEmailingID";
			public const string Approval = "ApprovalID";
			public const string Other = "OtherID";
		}
	}
}