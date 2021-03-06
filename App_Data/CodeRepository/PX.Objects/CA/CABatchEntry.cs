using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using PX.Api;
using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CM;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.AP.MigrationMode;


namespace PX.Objects.CA
{
	public class CABatchEntry : PXGraph<CABatchEntry>
	{
		#region Internal Variables
		private bool IsPrintProcess { get; set; } = false;
		#endregion

		#region Toolbar buttons
		public PXSave<CABatch> Save;
        public PXCancel<CABatch> Cancel;
        public PXDelete<CABatch> Delete;
        public PXFirst<CABatch> First;
        public PXPrevious<CABatch> Previous;
        public PXNext<CABatch> Next;
        public PXLast<CABatch> Last;
        #endregion

		#region Internal defintions
        public static class ExportProviderParams
		{
			public const string FileName = "FileName";
			public const string BatchNbr = "BatchNbr";
			public const string BatchSequenceStartingNbr = "BatchStartNumber";
		}

		public class PrintProcessContext : IDisposable
		{
			private CABatchEntry BatchEntryGraph { get; set; }

			public PrintProcessContext(CABatchEntry graph)
			{
				graph.IsPrintProcess = true;
				BatchEntryGraph = graph;
			}

			public void Dispose()
			{
				BatchEntryGraph.IsPrintProcess = false;
			}
		}
		#endregion

		#region Buttons
		public PXInitializeState<CABatch> initializeState;


		public PXAction<CABatch> putOnHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable PutOnHold(PXAdapter adapter) => adapter.Get();

		public PXAction<CABatch> releaseFromHold;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Remove Hold", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable ReleaseFromHold(PXAdapter adapter) => adapter.Get();


