using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.CS;

namespace PX.Objects.AR
{
	using State = ARDocStatus;
	using static ARInvoice;
	using static BoundedTo<ARInvoiceEntry, ARInvoice>;

	public class ARSetupDefinition : IPrefetchable
	{
		public bool? PrintBeforeRelease { get; private set; }
		public bool? EmailBeforeRelease { get; private set; }
		public bool? IntegratedCCProcessing { get; private set; }
		public bool? HoldEntry { get; private set; }
		public bool? MigrationMode { get; private set; }

		void IPrefetchable.Prefetch()
		{
			using (PXDataRecord rec =
				PXDatabase.SelectSingle<ARSetup>(
					new PXDataField("PrintBeforeRelease"),
					new PXDataField("EmailBeforeRelease"),
					new PXDataField("IntegratedCCProcessing"),
					new PXDataField("HoldEntry"),
					new PXDataField("MigrationMode")))
			{
				PrintBeforeRelease = rec != null ? rec.GetBoolean(0) : false;
				EmailBeforeRelease = rec != null ? rec.GetBoolean(1) : false;
				IntegratedCCProcessing = rec != null ? rec.GetBoolean(2) : false;
				HoldEntry = rec != null ? rec.GetBoolean(3) : false;
				MigrationMode = rec != null ? rec.GetBoolean(4) : false;
			}
		}

		public static ARSetupDefinition GetSlot()
		{
			return PXDatabase.GetSlot<ARSetupDefinition>(typeof(ARSetup).FullName, typeof(ARSetup));
		}
	}

	public class ARInvoiceEntry_Workflow : PXGraphExtension<ARInvoiceEntry>
	{
		public const string MarkAsDontEmail = "Mark as Do not Email";

		[PXWorkflowDependsOnType(typeof(ARSetup))]
		public override void Configure(PXScreenConfiguration config) =>
			Configure(config.GetScreenConfigurationContext<ARInvoiceEntry, ARInvoice>());

		public class Conditions : Condition.Pack
		{
			private readonly ARSetupDefinition _Definition = ARSetupDefinition.GetSlot();
			
			public Condition IsNotOnHold => GetOrCreate(c => c.FromBql<
				hold.IsEqual<False>.And<released.IsEqual<False>>
			>());

			public Condition IsCreditHoldChecked => GetOrCreate(c => c.FromBql<
				creditHold.IsEqual<False>
			>());

			public Condition IsPrinted => GetOrCreate(c =>
				_Definition.PrintBeforeRelease == true
					? c.FromBql<printInvoice.IsEqual<False>.Or<printed.IsEqual<True>>>()
					: c.FromBql<True.IsEqual<True>>());

			public Condition IsEmailed => GetOrCreate(c =>
				_Definition.EmailBeforeRelease == true
					? c.FromBql<dontEmail.IsEqual<True>.Or<emailed.IsEqual<True>>>()
					: c.FromBql<True.IsEqual<True>>());

			public Condition IsCCProcessed => GetOrCreate(c => c.FromBql<
				pendingProcessing.IsEqual<False>
			>());
			
			public Condition IsOpen => GetOrCreate(c => c.FromBql<
				openDoc.IsEqual<True>.And<released.IsEqual<True>>
			>());

			public Condition IsClosed => GetOrCreate(c => c.FromBql<
				openDoc.IsEqual<False>.And<released.IsEqual<True>>
			>());

			public Condition IsCreditMemo => GetOrCreate(c => c.FromBql<
				docType.IsEqual<ARDocType.creditMemo>
			>());

			public Condition IsNotSchedulable => GetOrCreate(c => c.FromBql<
				docType.IsNotIn<ARDocType.invoice, ARDocType.creditMemo, ARDocType.debitMemo>
			>());

			public Condition IsNotCreditMemo => GetOrCreate(c => c.FromBql<
				docType.IsNotEqual<ARDocType.creditMemo>
			>());

			public Condition IsFinCharge => GetOrCreate(c => c.FromBql<
				docType.IsEqual<ARDocType.finCharge>
			>());

