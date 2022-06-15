using PX.Data;
using PX.Data.WorkflowAPI;
using PX.Objects.AM.Attributes;
using PX.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.AM
{
	using State = VendorShipmentStatus;
	using static AMVendorShipment;
	using static BoundedTo<VendorShipmentEntry, AMVendorShipment>;

	public class VendorShipmentEntry_Workflow : PXGraphExtension<VendorShipmentEntry>
	{
		public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>();
        }

		public override void Configure(PXScreenConfiguration config) => Configure(config.GetScreenConfigurationContext<VendorShipmentEntry, AMVendorShipment>());

        protected virtual void Configure(WorkflowContext<VendorShipmentEntry, AMVendorShipment> context)
        {
            var processingCategory = context.Categories.CreateNew(ActionCategoryNames.Processing,
                category => category.DisplayName(ActionCategory.Processing));

            var printingEmailingCategory = context.Categories.CreateNew(ActionCategoryNames.PrintingEmailing,
                category => category.DisplayName(ActionCategory.PrintingEmailing));

            #region Conditions
            Condition Bql<T>() where T : IBqlUnary, new() => context.Conditions.FromBql<T>();
            var conditions = new
            {
                IsOnHold
                    = Bql<hold.IsEqual<True>>(),
                IsOpen
                    = Bql<hold.IsEqual<False>>(),
                IsCompleted
                    = Bql<status.IsEqual<State.completed>>(),
                IsCancelled
                    = Bql<status.IsEqual<State.cancelled>>(),
                IsCompletedOrCanceled
                    = Bql<status.IsEqual<State.completed>.Or<status.IsEqual<State.cancelled>>>()
            }.AutoNameConditions();
            #endregion

            const string initialState = "_";

            context.AddScreenConfigurationFor(screen =>
            {
                return screen
                    .StateIdentifierIs<status>()
                    .AddDefaultFlow(flow => flow
                        .WithFlowStates(fss =>
                        {
                            fss.Add(initialState, flowState => flowState.IsInitial(g => g.initializeState));
                            fss.Add<State.hold>(flowState =>
                            {
                                return flowState
                                    .WithActions(actions =>
                                    {
                                        actions.Add(g => g.removeHold, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
                                        actions.Add(g => g.printPackingList);
                                        actions.Add(g => g.printPickList);
                                    });
                            });
                            fss.Add<State.open>(flowState =>
                            {
                                return flowState
                                    .WithActions(actions =>
                                    {
                                        actions.Add(g => g.confirm, a => a.IsDuplicatedInToolbar().WithConnotation(ActionConnotation.Success));
                                        actions.Add(g => g.hold, a => a.IsDuplicatedInToolbar());
                                        actions.Add(g => g.cancelShip, a => a.IsDuplicatedInToolbar());
                                        actions.Add(g => g.printPackingList);
                                        actions.Add(g => g.printPickList);
                                    });
                            });
                            fss.Add<State.completed>(flowState =>
                            {
                                return flowState
                                    .WithActions(actions => {});
                            });
                            fss.Add<State.cancelled>(flowState =>
                            {
                                return flowState
                                    .WithActions(actions => { });
                            });
                        })
                        .WithTransitions(transitions =>
                        {
                            transitions.AddGroupFrom(initialState, ts =>
                            {
                                ts.Add(t => t.To<State.hold>().IsTriggeredOn(g => g.initializeState).When(conditions.IsOnHold));
                                ts.Add(t => t.To<State.open>().IsTriggeredOn(g => g.initializeState));
                            });
                            transitions.AddGroupFrom<State.hold>(ts =>
                            {
                                ts.Add(t => t
                                    .To<State.open>()
                                    .IsTriggeredOn(g => g.removeHold)
                                    .WithFieldAssignments(fas => fas.Add<hold>(f => f.SetFromValue(false))));
                            });
                            transitions.AddGroupFrom<State.open>(ts =>
                            {
                                ts.Add(t => t
                                    .To<State.completed>()
                                    .IsTriggeredOn(g => g.confirm));
                                ts.Add(t => t
                                    .To<State.hold>()
                                    .IsTriggeredOn(g => g.hold)
                                    .WithFieldAssignments(fas =>fas.Add<hold>(f => f.SetFromValue(true))));
                                ts.Add(t => t
                                    .To<State.cancelled>()
                                    .IsTriggeredOn(g => g.cancelShip));
                            });
                        })
                    )
                    .WithCategories(categories =>
                    {
                        categories.Add(processingCategory);
                        categories.Add(printingEmailingCategory);
                    })
                    .WithActions(actions => {
                        actions.Add(g => g.initializeState, a => a.IsHiddenAlways());
                        actions.Add(g => g.removeHold, c => c.WithCategory(processingCategory));
                        actions.Add(g => g.hold, c => c.WithCategory(processingCategory));
                        actions.Add(g => g.confirm, c => c.WithCategory(processingCategory));
                        actions.Add(g => g.cancelShip, c => c.WithCategory(processingCategory));

                        actions.Add(g => g.printPickList, c => c.WithCategory(printingEmailingCategory));
                        actions.Add(g => g.printPackingList, c => c.WithCategory(printingEmailingCategory));
                    });
            });
        }

        public static class ActionCategoryNames
        {
            public const string Processing = "Processing";
            public const string PrintingEmailing = "PrintingEmailing";
        }

        public static class ActionCategory
        {
            public const string Processing = "Processing";
            public const string PrintingEmailing = "Printing & Emailing";
        }

    }
}
