using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.MassProcess;
using PX.Objects.Common;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Api;
using FieldValue = PX.Data.MassProcess.FieldValue;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Common.Mail;
using System.Reflection;
using PX.Web.UI;
using PX.Objects.CR.Extensions.Cache;
using PX.Objects.CR.Extensions.SideBySideComparison.Merge;
using PX.Objects.CR.Extensions.SideBySideComparison;

namespace PX.Objects.CR.Extensions.CRDuplicateEntities
{
	/// <summary>
	/// Extension that is used for deduplication purposes. Extension uses CRGrams mechanizm. Works with BAccount and Contact entities. 
	/// </summary>
	public abstract class CRDuplicateEntities<TGraph, TMain> : PXGraphExtension<TGraph>
		where TGraph : PXGraph, new()
		where TMain : class, IBqlTable, INotable, new()
	{
		#region Views

		#region DocumentMapping Mapping
		protected class DocumentMapping : IBqlMapping
		{
			public Type Extension => typeof(Document);
			protected Type _table;
			public Type Table => _table;

			public DocumentMapping(Type table)
			{
				_table = table;
			}
			public Type Key = typeof(Document.key);
		}
		protected abstract DocumentMapping GetDocumentMapping();
		#endregion

		#region DuplicateDocumentMapping Mapping
		protected class DuplicateDocumentMapping : IBqlMapping
		{
			public Type Extension => typeof(DuplicateDocument);
			protected Type _table;
			public Type Table => _table;

			public DuplicateDocumentMapping(Type table)
			{
				_table = table;
			}
			public Type ContactID = typeof(DuplicateDocument.contactID);
			public Type RefContactID = typeof(DuplicateDocument.refContactID);
			public Type BAccountID = typeof(DuplicateDocument.bAccountID);
			public Type ContactType = typeof(DuplicateDocument.contactType);
			public Type DuplicateStatus = typeof(DuplicateDocument.duplicateStatus);
			public Type DuplicateFound = typeof(DuplicateDocument.duplicateFound);
			public Type Email = typeof(DuplicateDocument.email);
			public Type IsActive = typeof(DuplicateDocument.isActive);
		}
		protected abstract DuplicateDocumentMapping GetDuplicateDocumentMapping();
		#endregion

		[PXHidden]
		public PXSelectExtension<Document> Documents;

		[PXHidden]
		public PXSelectExtension<DuplicateDocument> DuplicateDocuments;

		[PXHidden]
		[PXCopyPasteHiddenView]
		public IN.PXSetupOptional<CRSetup> Setup;

		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXSelect<FieldValue, Where<FieldValue.attributeID, IsNull>, OrderBy<Asc<FieldValue.order>>> PopupConflicts;
		public IEnumerable popupConflicts()
		{
			return Base.Caches[typeof(FieldValue)].Cached.Cast<FieldValue>().Where(fld => fld.Hidden != true);
		}

		protected PXView dbView;

		//dummy, see delegate
		[PXOverride]
		public SelectFrom<
				CRDuplicateRecord>
			.LeftJoin<Contact2>
				.On<True.IsEqual<False>>
			.LeftJoin<DuplicateContact>
				.On<True.IsEqual<False>>
			.LeftJoin<BAccountR>
				.On<True.IsEqual<False>>
			.LeftJoin<CRLead>
				.On<True.IsEqual<False>>
			.LeftJoin<Address>
				.On<True.IsEqual<False>>
			.LeftJoin<CRActivityStatistics>
				.On<True.IsEqual<False>>
			.OrderBy<
				Asc<DuplicateContact.contactPriority>,
				Asc<DuplicateContact.contactID>>
			.View Duplicates;
		/// <summary>
		/// The delegate that fetches the duplicates for the current record, if there are some possible duplicates found already.
		/// </summary>
		/// <param name="forceSelect">Skip "if there are some possible duplicates found already" check on duplicates ckecking</param>
		/// <returns></returns>
		public virtual IEnumerable duplicates(bool? forceSelect = null)
		{
			var currentSetup = Base.Caches[typeof(CRSetup)].Current as CRSetup;
			if (currentSetup == null)
				yield break;

			var entity = DuplicateDocuments.Current ?? (DuplicateDocuments.Current = DuplicateDocuments.SelectSingle());

			if (entity == null)
				yield break;

			bool selectDuplicates = forceSelect == true || entity.DuplicateFound == true;
			if (!selectDuplicates)
				yield break;

			List<object> possibleDuplicates = null;

			if (DuplicateDocuments.Cache.GetStatus(entity) == PXEntryStatus.Inserted && !PXTransactionScope.IsScoped)
			{
				using (new PXTransactionScope())
				{
					// grams are needed to fetch the data from the db,
					// must insert them and "delete" after select
					PersistGrams(entity);

					possibleDuplicates = dbView.SelectMulti(true);

					// do not commit transaction
				}
			}
			else
			{
				possibleDuplicates = dbView.SelectMulti(true);
			}

			foreach (PXResult rec in possibleDuplicates)
			{
				CRGrams gram = rec.GetItem<CRGrams>();
				CRDuplicateGrams duplicateGram = rec.GetItem<CRDuplicateGrams>();
				DuplicateContact duplicateContact = rec.GetItem<DuplicateContact>();
				CRLead duplicateLead = rec.GetItem<CRLead>();

				var dupRecord = new CRDuplicateRecord()
				{
					ContactID = gram.EntityID,
					ValidationType = gram.ValidationType,
					DuplicateContactID = duplicateGram.EntityID,
					Score = gram.Score,
					DuplicateContactType = duplicateContact?.ContactType,
					DuplicateBAccountID = duplicateContact?.BAccountID,
					DuplicateRefContactID = duplicateLead?.RefContactID,
					Phone1 = duplicateContact?.Phone1
				};

				Address address = duplicateContact.ContactType switch
				{
					ContactTypesAttribute.Person => rec.GetItem<Address>(),
					ContactTypesAttribute.Lead => rec.GetItem<Address>(),
					ContactTypesAttribute.BAccountProperty => rec.GetItem<Address2>()
				};

				var activityStat = dupRecord.DuplicateContactType switch
				{
					ContactTypesAttribute.Person => rec.GetItem<CRActivityStatistics>(),
					ContactTypesAttribute.Lead => rec.GetItem<CRLeadActivityStatistics>(),
					ContactTypesAttribute.BAccountProperty => rec.GetItem<CRBAccountActivityStatistics>()
				};

				yield return new CRDuplicateResult(
					dupRecord,
					rec.GetItem<Contact2>(),
					duplicateContact,
					rec.GetItem<BAccountR>(),
					rec.GetItem<CRLead>(),
					address,
					activityStat
					);
			}
		}

		//dummy, see delegate
		[PXHidden]
		[PXCopyPasteHiddenView]
		public SelectFrom<
				CRDuplicateRecord>
			.LeftJoin<Contact>
				.On<True.IsEqual<False>>
			.LeftJoin<DuplicateContact>
				.On<True.IsEqual<False>>
			.LeftJoin<BAccountR>
				.On<True.IsEqual<False>>
			.LeftJoin<CRLead>
				.On<True.IsEqual<False>>
			.LeftJoin<Address>
				.On<True.IsEqual<False>>
			.LeftJoin<CRActivityStatistics>
				.On<True.IsEqual<False>>
			.OrderBy<
				Asc<DuplicateContact.contactPriority>,
				Asc<DuplicateContact.contactID>>
			.View DuplicatesForMerging;
		protected virtual IEnumerable duplicatesForMerging()
		{
			foreach (CRDuplicateResult rec in this.Duplicates
				.Select()
				.ToList()
				.Cast<CRDuplicateResult>()
				.Where(WhereMergingMet))
			{
				rec.GetItem<CRDuplicateRecord>().CanBeMerged = CanBeMerged(rec);
				yield return rec;
			}
		}

		//dummy, see delegate
		[PXHidden]
		[PXCopyPasteHiddenView]
		public SelectFrom<
				CRDuplicateRecordForLinking>
			.LeftJoin<Contact>
				.On<True.IsEqual<False>>
			.LeftJoin<DuplicateContact>
				.On<True.IsEqual<False>>
			.LeftJoin<BAccountR>
				.On<True.IsEqual<False>>
			.LeftJoin<CRLead>
				.On<True.IsEqual<False>>
			.LeftJoin<Address>
				.On<True.IsEqual<False>>
			.LeftJoin<CRActivityStatistics>
				.On<True.IsEqual<False>>
			.OrderBy<
				Asc<DuplicateContact.contactPriority>,
				Asc<DuplicateContact.contactID>>
			.View DuplicatesForLinking;
		protected virtual IEnumerable duplicatesForLinking()
		{
			foreach (CRDuplicateResult rec in this.Duplicates
				.Select()
				.ToList()
				.Cast<CRDuplicateResult>()
				.Where(WhereLinkingMet))
			{
				var dupRec = rec.GetItem<CRDuplicateRecord>();

				var dupRecForLinking = new CRDuplicateRecordForLinking()
				{
					ContactID = dupRec.ContactID,
					ValidationType = dupRec.ValidationType,
					DuplicateContactID = dupRec.DuplicateContactID,
					DuplicateRefContactID = dupRec.DuplicateRefContactID,
					DuplicateBAccountID = dupRec.DuplicateBAccountID,
					Score = dupRec.Score,
					DuplicateContactType = dupRec.DuplicateContactType
				};

				yield return new CRDuplicateResult(
					dupRecForLinking,
					rec.GetItem<Contact>(),
					rec.GetItem<DuplicateContact>(),
					rec.GetItem<BAccountR>(),
					rec.GetItem<CRLead>(),
					rec.GetItem<Address>(),
					rec.GetItem<CRActivityStatistics>());
			}
		}

		#endregion

		#region ctor

		#region BQL

		public virtual Type MatchingConditions => typeof(
			CRGrams.validationType.IsEqual<

				ValidationTypesAttribute.leadToLead
				.When<Brackets<
						Contact2.contactType.IsNotNull
							.And<Contact2.contactType.IsEqual<ContactTypesAttribute.lead>>
						.Or<DuplicateDocument.contactType.FromCurrent.IsEqual<ContactTypesAttribute.lead>>
					>
					.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.lead>>>

				.Else<ValidationTypesAttribute.leadToContact>
				.When<Brackets<
						Contact2.contactType.IsNotNull
							.And<Contact2.contactType.IsEqual<ContactTypesAttribute.lead>>
						.Or<DuplicateDocument.contactType.FromCurrent.IsEqual<ContactTypesAttribute.lead>>
					>
					.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.person>>>

				.Else<ValidationTypesAttribute.contactToContact>
				.When<Brackets<
						Contact2.contactType.IsNotNull
							.And<Contact2.contactType.IsEqual<ContactTypesAttribute.person>>
						.Or<DuplicateDocument.contactType.FromCurrent.IsEqual<ContactTypesAttribute.person>>
					>
					.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.person>>>

				.Else<ValidationTypesAttribute.contactToLead>
				.When<Brackets<
						Contact2.contactType.IsNotNull
							.And<Contact2.contactType.IsEqual<ContactTypesAttribute.person>>
						.Or<DuplicateDocument.contactType.FromCurrent.IsEqual<ContactTypesAttribute.person>>
					>
					.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.lead>>>

				.Else<ValidationTypesAttribute.leadToAccount>
				.When<Brackets<
						Contact2.contactType.IsNotNull
							.And<Contact2.contactType.IsEqual<ContactTypesAttribute.lead>>
						.Or<DuplicateDocument.contactType.FromCurrent.IsEqual<ContactTypesAttribute.lead>>
					>
					.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>>>

				.Else<ValidationTypesAttribute.contactToAccount>
				.When<Brackets<
						Contact2.contactType.IsNotNull
							.And<Contact2.contactType.IsEqual<ContactTypesAttribute.person>>
						.Or<DuplicateDocument.contactType.FromCurrent.IsEqual<ContactTypesAttribute.person>>
					>
					.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>>>

				.Else<ValidationTypesAttribute.accountToAccount>
				.When<Brackets<
						Contact2.contactType.IsNotNull
							.And<Contact2.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>>
						.Or<DuplicateDocument.contactType.FromCurrent.IsEqual<ContactTypesAttribute.bAccountProperty>>
					>
					.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>>>

				.Else<Empty>>);

		public virtual Type AdditionalConditions => typeof(True.IsEqual<True>);

		#endregion

		public virtual string WarningMessage => "";

		public virtual bool HardBlockOnly { get; set; }

		public virtual CRGramProcessor Processor { get; set; }

		protected static bool IsExtensionActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.contactDuplicate>();
		}