		public PXAction<CABatch> release;
		[PXUIField(DisplayName = Messages.Release, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		public virtual IEnumerable Release(PXAdapter adapter)
		{
			Save.Press();
			CABatch document = this.Document.Current;
			PXLongOperation.StartOperation(this, delegate () { CABatchEntry.ReleaseDoc(document); });

			return adapter.Get();
		}

		public PXAction<CABatch> export;
		[PXUIField(DisplayName = Messages.Export, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		public virtual IEnumerable Export(PXAdapter adapter)
		{
			CABatch document = this.Document.Current;			
			if (document != null && document.Released == true && document.Hold == false)
			{
				PXResult<PaymentMethod, SYMapping> res = (PXResult<PaymentMethod, SYMapping>) PXSelectJoin<PaymentMethod, 
									LeftJoin<SYMapping, On<SYMapping.mappingID, Equal<PaymentMethod.aPBatchExportSYMappingID>>>,
										Where<PaymentMethod.paymentMethodID, Equal<Optional<CABatch.paymentMethodID>>>>.Select(this, document.PaymentMethodID);
				PaymentMethod pt = res;
				SYMapping map = res;
				if (pt != null && pt.APCreateBatchPayment == true && pt.APBatchExportSYMappingID != null && map != null)
				{
					VerifySettings();

					string defaultFileName = this.GenerateFileName(document);
					PXLongOperation.StartOperation(this, delegate()
					{
						PX.Api.SYExportProcess.RunScenario(map.Name, 
							SYMapping.RepeatingOption.All,
							true,
							true,
							new PX.Api.PXSYParameter(ExportProviderParams.FileName, defaultFileName),
							new PX.Api.PXSYParameter(ExportProviderParams.BatchNbr, document.BatchNbr));
					});
				}
				else
				{
					throw new PXException(Messages.CABatchExportProviderIsNotConfigured);				
				}
			}
			return adapter.Get();
		}

		protected virtual void VerifySettings()
		{
			VarifyCashAccountPMDetailsSettings();
			VarifyVendorPaymentMethodDetailsSettings();
		}

		private void VarifyCashAccountPMDetailsSettings()
		{
			foreach(PXResult<PaymentMethodDetail, CashAccountPaymentMethodDetail> result in PXSelectReadonly2<PaymentMethodDetail,
							LeftJoin<CashAccountPaymentMethodDetail,
								On<CashAccountPaymentMethodDetail.paymentMethodID, Equal<PaymentMethodDetail.paymentMethodID>,
									And<CashAccountPaymentMethodDetail.detailID, Equal<PaymentMethodDetail.detailID>>>>,
							Where<PaymentMethodDetail.paymentMethodID, Equal<Current<CABatch.paymentMethodID>>,
								And<PaymentMethodDetail.useFor, Equal<PaymentMethodDetailUsage.useForCashAccount>,
								And<PaymentMethodDetail.isRequired, Equal<True>>>>>.Select(this))
			{
				CashAccountPaymentMethodDetail caDetail = result;

				if (!string.IsNullOrEmpty(caDetail?.DetailValue)) continue;

				PaymentMethodDetail detail = result;
				var ca = cashAccount.SelectSingle();
				Document.Cache.RaiseExceptionHandling<CABatch.cashAccountID>(Document.Current, ca?.CashAccountCD, new PXSetPropertyException(PXLocalizer.LocalizeFormat(Messages.CashAccountPMSettingsIsEmpty, detail.Descr, ca?.CashAccountCD, this.Document.Current?.PaymentMethodID), PXErrorLevel.Error));
				
				throw new PXException(PXLocalizer.LocalizeFormat(Messages.SomeCashAccountPMSettingsAreEmpty, ca?.CashAccountCD?.Trim(), this.Document.Current?.PaymentMethodID));
			}
		}

		private void VarifyVendorPaymentMethodDetailsSettings()
		{
			var errors = new Dictionary<string, string>();
			bool hasErrors = false;

			foreach (PXResult<CABatchDetail, APPayment> result in BatchPayments.Select())
			{
				var row = (CABatchDetail)result;
				var payment = (APPayment)result;
				var key = string.Format($"{payment.PaymentMethodID}_{payment.VendorID}_{payment.VendorLocationID}");

				if (errors.ContainsKey(key))
				{
					if (!string.IsNullOrEmpty(errors[key]))
					{
						BatchPayments.Cache.RaiseExceptionHandling<CABatchDetail.origRefNbr>(row, row.OrigRefNbr, new PXSetPropertyException(errors[key], PXErrorLevel.RowError));
					}
					continue;
				}

				errors[key] = string.Empty;

				foreach (PXResult<PaymentMethodDetail, Location, VendorPaymentMethodDetail> caresult in PXSelectReadonly2<PaymentMethodDetail,
								InnerJoin<Location,
									On<Location.vPaymentMethodID, Equal<PaymentMethodDetail.paymentMethodID>>,
								LeftJoin<VendorPaymentMethodDetail,
									On<VendorPaymentMethodDetail.paymentMethodID, Equal<PaymentMethodDetail.paymentMethodID>,
										And<VendorPaymentMethodDetail.detailID, Equal<PaymentMethodDetail.detailID>,
										And<VendorPaymentMethodDetail.bAccountID, Equal<Location.bAccountID>,
										And<VendorPaymentMethodDetail.locationID, Equal<Location.locationID>>>>>>>,
								Where<PaymentMethodDetail.paymentMethodID, Equal<Required<CABatch.paymentMethodID>>,
									And<PaymentMethodDetail.useFor, Equal<PaymentMethodDetailUsage.useForVendor>,
									And<PaymentMethodDetail.isRequired, Equal<True>,
									And<Location.bAccountID, Equal<Required<Location.bAccountID>>,
									And<Location.locationID, Equal<Required<Location.locationID>>>>>>>>.Select(this, payment.PaymentMethodID, payment.VendorID, payment.VendorLocationID))
				{
					VendorPaymentMethodDetail vendorDetail = caresult;

					if (!string.IsNullOrEmpty(vendorDetail?.DetailValue)) continue;

					PaymentMethodDetail detail = caresult;
					var currentVendor = vendor.SelectSingle(payment.VendorID);
					errors[key] = PXLocalizer.LocalizeFormat(Messages.VendorPaymentMethodSettingIsEmpty, detail.Descr, currentVendor?.AcctCD, this.Document.Current?.PaymentMethodID);
					hasErrors = true;
					break;
				}

				if (!string.IsNullOrEmpty(errors[key]))
				{
					BatchPayments.Cache.RaiseExceptionHandling<CABatchDetail.origRefNbr>(row, row.OrigRefNbr, new PXSetPropertyException(errors[key], PXErrorLevel.RowError));
				}
			}

			if (hasErrors)
			{
				throw new PXException(PXLocalizer.LocalizeFormat(Messages.SomeVendorPaymentMethodSettingAreEmpty, this.Document.Current?.PaymentMethodID));
			}
		}

		public PXAction<CABatch> ViewAPDocument;
		[PXUIField(
			DisplayName = PO.Messages.ViewAPDocument, 
			MapEnableRights = PXCacheRights.Select, 
			MapViewRights = PXCacheRights.Select, 
			Visible = false)]
		[PXLookupButton]
		public virtual IEnumerable viewAPDocument(PXAdapter adapter)
		{
			CABatchDetail detail = this.BatchPayments.Current;

			if (detail == null)
			{
				return adapter.Get();
			}

			APRegister apDocument = PXSelect<APRegister, 
							Where<APRegister.docType, Equal<Required<APRegister.docType>>,
							And<APRegister.refNbr, Equal<Required<APRegister.refNbr>>>>>.Select(this, detail.OrigDocType, detail.OrigRefNbr);

			if (apDocument == null)
			{
				return adapter.Get();
			}

			IDocGraphCreator creator = new APDocGraphCreator();

			PXGraph graph = creator.Create(apDocument.DocType, apDocument.RefNbr, apDocument.VendorID);
			if (graph != null)
				{
				throw new PXRedirectRequiredException(graph, true, "ViewDocument") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}					

			return adapter.Get();
		}
	    #endregion

        #region Ctor + Selects
		
		public CABatchEntry()
		{
			CASetup setup = CASetup.Current;
			APSetup apSetup = APSetup.Current;

			RowUpdated.AddHandler<CABatch>(ParentFieldUpdated);

            APPaymentApplications.Cache.AllowInsert = false;
            APPaymentApplications.Cache.AllowDelete = false;
            APPaymentApplications.Cache.AllowUpdate = false;
            BatchPayments.AllowUpdate = false;
		}
        
		public PXSelect<CABatch, Where<CABatch.origModule, Equal<GL.BatchModule.moduleAP>>> Document;
		
		public PXSelect<CABatchDetail, Where<CABatchDetail.batchNbr, Equal<Current<CABatch.batchNbr>>>> Details;
		public PXSelectJoin<CABatchDetail,
							LeftJoin<APPayment, 
								On<CABatchDetail.origDocType, Equal<APPayment.docType>,
								And<CABatchDetail.origRefNbr, Equal<APPayment.refNbr>>>>,
							Where<CABatchDetail.batchNbr, Equal<Current<CABatch.batchNbr>>>> BatchPayments;

		public PXSelectJoin<APPayment,
							InnerJoin<CABatchDetail, 
								On<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAP>,
								And<CABatchDetail.origDocType, Equal<APPayment.docType>,
								And<CABatchDetail.origRefNbr, Equal<APPayment.refNbr>>>>,
							InnerJoin<PaymentMethod, On<PaymentMethod.paymentMethodID, Equal<APPayment.paymentMethodID>>>>,
							Where<CABatchDetail.batchNbr, Equal<Current<CABatch.batchNbr>>,
								And<Where<APPayment.curyOrigDocAmt, NotEqual<decimal0>, Or<PaymentMethod.skipPaymentsWithZeroAmt, NotEqual<True>>>>>> APPaymentList;


        public PXSelectJoin<Address, InnerJoin<Location, On<Location.vRemitAddressID, Equal<Address.addressID>>>,
                                    Where<Location.bAccountID, Equal<Current<APPayment.vendorID>>,
                                       And<Location.locationID, Equal<Current<APPayment.vendorLocationID>>>>> VendorRemitAddress;

        public PXSelectJoin<Contact, InnerJoin<Location, On<Location.vRemitContactID, Equal<Contact.contactID>>>,                                    
                                    Where<Location.bAccountID, Equal<Current<APPayment.vendorID>>,
                                       And<Location.locationID, Equal<Current<APPayment.vendorLocationID>>>>> VendorRemitContact;

        public PXSelectJoin<APInvoice, InnerJoin<APAdjust, On<APInvoice.docType, Equal<APAdjust.adjdDocType>,
                            And<APInvoice.refNbr, Equal<APAdjust.adjdRefNbr>>>,
                            InnerJoin<APPayment, On<APAdjust.adjgDocType, Equal<APPayment.docType>,
                                And<APAdjust.adjgRefNbr, Equal<APPayment.refNbr>>>>>,
                    Where<APAdjust.adjgDocType, Equal<Current<APPayment.docType>>,
                    And<APAdjust.adjgRefNbr, Equal<Current<APPayment.refNbr>>>>> APPaymentApplications;
				
		public PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Current<CABatch.cashAccountID>>>> cashAccount;
		public PXSelect<Vendor, Where<Vendor.bAccountID, Equal<Required<Vendor.bAccountID>>>> vendor;

		public PXSetup<CASetup> CASetup;
		public APSetupNoMigrationMode APSetup;

		public PXSelectJoin<CABatchDetail,
							InnerJoin<APPayment, 
								On<APPayment.docType, Equal<CABatchDetail.origDocType>,
								And<APPayment.refNbr, Equal<CABatchDetail.origRefNbr>,
								And<APPayment.released, Equal<True>>>>>,
							Where<CABatchDetail.batchNbr, Equal<Current<CABatch.batchNbr>>>> ReleasedPayments;

		#region Selects, used in export
		public PXSelectReadonly<CashAccountPaymentMethodDetail,
		Where<CashAccountPaymentMethodDetail.paymentMethodID, Equal<Current<CABatch.paymentMethodID>>,
		And<Current<APPayment.docType>, IsNotNull,
		And<Current<APPayment.refNbr>, IsNotNull,
		And<CashAccountPaymentMethodDetail.accountID, Equal<Current<CABatch.cashAccountID>>,
		And<CashAccountPaymentMethodDetail.detailID, Equal<Required<CashAccountPaymentMethodDetail.detailID>>>>>>>> cashAccountSettings;

		public PXSelectReadonly2<VendorPaymentMethodDetail,
				InnerJoin<Location, On<Location.bAccountID, Equal<VendorPaymentMethodDetail.bAccountID>,
                    And<Location.vPaymentInfoLocationID, Equal<VendorPaymentMethodDetail.locationID>>>>,
				Where<VendorPaymentMethodDetail.paymentMethodID, Equal<Current<CABatch.paymentMethodID>>,
					And<Current<APPayment.docType>, IsNotNull,
					And<Current<APPayment.refNbr>, IsNotNull,
					And<Location.bAccountID, Equal<Current<APPayment.vendorID>>,
                    And<Location.locationID, Equal<Current<APPayment.vendorLocationID>>,
					And<VendorPaymentMethodDetail.detailID, Equal<Required<VendorPaymentMethodDetail.detailID>>>>>>>>> vendorPaymentSettings;
		
		#endregion
        #endregion

		#region Events

		#region CABatch Events
		protected virtual void CABatch_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CABatch row = e.Row as CABatch;

			if (row == null || IsPrintProcess)
				return;

			bool isReleased = row.Released == true;
			
			PXUIFieldAttribute.SetEnabled(sender, row, false);
			PXUIFieldAttribute.SetEnabled<CABatch.batchNbr>(sender, row, true);

			PXUIFieldAttribute.SetEnabled<CABatch.exportFileName>(sender, row, IsExport);
            PXUIFieldAttribute.SetEnabled<CABatch.exportTime>(sender, row, IsExport);

			bool allowDelete = !isReleased;

			if (allowDelete)
			{
				allowDelete = !ReleasedPayments.Select().Any();
			}

			sender.AllowDelete = allowDelete;

			CashAccount cashaccount = (CashAccount)PXSelectorAttribute.Select<CABatch.cashAccountID>(sender, row);
			bool clearEnabled = row.Released != true && cashaccount?.Reconcile == true;

			PXUIFieldAttribute.SetEnabled<CABatch.hold>(sender, row, !isReleased);
			PXUIFieldAttribute.SetEnabled<CABatch.tranDesc>(sender, row, !isReleased);
			PXUIFieldAttribute.SetEnabled<CABatch.tranDate>(sender, row, !isReleased);
			PXUIFieldAttribute.SetEnabled<CABatch.batchSeqNbr>(sender, row, !isReleased);
			PaymentMethod pt = PXSelect<PaymentMethod, Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>.Select(this, row.PaymentMethodID);
			if (pt != null)
				PXUIFieldAttribute.SetRequired<CABatch.batchSeqNbr>(sender, pt.RequireBatchSeqNum.Value);

			PXUIFieldAttribute.SetEnabled<CABatch.extRefNbr>(sender, row, !isReleased);

			if (!isReleased)
			{				
				bool hasDetails = BatchPayments.Select().Any();

				PXUIFieldAttribute.SetEnabled<CABatch.paymentMethodID>(sender, row, !hasDetails);
				PXUIFieldAttribute.SetEnabled<CABatch.cashAccountID>(sender, row, !hasDetails);
			}

			PXUIFieldAttribute.SetVisible<CABatch.dateSeqNbr>(sender, row, isReleased);
		}
	
		protected virtual void CABatch_CashAccountID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			CABatch row = (CABatch)e.Row;
			row.Cleared = false;
			row.ClearDate = null;

			if (cashAccount.Current == null || cashAccount.Current.CashAccountID != row.CashAccountID)
			{
				cashAccount.Current = (CashAccount)PXSelectorAttribute.Select<CABatch.cashAccountID>(sender, row);
			}

			if (cashAccount.Current.Reconcile != true)
			{
				row.Cleared = true;
				row.ClearDate = row.TranDate;			
			}

			sender.SetDefaultExt<CABatch.referenceID>(e.Row);
			sender.SetDefaultExt<CABatch.paymentMethodID>(e.Row);			
		}

        protected virtual void CABatch_PaymentMethodID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            CABatch row = (CABatch)e.Row;
            sender.SetDefaultExt<CABatch.batchSeqNbr>(e.Row);			
        }

        protected virtual void CABatch_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
        { 
            this._isMassDelete = true;
        }

        private bool _isMassDelete = false;

		protected virtual void CABatch_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			_isMassDelete = false;
		}

