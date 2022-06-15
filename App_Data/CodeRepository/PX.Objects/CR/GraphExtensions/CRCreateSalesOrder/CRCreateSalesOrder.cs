using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.Common.Discount;
using PX.Objects.CS;
using PX.Objects.Extensions.Discount;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.TX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.CR.Extensions.CRCreateSalesOrder
{
	#region SOOrderEntry extension

	public class CRCreateSalesOrder_SOOrderEntry : PXGraphExtension<SOOrderEntry>
	{
		public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.customerModule>();

		#region Events

		public virtual void _(Events.RowPersisting<SOOrder> e)
		{
			var row = e.Row as SOOrder;
			if (row == null || e.Operation != PXDBOperation.Insert)
				return;

			var cache = Base.Caches[typeof(CRQuote)];

			foreach (var quote in cache.Cached)
			{
				if (cache.GetStatus(quote) != PXEntryStatus.Held)
					continue;

				Base.EnsureCachePersistence<CRQuote>();

				SOOrder.Events.Select(o => o.OrderCreatedFromQuote).FireOn(Base, row, quote as CRQuote);
			}
		}

		#endregion
	}

	#endregion

	public abstract class CRCreateSalesOrder<TDiscountExt, TGraph, TMaster> : PXGraphExtension<TDiscountExt, TGraph>
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
			public Type ProjectID = typeof(Document.projectID);
			public Type BranchID = typeof(Document.branchID);
			public Type NoteID = typeof(Document.noteID);
			public Type TermsID = typeof(Document.termsID);
			public Type ExternalTaxExemptionNumber = typeof(Document.externalTaxExemptionNumber);
			public Type AvalaraCustomerUsageType = typeof(Document.avalaraCustomerUsageType);
			public Type IsPrimary = typeof(Document.isPrimary);
			public Type SiteID = typeof(Document.siteID);
			public Type CarrierID = typeof(Document.carrierID);
			public Type ShipTermsID = typeof(Document.shipTermsID);
			public Type ShipZoneID = typeof(Document.shipZoneID);
			public Type FOBPointID = typeof(Document.fOBPointID);
			public Type Resedential = typeof(Document.resedential);
			public Type SaturdayDelivery = typeof(Document.saturdayDelivery);
			public Type Insurance = typeof(Document.insurance);
			public Type ShipComplete = typeof(Document.shipComplete);
		}

		protected virtual DocumentMapping GetDocumentMapping()
		{
			return new DocumentMapping(typeof(TMaster));
		}

		#endregion

		public PXSelectExtension<Document> DocumentView;

		[PXCopyPasteHiddenView]
		[PXViewName(Messages.CreateSalesOrder)]
		public CRPopupFilter<CreateSalesOrderFilter> CreateOrderParams;

		#endregion

		#region Initialization

		protected static bool IsExtensionActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
		}

		#endregion

		#region Events

		public virtual void _(Events.RowUpdated<CreateSalesOrderFilter> e)
		{
			CreateSalesOrderFilter row = e.Row as CreateSalesOrderFilter;
			if (row == null)
				return;

			if (!row.RecalculatePrices.GetValueOrDefault())
			{
				CreateOrderParams.Cache.SetValue<CreateSalesOrderFilter.overrideManualPrices>(row, false);
			}
			if (!row.RecalculateDiscounts.GetValueOrDefault())
			{
				CreateOrderParams.Cache.SetValue<CreateSalesOrderFilter.overrideManualDiscounts>(row, false);
				CreateOrderParams.Cache.SetValue<CreateSalesOrderFilter.overrideManualDocGroupDiscounts>(row, false);
			}
		}

		public virtual void _(Events.RowSelected<CreateSalesOrderFilter> e)
		{
			CreateSalesOrderFilter row = e.Row as CreateSalesOrderFilter;
			Document masterEntity = DocumentView.Current;

			if (row == null || masterEntity == null)
				return;

			PXUIFieldAttribute.SetEnabled<CreateSalesOrderFilter.overrideManualPrices>(e.Cache, row, row.RecalculatePrices == true);
			PXUIFieldAttribute.SetEnabled<CreateSalesOrderFilter.overrideManualDiscounts>(e.Cache, row, row.RecalculateDiscounts == true);
			PXUIFieldAttribute.SetEnabled<CreateSalesOrderFilter.overrideManualDocGroupDiscounts>(e.Cache, row, row.RecalculateDiscounts == true);
			PXUIFieldAttribute.SetVisible<CreateSalesOrderFilter.confirmManualAmount>(e.Cache, row, masterEntity.ManualTotalEntry == true);
			PXUIFieldAttribute.SetEnabled<CreateSalesOrderFilter.makeQuotePrimary>(e.Cache, row, masterEntity.IsPrimary == false);
		}

		public virtual void _(Events.FieldVerifying<CreateSalesOrderFilter, CreateSalesOrderFilter.confirmManualAmount> e)
		{
			CreateSalesOrderFilter row = e.Row as CreateSalesOrderFilter;
			Document masterEntity = DocumentView.Current;

			if (row == null || masterEntity == null)
				return;

			if (masterEntity.ManualTotalEntry == true && e.NewValue as bool? != true)
			{
				e.Cache.RaiseExceptionHandling<CreateSalesOrderFilter.confirmManualAmount>(row, e.NewValue,
					new PXSetPropertyException(Messages.ConfirmSalesOrderCreation, PXErrorLevel.Error));

				throw new PXSetPropertyException(Messages.ConfirmSalesOrderCreation, PXErrorLevel.Error);
			}
		}

		public virtual void _(Events.FieldUpdated<CreateSalesOrderFilter, CreateSalesOrderFilter.confirmManualAmount> e)
		{
			if (e.Row == null)
				return;

			e.Cache.RaiseExceptionHandling<CreateSalesOrderFilter.confirmManualAmount>(e.Row, null, null);
		}

		#endregion

		#region Actions

		public PXAction<TMaster> CreateSalesOrder;

		[PXUIField(DisplayName = Messages.CreateSalesOrder, MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable createSalesOrder(PXAdapter adapter)
		{
			Customer customer = PXSelect<
					Customer,
				Where<
					Customer.bAccountID, Equal<Current<Document.bAccountID>>>>
				.SelectSingleBound(Base, new object[] { DocumentView.Current });

			if (customer == null)
			{
				throw new PXException(Messages.ProspectNotCustomer);
			}

			if (this.CreateOrderParams.AskExtFullyValid((graph, viewName) => { }, DialogAnswerType.Positive))
			{
				Base.Actions.PressSave();

				var graph = Base.CloneGraphState();

				PXLongOperation.StartOperation(Base, delegate()
				{
					var extension = graph.GetProcessingExtension<CRCreateSalesOrder<TDiscountExt, TGraph, TMaster>>();

					extension.DoCreateSalesOrder();
				});
			}

			return adapter.Get();
		}

		public virtual void DoCreateSalesOrder()
		{
			CreateSalesOrderFilter filter = this.CreateOrderParams.Current;
			Document masterEntity = this.DocumentView.Current;

			if (filter == null || masterEntity == null)
				return;

			SOOrderEntry docgraph = PXGraph.CreateInstance<SOOrderEntry>();

			DoCreateSalesOrder(docgraph, masterEntity, filter);
		}


		public virtual void DoCreateSalesOrder(SOOrderEntry docgraph, Document masterEntity, CreateSalesOrderFilter filter)
		{
			bool recalcAny =
				filter.RecalculatePrices == true
				|| filter.RecalculateDiscounts == true
				|| filter.OverrideManualDiscounts == true
				|| filter.OverrideManualDocGroupDiscounts == true
				|| filter.OverrideManualPrices == true;

			Customer customer = (Customer)PXSelect<Customer, Where<Customer.bAccountID, Equal<Current<Document.bAccountID>>>>.Select(Base);
			SOOrder salesOrder = new SOOrder();

			salesOrder.OrderType = CreateOrderParams.Current.OrderType ?? SOOrderTypeConstants.SalesOrder;
			salesOrder = docgraph.Document.Insert(salesOrder);
			salesOrder = PXCache<SOOrder>.CreateCopy(docgraph.Document.Search<SOOrder.orderNbr>(salesOrder.OrderNbr));

			salesOrder.CuryID = masterEntity.CuryID;
			salesOrder.CuryInfoID = CopyCurrenfyInfo(docgraph, masterEntity.CuryInfoID);

			salesOrder.OrderDate = Base.Accessinfo.BusinessDate;
			salesOrder.OrderDesc = masterEntity.Subject;
			salesOrder.TermsID = masterEntity.TermsID ?? customer.TermsID;
			salesOrder.CustomerID = masterEntity.BAccountID;
			salesOrder.CustomerLocationID = masterEntity.LocationID ?? customer.DefLocationID;
			salesOrder.ContactID = masterEntity.ContactID;

			salesOrder = docgraph.Document.Update(salesOrder);

			#region Shipping Details

			CRShippingContact _crShippingContact = Base.Caches[typeof(CRShippingContact)].Current as CRShippingContact;
			SOShippingContact _shippingContact = docgraph.Shipping_Contact.Select();
			if (_shippingContact != null && _crShippingContact != null)
			{
				_crShippingContact.RevisionID = _crShippingContact.RevisionID ?? _shippingContact.RevisionID;
				if (_shippingContact.RevisionID != _crShippingContact.RevisionID)
				{
					_crShippingContact.IsDefaultContact = false;
				}
				_crShippingContact.BAccountContactID = _crShippingContact.BAccountContactID ?? _shippingContact.CustomerContactID;

				ContactAttribute.CopyRecord<SOOrder.shipContactID>(docgraph.Document.Cache, salesOrder, _crShippingContact, true);
			}

			CRShippingAddress _crShippingAddress = Base.Caches[typeof(CRShippingAddress)].Current as CRShippingAddress;
			SOShippingAddress _shippingAddress = docgraph.Shipping_Address.Select();
			if (_shippingAddress != null && _crShippingAddress != null)
			{
				_crShippingAddress.RevisionID = _crShippingAddress.RevisionID ?? _shippingAddress.RevisionID;
				if (_shippingAddress.RevisionID != _crShippingAddress.RevisionID)
				{
					_crShippingAddress.IsDefaultAddress = false;
				}
				_crShippingAddress.BAccountAddressID = _crShippingAddress.BAccountAddressID ?? _shippingAddress.CustomerAddressID;

				AddressAttribute.CopyRecord<SOOrder.shipAddressID>(docgraph.Document.Cache, salesOrder, _crShippingAddress, true);
			}

			#endregion

			#region Billing Details

			CRBillingContact _crBillingContact = Base.Caches[typeof(CRBillingContact)].Current as CRBillingContact;
			SOBillingContact _soBillingContact = docgraph.Billing_Contact.Select();
			if (_soBillingContact != null && _crBillingContact != null)
			{
				_crBillingContact.RevisionID = _crBillingContact.RevisionID ?? _soBillingContact.RevisionID;
				if (_soBillingContact.RevisionID != _crBillingContact.RevisionID)
				{
					_crBillingContact.OverrideContact = true;
				}
				_crBillingContact.BAccountContactID = _crBillingContact.BAccountContactID ?? _soBillingContact.BAccountContactID;
				ContactAttribute.CopyRecord<ARInvoice.billContactID>(docgraph.Document.Cache, salesOrder, _crBillingContact, true);
			}

			CRBillingAddress _crBillingAddress = Base.Caches[typeof(CRBillingAddress)].Current as CRBillingAddress;
			SOBillingAddress _soBillingAddress = docgraph.Billing_Address.Select();
			if (_soBillingAddress != null && _crBillingAddress != null)
			{
				_crBillingAddress.RevisionID = _crBillingAddress.RevisionID ?? _soBillingAddress.RevisionID;
				if (_soBillingAddress.RevisionID != _crBillingAddress.RevisionID)
				{
					_crBillingAddress.OverrideAddress = true;
				}
				_crBillingAddress.BAccountAddressID = _crBillingAddress.BAccountAddressID ?? _soBillingAddress.BAccountAddressID;
				AddressAttribute.CopyRecord<ARInvoice.billAddressID>(docgraph.Document.Cache, salesOrder, _crBillingAddress, true);
			}

			#endregion

			if (masterEntity.TaxZoneID != null)
			{
				salesOrder.TaxZoneID = masterEntity.TaxZoneID;

				if (!recalcAny)
				{
					SOTaxAttribute.SetTaxCalc<SOLine.taxCategoryID>(docgraph.Transactions.Cache, null, TaxCalc.ManualCalc);
					SOTaxAttribute.SetTaxCalc<SOOrder.freightTaxCategoryID>(docgraph.Document.Cache, null, TaxCalc.ManualCalc);
				}
			}

			salesOrder.TaxCalcMode = masterEntity.TaxCalcMode;
			salesOrder.ExternalTaxExemptionNumber = masterEntity.ExternalTaxExemptionNumber;
			salesOrder.AvalaraCustomerUsageType = masterEntity.AvalaraCustomerUsageType;
			salesOrder.ProjectID = masterEntity.ProjectID;
			salesOrder.BranchID = masterEntity.BranchID;

			#region Shipping Instructions group
			salesOrder.DefaultSiteID = masterEntity.SiteID;
			salesOrder.ShipVia = masterEntity.CarrierID;
			salesOrder.ShipTermsID = masterEntity.ShipTermsID;
			salesOrder.ShipZoneID = masterEntity.ShipZoneID;
			salesOrder.FOBPoint = masterEntity.FOBPointID;
			salesOrder.Resedential = masterEntity.Resedential;
			salesOrder.SaturdayDelivery = masterEntity.SaturdayDelivery;
			salesOrder.Insurance = masterEntity.Insurance;
			salesOrder.ShipComplete = masterEntity.ShipComplete;
			#endregion

			salesOrder = docgraph.Document.Update(salesOrder);

			docgraph.customer.Current.CreditRule = customer.CreditRule;

			#region Relation: Opportunity -> Sales order

			var opportunity = (CROpportunity)PXSelectReadonly<CROpportunity, Where<CROpportunity.opportunityID, Equal<Current<Document.opportunityID>>>>.Select(Base);
			var relation = docgraph.RelationsLink.Insert();

			relation.RefNoteID = salesOrder.NoteID;
			relation.RefEntityType = salesOrder.GetType().FullName;
			relation.Role = CRRoleTypeList.Source;
			relation.TargetType = CRTargetEntityType.CROpportunity;
			relation.TargetNoteID = opportunity.NoteID;
			relation.DocNoteID = opportunity.NoteID;
			relation.EntityID = opportunity.BAccountID;
			relation.ContactID = opportunity.ContactID;

			docgraph.RelationsLink.Update(relation);

			#endregion

			#region Relation: Primary/Current Quote (Source) -> Sales order

			var quote = (CRQuote)PXSelectReadonly<CRQuote, Where<CRQuote.quoteID, Equal<Current<Document.quoteID>>>>.Select(Base);
			if (quote != null)
			{
				var salesOrderQuoteRelation = docgraph.RelationsLink.Insert();
				salesOrderQuoteRelation.RefNoteID = salesOrder.NoteID;
				salesOrderQuoteRelation.RefEntityType = salesOrder.GetType().FullName;
				salesOrderQuoteRelation.Role = CRRoleTypeList.Source;
				salesOrderQuoteRelation.TargetType = CRTargetEntityType.CRQuote;
				salesOrderQuoteRelation.TargetNoteID = quote.NoteID;
				salesOrderQuoteRelation.DocNoteID = quote.NoteID;
				salesOrderQuoteRelation.EntityID = quote.BAccountID;
				salesOrderQuoteRelation.ContactID = quote.ContactID;
				docgraph.RelationsLink.Update(salesOrderQuoteRelation);
			}

			#endregion

			#region Lines

			bool failed = false;
			foreach (CROpportunityProducts product in PXSelectJoin<
						CROpportunityProducts,
					InnerJoin<InventoryItem,
						On<InventoryItem.inventoryID, Equal<CROpportunityProducts.inventoryID>>>,
					Where<
						CROpportunityProducts.quoteID, Equal<Current<Document.quoteID>>>>
				.Select(Base))
			{
				if (product.SiteID == null)
				{
					InventoryItem item = (InventoryItem)PXSelectorAttribute.Select<CROpportunityProducts.inventoryID>(Base.Caches[typeof(CROpportunityProducts)], product);

					if (item != null && item.NonStockShip == true)
					{
						Base.Caches[typeof(CROpportunityProducts)].RaiseExceptionHandling<CROpportunityProducts.siteID>(product, null,
							new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(CROpportunityProducts.siteID)}]"));

						failed = true;
					}
				}

				var line = docgraph.Transactions.Insert();

				if (line != null)
				{
					line.InventoryID = product.InventoryID;
					line.SubItemID = product.SubItemID;
					line.TranDesc = product.Descr;
					line.OrderQty = product.Quantity;
					line.UOM = product.UOM;
					line.CuryUnitPrice = product.CuryUnitPrice;
					line.CuryExtPrice = product.CuryExtPrice;
					line.TaxCategoryID = product.TaxCategoryID;
					line.SiteID = product.SiteID;
					line.IsFree = product.IsFree;
					line.ProjectID = product.ProjectID;
					line.TaskID = product.TaskID;
					line.CostCodeID = product.CostCodeID;
					line.ManualPrice = true;
					line.ManualDisc = true;
					line.CuryDiscAmt = product.CuryDiscAmt;
					line.DiscAmt = product.DiscAmt;
					line.DiscPct = product.DiscPct;
					line.POCreate = product.POCreate;
					line.VendorID = product.VendorID;
					line.SortOrder = product.SortOrder;

					if (filter.RecalculatePrices != true)
					{
						line.ManualPrice = true;
					}
					else
					{
						if (filter.OverrideManualPrices != true)
							line.ManualPrice = product.ManualPrice;
						else
							line.ManualPrice = false;
					}

					if (filter.RecalculateDiscounts != true)
					{
						line.ManualDisc = true;
					}
					else
					{
						if (filter.OverrideManualDiscounts != true)
							line.ManualDisc = product.ManualDisc;
						else
							line.ManualDisc = false;
					}

					line.CuryDiscAmt = product.CuryDiscAmt;
					line.DiscAmt = product.DiscAmt;
					line.DiscPct = product.DiscPct;
				}

				line = docgraph.Transactions.Update(line);

				PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(CROpportunityProducts)], product, docgraph.Transactions.Cache, line, Base.Caches[typeof(CRSetup)].Current as PXNoteAttribute.IPXCopySettings);
			}

			#endregion

			PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(TMaster)], masterEntity.Base, docgraph.Document.Cache, salesOrder, Base.Caches[typeof(CRSetup)].Current as PXNoteAttribute.IPXCopySettings);

			if (failed)
				throw new PXException(Messages.SiteNotDefined);

			#region Discounts

			//Skip all customer dicounts
			if (filter.RecalculateDiscounts != true && filter.OverrideManualDiscounts != true)
			{
				var discounts = new Dictionary<string, SOOrderDiscountDetail>();

				foreach (SOOrderDiscountDetail discountDetail in docgraph.DiscountDetails.Select())
				{
					docgraph.DiscountDetails.SetValueExt<SOOrderDiscountDetail.skipDiscount>(discountDetail, true);
					string key = discountDetail.Type + ':' + discountDetail.DiscountID + ':' + discountDetail.DiscountSequenceID;
					discounts.Add(key, discountDetail);
				}

				foreach (Discount discount in Base1.Discounts.Select())
				{
					CROpportunityDiscountDetail discountDetail = discount.Base as CROpportunityDiscountDetail;
					SOOrderDiscountDetail detail;
					string key = discountDetail.Type + ':' + discountDetail.DiscountID + ':' + discountDetail.DiscountSequenceID;
					if (discounts.TryGetValue(key, out detail))
					{
						docgraph.DiscountDetails.SetValueExt<SOOrderDiscountDetail.skipDiscount>(detail, false);

						if (discountDetail.IsManual == true && discountDetail.Type == DiscountType.Document)
						{
							docgraph.DiscountDetails.SetValueExt<SOOrderDiscountDetail.extDiscCode>(detail, discountDetail.ExtDiscCode);
							docgraph.DiscountDetails.SetValueExt<SOOrderDiscountDetail.description>(detail, discountDetail.Description);
							docgraph.DiscountDetails.SetValueExt<SOOrderDiscountDetail.isManual>(detail, discountDetail.IsManual);
							docgraph.DiscountDetails.SetValueExt<SOOrderDiscountDetail.curyDiscountAmt>(detail, discountDetail.CuryDiscountAmt);
						}
					}
					else
					{
						detail = (SOOrderDiscountDetail)docgraph.DiscountDetails.Cache.CreateInstance();
						detail.Type = discountDetail.Type;
						detail.DiscountID = discountDetail.DiscountID;
						detail.DiscountSequenceID = discountDetail.DiscountSequenceID;
						detail.ExtDiscCode = discountDetail.ExtDiscCode;
						detail.Description = discountDetail.Description;
						detail = (SOOrderDiscountDetail)docgraph.DiscountDetails.Cache.Insert(detail);
						if (discountDetail.IsManual == true && (discountDetail.Type == DiscountType.Document || discountDetail.Type == DiscountType.ExternalDocument))
						{
							detail.CuryDiscountAmt = discountDetail.CuryDiscountAmt;
							detail.IsManual = discountDetail.IsManual;
							docgraph.DiscountDetails.Cache.Update(detail);
						}
					}
				}
				SOOrder old_row = PXCache<SOOrder>.CreateCopy(docgraph.Document.Current);
				docgraph.Document.Cache.SetValueExt<SOOrder.curyDiscTot>(docgraph.Document.Current, DiscountEngineProvider.GetEngineFor<SOLine, SOOrderDiscountDetail>().GetTotalGroupAndDocumentDiscount(docgraph.DiscountDetails));
				docgraph.Document.Cache.RaiseRowUpdated(docgraph.Document.Current, old_row);
			}

			#endregion

			UDFHelper.CopyAttributes(Base.Caches[typeof(TMaster)], masterEntity.Base, docgraph.Document.Cache, docgraph.Document.Cache.Current, salesOrder.OrderType);
			salesOrder = docgraph.Document.Update(salesOrder);

			#region Taxes

			if (masterEntity.TaxZoneID != null && !recalcAny)
			{
				foreach (CRTaxTran tax in PXSelect<CRTaxTran, Where<CRTaxTran.quoteID, Equal<Current<Document.quoteID>>>>.Select(Base))
				{
					SOTaxTran newtax = new SOTaxTran();
					newtax.OrderType = salesOrder.OrderType;
					newtax.OrderNbr = salesOrder.OrderNbr;
					newtax.TaxID = tax.TaxID;
					newtax.TaxRate = 0m;

					foreach (SOTaxTran existingTaxTran in docgraph
						.Taxes.Cache
						.Cached
						.RowCast<SOTaxTran>()
						.Where(a =>
							!docgraph.Taxes.Cache.GetStatus(a).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted)
							&& docgraph.Taxes.Cache.ObjectsEqual<SOTaxTran.orderNbr, SOTaxTran.orderType, SOTaxTran.taxID>(newtax, a)
						))
					{
						docgraph.Taxes.Delete(existingTaxTran);
					}

					newtax = docgraph.Taxes.Insert(newtax);
				}
			}

			#endregion

			if (recalcAny)
			{
				docgraph.recalcdiscountsfilter.Current.OverrideManualPrices = filter.OverrideManualPrices == true;
				docgraph.recalcdiscountsfilter.Current.RecalcDiscounts = filter.RecalculateDiscounts == true;
				docgraph.recalcdiscountsfilter.Current.RecalcUnitPrices = filter.RecalculatePrices == true;
				docgraph.recalcdiscountsfilter.Current.OverrideManualDiscounts = filter.OverrideManualDiscounts == true;
				docgraph.recalcdiscountsfilter.Current.OverrideManualDocGroupDiscounts = filter.OverrideManualDocGroupDiscounts == true;

				docgraph.Actions[nameof(SOOrderEntry.RecalculateDiscountsAction)].Press();
			}

			if (GetQuoteForWorkflowProcessing() is CRQuote workflowQuote)
			{
				docgraph.Caches[typeof(CRQuote)].Hold(workflowQuote);
			}

			if (!Base.IsContractBasedAPI)
				throw new PXRedirectRequiredException(docgraph, "");

			docgraph.Save.Press();
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