		public override void Initialize()
		{
			base.Initialize();

			Processor = new CRGramProcessor(Base);

			PXDBAttributeAttribute.Activate(Base.Caches[typeof(TMain)]);

			Base.EnsureCachePersistence(typeof(CRActivityStatistics));

			BqlCommand bqlCommand = BqlTemplate.OfCommand<
					SelectFrom<
						CRGrams>
					.InnerJoin<CRDuplicateGrams>
						.On<CRDuplicateGrams.validationType.IsEqual<CRGrams.validationType>
						.And<CRDuplicateGrams.fieldName.IsEqual<CRGrams.fieldName>>
						.And<CRDuplicateGrams.fieldValue.IsEqual<CRGrams.fieldValue>>
						.And<CRDuplicateGrams.entityID.IsNotEqual<CRGrams.entityID>>>
					.LeftJoin<Contact2>
						.On<Contact2.contactID.IsEqual<CRGrams.entityID>>
					.InnerJoin<DuplicateContact>
						.On<DuplicateContact.contactID.IsEqual<CRDuplicateGrams.entityID>>
					.LeftJoin<BAccountR>
						.On<BAccountR.bAccountID.IsEqual<DuplicateContact.bAccountID>>
					.LeftJoin<CRLead>
						.On<CRLead.contactID.IsEqual<CRDuplicateGrams.entityID>>
					.LeftJoin<CRValidation>
						.On<CRValidation.type.IsEqual<CRGrams.validationType>>
					.LeftJoin<Address>
						.On<DuplicateContact.contactType.IsIn<ContactTypesAttribute.person, ContactTypesAttribute.lead>
							.And<Address.addressID.IsEqual<DuplicateContact.defAddressID>>>
					.LeftJoin<Address2>
						.On<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>
							.And<Address2.addressID.IsEqual<BAccountR.defAddressID>>>
					.LeftJoin<CRActivityStatistics>
						.On<CRActivityStatistics.noteID.IsEqual<DuplicateContact.noteID>>
					.LeftJoin<CRLeadActivityStatistics>
						.On<CRLeadActivityStatistics.noteID.IsEqual<CRLead.noteID>>
					.LeftJoin<CRBAccountActivityStatistics>
						.On<CRBAccountActivityStatistics.noteID.IsEqual<BAccountR.noteID>>
					.Where<
						CRGrams.entityID.IsEqual<DuplicateDocument.contactID.FromCurrent>
						.And<True.IsEqual<@P.AsBool>>
						.And<Brackets<BqlPlaceholder.M>>
						.And<DuplicateContact.isActive.IsEqual<True>>
						.And<Brackets<
							DuplicateContact.contactType.IsNotEqual<ContactTypesAttribute.bAccountProperty>
								.Or<DuplicateContact.contactID.IsEqual<BAccountR.defContactID>>>>
						.And<Brackets<BqlPlaceholder.W>>
					>
					.AggregateTo<
						GroupBy<CRGrams.entityID>,
						GroupBy<CRGrams.validationType>,
						GroupBy<CRDuplicateGrams.entityID>,
						GroupBy<DuplicateContact.contactType>,

						Sum<CRGrams.score>,
						Max<CRValidation.validationThreshold>
					>
					.Having<
						CRGrams.score.Summarized.IsGreaterEqual<CRValidation.validationThreshold.Maximized>
					>
					>
					.Replace<BqlPlaceholder.M>(this.MatchingConditions)
					.Replace<BqlPlaceholder.W>(this.AdditionalConditions)
					.ToCommand();

			dbView = new PXView(Base, false, bqlCommand);

			GenerateUDFColumns<CRDuplicateRecord>();
			GenerateUDFColumns<CRDuplicateRecordForLinking>();
		}

