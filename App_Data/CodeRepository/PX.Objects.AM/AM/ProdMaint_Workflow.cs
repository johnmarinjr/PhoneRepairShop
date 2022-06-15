using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AM.Attributes;
using PX.Objects.CS;

namespace PX.Objects.AM
{
    using State = ProductionOrderStatus;
    using static AMProdItem;
    using static BoundedTo<ProdMaint, AMProdItem>;

    public class ProdMaint_Workflow : PXGraphExtension<ProdMaint>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.manufacturing>();

        public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<ProdMaint, AMProdItem>());

        protected virtual void Configure(WorkflowContext<ProdMaint, AMProdItem> context)
        {
            var processingCategory = context.Categories.CreateNew(ActionCategoryNames.Processing,
                category => category.DisplayName(ActionCategory.Processing));

            var replenishmentCategory = context.Categories.CreateNew(ActionCategoryNames.Replenishment,
                category => category.DisplayName(ActionCategory.Replenishment));

            var customOtherCategory = context.Categories.CreateNew(ActionCategoryNames.CustomOther,
                category => category.DisplayName(ActionCategory.Other));

			var schedulingCategory = context.Categories.CreateNew("Scheduling",
                category => category.DisplayName("Scheduling"));

            #region Conditions
            Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
            var conditions = new
            {
                IsCompletedOrCanceled
                    = Bql<statusID.IsEqual<State.closed>.Or<statusID.IsEqual<State.cancel>>>()
            }.AutoNameConditions();
            #endregion

            context.AddScreenConfigurationFor(screen =>
            {
                return screen

                    .WithCategories(categories =>
                    {
                        categories.Add(processingCategory);
                        categories.Add(replenishmentCategory);
                        categories.Add(customOtherCategory);
                        categories.Update(FolderType.InquiriesFolder, category => category.PlaceAfter(customOtherCategory));
                        categories.Update(FolderType.ReportsFolder, category => category.PlaceAfter(FolderType.InquiriesFolder));
						categories.Add(schedulingCategory);
                    })
                    .WithActions(actions =>
                    {
                        actions.Add(g => g.initializeState, a => a.IsHiddenAlways());

                        actions.Add(g => g.plan, a => a.WithCategory(processingCategory));
                        actions.Add(g => g.release, a => a.WithCategory(processingCategory));
                        actions.Add(g => g.completeorder, a => a.WithCategory(processingCategory));
                        actions.Add(g => g.cancelorder, a => a.WithCategory(processingCategory));
                        actions.Add(g => g.closeorder, a => a.WithCategory(processingCategory));
                        actions.Add(g => g.releaseMaterial, a => a.WithCategory(processingCategory));
                        actions.Add(g => g.createMove, a => a.WithCategory(processingCategory));

                        actions.Add(g => g.CreatePurchaseOrderInq, a => a.WithCategory(replenishmentCategory));
                        actions.Add(g => g.CreateProductionOrderInq, a => a.WithCategory(replenishmentCategory));

                        actions.Add(g => g.ProductionDetails, a => a.WithCategory(customOtherCategory));
                        actions.Add(g => g.CriticalMatl, a => a.WithCategory(customOtherCategory));
                        actions.Add(g => g.calculatePlanCost, a => a.WithCategory(customOtherCategory));
                        actions.Add(g => g.createLinkedOrders, a => a.WithCategory(customOtherCategory));
                        actions.Add(g => g.disassemble, a => a.WithCategory(customOtherCategory));
						actions.Add(g => g.LateAssignmentEntry, a => a.WithCategory(customOtherCategory));

                        actions.Add(g => g.InventoryAllocationDetailInq, a => a.WithCategory(PredefinedCategory.Inquiries));
                        actions.Add(g => g.TransactionsByProductionOrderInq, a => a.WithCategory(PredefinedCategory.Inquiries));
                        actions.Add(g => g.AttributesInq, a => a.WithCategory(PredefinedCategory.Inquiries));
                        actions.Add(g => g.ViewSchedule, a => a.WithCategory(PredefinedCategory.Inquiries));
						actions.Add(g => g.ProductionScheduleBoardRedirect, a => a.WithCategory(PredefinedCategory.Inquiries));

                        actions.Add(g => g.printProdTicket, c => c.WithCategory(PredefinedCategory.Reports).IsDisabledWhen(conditions.IsCompletedOrCanceled));

						actions.Add(g => g.RoughCutSchedule, a => a.WithCategory(schedulingCategory));
						actions.Add(g => g.RoughCutFirm, a => a.WithCategory(schedulingCategory));
						actions.Add(g => g.RoughCutUndoFirm, a => a.WithCategory(schedulingCategory));
                    });
            });
        }

        public static class ActionCategoryNames
        {
            public const string Processing = "Processing";
            public const string Replenishment = "Replenishment";
            public const string CustomOther = "CustomOther";
        }

        public static class ActionCategory
        {
            public const string Processing = "Processing";
            public const string Replenishment = "Replenishment";
            public const string Other = "Other";
        }
    }
}
