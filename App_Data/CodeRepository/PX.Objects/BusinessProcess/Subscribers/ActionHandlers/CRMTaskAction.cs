using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using PX.Api;
using PX.BusinessProcess.Event;
using PX.BusinessProcess.Subscribers.ActionHandlers;
using PX.Common.Extensions;
using PX.Data;
using PX.Data.PushNotifications;
using PX.Data.Wiki.Parser;
using PX.Objects.CR;
using PX.PushNotifications;
using PX.SM;

namespace PX.Objects.BusinessProcess.Subscribers.ActionHandlers
{
    internal class CRMTaskAction : TemplateMessageAction
    {
        private readonly SyFormulaProcessor _formulaProcessor = new SyFormulaProcessor();

        public CRMTaskAction(Guid id, IPushNotificationDefinitionProvider graphProvider, IEventDefinitionsProvider eventDefinitionProvider)
            : base(id, graphProvider, eventDefinitionProvider)
        {
            Id = id;
        }

        public override void Process(MatchedRow[] eventRows, CancellationToken cancellation)
        {
            var templateMaint = CreateGraphWithTaskTemplate(Id);
            var taskTemplate = templateMaint.TaskTemplates.Current;

			var taskSettings = templateMaint.TaskTemplateSettings.Select().RowCast<TaskTemplateSetting>();

            using (new PXLocaleScope(taskTemplate.LocaleName ?? PXLocalesProvider.GetCurrentLocale()))
            {
                var taskGraph = PXGraph.CreateInstance<CRTaskMaint>();
                var activity = taskGraph.Tasks.Cache.InitNewRow<CRActivity>();

                var parameters = GetParameters(eventRows);
                (PXGraph entityGraph, string primaryView) = GetDefinitionGraph(eventRows[0], taskTemplate.ScreenID);
                activity.Subject = PXTemplateContentParser.Instance.Process(taskTemplate.Summary, parameters, entityGraph, primaryView);
                
                SyFormulaFinalDelegate fieldValueGetter;
                if (entityGraph is PXGenericInqGrph)
                {
                    if (Guid.TryParse(PXTemplateContentParser.Instance.Process(taskTemplate.RefNoteID, parameters, entityGraph, primaryView), out Guid refNoteID))
                        activity.RefNoteID = refNoteID;
                    fieldValueGetter = (names) => 
                    {
                        if (names.Length > 0)
                        {
                            return eventRows[0].NewRow.TryGetValue(new KeyWithAlias(names.Last()), out object value)
                                ? ValueWithInternal.UnwrapInternalValue(value)
                                : null;
                        }
                        throw new PXArgumentException(nameof(names), ErrorMessages.ArgumentOutOfRangeException);
                    };
                }
                else
                {
                    if (taskTemplate.AttachActivity == true)
                    {
                        var primaryCache = entityGraph.Views[primaryView].Cache;
                        activity.RefNoteID = primaryCache.GetValue(primaryCache.Current, nameof(INotable.NoteID)) as Guid?;
                    }
                    fieldValueGetter = (names) => 
                    {
                        // HACK very shitty, but this is more more easier then rewrite ExpressionParser or View name generation
                        if (names.Length > 2)
                        {
                            names = new[] { string.Join(".", names.Take(names.Length - 1)), names.Last() };
                        }
                        if (names.Length == 1)
                        {
                            names = names[0].Split(SMNotificationMaint.ViewNameSeparator);
                        }
                        if (names.Length == 2)
                        {
                            string viewName = names[0];
                            if (viewName == null) return null;

                            string fieldName = names[1];
                            if (fieldName == null) return null;

                            var itemCache = entityGraph.Views[viewName]?.Cache;
                            if (itemCache == null) return null;

                            return itemCache.GetValue(itemCache.Current, fieldName);
                        }
                        throw new PXArgumentException(nameof(names), ErrorMessages.ArgumentOutOfRangeException);
                    };
                }

                activity = (CRActivity)taskGraph.Tasks.Cache.Insert(activity);

                foreach (var taskSetting in taskSettings)
                {
                    if (taskSetting.Value == null) continue;
                    if (taskSetting.IsActive != true) continue;
                    if (!PXFieldNamesListAttribute.SplitNames(taskSetting.FieldName, out var tableName, out var fieldName)) continue;

                    var itemCache = taskGraph.Caches[tableName];
                    if (itemCache == null) continue;

                    itemCache.SetValueExt(itemCache.Current, fieldName,
                        taskSetting.FromSchema == true ? taskSetting.Value : _formulaProcessor.Evaluate(taskSetting.Value, fieldValueGetter));

				}

                string ownerID = null;
                if (!string.IsNullOrEmpty(taskTemplate.OwnerName))
                {
	                ownerID = taskTemplate.OwnerName.LastSegment(TaskTemplateMaintGraphExtension.ValueSeparator);
	                ownerID = PXTemplateContentParser.NullableInstance.Process(ownerID, parameters, entityGraph, primaryView);
	                if (ownerID != string.Empty)
						activity.OwnerID = int.Parse(ownerID);
				}

				var activityBody = PXTemplateContentParser.ScriptInstance.Process(taskTemplate.Body, parameters, entityGraph, primaryView);
                activityBody = activityBody.Replace("data-field=\"yes\"", " ");
                activity.Body = activityBody;
                taskGraph.Tasks.Update(activity);
				taskGraph.Actions.PressSave();
            }
        }

		internal static TaskTemplateMaint CreateGraphWithTaskTemplate(Guid? noteID)
        {
            var graph = PXGraph.CreateInstance<TaskTemplateMaint>();
            graph.TaskTemplates.Current = graph.TaskTemplates.Search<TaskTemplate.noteID>(noteID);
            return graph;
        }
    }
}