		#endregion

		#region CABatch Detail events
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Type")]
        [APDocType.List()]
		public virtual void CABatchDetail_OrigDocType_CacheAttached(PXCache sender)
		{
		}

		protected virtual void CABatchDetail_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			CABatchDetail row = (CABatchDetail)e.Row;
			bool isReleased = false;

			if (row.OrigModule == GL.BatchModule.AP)
			{
				APRegister apDocument = PXSelect<APRegister,
										   Where<APRegister.docType, Equal<Required<APRegister.docType>>,
											 And<APRegister.refNbr, Equal<Required<APRegister.refNbr>>>>>
										.Select(this, row.OrigDocType, row.OrigRefNbr);

				isReleased = (bool)apDocument.Released;
			}

			if (row.OrigModule == GL.BatchModule.AR)
			{
				ARRegister arDocument = PXSelect<ARRegister,
										   Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
											 And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>
										.Select(this, row.OrigDocType, row.OrigRefNbr);
				isReleased = (bool)arDocument.Released;
			}

			if (isReleased)
			{
				throw new PXException(Messages.ReleasedDocumentMayNotBeAddedToCABatch);
			}
		}
		
		protected virtual void CABatchDetail_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			CABatchDetail row = (CABatchDetail)e.Row;
			UpdateDocAmount(row, false);			
		}

		protected virtual void CABatchDetail_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
		{
			CABatchDetail row = (CABatchDetail)e.Row;
			bool isReleased = false;
			bool isVoided = false;

			if (row.OrigModule == GL.BatchModule.AP)
			{
				APRegister apDocument = PXSelect<APRegister, Where<APRegister.docType, Equal<Required<APRegister.docType>>,
											And<APRegister.refNbr, Equal<Required<APRegister.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
				isReleased = (bool) apDocument.Released;
				isVoided = (bool)apDocument.Voided; 
			}

			if (row.OrigModule == GL.BatchModule.AR)
			{
				ARRegister arDocument = PXSelect<ARRegister, Where<ARRegister.docType, Equal<Required<ARRegister.docType>>,
											And<ARRegister.refNbr, Equal<Required<ARRegister.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
				isReleased = (bool)arDocument.Released;
				isVoided = (bool)arDocument.Voided; 
			}

			if (isReleased && !isVoided)
			{
				throw new PXException(Messages.ReleasedDocumentMayNotBeDeletedFromCABatch);
			}
		}

		protected virtual void CABatchDetail_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			CABatchDetail row = (CABatchDetail)e.Row;

            if (!this._isMassDelete)
            {
                UpdateDocAmount(row, true);
            }
			#region Update APPayment.status
			var cache = this.Caches<APPayment>();
			APPayment payment = PXSelect<APPayment,
								Where<APPayment.docType, Equal<Required<APPayment.docType>>,
									And<APPayment.refNbr, Equal<Required<APPayment.refNbr>>>>>
				.Select(this, row.OrigDocType, row.OrigRefNbr);
			APPayment.Events
				.Select(se => se.CancelPrintCheck)
				.FireOn(this, payment);
			#endregion
		}

		private CABatch UpdateDocAmount(CABatchDetail row, bool negative)
		{
			CABatch document = this.Document.Current;

			if (row.OrigDocType != null && row.OrigRefNbr != null)
			{
				decimal? curyAmount = null, amount = null;
				if (row.OrigModule == GL.BatchModule.AP)
				{
					APPayment payment = PXSelect<APPayment,
							Where<APPayment.docType, Equal<Required<APPayment.docType>>,
							And<APPayment.refNbr, Equal<Required<APPayment.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);
					if (payment != null)
					{
						curyAmount = payment.CuryOrigDocAmt;
						amount = payment.OrigDocAmt;
					}
				}
				else
				{
					ARPayment payment = PXSelect<ARPayment,
							Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
							And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>.Select(this, row.OrigDocType, row.OrigRefNbr);

					if (payment != null)
					{
						curyAmount = payment.CuryOrigDocAmt;
						amount = payment.OrigDocAmt;
					}
				}

				CABatch copy = (CABatch)this.Document.Cache.CreateCopy(document);

				if (curyAmount.HasValue)
				{
					document.CuryDetailTotal += negative ? -curyAmount : curyAmount;
				}

				if (amount.HasValue)
				{
					document.DetailTotal += negative ? -amount : amount;
				}

				document = this.Document.Update(document);
			}
			return document;
		}
		#endregion
		#endregion

		#region Methods
		public virtual CABatchDetail AddPayment(APPayment aPayment,  bool skipCheck)
		{
			if (!skipCheck)
			{
				foreach (CABatchDetail item in this.BatchPayments.Select())
				{
					if (IsKeyEqual(aPayment, item))
					{
						return item;
					}
				}
			}

			CABatchDetail detail = new CABatchDetail();
			detail.Copy(aPayment);
			detail = this.BatchPayments.Insert(detail);

			return detail; 			
		}

		public virtual CABatchDetail AddPayment(ARPayment aPayment, bool skipCheck)
		{
			if (!skipCheck)
			{
				foreach (CABatchDetail item in this.BatchPayments.Select())
				{
					if (IsKeyEqual(aPayment, item))
					{
						return item;
					}
				}
			}

			CABatchDetail detail = new CABatchDetail();
			detail.Copy(aPayment);
			detail = this.BatchPayments.Insert(detail);

			return detail;
		}

		protected virtual void ParentFieldUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<CABatch.tranDate>(e.Row, e.OldRow))
			{
				foreach (CABatchDetail detail in this.Details.Select())
				{
					this.Details.Cache.MarkUpdated(detail);
				}
			}
		}
		#endregion

		#region Internal Utilities
		public virtual string GenerateFileName(CABatch aBatch)
		{
			if (aBatch.CashAccountID != null && !string.IsNullOrEmpty(aBatch.PaymentMethodID))
			{
				CashAccount acct = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(this, aBatch.CashAccountID);
				if (acct != null)
				{
                    return string.Format(Messages.CABatchDefaultExportFilenameTemplate, aBatch.PaymentMethodID, acct.CashAccountCD, aBatch.TranDate.Value, aBatch.DateSeqNbr);
				}
			}
			return string.Empty;
		}

		[Obsolete(Common.Messages.MethodIsObsoleteAndWillBeRemoved2020R2)]
		public virtual void CalcDetailsTotal(ref decimal? aCuryTotal, ref decimal? aTotal) 
		{
			aCuryTotal = 0m;
			aTotal = 0m;

			foreach (PXResult<CABatchDetail, APPayment> item in this.BatchPayments.Select()) 
			{
				APPayment payment = item;

				if (!string.IsNullOrEmpty(payment.RefNbr)) 
				{
					aCuryTotal += payment.CuryOrigDocAmt;
					aTotal += payment.OrigDocAmt;
				}
			}
		}
		#endregion
		
		#region Static Methods
		public static void ReleaseDoc(CABatch aDocument) 
		{
			if ((bool)aDocument.Released || (bool)aDocument.Hold)
			{
				throw new PXException(Messages.CABatchStatusIsNotValidForProcessing);
			}

			CABatchUpdate batchEntry = PXGraph.CreateInstance<CABatchUpdate>();
			CABatch document = batchEntry.Document.Select(aDocument.BatchNbr);
			batchEntry.Document.Current = document;

			if ((bool)document.Released || (bool)document.Hold)
			{
				throw new PXException(Messages.CABatchStatusIsNotValidForProcessing);
			}

			PaymentMethod pt = PXSelect<PaymentMethod, Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>.Select(batchEntry, document.PaymentMethodID);

			if (string.IsNullOrEmpty(document.BatchSeqNbr) && (pt.RequireBatchSeqNum == true))
			{
				batchEntry.Document.Cache.RaiseExceptionHandling<CABatch.batchSeqNbr>(document, null,
					new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXErrorLevel.Error, PXUIFieldAttribute.GetDisplayName<CABatch.batchSeqNbr>(batchEntry.Document.Cache)));
				throw new PXException(ErrorMessages.RecordRaisedErrors, Messages.Releasing, batchEntry.Document.Cache.DisplayName);
			}

			APRegister voided = PXSelectReadonly2<APRegister,
							InnerJoin<CABatchDetail, On<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAP>,
							And<CABatchDetail.origDocType, Equal<APRegister.docType>,
							And<CABatchDetail.origRefNbr, Equal<APRegister.refNbr>>>>>,
							Where<CABatchDetail.batchNbr, Equal<Required<CABatch.batchNbr>>,
								And<APRegister.voided, Equal<True>>>>.Select(batchEntry, document.BatchNbr);

			if (voided != null && string.IsNullOrEmpty(voided.RefNbr) == false) 
			{
				throw new PXException(Messages.CABatchContainsVoidedPaymentsAndConnotBeReleased);
			}

            List<APRegister> unreleasedList = new List<APRegister>();
            PXSelectBase<APPayment> selectUnreleased = new PXSelectReadonly2<APPayment,
                            InnerJoin<CABatchDetail, 
								On<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAP>,
								And<CABatchDetail.origDocType, Equal<APPayment.docType>,
								And<CABatchDetail.origRefNbr, Equal<APPayment.refNbr>>>>>,
                            Where<CABatchDetail.batchNbr, Equal<Optional<CABatch.batchNbr>>,
                                And<APPayment.released, Equal<boolFalse>>>>(batchEntry);

            foreach (APPayment item in selectUnreleased.Select(document.BatchNbr)) 
			{
				if (item.Released != true) 
				{
					unreleasedList.Add(item);
				}
			}

			if (unreleasedList.Count > 0)
			{
				APDocumentRelease.ReleaseDoc(unreleasedList, true);
			}

            selectUnreleased.View.Clear();

			APPayment payment = selectUnreleased.Select(document.BatchNbr);
			if (payment != null) 
			{
				throw new PXException(Messages.CABatchContainsUnreleasedPaymentsAndCannotBeReleased);
			}

			document.Released = true;
			document.DateSeqNbr = GetNextDateSeqNbr(batchEntry, aDocument);
			batchEntry.RecalcTotals();
			document = batchEntry.Document.Update(document);
			batchEntry.Actions.PressSave();
			batchEntry.Document.Cache.RestoreCopy(aDocument, document);
		}

		public static bool IsKeyEqual(APPayment payment, CABatchDetail detail)
		{
			return (detail.OrigModule == BatchModule.AP && payment.DocType == detail.OrigDocType && payment.RefNbr == detail.OrigRefNbr);
		}

		public static bool IsKeyEqual(AR.ARPayment payment, CABatchDetail detail)
		{
			return (detail.OrigModule == BatchModule.AR && payment.DocType == detail.OrigDocType && payment.RefNbr == detail.OrigRefNbr);
		}

		public static short GetNextDateSeqNbr(PXGraph graph, CABatch aDocument)
		{
			short result = 0;
			CABatch lastCABatch = PXSelectReadonly<CABatch, 
							Where<CABatch.cashAccountID, Equal<Required<CABatch.cashAccountID>>,
							And<CABatch.paymentMethodID, Equal<Required<CABatch.paymentMethodID>>,
							And<CABatch.released, Equal<True>,
							And<CABatch.tranDate, Equal<Required<CABatch.tranDate>>>>>>,
							OrderBy<Desc<CABatch.dateSeqNbr>>>.Select(graph, aDocument.CashAccountID, aDocument.PaymentMethodID, aDocument.TranDate);

			if (lastCABatch != null) 
			{
				result = lastCABatch.DateSeqNbr ?? (short)0;
				if (result >= short.MaxValue || result < short.MinValue)
				{
					throw new PXException(Messages.DateSeqNumberIsOutOfRange);
				}
				result++;
			}
			return result;
		}	
		#endregion

		#region Processing Grpah Definition
		[PXHidden]
		public class CABatchUpdate : PXGraph<CABatchUpdate>
		{
			public PXSelect<CABatch, Where<CABatch.batchNbr, Equal<Required<CABatch.batchNbr>>>> Document;
			public PXSelectJoin<APPayment,
							InnerJoin<CABatchDetail, On<CABatchDetail.origDocType, Equal<APPayment.docType>,
							And<CABatchDetail.origRefNbr, Equal<APPayment.refNbr>,
							And<CABatchDetail.origModule, Equal<GL.BatchModule.moduleAP>>>>>,
							Where<CABatchDetail.batchNbr, Equal<Optional<CABatch.batchNbr>>>> APPaymentList;

			public virtual void RecalcTotals()
			{
				CABatch row = this.Document.Current;
				if (row != null)
				{
					row.DetailTotal = row.CuryDetailTotal = row.Total = decimal.Zero;
					foreach (PXResult<APPayment, CABatchDetail> item in this.APPaymentList.Select())
					{
						APPayment payment = item;
						if (!string.IsNullOrEmpty(payment.RefNbr))
						{
							row.CuryDetailTotal += payment.CuryOrigDocAmt;
							row.DetailTotal += payment.OrigDocAmt;
						}
					}
				}
			}
		}
		#endregion
	}
}
