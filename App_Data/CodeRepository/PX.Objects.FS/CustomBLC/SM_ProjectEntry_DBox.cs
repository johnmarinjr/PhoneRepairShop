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
using PX.Objects.PM;

namespace PX.Objects.FS
{
    public class SM_ProjectEntry_DBox
    : DialogBoxSOApptCreation<SM_ProjectEntry, ProjectEntry, PMProject>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public override void Configure(PXScreenConfiguration configuration)
        {
            var context = configuration.GetScreenConfigurationContext<ProjectEntry, PMProject>();

            var servicesCategory = context.Categories.CreateNew(ToolbarCategory.ActionCategoryNames.Services,
                category => category.DisplayName(ToolbarCategory.ActionCategory.Services));

            context.UpdateScreenConfigurationFor(config => config
                    .WithActions(a =>
                    {
                        a.Add<SM_ProjectEntry_DBox>(e => e.CreateSrvOrdDocument, c => c.InFolder(servicesCategory));
                        a.Add<SM_ProjectEntry_DBox>(e => e.CreateApptDocument, c => c.InFolder(servicesCategory));
                    })
                    .UpdateDefaultFlow(flow =>
                    {
                        return flow.WithFlowStates(states =>
                        {
                            states.Update(ProjectStatus.Active,
                                state => state.WithActions(actions =>
                                {
                                    actions.Add<SM_ProjectEntry_DBox>(e => e.CreateSrvOrdDocument);
                                    actions.Add<SM_ProjectEntry_DBox>(e => e.CreateApptDocument);
                                }));
                        });
                    })
                    .WithCategories(categories =>
                    {
                        categories.Add(servicesCategory);
                        categories.Update(ToolbarCategory.ActionCategoryNames.Services, category => category.PlaceAfter(context.Categories.Get(ToolbarCategory.ActionCategoryNames.Commitments)));
                    })
                );
        }

        #region Events
        protected virtual void _(Events.RowSelected<PMProject> e)

        {
            bool isSMSetup = GetFSSetup() != null;
            bool insertedStatus = Base.Project.Cache.GetStatus(Base.Project.Current) == PXEntryStatus.Inserted;

            CreateSrvOrdDocument.SetEnabled(isSMSetup && e.Row != null && insertedStatus == false);
            CreateApptDocument.SetEnabled(isSMSetup && e.Row != null && insertedStatus == false);
        }

        protected virtual void _(Events.RowSelected<DBoxDocSettings> e)

        {
            PXUIFieldAttribute.SetEnabled<DBoxDocSettings.projectID>(e.Cache, e.Row, false);
        }
        #endregion

        #region ParentAbstractImplementation
        public override void PrepareDBoxDefaults()
        {
            PMProject pmProjectRow = Base.Project.Current;
            DocumentSettings.Current.CustomerID = pmProjectRow.CustomerID;
            DocumentSettings.Current.Description = pmProjectRow.Description;
            DocumentSettings.Cache.SetValueExt<DBoxDocSettings.branchID>(DocumentSettings.Current, pmProjectRow.DefaultBranchID);
            DocumentSettings.Cache.SetValueExt<DBoxDocSettings.projectID>(DocumentSettings.Current, pmProjectRow.ContractID);
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

            PMProject pmProjectRow = Base.Project.Current;

            header.LocationID = pmProjectRow.LocationID;
            header.CuryID = pmProjectRow.BillingCuryID;
            header.sourceDocument = pmProjectRow;
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
                null,
                null,
                null,
                null,
                Base.Project.Cache,
                null,
                header,
                details,
                header.CreateAppointment == true);
        }
        #endregion

        #region VirtualFunctions
        public virtual FSSetup GetFSSetup()
        {
            if (Base1.SetupRecord.Current == null)
            {
                return Base1.SetupRecord.Select();
            }
            else
            {
                return Base1.SetupRecord.Current;
            }
        }
        #endregion
    }
}