		public virtual void GenerateUDFColumns<T>()
			where T : CRDuplicateRecord
		{
			if (Base.Accessinfo.ScreenID == null)
				return;

			var screenId = Base.Accessinfo.ScreenID.Replace(".", "");
			var udfFields = PX.CS.KeyValueHelper.GetAttributeFields(screenId)
				.OrderBy(t => t.Item3)
				.ThenBy(t => t.Item2);

			foreach (var (fieldState, row, col, defaultValue) in udfFields)
			{
				if (!Base.Caches[typeof(T)].Fields.Contains(fieldState.Name))
				{
					Base.Caches[typeof(T)].Fields.Add(fieldState.Name);

					Base.FieldSelecting.AddHandler(typeof(T), fieldState.Name, (PXCache sender, PXFieldSelectingEventArgs args) =>
					{
						T rec = args.Row as T;

						if (rec == null)
						{
							args.ReturnState = fieldState;
						}
						else
						{
							var result = Duplicates
								.Select()
								.ToList()
								.FirstOrDefault(c => c.GetItem<DuplicateContact>()?.ContactID == rec.DuplicateContactID);

							var dupContact = result?.GetItem<DuplicateContact>();

							if (dupContact == null)
								return;

							switch (dupContact.ContactType)
							{
								case ContactTypesAttribute.BAccountProperty:
									var dupAccount = result.GetItem<BAccount>();

									args.ReturnValue = Base.Caches[typeof(BAccount)].GetValueExt(dupAccount, fieldState.Name);
									break;

								case ContactTypesAttribute.Lead:
									var dupLead = result.GetItem<CRLead>();

									args.ReturnValue = Base.Caches[typeof(CRLead)].GetValueExt(dupLead, fieldState.Name);
									break;

								case ContactTypesAttribute.Person:
								default:
									args.ReturnValue = Base.Caches[typeof(Contact)].GetValueExt(dupContact, fieldState.Name);
									break;
							}
						}

						if (!(args.ReturnState is PXFieldState))
							return;

						PXFieldState fs = args.ReturnState as PXFieldState;
						fs.SetFieldName(fieldState.Name);
						fs.Visible = false;
						fs.Visibility = PXUIVisibility.Dynamic;
						fs.DisplayName = fieldState.DisplayName;
						fs.Enabled = false;
					});
				}
			}
		}

