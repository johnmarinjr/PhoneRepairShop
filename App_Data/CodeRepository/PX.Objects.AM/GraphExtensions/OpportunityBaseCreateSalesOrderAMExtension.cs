using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.AM.CacheExtensions;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CR.Extensions.CRCreateSalesOrder;
using PX.Objects.IN;
using PX.Objects.SO;

namespace PX.Objects.AM.GraphExtensions
{
    using AM;
    using AM.Attributes;

    public class OpportunityMaintCreateSalesOrderAMExtension : OpportunityBaseCreateSalesOrderAMExtensionBase<OpportunityMaint.CRCreateSalesOrderExt, OpportunityMaintAMExtension, OpportunityMaint, CROpportunity>
    {
        public static bool IsActive() => IsExtensionActive();

        protected virtual void CROpportunity_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected del)
        {
            del?.Invoke(sender, e);

            if (!Base1.AllowEstimates || !Base1.ContainsEstimates)
            {
                return;
            }

            // enable create sales order as the base call will disable if no products entered
            // Including the other logic for create sales order - removing the checks for products
            CRQuote primaryQt = Base.PrimaryQuoteQuery.SelectSingle();
            bool hasQuotes = primaryQt != null;
            Base.Actions[nameof(OpportunityMaint.CRCreateSalesOrderExt.CreateSalesOrder)].SetEnabled(!hasQuotes || primaryQt.QuoteType == CRQuoteTypeAttribute.Distribution);
        }
    }

    public class QuoteMaintCreateSalesOrderAMExtension : OpportunityBaseCreateSalesOrderAMExtensionBase<QuoteMaint.CRCreateSalesOrderExt, QuoteMaintAMExtension, QuoteMaint, CRQuote>
    {
        public static bool IsActive() => IsExtensionActive();

        protected virtual void CRQuote_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected del)
        {
            del?.Invoke(sender, e);

            CRQuote row = e.Row as CRQuote;
            if (row == null)
                return;

            if (!Base1.AllowEstimates || !Base1.ContainsEstimates)
            {
                return;
            }

            // enable create sales order as the base call will disable if no products entered
            // Including the other logic for create sales order - removing the checks for products
            Base.Actions[nameof(OpportunityMaint.CRCreateSalesOrderExt.CreateSalesOrder)].SetEnabled(row.QuoteType == CRQuoteTypeAttribute.Distribution);
        }
    }

    public class OpportunityBaseCreateSalesOrderAMExtensionBase<TBaseExtension, Extension1, Graph, TPrimary> : PXGraphExtension<TBaseExtension, Extension1, Graph>
        where TBaseExtension : PXGraphExtension<Graph>
        where Extension1 : OpportunityBaseAMExtension<Graph, TPrimary>
        where Graph : PXGraph
        where TPrimary : class, IBqlTable, new()
    {
        public static bool IsExtensionActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturingEstimating>()
                   || PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturingProductConfigurator>();
        }

        /// <summary>
        /// Indicates if estimates are allowed for Order type from the create order process
        /// </summary>
        public bool CreateSalesOrderTypeAllowsEstimates
        {
            get
            {
                SOOrderType orderType = PXSelect<SOOrderType,
                    Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>
                    >>.Select(Base, (Base.Caches[typeof(CreateSalesOrderFilter)].Current as CreateSalesOrderFilter)?.OrderType);

                var orderTypeExt = orderType.GetExtension<SOOrderTypeExt>();

                if (orderTypeExt != null)
                {
                    return orderTypeExt.AMEstimateEntry.GetValueOrDefault();
                }

                return false;
            }
        }

        /// <summary>
        /// Indicates if configurations are allowed for Order type from the create order process
        /// </summary>
        public bool CreateSalesOrderFilterAllowsConfigurations
        {
            get
            {
                SOOrderType orderType = PXSelect<SOOrderType,
                    Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>
                    >>.Select(Base, (Base.Caches[typeof(CreateSalesOrderFilter)].Current as CreateSalesOrderFilter)?.OrderType);

                var orderTypeExt = orderType.GetExtension<SOOrderTypeExt>();

                if (orderTypeExt != null)
                {
                    return orderTypeExt.AMConfigurationEntry.GetValueOrDefault() && Base1.AllowConfigurations;
                }

                return false;
            }
        }

        public virtual void CreateSalesOrderFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e, PXRowSelected del)
        {
            del?.Invoke(sender, e);

            var row = (CreateSalesOrderFilter)e.Row;
            if (row == null)
            {
                return;
            }

            PXUIFieldAttribute.SetVisible<CRCreateSalesOrderFilterExt.aMIncludeEstimate>(sender, row, Base1.AllowEstimates && Base1.ContainsEstimates);
            PXUIFieldAttribute.SetEnabled<CRCreateSalesOrderFilterExt.aMIncludeEstimate>(sender, row, Base1.ContainsEstimates);
            PXUIFieldAttribute.SetVisible<CRCreateSalesOrderFilterExt.aMCopyConfigurations>(sender, row, CreateSalesOrderFilterAllowsConfigurations);
        }

        [PXOverride]
        public virtual void DoCreateSalesOrder(Action del)
        {
            if (del == null)
            {
                return;
            }

            CreateSalesOrderFilter filter = Base.Caches[typeof(CreateSalesOrderFilter)].Current as CreateSalesOrderFilter;

            var paramExtension = filter?.GetExtension<CRCreateSalesOrderFilterExt>();

            var canConvertEstimates = paramExtension != null && paramExtension.AMIncludeEstimate.GetValueOrDefault() && Base1.ContainsEstimates;
            if (canConvertEstimates && !Base1.CurrentEstimatesValidForSalesDetails(out var ex))
            {
                if (ex == null)
                {
                    var entity = Base.Caches[typeof(Document)].Current as Document;
                    ex = new PXException(AM.Messages.UnableToConvertEstimatesForOpportunity, entity == null ? "?" : entity.OpportunityID.TrimIfNotNullEmpty());
                }
                throw ex;
            }

            //Add estimate to document details tab if the order type doesn't allow estimates
            if(canConvertEstimates && !CreateSalesOrderTypeAllowsEstimates && Base1.AllowEstimates)
            {
                PXGraph.InstanceCreated.AddHandler<SOOrderEntry>(graph =>
                {
                    graph.RowInserted.AddHandler<SOOrder>((cache, e) =>
                    {
                        // SetTaxCalc required when adding to order from opportunity but in no other add to order scenario. 
                        //  This is due to taxes not working when adding from oppiroty but does work when from sales order or estimate. 
                        SOTaxAttribute.SetTaxCalc<SOLine.taxCategoryID>(graph.Transactions.Cache, null, PX.Objects.TX.TaxCalc.ManualCalc);
                        SOTaxAttribute.SetTaxCalc<SOOrder.freightTaxCategoryID>(graph.Document.Cache, null, PX.Objects.TX.TaxCalc.ManualCalc);

                        Base1.AddEstimatesToSalesOrder(graph);
                    });
                });
            }

            if (paramExtension != null && paramExtension.AMCopyConfigurations.GetValueOrDefault() && CreateSalesOrderFilterAllowsConfigurations)
            {
                PXGraph.InstanceCreated.AddHandler<SOOrderEntry>(graph =>
                {

                    graph.RowInserting.AddHandler<SOLine>((cache, args) =>
                    {
                        var soLine = (SOLine)args.Row;
                        if (soLine == null)
                        {
                            return;
                        }
                        CROpportunityProducts opProduct = PXResult<CROpportunityProducts>.Current;
                        if (opProduct == null)
                        {
                            return;
                        }

                        var opProductExt = PXCache<CROpportunityProducts>.GetExtension<CROpportunityProductsExt>(opProduct);
                        var soLineExt = PXCache<SOLine>.GetExtension<SOLineExt>(soLine);
                        soLineExt.AMOrigParentLineNbr = opProductExt.AMParentLineNbr;
                        soLineExt.AMIsSupplemental = opProductExt.AMIsSupplemental;

                        if (!opProductExt.AMIsSupplemental.GetValueOrDefault())
                        {
                            return;
                        }

                        // ***
                        // *** Continue only for supplemental line items coming from an opp. product
                        // ***

                        var parent = ConfigSupplementalItemsHelper.FindParentConfigLineByOrigParentLineNbr(cache, soLine);
                        if (parent == null)
                        {
                            return;
                        }

                        soLineExt.AMParentLineNbr = parent.LineNbr;
                        soLine.SortOrder = parent.SortOrder;
#if DEBUG
                        AMDebug.TraceWriteMethodName($"SOLine[{soLine.OrderType}-{soLine.OrderNbr}-{soLine.LineNbr}] AMOrigParentLineNbr={opProductExt.AMParentLineNbr} ; AMParentLineNbr={soLineExt.AMParentLineNbr}");
#endif
                        //Check for inserted supps already and delete those lines that match by parent (no orig parent) and inventory item
                        foreach (var supSoLine in ConfigSupplementalItemsHelper.GetInsertedSupplementalLinesByParent((SOOrderEntry)cache.Graph, parent.LineNbr))
                        {
                            var supSoLineExt = supSoLine.GetExtension<SOLineExt>();
                            if (supSoLine.InventoryID == opProduct.InventoryID
                                //Sups inserted from config copy do not have an orig line pointing to a product from opportunity... we want only these sups...
                                && supSoLineExt != null && supSoLineExt.AMOrigParentLineNbr == null)
                            {
                                var copy = (SOLine)cache.CreateCopy(supSoLine);
                                copy.InventoryID = opProduct.InventoryID;
                                copy.SubItemID = opProduct.SubItemID;
                                copy.TranDesc = opProduct.Descr;
                                copy.OrderQty = opProduct.Quantity;
                                copy.UOM = opProduct.UOM;
                                copy.CuryUnitPrice = opProduct.CuryUnitPrice;
                                //copy.CuryLineAmt = opProduct.CuryAmount;
                                copy.TaxCategoryID = opProduct.TaxCategoryID;
                                copy.SiteID = opProduct.SiteID;
                                copy.IsFree = opProduct.IsFree;
                                copy.ProjectID = opProduct.ProjectID;
                                copy.TaskID = opProduct.TaskID;
                                copy.ManualPrice = filter?.RecalculatePrices != true;
                                copy.ManualDisc = filter?.RecalculateDiscounts != true;
                                copy.CuryDiscAmt = opProduct.CuryDiscAmt;
                                copy.DiscAmt = opProduct.DiscAmt;
                                copy.DiscPct = opProduct.DiscPct;

                                var copyExt = copy.GetExtension<SOLineExt>();
                                if (copyExt != null)
                                {
                                    copyExt.AMOrigParentLineNbr = opProductExt.AMParentLineNbr;
                                }
                                copy = (SOLine)cache.Update(copy);

                                PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(CROpportunityProducts)], opProduct, cache, copy, Base.Caches[typeof(CRSetup)].Current as PXNoteAttribute.IPXCopySettings);
                            }
                        }

                        //CANCEL ALL COPIED SUPS FROM PRODUCTS AS THE COPY CONFIG WILL RE-ADD THE CORRECT SUPS TO ACCOUNT FOR CONFIG CHANGES...
                        args.Cancel = true;
                    });
                });

                PXGraph.InstanceCreated.AddHandler<SOOrderEntry>(graph =>
                {
                    graph.RowUpdated.AddHandler<SOLine>((cache, args) =>
                    {
                        var row = (SOLine)args.Row;
                        var graphExt = cache.Graph.GetExtension<SOOrderEntryAMExtension>();
                        if (row == null || graphExt?.ItemConfiguration == null)
                        {
                            return;
                        }

                        var rowExt = row.GetExtension<SOLineExt>();
                        if (rowExt == null)
                        {
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(rowExt.AMConfigurationID))
                        {
                            //Most likely not a configured line...
                            return;
                        }

                        graphExt.ItemConfiguration.RemoveSOLineHandlers();

                        CROpportunityProducts opProduct = PXResult<CROpportunityProducts>.Current;
                        if (opProduct == null)
                        {
                            return;
                        }
#if DEBUG
                        AMDebug.TraceWriteMethodName($"SOLine[{row.GetRowKeyValues(graph)}] Product[{opProduct.GetRowKeyValues(graph)}] IsSup={rowExt.AMIsSupplemental}");
#endif

                        AMConfigurationResults fromConfigResult = PXSelect<
                            AMConfigurationResults,
                            Where<AMConfigurationResults.opportunityQuoteID, Equal<Required<AMConfigurationResults.opportunityQuoteID>>,
                                And<AMConfigurationResults.opportunityLineNbr, Equal<Required<AMConfigurationResults.opportunityLineNbr>>>>>
                            .Select(cache.Graph, opProduct.QuoteID, opProduct.LineNbr);

                        if (fromConfigResult == null)
                        {
                            return;
                        }

                        AMConfigurationResults toConfigResult = graphExt.ItemConfiguration.Select();

                        if (toConfigResult == null)
                        {
                            return;
                        }

                        graphExt.CopyConfiguration(row, toConfigResult, fromConfigResult);
                    });
                });
            }

            del?.Invoke();
        }
    }
}
