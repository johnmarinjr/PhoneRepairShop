using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.CM.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.IN;
using PX.SM;
using System;
using System.Collections;
using System.Linq;
using PX.Objects.CR.Workflows;
using System.Collections.Generic;

namespace PX.Objects.FS
{
    public class SM_OpportunityMaint : PXGraphExtension<CR.OpportunityMaint.Discount, OpportunityMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }


        [PXHidden]
        public PXSelect<FSSetup> SetupRecord;

        [PXHidden]
        public PXSetup<CRSetup> CRSetupRecord;

        public PXSetup<FSSrvOrdType>.Where<
               Where<
                   FSSrvOrdType.srvOrdType, Equal<Optional<FSxCROpportunity.srvOrdType>>>> ServiceOrderTypeSelected;

        [PXCopyPasteHiddenView]
        public PXFilter<FSCreateServiceOrderFilter> CreateServiceOrderFilter;

        #region CacheAttached

        #region CROpportunityProducts_CuryUnitPrice
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXDBCurrencyPriceCost(typeof(CROpportunityProducts.curyInfoID), typeof(CROpportunityProducts.unitPrice))]
        [PXUIField(DisplayName = "Unit Price", Visibility = PXUIVisibility.SelectorVisible)]
        [PXFormula(typeof(Switch<
                                Case<
                                    Where<
                                        Current<CROpportunityProducts.stockItemType>, Equal<INItemTypes.serviceItem>,
                                        And<
                                            FSxCROpportunityProducts.billingRule, Equal<FSxCROpportunityProducts.billingRule.None>>>,
                                    SharedClasses.decimal_0>,
                                CROpportunityProducts.curyUnitPrice>))]
        public virtual void CROpportunityProducts_CuryUnitPrice_CacheAttached(PXCache sender) { }
        #endregion

        #region CROpportunityProducts_CuryExtPrice
        [PXDBCurrency(typeof(CROpportunityProducts.curyInfoID), typeof(CROpportunityProducts.extPrice))]
        [PXUIField(DisplayName = "Ext. Price")]
        [PXFormula(typeof(Switch<
                                Case<
                                    Where<
                                        Current<CROpportunityProducts.stockItemType>, Equal<INItemTypes.serviceItem>,
                                        And<
                                            FSxCROpportunityProducts.billingRule, Equal<FSxCROpportunityProducts.billingRule.None>>>,
                                    SharedClasses.decimal_0>,
                                Mult<CROpportunityProducts.curyUnitPrice, CROpportunityProducts.quantity>>))]
        [PXDefault(TypeCode.Decimal, "0.0")]
        public virtual void CROpportunityProducts_CuryExtPrice_CacheAttached(PXCache sender) { }
        #endregion
        #region CROpportunityProducts_UnitCost
        [PXDBPriceCost()]
        [PXDefault(TypeCode.Decimal, "0.0", typeof(Coalesce<
                                                        Search2<INItemSite.tranUnitCost,
                                                            InnerJoin<InventoryItem,
                                                                    On<InventoryItem.inventoryID, Equal<INItemSite.inventoryID>>>,
                                                            Where<INItemSite.inventoryID, Equal<Current<CROpportunityProducts.inventoryID>>,
                                                                And<INItemSite.siteID, Equal<Current<CROpportunityProducts.siteID>>,
                                                                    And<InventoryItem.stkItem, Equal<True>>>>>,
                                                        Search2<InventoryItemCurySettings.stdCost,
                                                            InnerJoin<InventoryItem, On<InventoryItem.inventoryID, Equal<InventoryItemCurySettings.inventoryID>>>,
                                                            Where<InventoryItemCurySettings.inventoryID, Equal<Current<CROpportunityProducts.inventoryID>>,
                                                                And<InventoryItemCurySettings.curyID, EqualBaseCuryID<Current2<CROpportunity.branchID>>,
                                                                And<InventoryItem.stkItem, Equal<False>>>>>>))]
        public virtual void CROpportunityProducts_UnitCost_CacheAttached(PXCache sender) { }
        #endregion
        #region CROpportunityProducts_CuryUnitCost
        [PXDBCurrencyPriceCost(typeof(CROpportunityProducts.curyInfoID), typeof(CROpportunityProducts.unitCost))]
        [PXUIField(DisplayName = "Unit Cost")]
        [PXDefault(TypeCode.Decimal, "0.0")]
        [PXFormula(typeof(Default<CROpportunityProducts.pOCreate>))]
        public virtual void CROpportunityProducts_CuryUnitCost_CacheAttached(PXCache sender) { }
        #endregion

        #region FSCreateServiceOrderFilter_SrvOrdType
        #region SrvOrdType
        [PXDefault(typeof(Coalesce<
            Search<FSxUserPreferences.dfltSrvOrdType,
            Where<
                PX.SM.UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>>>,
            Search<FSSetup.dfltSrvOrdType>>), PersistingCheck = PXPersistingCheck.NullOrBlank)]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        protected virtual void FSCreateServiceOrderFilter_SrvOrdType_CacheAttached(PXCache sender)
        {
        }
        #endregion
        #endregion
        #endregion

        #region WorkflowChanges
        public class WorkflowChanges : PXGraphExtension<OpportunityWorkflow, OpportunityMaint>
        {
            public static bool IsActive() => SM_OpportunityMaint.IsActive();

            public override void Configure(PXScreenConfiguration config)
            {
                Configure(config.GetScreenConfigurationContext<OpportunityMaint, CROpportunity>());
            }

            protected virtual void Configure(WorkflowContext<OpportunityMaint, CROpportunity> context)
            {
                var categoryServices = context.Categories.CreateNew(OpportunityWorkflow.CategoryNames.Services,
                    c => c.DisplayName(OpportunityWorkflow.CategoryDisplayNames.Services));

                var actionCreateServiceOrder = context.ActionDefinitions.CreateExisting<SM_OpportunityMaint_DBox>(
                    e => e.CreateSrvOrdDocument,
                    a => a.WithCategory(categoryServices));

                var actionViewServiceOrder = context.ActionDefinitions.CreateExisting<SM_OpportunityMaint>(
                    e => e.ViewServiceOrder,
                    a => a
                        .WithCategory(categoryServices)
                        .PlaceAfter(actionCreateServiceOrder));

                var actionOpenAppointmentBoard = context.ActionDefinitions.CreateExisting<SM_OpportunityMaint>(
                    e => e.OpenAppointmentBoard,
                    a => a
                        .WithCategory(categoryServices, actionViewServiceOrder)
                        .PlaceAfter(actionViewServiceOrder));

                context.UpdateScreenConfigurationFor(config =>
                {
                    return config
                        .WithActions(a =>
                        {
                            a.Add(actionViewServiceOrder);
                            a.Add(actionOpenAppointmentBoard);
                        });
                });
            }
        }
        #endregion

        #region Methods

        public FSSetup GetFSSetup()
        {
            if (SetupRecord.Current == null)
            {
                return SetupRecord.Select();
            }
            else
            {
                return SetupRecord.Current;
            }
        }

        public virtual void EnableDisableExtensionFields(PXCache cache, FSxCROpportunity fsxCROpportunityRow, FSServiceOrder fsServiceOrderRow)
        {
            if (fsxCROpportunityRow == null)
                return;

            bool isSMSetup = GetFSSetup() != null;

            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.sDEnabled>(cache, null, isSMSetup && fsServiceOrderRow == null);
            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.srvOrdType>(cache, null, isSMSetup && fsxCROpportunityRow.SDEnabled == true && fsServiceOrderRow == null);
            PXUIFieldAttribute.SetEnabled<FSxCROpportunity.branchLocationID>(cache, null, isSMSetup && fsxCROpportunityRow.SDEnabled == true);
        }

        public virtual void EnableDisableActions(PXCache cache,
                                                 CROpportunity crOpportunityRow,
                                                 FSxCROpportunity fsxCROpportunityRow,
                                                 FSServiceOrder fsServiceOrderRow,
                                                 FSSrvOrdType fsSrvOrdTypeRow)
        {
            bool isSMSetup = GetFSSetup() != null;
            bool insertedStatus = Base.Opportunity.Cache.GetStatus(Base.Opportunity.Current) == PXEntryStatus.Inserted;

            ViewServiceOrder.SetEnabled(isSMSetup && crOpportunityRow != null && crOpportunityRow.OpportunityID != null && fsServiceOrderRow != null);
            OpenAppointmentBoard.SetEnabled(fsServiceOrderRow != null);

            if (fsServiceOrderRow != null)
            {
                Base.Actions[nameof(OpportunityMaint.CRCreateSalesOrderExt.CreateSalesOrder)].SetEnabled(false);
                Base.Actions[nameof(OpportunityMaint.CRCreateInvoiceExt.CreateInvoice)].SetEnabled(false);
            }
        }

        public virtual void SetPersistingChecks(PXCache cache,
                                                CROpportunity crOpportunityRow,
                                                FSxCROpportunity fsxCROpportunityRow,
                                                FSSrvOrdType fsSrvOrdTypeRow)
        {
            if (fsxCROpportunityRow.SDEnabled == true)
            {
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.srvOrdType>(cache, crOpportunityRow, PXPersistingCheck.NullOrBlank);
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.branchLocationID>(cache, crOpportunityRow, PXPersistingCheck.NullOrBlank);

                if (fsSrvOrdTypeRow == null)
                {
                    fsSrvOrdTypeRow = CRExtensionHelper.GetServiceOrderType(Base, fsxCROpportunityRow.SrvOrdType);
                }

                if (fsSrvOrdTypeRow != null
                        && fsSrvOrdTypeRow.Behavior != FSSrvOrdType.behavior.Values.InternalAppointment)
                {
                    PXDefaultAttribute.SetPersistingCheck<CROpportunity.bAccountID>(cache, crOpportunityRow, PXPersistingCheck.NullOrBlank);
                    PXDefaultAttribute.SetPersistingCheck<CROpportunity.locationID>(cache, crOpportunityRow, PXPersistingCheck.NullOrBlank);
                }
            }
            else
            {
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.srvOrdType>(cache, crOpportunityRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<FSxCROpportunity.branchLocationID>(cache, crOpportunityRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<CROpportunity.bAccountID>(cache, crOpportunityRow, PXPersistingCheck.Nothing);
                PXDefaultAttribute.SetPersistingCheck<CROpportunity.locationID>(cache, crOpportunityRow, PXPersistingCheck.Nothing);
            }
        }

        public virtual void SetBranchLocationID(PXGraph graph, CROpportunity crOpportunityRow, FSxCROpportunity fsxCROpportunityRow)
        {
            if (crOpportunityRow.BranchID != null)
            {
                UserPreferences userPreferencesRow =
                    PXSelect<UserPreferences,
                    Where<
                        UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>>>.Select(graph);

                if (userPreferencesRow != null
                        && userPreferencesRow.DefBranchID == crOpportunityRow.BranchID)
                {
                    FSxUserPreferences fsxUserPreferencesRow = PXCache<UserPreferences>.GetExtension<FSxUserPreferences>(userPreferencesRow);

                    if (fsxUserPreferencesRow != null)
                    {
                        fsxCROpportunityRow.BranchLocationID = fsxUserPreferencesRow.DfltBranchLocationID;
                    }
                }
                else
                {
                    fsxCROpportunityRow.BranchLocationID = null;
                }
            }
            else
            {
                fsxCROpportunityRow.BranchLocationID = null;
            }
        }

        public virtual void HideOrShowFieldsActionsByInventoryFeature()
        {
            bool isInventoryFeatureInstalled = PXAccess.FeatureInstalled<FeaturesSet.inventory>();

            PXUIVisibility visibility = isInventoryFeatureInstalled ? PXUIVisibility.Visible : PXUIVisibility.Invisible;

            PXUIFieldAttribute.SetVisibility<CROpportunityProducts.pOCreate>(Base.Products.Cache, null, visibility);
            PXUIFieldAttribute.SetVisibility<CROpportunityProducts.vendorID>(Base.Products.Cache, null, visibility);
            PXUIFieldAttribute.SetVisibility<CROpportunityProducts.curyUnitCost>(Base.Products.Cache, null, visibility);
            PXUIFieldAttribute.SetVisibility<FSxCROpportunityProducts.vendorLocationID>(Base.Products.Cache, null, visibility);

            PXUIFieldAttribute.SetVisible<CROpportunityProducts.pOCreate>(Base.Products.Cache, null, isInventoryFeatureInstalled);
            PXUIFieldAttribute.SetVisible<CROpportunityProducts.vendorID>(Base.Products.Cache, null, isInventoryFeatureInstalled);
            PXUIFieldAttribute.SetVisible<CROpportunityProducts.curyUnitCost>(Base.Products.Cache, null, isInventoryFeatureInstalled);
            PXUIFieldAttribute.SetVisible<FSxCROpportunityProducts.vendorLocationID>(Base.Products.Cache, null, isInventoryFeatureInstalled);
        }

        #endregion

        #region Actions
        public PXMenuInquiry<CROpportunity> InqueriesMenu;
        [PXButton(MenuAutoOpen = true, SpecialType = PXSpecialButtonType.InquiriesFolder)]
        [PXUIField(DisplayName = "Inquiries")]
        public virtual IEnumerable inqueriesMenu(PXAdapter adapter)
        {
            return adapter.Get();
        }

        public PXAction<CROpportunity> ViewServiceOrder;
        [PXButton]
        [PXUIField(DisplayName = "View Service Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void viewServiceOrder()
        {
            if (Base.Opportunity == null || Base.Opportunity.Current == null)
            {
                return;
            }

            if (Base.IsDirty)
            {
                Base.Save.Press();
            }

            FSxCROpportunity fsxCROpportunityRow = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(Base.Opportunity.Current);

            if (fsxCROpportunityRow != null && fsxCROpportunityRow.SOID != null)
            {
                CRExtensionHelper.LaunchServiceOrderScreen(Base, fsxCROpportunityRow.SOID);
            }
        }

        public PXAction<CROpportunity> OpenAppointmentBoard;
        [PXButton]
        [PXUIField(DisplayName = "Schedule on the Calendar Board", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        public virtual void openAppointmentBoard()
        {
            if (Base.Opportunity == null || Base.Opportunity.Current == null)
            {
                return;
            }

            if (Base.IsDirty)
            {
                Base.Save.Press();
            }

            FSxCROpportunity fsxCROpportunityRow = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(Base.Opportunity.Current);

            if (fsxCROpportunityRow != null && fsxCROpportunityRow.SOID != null)
            {
                CRExtensionHelper.LaunchEmployeeBoard(Base, fsxCROpportunityRow.SOID);
            }
        }

        #endregion

        #region Events

        #region CROpportunity

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<CROpportunity, FSxCROpportunity.sDEnabled> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunity crOpportunityRow = (CROpportunity)e.Row;
            FSxCROpportunity fsxCROpportunityRow = e.Cache.GetExtension<FSxCROpportunity>(crOpportunityRow);

            if (fsxCROpportunityRow.SDEnabled == true)
            {
                FSSetup fsSetupRow = GetFSSetup();

                if (fsSetupRow != null
                        && fsSetupRow.DfltOpportunitySrvOrdType != null)
                {
                    fsxCROpportunityRow.SrvOrdType = fsSetupRow.DfltOpportunitySrvOrdType;
                }

                SetBranchLocationID(Base, crOpportunityRow, fsxCROpportunityRow);
                Base.Opportunity.SetValueExt<CROpportunity.allowOverrideContactAddress>(crOpportunityRow, true);
            }
            else
            {
                fsxCROpportunityRow.BranchLocationID = null;
            }
        }

        protected virtual void _(Events.FieldUpdated<CROpportunity, CROpportunity.branchID> e)
        {
            if (e.Row == null)
            {
                return;
            }
            CROpportunity crOpportunityRow = (CROpportunity)e.Row;
            FSxCROpportunity fsxCROpportunityRow = e.Cache.GetExtension<FSxCROpportunity>(crOpportunityRow);
            SetBranchLocationID(Base, crOpportunityRow, fsxCROpportunityRow);
        }

        #endregion

        protected virtual void _(Events.RowSelecting<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowSelected<CROpportunity> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunity crOpportunityRow = (CROpportunity)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunity fsxCROpportunityRow = cache.GetExtension<FSxCROpportunity>(crOpportunityRow);

            FSServiceOrder fsServiceOrderRow = CRExtensionHelper.GetRelatedServiceOrder(Base, cache, crOpportunityRow, fsxCROpportunityRow.SOID);
            FSSrvOrdType fsSrvOrdTypeRow = null;

            if (fsServiceOrderRow != null)
            {
                fsSrvOrdTypeRow = CRExtensionHelper.GetServiceOrderType(Base, fsServiceOrderRow.SrvOrdType);
            }

            EnableDisableExtensionFields(cache, fsxCROpportunityRow, fsServiceOrderRow);
            EnableDisableActions(cache, crOpportunityRow, fsxCROpportunityRow, fsServiceOrderRow, fsSrvOrdTypeRow);
            SetPersistingChecks(cache, crOpportunityRow, fsxCROpportunityRow, fsSrvOrdTypeRow);

            HideOrShowFieldsActionsByInventoryFeature();
        }

        protected virtual void _(Events.RowInserting<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowInserted<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowUpdating<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowUpdated<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowDeleting<CROpportunity> e)
        {
            if (e.Row == null || SharedFunctions.isFSSetupSet(Base) == false)
            {
                return;
            }

            FSxCROpportunity fsxCROpportunityRow = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(Base.Opportunity.Current);

            if (fsxCROpportunityRow != null 
                && fsxCROpportunityRow.SOID != null
                && Base.Opportunity.Ask(TX.Warning.OpportunityLinkedToFSDocument, MessageButtons.OKCancel) != WebDialogResult.OK)
            {
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.RowDeleted<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowPersisting<CROpportunity> e)
        {
        }

        protected virtual void _(Events.RowPersisted<CROpportunity> e)
        {
        }

        #endregion
        #region CROpportunityProducts

        #region FieldSelecting
        #endregion
        #region FieldDefaulting

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, CROpportunityProducts.quantity> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null
                && fsxCROpportunityProductsRow.BillingRule == ID.BillingRule.TIME
                && fsxCROpportunityProductsRow.EstimatedDuration != null
                && fsxCROpportunityProductsRow.EstimatedDuration > 0)
            {
                e.NewValue = fsxCROpportunityProductsRow.EstimatedDuration / 60m;
            }
            else
            {
                e.NewValue = 0m;
            }
        }

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, CROpportunityProducts.curyUnitCost> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (string.IsNullOrEmpty(crOpportunityProductsRow.UOM) == false && crOpportunityProductsRow.InventoryID != null && crOpportunityProductsRow.POCreate == true)
            {
                object unitcost;
                cache.RaiseFieldDefaulting<CROpportunityProducts.unitCost>(e.Row, out unitcost);

                if (unitcost != null && (decimal)unitcost != 0m)
                {
                    decimal newval = INUnitAttribute.ConvertToBase<CROpportunityProducts.inventoryID, CROpportunityProducts.uOM>(cache, crOpportunityProductsRow, (decimal)unitcost, INPrecision.NOROUND);

                    IPXCurrencyHelper currencyHelper = Base.FindImplementation<IPXCurrencyHelper>();

                    if (currencyHelper != null)
                    {
                        newval = currencyHelper.GetDefaultCurrencyInfo().CuryConvCury((decimal)unitcost);
                    }
                    else
                    {
                        CM.PXDBCurrencyAttribute.CuryConvCury(cache, crOpportunityProductsRow, newval, out newval, true);
                    }

                    e.NewValue = Math.Round(newval, CommonSetupDecPl.PrcCst, MidpointRounding.AwayFromZero);
                    e.Cancel = true;
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, FSxCROpportunityProducts.billingRule> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (crOpportunityProductsRow.InventoryID != null)
            {
                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(Base, crOpportunityProductsRow.InventoryID);

                if (inventoryItemRow.ItemType == INItemTypes.ServiceItem)
                {
                    FSxService fsxServiceRow = PXCache<InventoryItem>.GetExtension<FSxService>(inventoryItemRow);
                    e.NewValue = fsxServiceRow?.BillingRule;
                    e.Cancel = true;
                }
                else
                {
                    e.NewValue = ID.BillingRule.FLAT_RATE;
                    e.Cancel = true;
                }
            }
        }

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, FSxCROpportunityProducts.vendorLocationID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (crOpportunityProductsRow.POCreate == false || crOpportunityProductsRow.InventoryID == null)
            {
                e.Cancel = true;
            }
        }

        protected virtual void _(Events.FieldDefaulting<CROpportunityProducts, FSxCROpportunityProducts.estimatedDuration> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null)
            {
                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(Base, crOpportunityProductsRow.InventoryID);

                if (inventoryItemRow != null)
                {
                    FSxService fsxServiceRow = PXCache<InventoryItem>.GetExtension<FSxService>(inventoryItemRow);

                    if (inventoryItemRow.ItemType == INItemTypes.ServiceItem
                            || inventoryItemRow.ItemType == INItemTypes.NonStockItem)
                    {

                        e.NewValue = fsxServiceRow?.EstimatedDuration;
                        e.Cancel = true;
                    }
                }
            }
        }

        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, FSxCROpportunityProducts.estimatedDuration> e)
        {
            if (e.Row == null || Base.IsImportFromExcel == true || Base.IsImport == true)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            FSxCROpportunityProducts fsxCROpportunityProductsRow = e.Cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow.EstimatedDuration != null && fsxCROpportunityProductsRow.BillingRule == ID.BillingRule.TIME)
            {
                e.Cache.SetDefaultExt<CROpportunityProducts.quantity>(crOpportunityProductsRow);
            }
        }

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, FSxCROpportunityProducts.billingRule> e)
        {
            if (e.Row == null || Base.IsImportFromExcel == true || Base.IsImport == true)
            {
                return;
            }

            e.Cache.SetDefaultExt<CROpportunityProducts.quantity>(e.Row);
        }

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, CROpportunityProducts.uOM> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null && Base.IsImportFromExcel == false)
            {
                cache.SetDefaultExt<CROpportunityProducts.curyUnitCost>(e.Row);
            }
        }

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, CROpportunityProducts.siteID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null && Base.IsImportFromExcel == false)
            {
                cache.SetDefaultExt<CROpportunityProducts.curyUnitCost>(e.Row);
            }
        }

        protected virtual void _(Events.FieldUpdated<CROpportunityProducts, CROpportunityProducts.inventoryID> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null)
            {
                if (Base.IsImportFromExcel == false)
                {
                    cache.SetDefaultExt<CROpportunityProducts.curyUnitCost>(e.Row);
                }

                cache.SetDefaultExt<FSxCROpportunityProducts.billingRule>(e.Row);
            }
        }

        #endregion

        protected virtual void _(Events.RowSelecting<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowSelected<CROpportunityProducts> e)
        {
            if (e.Row == null)
            {
                return;
            }

            CROpportunityProducts crOpportunityProductsRow = (CROpportunityProducts)e.Row;
            PXCache cache = e.Cache;

            FSxCROpportunityProducts fsxCROpportunityProductsRow = cache.GetExtension<FSxCROpportunityProducts>(crOpportunityProductsRow);

            if (fsxCROpportunityProductsRow != null)
            {
                InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(Base, crOpportunityProductsRow.InventoryID);

                if (inventoryItemRow != null)
                {
                    PXUIFieldAttribute.SetEnabled<FSxCROpportunityProducts.billingRule>(cache, crOpportunityProductsRow, inventoryItemRow.ItemType == INItemTypes.ServiceItem);
                    PXUIFieldAttribute.SetEnabled<FSxCROpportunityProducts.estimatedDuration>(cache, crOpportunityProductsRow, inventoryItemRow.ItemType == INItemTypes.ServiceItem || inventoryItemRow.ItemType == INItemTypes.NonStockItem);
                    PXUIFieldAttribute.SetEnabled<CROpportunityProducts.curyUnitCost>(cache, crOpportunityProductsRow, crOpportunityProductsRow.POCreate == true);
                    PXUIFieldAttribute.SetEnabled<FSxCROpportunityProducts.vendorLocationID>(cache, crOpportunityProductsRow, crOpportunityProductsRow.POCreate == true);
                    PXUIFieldAttribute.SetEnabled<CROpportunityProducts.quantity>(cache, crOpportunityProductsRow, fsxCROpportunityProductsRow.BillingRule != ID.BillingRule.TIME);
                }
            }
        }

        protected virtual void _(Events.RowInserting<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowInserted<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowUpdating<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowUpdated<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowDeleting<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowDeleted<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowPersisting<CROpportunityProducts> e)
        {
        }

        protected virtual void _(Events.RowPersisted<CROpportunityProducts> e)
        {
        }

        #endregion

        #endregion
    }
}
