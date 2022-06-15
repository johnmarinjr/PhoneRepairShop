using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Api;
using PX.Common;
using PX.Common.Extensions;
using PX.Data;
using PX.Objects.CR;
using PX.SM;
using PX.TM;
using PX.Web.UI;

namespace PX.Objects
{
    // Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod [extension should be constantly active]
    public class TaskTemplateMaintGraphExtension : PXGraphExtension<TaskTemplateMaint>
    {
        #region Constants and fields

        private const string EntityKey = "Entity";
        private const string OwnersKey = "Owners";

        private const char KeySeparator = '-';
        internal const char ValueSeparator = '|';

        private readonly PXGraph taskGraph = PXGraph.CreateInstance<CRTaskMaint>();
        private readonly BqlCommand ownerSelect = new OwnerAttribute().SelectorAttr.GetSelect();

        private static readonly Type[] fieldsList = new[] 
        {
            typeof(CRActivity.startDate),
            typeof(CRActivity.endDate),
            typeof(CRActivity.priority),
            typeof(CRActivity.uistatus),
            typeof(CRActivity.categoryID),
            typeof(CRActivity.workgroupID),
            typeof(CRActivity.contactID),
            typeof(CRActivity.bAccountID),
            typeof(CRActivity.isPrivate),
            typeof(CRReminder.isReminderOn),
            typeof(CRReminder.reminderDate),
            typeof(PMTimeActivity.projectID),
            typeof(PMTimeActivity.projectTaskID),
        };

        #endregion

        #region Event handlers

        [PXFieldNamesList(typeof(CRTaskMaint),
            typeof(CRActivity.startDate),
            typeof(CRActivity.endDate),
            typeof(CRActivity.priority),
            typeof(CRActivity.uistatus),
            typeof(CRActivity.categoryID),
            typeof(CRActivity.workgroupID),
            typeof(CRActivity.contactID),
            typeof(CRActivity.bAccountID),
            typeof(CRActivity.isPrivate),
            typeof(CRReminder.isReminderOn),
            typeof(CRReminder.reminderDate),
            typeof(PMTimeActivity.projectID),
            typeof(PMTimeActivity.projectTaskID))]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        public void TaskTemplateSetting_FieldName_CacheAttached(PXCache cache) { }

        [PXFieldValuesList(4000, typeof(CRTaskMaint), typeof(TaskTemplateSetting.fieldName), ExclusiveValues = false, IsActive = false)]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        public void TaskTemplateSetting_Value_CacheAttached(PXCache cache) { }

        protected virtual void TaskTemplate_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
        {
            if (!(e.Row is TaskTemplate row)) return;
            
            var settingsCache = Base.TaskTemplateSettings.Cache;
            if (row.TaskTemplateID < 0 && !settingsCache.Inserted.Any_())
            {
                foreach (var fieldType in fieldsList)
                {
                    ((TaskTemplateSetting)settingsCache.NonDirtyInsert()).FieldName = 
                        PXFieldNamesListAttribute.MergeNames(fieldType.DeclaringType.Name, fieldType.Name);
                }
            }
        }

        protected virtual void TaskTemplateSetting_Value_FieldSelecting(PXCache cache, PXFieldSelectingEventArgs e)
        {
            if (e.Row is TaskTemplateSetting row)
            {
                Base.UpdateValueFieldState(cache, row);
                InsertOrUpdateValueInCache(row);
            }
        }

        protected virtual void TaskTemplateSetting_Value_FieldUpdated(PXCache cache, PXFieldUpdatedEventArgs e)
        {
            if (e.Row is TaskTemplateSetting row)
            {
                InsertOrUpdateValueInCache(row);
            }
        }

        #endregion

        #region Delegates

        protected IEnumerable screenOwnerItems(string parent)
	    {
            if (Base.CurrentSiteMapNode == null)
                yield break;
            if (PXView.Searches?.FirstOrDefault() is string search)
                parent = search.FirstSegment(ValueSeparator);
            if (parent == null)
            {
                yield return new CacheEntityItem {Key = EntityKey, Name = EntityKey, Number = 0};
                yield return new CacheEntityItem {Key = OwnersKey, Name = OwnersKey, Number = 1};
            }
            else
            {
                if (parent.OrdinalEquals(OwnersKey))
                {
                    foreach (var item in GetFirstLettersOfOwnerNames(parent))
                        yield return item;
                }
                else if (parent.StartsWith(OwnersKey))
                {
                    foreach (var item in GetAllOwnerNames(parent))
                        yield return item;
                }
                else if (parent.OrdinalEquals(EntityKey))
                {
                    if (Base.CurrentScreenIsGI)
                    {
                        foreach (var item in GetAllOwnerFieldsForGI(parent))
                            yield return item;
                    }
                    else
                    {
                        //Acuminator disable once PX1084 GraphCreationInDataViewDelegate
                        //[graph is needed to form the list of fields of primary view with OwnerAttribute]
                        foreach (var item in GetOwnerFieldsForEntry(parent))
                            yield return item;
                    }
                }
            }
            yield break;
        }

        #endregion

        #region Functions

        ///<summary>Inserts or updates data from the Value field of TaskTemplateSetting to the current record of an appropriate cache.</summary>
        ///<remarks>Procures work of connected selectors, such as on PMTimeActivity.ProjectID and PMTimeActivity.ProjectTaskID fields.</remarks>
        private void InsertOrUpdateValueInCache(TaskTemplateSetting row)
        {
            if (PXFieldNamesListAttribute.SplitNames(row.FieldName, out var tableName, out var fieldName))
            {
                var taskCache = taskGraph.Caches[tableName];
                if (taskCache == null) return;
                var itemCache = Base.Caches[taskCache.GetItemType()];
                if (itemCache.Current == null) itemCache.Current = itemCache.CreateInstance();
                try { itemCache.SetValueExt(itemCache.Current, fieldName, row.Value); }
                catch {/* Prevents errors when connected field value is not set */}
            }
        }