		#endregion

		#region Actions

		public PXAction<TMain> CheckForDuplicates;

		[PXUIField(DisplayName = Messages.CheckForDuplicates, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Update)]
		[PXButton(DisplayOnMainToolbar = false)]
		public virtual IEnumerable checkForDuplicates(PXAdapter adapter)
		{
			DuplicateDocument duplicateDocument = DuplicateDocuments.Current ?? DuplicateDocuments.SelectSingle();

			if (duplicateDocument == null)
				return adapter.Get();

			var prevStatus = duplicateDocument.DuplicateStatus;

			if (CheckIsActive())
			{
				CheckIfAnyDuplicates(duplicateDocument, withUpdate: true);
			}

			if (duplicateDocument.DuplicateStatus != prevStatus)
			{
				DuplicateDocuments.Cache.MarkUpdated(duplicateDocument);

				Base.Actions.PressSave();
			}

			if (duplicateDocument.DuplicateStatus == DuplicateStatusAttribute.PossibleDuplicated || duplicateDocument.DuplicateFound == true)
			{
				DuplicateDocuments.Cache.RaiseExceptionHandling<DuplicateDocument.duplicateStatus>(duplicateDocument,
					duplicateDocument.DuplicateStatus,
					new PXSetPropertyException(WarningMessage, PXErrorLevel.Warning));
			}
			else
			{
				DuplicateDocuments.Cache.RaiseExceptionHandling<DuplicateDocument.duplicateStatus>(duplicateDocument,
					duplicateDocument.DuplicateStatus,
					null);
			}

			return adapter.Get();
		}

		public PXAction<TMain> DuplicateMerge;
		[PXUIField(DisplayName = Messages.Merge, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Update)]
		[PXButton(DisplayOnMainToolbar = false)]
		public virtual IEnumerable duplicateMerge(PXAdapter adapter)
		{
			var ext = Base.GetProcessingExtension<MergeEntitiesExt<TGraph, TMain>>();

			if (!ext.AskMerge(Duplicates.Current.DuplicateContactID).IsPositive())
				return adapter.Get();

			Base.Actions.PressSave();
			var graph = Base.CloneGraphState();
			var mergeExt = graph.GetProcessingExtension<MergeEntitiesExt<TGraph, TMain>>();
			var thisExt = graph.GetProcessingExtension<CRDuplicateEntities<TGraph, TMain>>();

			PXLongOperation.StartOperation(Base, () =>
			{
				var result = mergeExt.ProcessMerge();

				graph.Actions.PressSave();

				var targetGraph = thisExt.MergePostProcessing(result.Target, result.Duplicate);

				if (!graph.IsContractBasedAPI)
					PXRedirectHelper.TryRedirect(targetGraph, PXRedirectHelper.WindowMode.Same);
			});

			return adapter.Get();
		}