			public Condition IsSmallCreditWO => GetOrCreate(c => c.FromBql<
				docType.IsEqual<ARDocType.smallCreditWO>
			>());
			
			public Condition IsNotAllowRecalcPrice => GetOrCreate(c => c.FromBql<
				pendingPPD.IsEqual<True>
				.Or<ARRegister.curyRetainageTotal.IsGreater<decimal0>
					.Or<isRetainageDocument.IsEqual<True>>>
			>());
			
			public Condition IsARInvoice => GetOrCreate(c => c.FromBql<
				origModule.IsNotEqual<GL.BatchModule.moduleSO>
			>());

			public Condition IsMigrationMode => GetOrCreate(c =>
				_Definition.MigrationMode == true
					? c.FromBql<True.IsEqual<True>>()
					: c.FromBql<True.IsEqual<False>>()
			);
		}

		protected virtual void Configure(WorkflowContext<ARInvoiceEntry, ARInvoice> context)
		{
			var conditions = context.Conditions.GetPack<Conditions>();
			
			#region Categories

			var processingCategory = context.Categories.CreateNew(CategoryID.Processing,
				category => category.DisplayName(CategoryNames.Processing));
			var approvalCategory = context.Categories.CreateNew(CategoryID.Approval,
				category => category.DisplayName(CategoryNames.Approval));
			var printingAndEmailingCategory = context.Categories.CreateNew(CategoryID.PrintingAndEmailing,
				category => category.DisplayName(CategoryNames.PrintingAndEmailing));
			var correctionsCategory = context.Categories.CreateNew(CategoryID.Corrections,
				category => category.DisplayName(CategoryNames.Corrections));
			var intercompanyCategory = context.Categories.CreateNew(CategoryID.Intercompany,
				category => category.DisplayName(CategoryNames.Intercompany));
			var relatedDocumentsCategory = context.Categories.CreateNew(CategoryID.RelatedDocuments,
				category => category.DisplayName(CategoryNames.RelatedDocuments));
			var customOtherCategory = context.Categories.CreateNew(CategoryID.Other,
				category => category.DisplayName(CategoryNames.Other));

			#endregion

			const string initialState = "_";
			var markDontEmail = context.ActionDefinitions.CreateNew(MarkAsDontEmail, a => a
				.DisplayName("Mark as Do not Email")
				.WithCategory(printingAndEmailingCategory, g => g.emailInvoice)
				.MassProcessingScreen<ARPrintInvoices>()
				.PlaceAfter(g => g.createSchedule)
				.IsDisabledWhen(conditions.IsEmailed)
				.WithFieldAssignments(fa => fa.Add<dontEmail>(e => e.SetFromValue(true))));

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
											sss.Add<State.creditHold>(flowState =>
											{
												return flowState
													.IsSkippedWhen(conditions.IsCreditHoldChecked)
													.WithActions(actions =>
													{
														actions.Add(g => g.releaseFromCreditHold,
															act => act.IsDuplicatedInToolbar());
														actions.Add(g => g.putOnHold);
													});
											});
											sss.Add<State.pendingPrint>(flowState =>
											{
												return flowState
													.IsSkippedWhen(conditions.IsPrinted)
													.WithActions(actions =>
													{
														actions.Add(g => g.putOnHold);
														actions.Add(g => g.printInvoice,
															act => act.IsDuplicatedInToolbar()
																.WithConnotation(ActionConnotation.Success));
														actions.Add(g => g.createSchedule);
														actions.Add(g => g.putOnCreditHold);
														actions.Add(g => g.emailInvoice);
													});
											});
											sss.Add<State.pendingEmail>(flowState =>
											{
												return flowState
													.IsSkippedWhen(conditions.IsEmailed)
													.WithActions(actions =>
													{
														actions.Add(g => g.putOnHold);
														actions.Add(g => g.createSchedule);
														actions.Add(g => g.emailInvoice,
															act => act.IsDuplicatedInToolbar()
																.WithConnotation(ActionConnotation.Success));
														actions.Add(g => g.putOnCreditHold);
													});
											});
											sss.Add<State.cCHold>(flowState =>
											{
												return flowState
													.IsSkippedWhen(conditions.IsCCProcessed)
													.WithActions(actions =>
													{
														actions.Add(a => a.putOnHold);
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
														actions.Add(g => g.putOnHold);
														actions.Add(g => g.putOnCreditHold);
														actions.Add(g => g.createSchedule);
														actions.Add(g => g.emailInvoice);
													});
											});
										})
										.WithActions(actions =>
										{
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.recalculateDiscountsAction);
											actions.Add(g => g.sendEmail);
											actions.Add(g => g.printAREdit);
											actions.Add(g => g.printInvoice);
											actions.Add(g => g.customerDocuments);
											actions.Add(markDontEmail);
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
											actions.Add(g => g.printAREdit);
											actions.Add(g => g.printInvoice);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.customerDocuments);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnConfirmSchedule);
											handlers.Add(g => g.OnVoidSchedule);
										});
								});
								fss.Add<State.open>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.payInvoice,
												a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
											actions.Add(g => g.emailInvoice);
											actions.Add(g => g.reverseInvoice);
											actions.Add(g => g.reverseInvoiceAndApplyToMemo);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.writeOff);
											actions.Add(g => g.reclassifyBatch);
											actions.Add(g => g.customerRefund);
											actions.Add(g => g.sendEmail);
											actions.Add(markDontEmail);
											actions.Add(g => g.printInvoice);
											actions.Add(g => g.printARRegister);
											actions.Add(g => g.customerDocuments);
											actions.Add(g => g.sOInvoice);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnCloseDocument);
											handlers.Add(g => g.OnCancelDocument);
										});
								});
								fss.Add<State.closed>(flowState =>
								{
									return flowState
										.WithActions(actions =>
										{
											actions.Add(g => g.emailInvoice);
											actions.Add(g => g.reverseInvoice);
											actions.Add(g => g.validateAddresses);
											actions.Add(g => g.reclassifyBatch);

											actions.Add(g => g.printInvoice);
											actions.Add(g => g.printARRegister);
											actions.Add(g => g.customerDocuments);
											actions.Add(g => g.sOInvoice);
											actions.Add(g => g.sendEmail);
										})
										.WithEventHandlers(handlers =>
										{
											handlers.Add(g => g.OnOpenDocument);
											handlers.Add(g => g.OnVoidDocument);
											handlers.Add(g => g.OnCancelDocument);
										});
								});
								fss.Add<State.canceled>(flowState =>
								{
									return flowState
										.WithActions(actions => { actions.Add(g => g.printInvoice); })
										.WithFieldStates(states => { states.AddTable<ARInvoice>(state => state.IsDisabled()); });
								});
								fss.Add<State.reserved>();
								fss.Add<State.voided>();
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
											.IsTriggeredOn(g => g.OnUpdateStatus)
											.When(conditions.IsARInvoice));
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
									transitions.AddGroupFrom<State.scheduled>(ts =>
									{
										ts.Add(t => t
											.To<State.voided>()
											.IsTriggeredOn(g => g.OnVoidSchedule)
											.WithFieldAssignments(fas =>
											{
												fas.Add<voided>(e => e.SetFromValue(true));
												fas.Add<scheduled>(e => e.SetFromValue(false));
												fas.Add<scheduleID>(e => e.SetFromValue(null));
											}));
										ts.Add(t => t.To<State.scheduled>()
											.IsTriggeredOn(g => g.OnConfirmSchedule)
											.WithFieldAssignments(fas =>
											{
												fas.Add<scheduled>(e => e.SetFromValue(true));
												fas.Add<scheduleID>(e => e.SetFromExpression("@ScheduleID"));
											}));
									});
									transitions.AddGroupFrom<State.open>(ts =>
									{
										ts.Add(t => t
											.To<State.closed>()
											.IsTriggeredOn(g => g.OnReleaseDocument)
											.When(conditions.IsClosed));
										ts.Add(t => t
											.To<State.closed>()
											.IsTriggeredOn(g => g.OnCloseDocument));
										ts.Add(t => t
											.To<State.canceled>()
											.IsTriggeredOn(g => g.OnCancelDocument));
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
											.To<State.canceled>()
											.IsTriggeredOn(g => g.OnCancelDocument));
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
						actions.Add(g => g.releaseFromCreditHold, c => c
							.WithCategory(approvalCategory)
							.IsHiddenWhen(conditions.IsCreditMemo)
							.WithFieldAssignments(fass => { fass.Add<creditHold>(v => v.SetFromValue(false)); }));
						actions.Add(g => g.putOnCreditHold, c => c
							.WithCategory(approvalCategory, g => g.releaseFromCreditHold)
							.IsHiddenWhen(conditions.IsCreditMemo)
							.WithFieldAssignments(fass =>
							{
								fass.Add<creditHold>(v => v.SetFromValue(true));
								fass.Add<approvedCredit>(v => v.SetFromValue(false));
								fass.Add<approvedCreditAmt>(v => v.SetFromValue(0));
							}));
						actions.Add(g => g.printInvoice, c => c
							.WithCategory(printingAndEmailingCategory)
							.MassProcessingScreen<ARPrintInvoices>().InBatchMode()
							.WithFieldAssignments(fa => fa.Add<printed>(e => e.SetFromValue(true))));
						actions.Add(g => g.emailInvoice, c => c
							.WithCategory(printingAndEmailingCategory, g => g.printInvoice)
							.MassProcessingScreen<ARPrintInvoices>()
							.WithFieldAssignments(fa => fa.Add<emailed>(e => e.SetFromValue(true))));
						actions.Add(g => g.release, c => c
							.WithCategory(processingCategory));
						actions.Add(g => g.payInvoice, c => c
							.WithCategory(processingCategory)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.reverseInvoice, c => c
							.WithCategory(correctionsCategory)
							.IsHiddenWhen(conditions.IsFinCharge || conditions.IsSmallCreditWO));
						actions.Add(g => g.reverseInvoiceAndApplyToMemo, c => c
							.WithCategory(correctionsCategory)
							.IsHiddenWhen(conditions.IsFinCharge));
						actions.Add(g => g.customerRefund, c => c
							.WithCategory(processingCategory)
							.IsHiddenWhen(conditions.IsNotCreditMemo));
						actions.Add(g => g.writeOff, c => c
							.WithCategory(correctionsCategory)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.createSchedule, c => c
							.WithCategory(customOtherCategory)
							.IsHiddenWhen(conditions.IsNotSchedulable || conditions.IsMigrationMode));
						actions.Add(markDontEmail);
						actions.Add(g => g.recalculateDiscountsAction, c => c
							.WithCategory(customOtherCategory)
							.IsHiddenWhen(conditions.IsMigrationMode)
							.IsDisabledWhen(conditions.IsNotAllowRecalcPrice));
						actions.Add(g => g.reclassifyBatch, c => c
							.WithCategory(correctionsCategory)
							.IsHiddenWhen(conditions.IsMigrationMode));
						actions.Add(g => g.validateAddresses, c => c
							.WithCategory(customOtherCategory));
						actions.Add(g => g.sendEmail, c => c
							.WithCategory(customOtherCategory));

						actions.Add(g => g.customerDocuments, c => c.WithCategory(PredefinedCategory.Inquiries));
						actions.Add(g => g.sOInvoice,
							c => c.WithCategory(relatedDocumentsCategory)
								.IsHiddenWhen(conditions.IsFinCharge || conditions.IsSmallCreditWO || conditions.IsARInvoice));
						actions.Add(g => g.printAREdit, c => c.WithCategory(PredefinedCategory.Reports));
						actions.Add(g => g.printARRegister, c => c.WithCategory(PredefinedCategory.Reports));
					})
					.WithHandlers(handlers =>
					{
						handlers.Add(handler => handler
							.WithTargetOf<ARRegister>()
							.WithParametersOf<GL.Schedule>()
							.OfEntityEvent<ARRegister.Events>(e => e.ConfirmSchedule)
							.Is(g => g.OnConfirmSchedule)
							.UsesPrimaryEntityGetter<
								SelectFrom<ARInvoice>.
								Where<ARInvoice.docType.IsEqual<ARRegister.docType.FromCurrent>.
									And<ARInvoice.refNbr.IsEqual<ARRegister.refNbr.FromCurrent>>>
							>());
						handlers.Add(handler => handler
							.WithTargetOf<ARRegister>()
							.WithParametersOf<GL.Schedule>()
							.OfEntityEvent<ARRegister.Events>(e => e.VoidSchedule)
							.Is(g => g.OnVoidSchedule)
							.UsesPrimaryEntityGetter<
								SelectFrom<ARInvoice>.
								Where<ARInvoice.docType.IsEqual<ARRegister.docType.FromCurrent>.
									And<ARInvoice.refNbr.IsEqual<ARRegister.refNbr.FromCurrent>>>
							>());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.ReleaseDocument)
							.Is(g => g.OnReleaseDocument)
							.UsesTargetAsPrimaryEntity()
							.WithUpcastTo<ARRegister>());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.OpenDocument)
							.Is(g => g.OnOpenDocument)
							.UsesTargetAsPrimaryEntity()
							.WithUpcastTo<ARRegister>());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.CloseDocument)
							.Is(g => g.OnCloseDocument)
							.UsesTargetAsPrimaryEntity()
							.WithUpcastTo<ARRegister>());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.CancelDocument)
							.Is(g => g.OnCancelDocument)
							.UsesTargetAsPrimaryEntity());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfEntityEvent<ARInvoice.Events>(e => e.VoidDocument)
							.Is(g => g.OnVoidDocument)
							.UsesTargetAsPrimaryEntity()
							.WithUpcastTo<ARRegister>());
						handlers.Add(handler => handler
							.WithTargetOf<ARInvoice>()
							.OfFieldsUpdated<OnUpdateStatusFields>()
							.Is(g => g.OnUpdateStatus)
							.UsesTargetAsPrimaryEntity());
					})
					.WithCategories(categories =>
					{
						categories.Add(processingCategory);
						categories.Add(correctionsCategory);
						categories.Add(intercompanyCategory);
						categories.Add(approvalCategory);
						categories.Add(printingAndEmailingCategory);
						categories.Add(customOtherCategory);
						categories.Add(relatedDocumentsCategory);
						categories.Update(FolderType.InquiriesFolder,
							category => category.PlaceAfter(relatedDocumentsCategory));
						categories.Update(FolderType.ReportsFolder,
							category => category.PlaceAfter(FolderType.InquiriesFolder));
					})
			);
		}

		public class OnUpdateStatusFields : TypeArrayOf<IBqlField>
			.FilledWith<ARInvoice.hold, ARInvoice.creditHold, ARInvoice.printed,
				ARInvoice.dontPrint, ARInvoice.emailed, ARInvoice.dontEmail,
				ARInvoice.pendingProcessing>
		{
			
		}

		public static class CategoryNames
		{
			public const string Processing = "Processing";
			public const string Approval = "Approval";
			public const string PrintingAndEmailing = "Printing and Emailing";
			public const string Corrections = "Corrections";
			public const string Intercompany = "Intercompany";
			public const string Other = "Other";
			public const string RelatedDocuments = "Related Documents";
			public const string Inquiries = "Inquiries";
			public const string Reports = "Reports";
		}

		public static class CategoryID
		{
			public const string Processing = "ProcessingID";
			public const string Approval = "ApprovalID";
			public const string PrintingAndEmailing = "PrintingAndEmailingID";
			public const string Corrections = "CorrectionsID";
			public const string Intercompany = "IntercompanyID";
			public const string Other = "OtherID";
			public const string RelatedDocuments = "Related DocumentsID";
			public const string Inquiries = "InquiriesID";
			public const string Reports = "ReportsID";
		}
	}
}