        private IEnumerable<CacheEntityItem> GetFirstLettersOfOwnerNames(string parent)
        {
            foreach (var letter in new PXView(Base, false, ownerSelect)
                .SelectMulti().Cast<Contact>().Where(c => !string.IsNullOrEmpty(c.DisplayName))
                .Select(c => c.DisplayName.Substring(0, 1).ToUpper()).OrderBy(c => c).Distinct())
            {
                yield return new CacheEntityItem {Name = letter, Key = parent + KeySeparator + letter};
            }
        }

        private IEnumerable<CacheEntityItem> GetAllOwnerNames(string parent)
        {
            var letter = parent.LastSegment(KeySeparator);
            if (letter.IndexOf(ValueSeparator) > 0) yield break;

            foreach (Contact owner in new PXView(Base, false, ownerSelect)
                .SelectMulti().Cast<Contact>().Where(c => c.DisplayName.OrdinalStartsWith(letter)))
            {
                yield return new CacheEntityItem
                {
                    Name = owner.DisplayName + (string.IsNullOrEmpty(owner.Salutation) ? "" : $" ({owner.Salutation})"),
                    Key = parent + ValueSeparator + owner.ContactID,
                    Path = parent + ValueSeparator + owner.ContactID
                };
            }
        }

        private IEnumerable<CacheEntityItem> GetOwnerFieldsForEntry(string parent)
        {
            var result = new Dictionary<string, string>();
            var tables = new List<Type>();
            var graph = PXGraph.CreateInstance(GraphHelper.GetType(Base.CurrentSiteMapNode.GraphType));
            var cache = graph.Views[graph.PrimaryView].Cache;
            var fields = cache.Fields.ToDictionary(c => c, c => c, StringComparer.OrdinalIgnoreCase);
            var views = EMailSourceHelper.TemplateEntity(Base, null, null, Base.CurrentSiteMapNode.GraphType, true, true);

            foreach (string viewName in views.OfType<CacheEntityItem>().Select(c => c.Key))
                if (graph.Views.TryGetOrCreateValue(viewName, out var view))
                    EnumOwnerFields(null, null, graph, view.CacheType(), tables, result, fields);

            var num = 0;
            return result.Select(c => new CacheEntityItem { Key = c.Value, Name = c.Value, Number = num++, Path = parent + ValueSeparator + $"(({c.Key}))" });
        }

        private IEnumerable<CacheEntityItem> GetAllOwnerFieldsForGI(string parent)
        {
            var result = new Dictionary<string, string>();
            var tables = new List<Type>();
            var graph = PXGenericInqGrph.CreateInstance(Base.CurrentSiteMapNode.ScreenID);
            var usedTables = graph.BaseQueryDescription.UsedTables;

            foreach (var group in graph.ResultColumns.GroupBy(c => c.ObjectName))
            {
                if (usedTables.TryGetValue(group.Key, out PX.Data.Description.GI.PXTable table))
                {
                    var fields = group.ToDictionary(c => c.Field, c => c.FieldName, StringComparer.OrdinalIgnoreCase);
                    EnumOwnerFields(null, null, graph, table.CacheType, tables, result, fields);
                }
            }

            var num = 0;
            return result.Select(c => new CacheEntityItem { Key = c.Value, Name = c.Value, Number = num++, Path = parent + ValueSeparator + $"(({c.Key}))" });
        }

        private static void EnumOwnerFields(string internalPath, string displayPath, PXGraph graph, Type table, List<Type> tables, Dictionary<string, string> names, Dictionary<string, string> fields)
        {
            PXCache cache = graph.Caches[table];
            foreach (string fieldName in OwnerAttribute.GetFields(table))
            {
                GetFieldNames(cache, table, fields, internalPath, displayPath, fieldName, out string internalName, out string displayName);
                if (internalName != null && !names.ContainsKey(internalName)) names.Add(internalName, displayName);
            }
            if (tables.Count < 2)
            {
                tables.Add(table);
                var selectors = PXSelectorAttribute.GetSelectorFields(table).Where(s => fields == null || fields.ContainsKey(s.Key));
                foreach (KeyValuePair<string, Type> pair in selectors)
                {
                    var tableType = BqlCommand.GetItemType(pair.Value);
                    if (tableType == null) continue;
                    var tableIndex = tables.IndexOf(tableType);
                    if (tableIndex == -1 || tableIndex == 0 && tables.Count > 1)
                    {
                        GetFieldNames(cache, table, fields, internalPath, displayPath, pair.Key, out string internalName, out string displayName);
                        EnumOwnerFields(internalName, displayName, graph, tableType, tables, names, null);
                    }
                }
                tables.Remove(table);
            }
        }

        private static void GetFieldNames(PXCache cache, Type table, Dictionary<string, string> fields, string internalPath, string displayPath, string fieldName, out string internalName, out string displayName)
        {
            internalName = string.IsNullOrEmpty(internalPath) ? (fields != null && fields.TryGetValue(fieldName, out internalName) ? internalName : null) : internalPath + "!" + fieldName;
            displayName = (string.IsNullOrEmpty(displayPath) ? table.Name : displayPath) + "->" + (cache.GetStateExt(null, fieldName) as PXFieldState)?.DisplayName ?? fieldName;
        }
        #endregion
    }
}
