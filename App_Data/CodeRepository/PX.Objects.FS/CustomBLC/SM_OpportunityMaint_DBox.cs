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
    public class SM_OpportunityMaint_DBox : DialogBoxSOApptCreation<SM_OpportunityMaint, OpportunityMaint, CROpportunity>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Configure(PXScreenConfiguration configuration)
        {
            var context = configuration.GetScreenConfigurationContext<OpportunityMaint, CROpportunity>();

            var categoryServices = context.Categories.CreateNew(OpportunityWorkflow.CategoryNames.Services,
                c => c.DisplayName(OpportunityWorkflow.CategoryDisplayNames.Services));

            configuration
                .GetScreenConfigurationContext<OpportunityMaint, CROpportunity>()
                .UpdateScreenConfigurationFor(config => config
                    .WithActions(a =>
                    {
                        a.Add<SM_OpportunityMaint_DBox>(e => e.CreateSrvOrdDocument, c => c
                            .WithCategory(categoryServices));
                        a.Add<SM_OpportunityMaint_DBox>(e => e.CreateApptDocument, c => c
                            .WithCategory(categoryServices));
                    })
                    .UpdateDefaultFlow(flow =>
                    {
                        return flow.WithFlowStates(states =>
                        {
                            states.Update(OpportunityStatus.New,
                                state => state.WithActions(actions =>
                                {
                                    actions.Add<SM_OpportunityMaint_DBox>(e => e.CreateSrvOrdDocument);
                                    actions.Add<SM_OpportunityMaint_DBox>(e => e.CreateApptDocument);
                                }));
                            states.Update(OpportunityStatus.Open,
                                state => state.WithActions(actions =>
                                {
                                    actions.Add<SM_OpportunityMaint_DBox>(e => e.CreateSrvOrdDocument);
                                    actions.Add<SM_OpportunityMaint_DBox>(e => e.CreateApptDocument);
                                }));
                            states.Update(OpportunityStatus.Won,
                                state => state.WithActions(actions =>
                                {
                                    actions.Add<SM_OpportunityMaint_DBox>(e => e.CreateSrvOrdDocument);
                                    actions.Add<SM_OpportunityMaint_DBox>(e => e.CreateApptDocument);
                                })
                            );
                        });
                    })
                );
        }

        #region Events
        protected virtual void _(Events.RowSelected<CROpportunity> e)
        {
            if (e.Row == null) return;

            bool isSMSetup = Base1.GetFSSetup() != null;
            bool insertedStatus = Base.Opportunity.Cache.GetStatus(Base.Opportunity.Current) == PXEntryStatus.Inserted;
            FSxCROpportunity ext = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(e.Row);

            bool enableCreateActions = isSMSetup && e.Row != null && insertedStatus == false && ext?.SOID == null;

            if (enableCreateActions == true)
            {
                CRQuote primaryQt = Base.PrimaryQuoteQuery.SelectSingle();

                bool hasQuotes = primaryQt != null;

                enableCreateActions = (
                        (!hasQuotes
                            || (primaryQt.Status == CRQuoteStatusAttribute.Approved
                                || primaryQt.Status == CRQuoteStatusAttribute.Sent
                                || primaryQt.Status == CRQuoteStatusAttribute.Accepted
                                || primaryQt.Status == CRQuoteStatusAttribute.Draft
                                )
                            )
                        )
                    && (!hasQuotes || primaryQt.QuoteType == CRQuoteTypeAttribute.Distribution)
                    && e.Row.BAccountID != null;
            }

            CreateSrvOrdDocument.SetEnabled(enableCreateActions);
            CreateApptDocument.SetEnabled(enableCreateActions);
        }
        #endregion

        #region ParentAbstractImplementation
        public override void PrepareDBoxDefaults()
        {
            CROpportunity crOpportunityRow = Base.Opportunity.Current;
            FSxCROpportunity fsxCROpportunityRow = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(crOpportunityRow);

            var products = Base.Products.View.SelectMultiBound(new object[] { crOpportunityRow }).RowCast<CROpportunityProducts>();

            if (products.Any(_ => _.InventoryID == null))
            {
                if (Base.OpportunityCurrent.Ask(TX.Messages.ASK_SALES_ORDER_HAS_NON_INVENTORY_LINES, MessageButtons.OKCancel) == WebDialogResult.Cancel)
                {
                    return;
                }
            }

            DocumentSettings.Current.CustomerID = crOpportunityRow.BAccountID;
            DocumentSettings.Cache.SetValueExt<DBoxDocSettings.branchID>(DocumentSettings.Current, crOpportunityRow.BranchID);
            DocumentSettings.Cache.SetValueExt<DBoxDocSettings.projectID>(DocumentSettings.Current, crOpportunityRow.ProjectID);
            DocumentSettings.Current.OrderDate = crOpportunityRow.CloseDate;
            DocumentSettings.Current.Description = crOpportunityRow.Subject;
			DocumentSettings.Current.LongDescr = crOpportunityRow.Details;
			DocumentSettings.Current.ScheduledDateTimeBegin = crOpportunityRow.CloseDate;
        }

        public override void PrepareHeaderAndDetails(
            DBoxHeader header,
            List<DBoxDetails> details)
        {
            if (header == null
                || DocumentSettings.Current == null)
            {
                return;
            }

            CROpportunity crOpportunityRow = Base.Opportunity.Current;
            FSxCROpportunity fsxCROpportunityRow = Base.Opportunity.Cache.GetExtension<FSxCROpportunity>(crOpportunityRow);
            CRContact crContactRow = Base.Opportunity_Contact.Current;
            CRShippingAddress crAddressRow = Base.Shipping_Address.Current;
            CRSetup crSetupRow = Base1.CRSetupRecord.Current;

            fsxCROpportunityRow.SDEnabled = true;
            fsxCROpportunityRow.BranchLocationID = header.BranchLocationID;
            fsxCROpportunityRow.SrvOrdType = header.SrvOrdType;

            header.LocationID = crOpportunityRow.LocationID;
            header.CuryID = crOpportunityRow.CuryID;
            header.ContactID = crOpportunityRow.ContactID;

            int? salesPersonID = CRExtensionHelper.GetSalesPersonID(Base, crOpportunityRow.OwnerID);
            if (salesPersonID != null)
            {
                header.SalesPersonID = salesPersonID;
            }

            header.TaxZoneID = crOpportunityRow.TaxZoneID;

            header.Contact = crContactRow;
            header.Address = crAddressRow;
            header.CopyFiles = crSetupRow.CopyFiles;
            header.CopyNotes = crSetupRow.CopyNotes;

            header.sourceDocument = crOpportunityRow;

            foreach (CROpportunityProducts line in Base.Products.Select())
            {
                if (line.InventoryID != null)
                {
                    DBoxDetails dBoxLine = line;

                    InventoryItem inventoryItemRow = SharedFunctions.GetInventoryItemRow(Base, dBoxLine.InventoryID);

                    dBoxLine.LineType = GetLineTypeFromInventoryItem(inventoryItemRow);

                    FSxCROpportunityProducts extLine = Base.Products.Cache.GetExtension<FSxCROpportunityProducts>(line);
                    dBoxLine.BillingRule = extLine.BillingRule;
                    dBoxLine.EstimatedDuration = extLine.EstimatedDuration;
                    dBoxLine.POVendorLocationID = extLine.VendorLocationID;

                    details.Add(dBoxLine);
                }
            }
        }

        public override void CreateDocument(
            ServiceOrderEntry srvOrdGraph,
            AppointmentEntry apptGraph,
            DBoxHeader header,
            List<DBoxDetails> details)
        {
            CreateDocument(
                srvOrdGraph,
                apptGraph,
                ID.SourceType_ServiceOrder.OPPORTUNITY,
                null,
                Base.Opportunity.Current?.OpportunityID,
                null,
                Base.Opportunity.Cache,
                Base.Products.Cache,
                header,
                details,
                header.CreateAppointment == true);
        }
        #endregion

        #region Virtual Methods
        public virtual string GetLineTypeFromInventoryItem(InventoryItem inventoryItemRow)
        {
            return inventoryItemRow.StkItem == true ? ID.LineType_ALL.INVENTORY_ITEM :
                            inventoryItemRow.ItemType == INItemTypes.ServiceItem ? ID.LineType_ALL.SERVICE : ID.LineType_ALL.NONSTOCKITEM;
        }
        #endregion
    }
}
