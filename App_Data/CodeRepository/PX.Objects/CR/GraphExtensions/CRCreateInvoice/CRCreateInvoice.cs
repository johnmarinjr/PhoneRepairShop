using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common.Discount;
using PX.Objects.CS;
using PX.Objects.Extensions.Discount;
using PX.Objects.IN;
using PX.Objects.TX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace PX.Objects.CR.Extensions.CRCreateInvoice
{
	#region ARInvoiceEntry extension

	public class CRCreateInvoice_ARInvoiceEntry : PXGraphExtension<ARInvoiceEntry>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.customerModule>();

		#region Events
		public virtual void _(Events.RowPersisting<ARInvoice> e)
		{
			var row = e.Row as ARInvoice;
			if (row == null || e.Operation != PXDBOperation.Insert)
				return;

			var cache = Base.Caches[typeof(CRQuote)];

			foreach (var quote in cache.Cached)
			{
				if (cache.GetStatus(quote) != PXEntryStatus.Held)
					continue;

				Base.EnsureCachePersistence<CRQuote>();

				ARInvoice.Events.Select(o => o.ARInvoiceCreatedFromQuote).FireOn(Base, row, quote as CRQuote);
			}
		}
		#endregion
	}

	#endregion

	public abstract class CRCreateInvoice<TDiscountExt, TGraph, TMaster> : PXGraphExtension<TDiscountExt, TGraph>
		where TDiscountExt : DiscountGraph<TGraph, TMaster>, new()
		where TGraph : PXGraph, new()
		where TMaster : class, IBqlTable, new()
	{
		#region Views

		#region Document Mapping

		protected class DocumentMapping : IBqlMapping
		{
			public Type Extension => typeof(Document);
			protected Type _table;
			public Type Table => _table;

			public DocumentMapping(Type table)
			{
				_table = table;
			}
			public Type OpportunityID = typeof(Document.opportunityID);
			public Type QuoteID = typeof(Document.quoteID);
			public Type Subject = typeof(Document.subject);
			public Type BAccountID = typeof(Document.bAccountID);
			public Type LocationID = typeof(Document.locationID);
			public Type ContactID = typeof(Document.contactID);
			public Type TaxZoneID = typeof(Document.taxZoneID);
			public Type TaxCalcMode = typeof(Document.taxCalcMode);
			public Type ManualTotalEntry = typeof(Document.manualTotalEntry);
			public Type CuryID = typeof(Document.curyID);
			public Type CuryInfoID = typeof(Document.curyInfoID);
			public Type CuryAmount = typeof(Document.curyAmount);
			public Type CuryDiscTot = typeof(Document.curyDiscTot);
			public Type ProjectID = typeof(Document.projectID);
			public Type BranchID = typeof(Document.branchID);
			public Type NoteID = typeof(Document.noteID);
			public Type CloseDate = typeof(Document.closeDate);
			public Type TermsID = typeof(Document.termsID);
			public Type ExternalTaxExemptionNumber = typeof(Document.externalTaxExemptionNumber);
			public Type AvalaraCustomerUsageType = typeof(Document.avalaraCustomerUsageType);
			public Type IsPrimary = typeof(Document.isPrimary);
		}

		protected virtual DocumentMapping GetDocumentMapping()
		{
			return new DocumentMapping(typeof(TMaster));
		}

		#endregion

		public PXSelectExtension<Document> DocumentView;

		[PXCopyPasteHiddenView]
		[PXViewName(Messages.CreateInvoice)]
		public CRPopupFilter<CreateInvoicesFilter> CreateInvoicesParams;

		#endregion

		#region Initialization

		protected static bool IsExtensionActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.financialModule>();
		}
		#endregion

		#region Events
		public virtual void _(Events.RowUpdated<CreateInvoicesFilter> e)
		{
			CreateInvoicesFilter row = e.Row as CreateInvoicesFilter;
			if (row == null)
				return;

			if (!row.RecalculatePrices.GetValueOrDefault())
			{
				CreateInvoicesParams.Cache.SetValue<CreateInvoicesFilter.overrideManualPrices>(row, false);
			}
			if (!row.RecalculateDiscounts.GetValueOrDefault())
			{
				CreateInvoicesParams.Cache.SetValue<CreateInvoicesFilter.overrideManualDiscounts>(row, false);
				CreateInvoicesParams.Cache.SetValue<CreateInvoicesFilter.overrideManualDocGroupDiscounts>(row, false);
			}
		}

		public virtual void _(Events.RowSelected<CreateInvoicesFilter> e)
		{
			CreateInvoicesFilter row = e.Row as CreateInvoicesFilter;
			Document masterEntity = DocumentView.Current;

			if (row == null || masterEntity == null)
				return;

			PXUIFieldAttribute.SetEnabled<CreateInvoicesFilter.overrideManualPrices>(e.Cache, row, row.RecalculatePrices == true);
			PXUIFieldAttribute.SetEnabled<CreateInvoicesFilter.overrideManualDiscounts>(e.Cache, row, row.RecalculateDiscounts == true);
			PXUIFieldAttribute.SetEnabled<CreateInvoicesFilter.overrideManualDocGroupDiscounts>(e.Cache, row, row.RecalculateDiscounts == true);
			PXUIFieldAttribute.SetVisible<CreateInvoicesFilter.confirmManualAmount>(e.Cache, row, masterEntity.ManualTotalEntry == true);
			PXUIFieldAttribute.SetEnabled<CreateInvoicesFilter.makeQuotePrimary>(e.Cache, row, masterEntity.IsPrimary == false);
		}

		public virtual void _(Events.FieldVerifying<CreateInvoicesFilter, CreateInvoicesFilter.confirmManualAmount> e)
		{
			CreateInvoicesFilter row = e.Row as CreateInvoicesFilter;
			Document masterEntity = DocumentView.Current;

			if (row == null || masterEntity == null)
				return;

			if (masterEntity.ManualTotalEntry == true && e.NewValue as bool? != true)
			{
				e.Cache.RaiseExceptionHandling<CreateInvoicesFilter.confirmManualAmount>(row, e.NewValue,
					new PXSetPropertyException(Messages.ConfirmSalesOrderCreation, PXErrorLevel.Error));

				throw new PXSetPropertyException(Messages.ConfirmSalesOrderCreation, PXErrorLevel.Error);
			}
		}

		public virtual void _(Events.FieldUpdated<CreateInvoicesFilter, CreateInvoicesFilter.confirmManualAmount> e)
		{
			if (e.Row == null)
				return;

			e.Cache.RaiseExceptionHandling<CreateInvoicesFilter.confirmManualAmount>(e.Row, null, null);
		}

		#endregion

		#region Actions

		public PXAction<TMaster> CreateInvoice;

		[PXUIField(DisplayName = Messages.CreateInvoice, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable createInvoice(PXAdapter adapter)
		{
			Document masterEntity = this.DocumentView.Current;

			Customer customer = PXSelect<
					Customer,
				Where<
					Customer.bAccountID, Equal<Current<Document.bAccountID>>>>
				.SelectSingleBound(Base, new object[] { DocumentView.Current });
			if (customer == null)
			{
				throw new PXException(Messages.ProspectNotCustomer);
			}

			var quouteID = this.DocumentView.GetValueExt<Document.quoteID>(masterEntity);
			var nonStockItems = PXSelectJoin<CROpportunityProducts,
				InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<CROpportunityProducts.inventoryID>>>,
				Where<InventoryItem.stkItem, Equal<False>,
					And<CROpportunityProducts.quoteID, Equal<Required<Document.quoteID>>>>>.
				SelectMultiBound(Base, new object[] { DocumentView.Current }, quouteID);
			if (nonStockItems.Any_() == false)
			{
				throw new PXException(Messages.InvoiceHasOnlyNonStockLines);
			}

			if (masterEntity.BAccountID != null)
			{
				BAccount baccount = PXSelectJoin<
						BAccount, 
						LeftJoin<Contact,
							On<Contact.contactID, Equal<BAccount.defContactID>>>,
						Where<
							BAccount.bAccountID, Equal<Current<Document.bAccountID>>>>
						.SelectSingleBound(Base, new object[] { DocumentView.Current });
				if (baccount.Type == BAccountType.VendorType || baccount.Type == BAccountType.ProspectType)
				{
					WebDialogResult result = DocumentView.View.Ask(masterEntity, Messages.AskConfirmation, Messages.InvoiceRequiredConvertBusinessAccountToCustomerAccount, MessageButtons.YesNo, MessageIcon.Question);
					if (result == WebDialogResult.Yes)
					{
						PXLongOperation.StartOperation(this, () => ConvertToCustomerAccount(baccount, masterEntity));
					}

					return adapter.Get();
				}
			}

			if (customer != null)
			{
				if (CreateInvoicesParams.View.Answer == WebDialogResult.None)
				{
					CreateInvoicesParams.Cache.Clear();
					CreateInvoicesParams.Cache.Insert();
				}

				if (this.CreateInvoicesParams.AskExtFullyValid((graph, viewName) => { }, DialogAnswerType.Positive))
				{
					Base.Actions.PressSave();

					var graph = Base.CloneGraphState();

					PXLongOperation.StartOperation(Base, delegate()
					{
						var extension = graph.GetProcessingExtension<CRCreateInvoice<TDiscountExt, TGraph, TMaster>>();
						extension.DoCreateInvoice();
					});
				}
			}

			return adapter.Get();
		}

		protected virtual void DoCreateInvoice()
		{
			CreateInvoicesFilter filter = this.CreateInvoicesParams.Current;
			Document masterEntity = this.DocumentView.Current;

			if (filter == null || masterEntity == null)
				return;

			ARInvoiceEntry docgraph = PXGraph.CreateInstance<ARInvoiceEntry>();

			DoCreateInvoice(docgraph, masterEntity, filter);
		}

		protected virtual void DoCreateInvoice(ARInvoiceEntry docgraph, Document masterEntity, CreateInvoicesFilter filter)
		{
			bool recalcAny =
				filter.RecalculatePrices == true 
				|| filter.RecalculateDiscounts == true 
				|| filter.OverrideManualDiscounts == true 
				|| filter.OverrideManualDocGroupDiscounts == true 
				|| filter.OverrideManualPrices == true;

			Customer customer = (Customer)PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<Document.bAccountID>>>>.Select(Base);
			docgraph.customer.Current = customer;

			ARInvoice invoice = new ARInvoice();
			invoice.DocType = ARDocType.Invoice;
			invoice.CuryID = masterEntity.CuryID;
			invoice.CuryInfoID = masterEntity.CuryInfoID;
			invoice.DocDate = masterEntity.CloseDate;
			invoice.Hold = true;
			invoice.BranchID = masterEntity.BranchID;
			invoice.CustomerID = masterEntity.BAccountID;
			invoice.ProjectID = masterEntity.ProjectID;
			invoice = PXCache<ARInvoice>.CreateCopy(docgraph.Document.Insert(invoice));

			invoice.TermsID = masterEntity.TermsID ?? customer.TermsID;
			invoice.InvoiceNbr = masterEntity.OpportunityID;
			invoice.DocDesc = masterEntity.Subject;
			invoice.CustomerLocationID = masterEntity.LocationID ?? customer.DefLocationID;

			#region Shipping Details

			CRShippingContact _crShippingContact = Base.Caches[typeof(CRShippingContact)].Current as CRShippingContact;
			ARShippingContact _shippingContact = docgraph.Shipping_Contact.Select();
			if (_shippingContact != null && _crShippingContact != null)
			{
				_crShippingContact.RevisionID = _crShippingContact.RevisionID ?? _shippingContact.RevisionID;
				if (_shippingContact.RevisionID != _crShippingContact.RevisionID)
				{
					_crShippingContact.IsDefaultContact = false;
				}
				_crShippingContact.BAccountContactID = _crShippingContact.BAccountContactID ?? _shippingContact.CustomerContactID;
				ContactAttribute.CopyRecord<ARInvoice.shipContactID>(docgraph.Document.Cache, invoice, _crShippingContact, true);
			}

			CRShippingAddress _crShippingAddress = Base.Caches[typeof(CRShippingAddress)].Current as CRShippingAddress;
			ARShippingAddress _shippingAddress = docgraph.Shipping_Address.Select();
			if (_shippingAddress != null && _crShippingAddress != null)
			{
				_crShippingAddress.RevisionID = _crShippingAddress.RevisionID ?? _shippingAddress.RevisionID;
				if (_shippingAddress.RevisionID != _crShippingAddress.RevisionID)
				{
					_crShippingAddress.IsDefaultAddress = false;
				}
				_crShippingAddress.BAccountAddressID = _crShippingAddress.BAccountAddressID ?? _shippingAddress.CustomerAddressID;
				AddressAttribute.CopyRecord<ARInvoice.shipAddressID>(docgraph.Document.Cache, invoice, _crShippingAddress, true);
			}

			#endregion

			#region Billing Details

			CRBillingContact _crBillingContact = Base.Caches[typeof(CRBillingContact)].Current as CRBillingContact;
			ARContact _arContactContact = docgraph.Billing_Contact.Select();
			if (_arContactContact != null && _crBillingContact != null)
			{
				_crBillingContact.RevisionID = _crBillingContact.RevisionID ?? _arContactContact.RevisionID;
				if (_arContactContact.RevisionID != _crBillingContact.RevisionID)
				{
					_crBillingContact.OverrideContact = true;
				}
				_crBillingContact.BAccountContactID = _crBillingContact.BAccountContactID ?? _arContactContact.BAccountContactID;
				ContactAttribute.CopyRecord<ARInvoice.billContactID>(docgraph.Document.Cache, invoice, _crBillingContact, true);
			}

			CRBillingAddress _crBillingAddress = Base.Caches[typeof(CRBillingAddress)].Current as CRBillingAddress;
			ARAddress _arAddressAddress = docgraph.Billing_Address.Select();
			if (_arAddressAddress != null && _crBillingAddress != null)
			{
				_crBillingAddress.RevisionID = _crBillingAddress.RevisionID ?? _arAddressAddress.RevisionID;
				if (_arAddressAddress.RevisionID != _crBillingAddress.RevisionID)
				{
					_crBillingAddress.OverrideAddress = true;
				}
				_crBillingAddress.BAccountAddressID = _crBillingAddress.BAccountAddressID ?? _arAddressAddress.BAccountAddressID;
				AddressAttribute.CopyRecord<ARInvoice.billAddressID>(docgraph.Document.Cache, invoice, _crBillingAddress, true);
			}

			#endregion

			if (masterEntity.ManualTotalEntry == true)
				recalcAny = false;

			#region Tax Info

			
			if (masterEntity.TaxZoneID != null)
			{
				invoice.TaxZoneID = masterEntity.TaxZoneID;
				if (!recalcAny && masterEntity.ManualTotalEntry != true)
					TaxAttribute.SetTaxCalc<ARTran.taxCategoryID>(docgraph.Transactions.Cache, null, TaxCalc.ManualCalc);
			}
			invoice.TaxCalcMode = masterEntity.TaxCalcMode;
			invoice.ExternalTaxExemptionNumber = masterEntity.ExternalTaxExemptionNumber;
			invoice.AvalaraCustomerUsageType = masterEntity.AvalaraCustomerUsageType;
						
			invoice = docgraph.Document.Update(invoice);

			#endregion

			#region Relation: Opportunity -> Invoice

			var opportunity = (CROpportunity)PXSelectReadonly<CROpportunity, Where<CROpportunity.opportunityID, Equal<Current<Document.opportunityID>>>>.Select(Base);
			var relation = docgraph.RelationsLink.Insert();
			relation.RefNoteID = invoice.NoteID;
			relation.RefEntityType = invoice.GetType().FullName;
			relation.Role = CRRoleTypeList.Source;
			relation.TargetType = CRTargetEntityType.CROpportunity;
			relation.TargetNoteID = opportunity.NoteID;
			relation.DocNoteID = opportunity.NoteID;
			relation.EntityID = opportunity.BAccountID;
			relation.ContactID = opportunity.ContactID;
			docgraph.RelationsLink.Update(relation);

			#endregion

			#region Relation: Primary/Current Quote (Source) -> Invoice

			var quote = (CRQuote)PXSelectReadonly<CRQuote, Where<CRQuote.quoteID, Equal<Current<Document.quoteID>>>>.Select(Base);
			if (quote != null)
			{
				var invoiceQuoteRelation = docgraph.RelationsLink.Insert();
				invoiceQuoteRelation.RefNoteID = invoice.NoteID;
				invoiceQuoteRelation.RefEntityType = invoice.GetType().FullName;
				invoiceQuoteRelation.Role = CRRoleTypeList.Source;
				invoiceQuoteRelation.TargetType = CRTargetEntityType.CRQuote;
				invoiceQuoteRelation.TargetNoteID = quote.NoteID;
				invoiceQuoteRelation.DocNoteID = quote.NoteID;
				invoiceQuoteRelation.EntityID = quote.BAccountID;
				invoiceQuoteRelation.ContactID = quote.ContactID;
				docgraph.RelationsLink.Update(invoiceQuoteRelation);
			}

			#endregion

			if (masterEntity.ManualTotalEntry == true)
			{
				ARTran tran = new ARTran();
				tran.Qty = 1;
				tran.CuryUnitPrice = masterEntity.CuryAmount;
				tran = docgraph.Transactions.Insert(tran);
				if (tran != null)
				{
					tran.CuryDiscAmt = masterEntity.CuryDiscTot;

					using (new PXLocaleScope(customer.LocaleName))
					{
						tran.TranDesc = PXMessages.LocalizeNoPrefix(Messages.ManualAmount);
					}
				}
				tran = docgraph.Transactions.Update(tran);
			}
			else
			{
				foreach (PXResult<CROpportunityProducts, InventoryItem> res in PXSelectJoin<CROpportunityProducts,
					LeftJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<CROpportunityProducts.inventoryID>>>,
					Where<CROpportunityProducts.quoteID, Equal<Current<Document.quoteID>>,
					And<InventoryItem.stkItem, Equal<False>>>>
				.Select(Base))
				{
					CROpportunityProducts product = (CROpportunityProducts)res;
					InventoryItem item = (InventoryItem)res;

					ARTran tran = new ARTran();
					tran = docgraph.Transactions.Insert(tran);
					if (tran != null)
					{
						tran.InventoryID = product.InventoryID;
						using (new PXLocaleScope(customer.LocaleName))
						{
							tran.TranDesc = PXDBLocalizableStringAttribute.GetTranslation(Base.Caches[typeof(CROpportunityProducts)],
														  product, typeof(CROpportunityProducts.descr).Name, Base.Culture.Name);
						}

						tran.Qty = product.Quantity;
						tran.UOM = product.UOM;
						tran.CuryUnitPrice = product.CuryUnitPrice;
						tran.IsFree = product.IsFree;
						tran.SortOrder = product.SortOrder;

						tran.CuryTranAmt = product.CuryAmount;
						tran.TaxCategoryID = product.TaxCategoryID;
						tran.ProjectID = product.ProjectID;
						tran.TaskID = product.TaskID;
						tran.CostCodeID = product.CostCodeID;

						if (filter.RecalculatePrices != true)
						{
							tran.ManualPrice = true;
						}
						else
						{
							if (filter.OverrideManualPrices != true)
								tran.ManualPrice = product.ManualPrice;
							else
								tran.ManualPrice = false;
						}

						if (filter.RecalculateDiscounts != true)
						{
							tran.ManualDisc = true;
						}
						else
						{
							if (filter.OverrideManualDiscounts != true)
								tran.ManualDisc = product.ManualDisc;
							else
								tran.ManualDisc = false;
						}

						tran.CuryDiscAmt = product.CuryDiscAmt;
						tran.DiscAmt = product.DiscAmt;
						tran.DiscPct = product.DiscPct;

						if (item.Commisionable.HasValue)
						{
							tran.Commissionable = item.Commisionable;
						}
					}

					tran = docgraph.Transactions.Update(tran);

					PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(CROpportunityProducts)], product, docgraph.Transactions.Cache, tran, Base.Caches[typeof(CRSetup)].Current as PXNoteAttribute.IPXCopySettings);
				}
			}
			PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(TMaster)], masterEntity.Base, docgraph.Document.Cache, invoice, Base.Caches[typeof(CRSetup)].Current as PXNoteAttribute.IPXCopySettings);

			//Skip all customer dicounts
			if (filter.RecalculateDiscounts != true && filter.OverrideManualDiscounts != true)
			{
				var discounts = new Dictionary<string, ARInvoiceDiscountDetail>();
				foreach (ARInvoiceDiscountDetail discountDetail in docgraph.ARDiscountDetails.Select())
				{
					docgraph.ARDiscountDetails.SetValueExt<ARInvoiceDiscountDetail.skipDiscount>(discountDetail, true);
					string key = discountDetail.Type + ':' + discountDetail.DiscountID + ':' + discountDetail.DiscountSequenceID;
					discounts.Add(key, discountDetail);
				}

				foreach (Discount discount in Base1.Discounts.Select())
				{
					CROpportunityDiscountDetail discountDetail = discount.Base as CROpportunityDiscountDetail;
					ARInvoiceDiscountDetail detail;
					string key = discountDetail.Type + ':' + discountDetail.DiscountID + ':' + discountDetail.DiscountSequenceID;
					if (discounts.TryGetValue(key, out detail))
					{
						docgraph.ARDiscountDetails.SetValueExt<ARInvoiceDiscountDetail.skipDiscount>(detail, false);
						if (discountDetail.IsManual == true && discountDetail.Type == DiscountType.Document)
						{
							docgraph.ARDiscountDetails.SetValueExt<ARInvoiceDiscountDetail.extDiscCode>(detail, discountDetail.ExtDiscCode);
							docgraph.ARDiscountDetails.SetValueExt<ARInvoiceDiscountDetail.description>(detail, discountDetail.Description);
							docgraph.ARDiscountDetails.SetValueExt<ARInvoiceDiscountDetail.isManual>(detail, discountDetail.IsManual);
							docgraph.ARDiscountDetails.SetValueExt<ARInvoiceDiscountDetail.curyDiscountAmt>(detail, discountDetail.CuryDiscountAmt);
						}
					}
					else
					{
						detail = (ARInvoiceDiscountDetail)docgraph.ARDiscountDetails.Cache.CreateInstance();
						detail.Type = discountDetail.Type;
						detail.DiscountID = discountDetail.DiscountID;
						detail.DiscountSequenceID = discountDetail.DiscountSequenceID;
						detail.ExtDiscCode = discountDetail.ExtDiscCode;
						detail.Description = discountDetail.Description;
						detail = (ARInvoiceDiscountDetail)docgraph.ARDiscountDetails.Cache.Insert(detail);
						if (discountDetail.IsManual == true && (discountDetail.Type == DiscountType.Document || discountDetail.Type == DiscountType.ExternalDocument))
						{
							detail.CuryDiscountAmt = discountDetail.CuryDiscountAmt;
							detail.IsManual = discountDetail.IsManual;
							docgraph.ARDiscountDetails.Cache.Update(detail);
						}
					}
				}
				ARInvoice old_row = PXCache<ARInvoice>.CreateCopy(docgraph.Document.Current);
				docgraph.Document.Cache.SetValueExt<ARInvoice.curyDiscTot>(docgraph.Document.Current, DiscountEngineProvider.GetEngineFor<ARTran, ARInvoiceDiscountDetail>().GetTotalGroupAndDocumentDiscount(docgraph.ARDiscountDetails));
				docgraph.Document.Cache.RaiseRowUpdated(docgraph.Document.Current, old_row);
				invoice = docgraph.Document.Update(invoice);
			}

			if (masterEntity.TaxZoneID != null && !recalcAny)
			{
				foreach (CRTaxTran tax in PXSelect<CRTaxTran,
					Where<CRTaxTran.quoteID, Equal<Current<Document.quoteID>>>>.Select(Base))
				{
					if (masterEntity.TaxZoneID == null)
					{
						Base.Caches[typeof(Document)].RaiseExceptionHandling<Document.taxZoneID>(
							masterEntity, null,
								new PXSetPropertyException<Document.taxZoneID>(ErrorMessages.FieldIsEmpty,
									$"[{nameof(Document.taxZoneID)}]"));
					}

					ARTaxTran new_artax = new ARTaxTran();
					new_artax.TaxID = tax.TaxID;

					new_artax = docgraph.Taxes.Insert(new_artax);

					if (new_artax != null)
					{
						new_artax = PXCache<ARTaxTran>.CreateCopy(new_artax);
						new_artax.TaxRate = tax.TaxRate;
						new_artax.TaxBucketID = 0;
						new_artax.CuryTaxableAmt = tax.CuryTaxableAmt;
						new_artax.CuryTaxAmt = tax.CuryTaxAmt;
						new_artax = docgraph.Taxes.Update(new_artax);
					}
				}
			}

			if (recalcAny)
			{
				docgraph.recalcdiscountsfilter.Current.OverrideManualPrices = filter.OverrideManualPrices == true;
				docgraph.recalcdiscountsfilter.Current.RecalcDiscounts = filter.RecalculateDiscounts == true;
				docgraph.recalcdiscountsfilter.Current.RecalcUnitPrices = filter.RecalculatePrices == true;
				docgraph.recalcdiscountsfilter.Current.OverrideManualDiscounts = filter.OverrideManualDiscounts == true;
				docgraph.recalcdiscountsfilter.Current.OverrideManualDocGroupDiscounts = filter.OverrideManualDocGroupDiscounts == true;

				docgraph.Actions[nameof(ARInvoiceEntry.RecalculateDiscountsAction)].Press();
			}

			invoice.CuryOrigDocAmt = invoice.CuryDocBal;
			invoice.Hold = true;
			UDFHelper.CopyAttributes(Base.Caches[typeof(TMaster)], masterEntity.Base, docgraph.Document.Cache, docgraph.Document.Cache.Current, invoice.DocType);
			docgraph.Document.Update(invoice);

			docgraph.customer.Current.CreditRule = customer.CreditRule;

			if (GetQuoteForWorkflowProcessing() is CRQuote workflowQuote)
			{
				docgraph.Caches[typeof(CRQuote)].Hold(workflowQuote);
			}

			if (!Base.IsContractBasedAPI)
				throw new PXRedirectRequiredException(docgraph, "");

			docgraph.Save.Press();
		}

		public virtual void ConvertToCustomerAccount(BAccount account, Document opportunity)
		{
			BusinessAccountMaint accountMaint = PXGraph.CreateInstance<BusinessAccountMaint>();
			accountMaint.BAccount.Insert(account);
			accountMaint.BAccount.Current = account;
			accountMaint.BAccount.Cache.SetStatus(accountMaint.BAccount.Current, PXEntryStatus.Updated);

			accountMaint.Actions[nameof(BusinessAccountMaint.ExtendToCustomer.extendToCustomer)].Press();
		}

		public virtual long? CopyCurrenfyInfo(PXGraph graph, long? SourceCuryInfoID)
		{
			CM.CurrencyInfo curryInfo = CM.CurrencyInfo.PK.Find(graph, SourceCuryInfoID);
			curryInfo.CuryInfoID = null;
			graph.Caches[typeof(CM.CurrencyInfo)].Clear();
			curryInfo = (CM.CurrencyInfo)graph.Caches[typeof(CM.CurrencyInfo)].Insert(curryInfo);
			return curryInfo.CuryInfoID;
		}

		public abstract CRQuote GetQuoteForWorkflowProcessing();

		#endregion
	}
}
