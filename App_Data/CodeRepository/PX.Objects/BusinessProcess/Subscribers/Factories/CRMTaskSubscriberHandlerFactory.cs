using System;
using System.Collections.Generic;
using System.Linq;
using PX.BusinessProcess.DAC;
using PX.BusinessProcess.Event;
using PX.BusinessProcess.Subscribers;
using PX.BusinessProcess.Subscribers.ActionHandlers;
using PX.BusinessProcess.Subscribers.Factories;
using PX.BusinessProcess.UI;
using PX.Data;
using PX.Data.PushNotifications;
using PX.Objects.BusinessProcess.Subscribers.ActionHandlers;
using PX.SM;

namespace PX.Objects.BusinessProcess.Subscribers.Factories
{
    class CRMTaskSubscriberHandlerFactory : CreateSubscriberBase, IBPSubscriberActionHandlerFactoryWithCreateAction
    {
        private readonly IPushNotificationDefinitionProvider _pushDefinitionsProvider;

        public CRMTaskSubscriberHandlerFactory(IPushNotificationDefinitionProvider pushDefinitionsProvider)
        {
            _pushDefinitionsProvider = pushDefinitionsProvider;
        }

        public IEventAction CreateActionHandler(Guid handlerId, bool stopOnError, IEventDefinitionsProvider eventDefinitionsProvider)
        {
            return new CRMTaskAction(handlerId, _pushDefinitionsProvider, eventDefinitionsProvider);
        }

        public Tuple<PXButtonDelegate, PXEventSubscriberAttribute[]> getCreateActionDelegate(BusinessProcessEventMaint maintGraph)
        {
            PXButtonDelegate handler = (PXAdapter adapter) => CreateSubscriber<TaskTemplateMaint, TaskTemplate>(maintGraph, adapter, Type);
            return Tuple.Create(handler, new PXEventSubscriberAttribute[] {new PXButtonAttribute {OnClosingPopup = PXSpecialButtonType.Refresh}});
        }

        public IEnumerable<BPHandler> GetHandlers(PXGraph graph)
        {
            return PXSelect<TaskTemplate, Where<TaskTemplate.screenID, Equal<Current<BPEvent.screenID>>, Or<Current<BPEvent.screenID>, IsNull>>>
                .Select(graph).FirstTableItems.Where(t => t != null).Select(t => new BPHandler { Id = t.NoteID, Name = t.Name, Type = TypeName });
        }

        public void RedirectToHandler(Guid? handlerId)
        {
            PXRedirectHelper.TryRedirect(CRMTaskAction.CreateGraphWithTaskTemplate(handlerId), PXRedirectHelper.WindowMode.New);
        }

        public string Type
        {
            get { return BPEventSubscriber.type.TASKType; }
        }
        public string TypeName
        {
            get { return PX.BusinessProcess.Messages.CRMTaskCreation; }
        }

        public string CreateActionName
        {
            get { return "NewCRMTask"; }
        }
        public string CreateActionLabel
        {
            get { return PX.BusinessProcess.Messages.CRMTaskCreation; }
        }
    }
}