		public PXAction<TMain> DuplicateAttach;
		[PXUIField(DisplayName = Messages.Associate, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Update)]
		[PXButton(DisplayOnMainToolbar = false)]
		public virtual void duplicateAttach()
		{
			DuplicateDocument duplicateDocument = DuplicateDocuments.Current ?? DuplicateDocuments.SelectSingle();
			if (duplicateDocument == null)
				return;

			DoDuplicateAttach(duplicateDocument);

			if (Base.IsContractBasedAPI)
				Base.Actions.PressSave();
		}

		public PXAction<TMain> ViewMergingDuplicate;
		[PXUIField(DisplayName = Messages.View, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewMergingDuplicate()
		{
			ViewDuplicate(DuplicatesForMerging.Current);
		}

		public PXAction<TMain> ViewLinkingDuplicate;
		[PXUIField(DisplayName = Messages.View, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewLinkingDuplicate()
		{
			ViewDuplicate(DuplicatesForLinking.Current);
		}

		public PXAction<TMain> ViewLinkingDuplicateRefContact;
		[PXUIField(DisplayName = Messages.View, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewLinkingDuplicateRefContact()
		{
			ViewDuplicateRefContact(DuplicatesForLinking.Current);
		}

		public PXAction<TMain> ViewMergingDuplicateBAccount;
		[PXUIField(DisplayName = Messages.View, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewMergingDuplicateBAccount()
		{
			ViewDuplicateBAccount(DuplicatesForMerging.Current);
		}

		public PXAction<TMain> ViewLinkingDuplicateBAccount;
		[PXUIField(DisplayName = Messages.View, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewLinkingDuplicateBAccount()
		{
			ViewDuplicateBAccount(DuplicatesForLinking.Current);
		}

		public PXAction<TMain> MarkAsValidated;
		[PXUIField(DisplayName = Messages.MarkAsValidated, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Update)]
		[PXButton(DisplayOnMainToolbar = false)]
		public virtual IEnumerable markAsValidated(PXAdapter adapter)
		{
			DuplicateDocument duplicateDocument = DuplicateDocuments.Current ?? DuplicateDocuments.SelectSingle();
			if (duplicateDocument == null)
				return adapter.Get();

			duplicateDocument.DuplicateStatus = DuplicateStatusAttribute.Validated;
			duplicateDocument.DuplicateFound = false;

			DuplicateDocuments.Update(duplicateDocument);

			Base.Actions.PressSave();

			return adapter.Get();
		}

		public PXAction<TMain> CloseAsDuplicate;
		[PXUIField(DisplayName = Messages.CloseAsDuplicate, MapViewRights = PXCacheRights.Select, MapEnableRights = PXCacheRights.Update)]
		[PXButton(DisplayOnMainToolbar = false)]
		public virtual IEnumerable closeAsDuplicate(PXAdapter adapter)
		{
			DuplicateDocument duplicateDocument = DuplicateDocuments.Current ?? DuplicateDocuments.SelectSingle();
			if (duplicateDocument == null)
				return adapter.Get();

			duplicateDocument.DuplicateStatus = DuplicateStatusAttribute.Duplicated;
			duplicateDocument.IsActive = false;

			DuplicateDocuments.Update(duplicateDocument);
			
			Base.Actions.PressSave();

			return adapter.Get();
		}

		#endregion

		#region Events

		[PXUIField(DisplayName = Messages.BAccountType, Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<BAccountR.type> e) { }

		protected virtual void _(Events.RowSelected<DuplicateDocument> e)
		{
			if (e.Row == null) return;
			bool persisted = e.Cache.GetOriginal(e.Row) != null;

			MarkAsValidated.SetEnabled(persisted && e.Row.IsActive == true && e.Row.DuplicateStatus != DuplicateStatusAttribute.Validated);
			CloseAsDuplicate.SetEnabled(persisted && e.Row.IsActive == true && e.Row.DuplicateStatus != DuplicateStatusAttribute.Duplicated);
			DuplicateMerge.SetEnabled(persisted);
		}

		protected virtual void _(Events.RowSelected<CRDuplicateRecord> e)
		{
			e.Cache.IsDirty = false;
			if (e.Row == null) return;
			PXUIFieldAttribute.SetReadOnly<CRDuplicateRecord.duplicateRefContactID>(e.Cache, e.Row);
		}

		protected virtual void _(Events.RowPersisting<CRDuplicateRecord> e)
		{
			e.Cancel = true;
		}

		protected virtual void _(Events.RowPersisting<FieldValue> e)
		{
			e.Cancel = true;
		}

		protected virtual void _(Events.RowPersisted<DuplicateDocument> e)
		{
			DuplicateDocument row = e.Row as DuplicateDocument;
			if (row == null || e.TranStatus != PXTranStatus.Open)
				return;

			// real grams persisting
			// Acuminator disable once PX1073 ExceptionsInRowPersisted [try catch inside]
			if (PersistGrams(row))
			{
				DuplicateDocuments.Cache.SetValue<DuplicateDocument.duplicateStatus>(row, row.DuplicateStatus);
				DuplicateDocuments.Cache.SetValue<DuplicateDocument.grammValidationDateTime>(row, row.GrammValidationDateTime);

				if (e.Operation == PXDBOperation.Insert)
				{
					// Acuminator disable once PX1073 ExceptionsInRowPersisted [Grams are already in DB, so this should be the optimal place to check for dups of non-existing record]
					CheckBlockingOnEntry(row);
				}
			}
		}

		[PXOverride]
		public virtual void Persist(Action del)
		{
			if (this.Documents.View.Answer == WebDialogResult.No)
			{
				if (Wizard.WizardScope.IsScoped)
				{
					// ask on next "turn"
					this.Documents.View.Answer = WebDialogResult.None;
				}

				// don't save
				return;
			}

			del();

			OnAfterPersist();
		}

		#endregion

		#region Implementation

		public virtual PXGraph MergePostProcessing(TMain target, TMain duplicate)
		{
			var entityHelper = new EntityHelper(Base);

			object duplicateEntity = duplicate;
			var duplicateGraphType = entityHelper.GetPrimaryGraphType(ref duplicateEntity, checkRights: false);
			var duplicateGraph = PXGraph.CreateInstance(duplicateGraphType);

			RunActionWithAppliedSearch(duplicateGraph, duplicateEntity, nameof(CloseAsDuplicate));

			duplicateGraph.Actions.PressSave();

			object targetEntity = target;
			var targetGraphType = entityHelper.GetPrimaryGraphType(ref targetEntity, checkRights: false);
			var targetGraph = PXGraph.CreateInstance(targetGraphType);

			Base.Actions.PressCancel();
			RunActionWithAppliedSearch(Base, targetEntity, nameof(CheckForDuplicates));
			RunActionWithAppliedSearch(targetGraph, targetEntity, "Cancel");

			return targetGraph;
		}

		public abstract TMain GetTargetEntity(int targetID);
		public abstract Contact GetTargetContact(TMain targetEntity);
		public abstract Address GetTargetAddress(TMain targetEntity);

		public abstract void DoDuplicateAttach(DuplicateDocument duplicateDocument);

		public virtual void ValidateEntitiesBeforeMerge(List<TMain> duplicateEntities) { }

		protected abstract bool WhereMergingMet(CRDuplicateResult result);

		protected virtual bool WhereLinkingMet(CRDuplicateResult result)
		{
			return !WhereMergingMet(result);
		}

		protected abstract bool CanBeMerged(CRDuplicateResult result);

		public virtual bool CheckIsActive()
		{
			DuplicateDocument duplicateDocument = DuplicateDocuments.Current ?? DuplicateDocuments.SelectSingle();

			if (duplicateDocument == null)
				return false;

			return duplicateDocument.IsActive == true;
		}

		public virtual void CheckBlockingOnEntry(DuplicateDocument duplicateDocument)
		{
			if (duplicateDocument == null)
				return;

			// need for dbView.Select, as it is based on DuplicateDocuments.Current
			if (DuplicateDocuments.Current == null)
				DuplicateDocuments.Current = duplicateDocument;

			if (Base.IsImport || Base.IsContractBasedAPI)
			{
				this.Documents.View.Answer = WebDialogResult.None;
			}

			if (this.Documents.View.Answer == WebDialogResult.None
				&& DuplicateDocuments.Cache.GetStatus(duplicateDocument).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted)
				&& Processor.IsValidationOnEntryActive(duplicateDocument.ContactType)
				&& CheckIsActive()
				&& Processor.IsAnyBlockingRulesConfigured(duplicateDocument.ContactType)
				&& !Processor.GrammSourceUpdated(duplicateDocument))
			{
				(bool anyFound, var duplicates) = CheckIfAnyDuplicates(duplicateDocument);

				if (anyFound)
				{
					var blocked = Processor.CheckIsBlocked(duplicateDocument, duplicates)?.ToList();

					if (blocked != null)
					{
						DuplicateDocuments.Cache.SetValue<DuplicateDocument.duplicateFound>(duplicateDocument, true);
						DuplicateDocuments.Cache.SetValue<DuplicateDocument.duplicateStatus>(duplicateDocument, DuplicateStatusAttribute.PossibleDuplicated);

						if (blocked.Any(_ => _.IsBlocked && _.BlockType == CreateOnEntryAttribute.Block))
						{
							PXUIFieldAttribute.SetError<DuplicateDocument.duplicateStatus>(DuplicateDocuments.Cache, duplicateDocument, WarningMessage, duplicateDocument.DuplicateStatus);
							
							throw new PXSetPropertyException(Messages.ErrorSavingWithDuplicates, GetEntityNameByType(duplicateDocument.ContactType));
						}

						if (blocked.Any(_ => _.IsBlocked))
						{
							if (Base.IsImport || Base.IsContractBasedAPI || HardBlockOnly)
							{
								this.Documents.View.Answer = WebDialogResult.Yes;
							}

							this.Documents.Ask(Messages.Warning, Messages.SureToSaveWithDuplicates, MessageButtons.YesNo, MessageIcon.Warning, true);
						}
					}
				}
			}

			if (this.Documents.View.Answer == WebDialogResult.No)
			{
				PXUIFieldAttribute.SetWarning<DuplicateDocument.duplicateStatus>(DuplicateDocuments.Cache, duplicateDocument, WarningMessage);
			}
		}

		private string GetEntityNameByType(string contactType)
		{
			switch (contactType)
			{
				case ContactTypesAttribute.Lead:
					return Messages.Lead;

				case ContactTypesAttribute.Person:
					return Messages.Contact;

				case ContactTypesAttribute.BAccountProperty:
					return Messages.BusinessAccount;
			}

			return Messages.Entity;
		}

		public virtual void OnAfterPersist()
		{
			DuplicateDocument duplicateDocument = DuplicateDocuments.Current ?? DuplicateDocuments.SelectSingle();

			if (duplicateDocument == null)
				return;

			if (DuplicateDocuments.Cache.GetStatus(duplicateDocument).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted)
				&& Processor.IsValidationOnEntryActive(duplicateDocument.ContactType)
				&& CheckIsActive()
				&& !Processor.GrammSourceUpdated(duplicateDocument))
			{
				CheckForDuplicates.Press();
			}
		}

		public virtual (bool, List<CRDuplicateResult>) CheckIfAnyDuplicates(DuplicateDocument duplicateDocument, bool withUpdate = false)
		{
			Duplicates.View.Clear();
			var result = Duplicates.Select(true).Cast<CRDuplicateResult>().ToList();

			bool anyFound = result.Count > 0;

			if (withUpdate)
			{
				DuplicateDocuments.Cache.SetValue<DuplicateDocument.duplicateFound>(duplicateDocument, anyFound);
				DuplicateDocuments.Cache.SetValue<DuplicateDocument.duplicateStatus>(duplicateDocument, anyFound ? DuplicateStatusAttribute.PossibleDuplicated : DuplicateStatusAttribute.Validated);
			}

			return (anyFound, result);
		}

		public virtual bool PersistGrams(DuplicateDocument document)
		{
			if (document != null)
			{
				try
				{
					return Processor.PersistGrams(document);
				}
				catch (Exception e)
				{
					PXTrace.WriteError(e);
				}
			}

			return false;
		}

		public static IEnumerable<FieldValue> GetMarkedPropertiesOf<TPrimary>(PXGraph graph, ref int firstSortOrder)
			where TPrimary : class, IBqlTable, IPXSelectable, new()
		{
			PXCache cache = graph.Caches[typeof(TPrimary)];
			int order = firstSortOrder;
			List<FieldValue> res = (cache
				.GetFields_MassMergable()
				.Select(fieldname => new { fieldname, state = cache.GetStateExt(null, fieldname) as PXFieldState })
				.Where(@t => @t.state != null)
				.Select(@t => new FieldValue()
				{
					Selected = false,
					CacheName = typeof(TPrimary).FullName,
					Name = @t.fieldname,
					DisplayName = @t.state.DisplayName,
					AttributeID = null,
					Order = order++
				})).ToList();

			firstSortOrder = order;
			return res;
		}

		protected static IEnumerable<FieldValue> GetUdfProperties(PXGraph graph, ref int firstSortOrder)
		{
			int order = firstSortOrder;
			List<FieldValue> res = new List<FieldValue>();
			var screenId = graph.Accessinfo.ScreenID.Replace(".", "");
			var udfFields = PX.CS.KeyValueHelper.GetAttributeFields(screenId)
								.OrderBy(t => t.Item3)
								.OrderBy(t => t.Item2);

			foreach (var (fieldState, row, col, defaultValue) in udfFields)
			{
				res.Add(new FieldValue
				{
					Selected = false,
					CacheName = typeof(TMain).FullName,
					Name = fieldState.Name,
					DisplayName = fieldState.DisplayName,
					AttributeID = null,
					Order = order++
				});
			}

			firstSortOrder = order;
			return res;
		}

		public static IEnumerable<FieldValue> GetAttributeProperties(PXGraph graph, ref int firstSortOrder, List<string> suffixes)
		{
			int order = firstSortOrder;

			GetAttributeSuffixes(graph, ref suffixes);

			List<FieldValue> res = new List<FieldValue>();
			PXCache cache = graph.Caches[typeof(TMain)];

			foreach (string field in cache.Fields)
			{
				if (!suffixes.Any(suffix => field.EndsWith(string.Format("_{0}", suffix))))
					continue;

				PXFieldState state = cache.GetStateExt(null, field) as PXFieldState;

				if (state == null)
					continue;

				string displayName = state.DisplayName;
				string attrID = field;
				string local = field;

				foreach (string suffix in suffixes.Where(suffix => local.EndsWith(string.Format("_{0}", suffix))))
				{
					attrID = field.Replace(string.Format("_{0}", suffix), string.Empty);
					displayName = state.DisplayName.Replace(string.Format("${0}$-", suffix), string.Empty);
					break;
				}

				res.Add(new FieldValue
				{
					Selected = false,
					CacheName = typeof(TMain).FullName,
					Name = field,
					DisplayName = displayName,
					AttributeID = attrID,
					Order = order++ + 1000
				});
			}

			firstSortOrder = order;
			return res;
		}

		protected static void GetAttributeSuffixes(PXGraph graph, ref List<string> suffixes)
		{
			suffixes = suffixes 
				?? new List<string>(graph.Caches[typeof(TMain)].BqlTable
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.SelectMany(p => p.GetCustomAttributes(true).Where(atr => atr is PXDBAttributeAttribute), (p, atr) => p.Name));
		}

		public virtual void GetAllProperties(List<FieldValue> values, HashSet<string> fieldNames)
		{
			int order = 0;

			values.AddRange(
				GetMarkedPropertiesOf<Address>(Base, ref order)
					.Union(GetUdfProperties(Base, ref order))
					.Union(GetAttributeProperties(Base, ref order, null))
					.Where(fld => fieldNames.Add(fld.Name)));
		}

		internal static object RunActionWithAppliedSearch(PXGraph graph, object entity, string actionName)
		{
			graph.Views[graph.PrimaryView].Cache.Current = entity;

			List<object> searches = new List<object>();
			List<string> sorts = new List<string>();

			foreach (string key in graph.Views[graph.PrimaryView].Cache.Keys)
			{
				searches.Add(graph.Views[graph.PrimaryView].Cache.GetValue(entity, key));
				sorts.Add(key);
			}

			PXAdapter a = new PXAdapter(graph.Views[graph.PrimaryView])
			{
				StartRow = 0,
				MaximumRows = 1,
				Searches = searches.ToArray(),
				SortColumns = sorts.ToArray()
			};

			if (graph.Actions.Contains(actionName))
			{
				foreach (var c in graph.Actions[actionName].Press(a))
				{
					return c;
				}
			}

			return null;
		}

		public virtual void Highlight(PX.Web.UI.PXGridCellCollection cells, CRDuplicateResult row)
		{
			if (row == null)
				return;

			var mainRecord = Documents.Current.Base as TMain;
			var mainContact = GetTargetContact(mainRecord);
			var mainAddress = GetTargetAddress(mainRecord);

			if (mainRecord == null)
				return;

			var mainRecordCache = Base.Caches[mainRecord.GetType()];
			var mainContactCache = Base.Caches[mainContact.GetType()];
			var mainAddressCache = Base.Caches[mainAddress.GetType()];

			foreach (PXGridCell cell in cells)
			{
				if (cell.Value == null || !cell.Column.Visible)
					continue;

				var dataField = cell.DataField.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

				if (dataField.Length != 2)
					continue;

				var entityType = dataField[0];
				var fieldName = dataField[1];

				var entity = row[entityType];

				if (entity == null)
					continue;

				bool FieldsEqual()
				{
					var cache = Base.Caches[entity.GetType()];

					switch (entity.GetType())
					{
						case Type mainType when typeof(TMain).IsAssignableFrom(mainType):
							return object.Equals(cache.GetValue(entity, fieldName), mainRecordCache.GetValue(mainRecord, fieldName));
						case Type mainType when typeof(Contact).IsAssignableFrom(mainType):
							return object.Equals(cache.GetValue(entity, fieldName), mainContactCache.GetValue(mainContact, fieldName));
						case Type mainType when typeof(Address).IsAssignableFrom(mainType):
							return object.Equals(cache.GetValue(entity, fieldName), mainAddressCache.GetValue(mainAddress, fieldName));
					}

					return false;
				}

				if (FieldsEqual())
				{
					cell.Style.CssClass = "green20";
				}
			}

		}

		public virtual void ViewDuplicate(CRDuplicateRecord duplicateRecord)
		{
			if (duplicateRecord == null)
				return;

			Contact contact = PXSelect<Contact,
				Where<Contact.contactID, Equal<Required<CRDuplicateRecord.duplicateContactID>>>>.Select(Base, duplicateRecord.DuplicateContactID);

			OpenEntityScreen(contact, PXRedirectHelper.WindowMode.New);
		}

		public virtual void ViewDuplicateRefContact(CRDuplicateRecord duplicateRecord)
		{
			if (duplicateRecord == null)
				return;

			Contact contact = PXSelect<Contact,
				Where<Contact.contactID, Equal<Required<CRDuplicateRecord.duplicateRefContactID>>>>.Select(Base, duplicateRecord.DuplicateRefContactID);

			OpenEntityScreen(contact, PXRedirectHelper.WindowMode.New);
		}
		
		public virtual void ViewDuplicateBAccount(CRDuplicateRecord duplicateRecord)
		{
			if (duplicateRecord == null)
				return;

			BAccountR bAccount = PXSelect<BAccountR,
				Where<BAccountR.bAccountID, Equal<Required<CRDuplicateRecord.duplicateBAccountID>>>>.Select(Base, duplicateRecord.DuplicateBAccountID);

			OpenEntityScreen(bAccount, PXRedirectHelper.WindowMode.New);
		}

		private void OpenEntityScreen(IBqlTable entity, PXRedirectHelper.WindowMode windowMode)
		{
			if (entity == null)
				return;

			PXPrimaryGraphCollection primaryGraph = new PXPrimaryGraphCollection(Base);

			PXRedirectHelper.TryRedirect(primaryGraph[entity], entity, windowMode);
		}

		#endregion
	}
}
