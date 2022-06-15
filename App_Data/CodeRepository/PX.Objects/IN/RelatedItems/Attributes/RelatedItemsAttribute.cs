using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Bql;
using PX.Objects.Common.Exceptions;
using PX.Objects.CS;
using PX.Objects.SO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RelationType = PX.Objects.IN.RelatedItems.InventoryRelation.RelationType;

namespace PX.Objects.IN.RelatedItems
{
    public static class RelatedItemIcons
    {
        public const string Any = "~/Icons/dollarGreen.svg";
        public class any : BqlString.Constant<any> { public any() : base(Any) { } }

        public static class Required
        {
            public const string CrossSell = "~/Icons/dollarRed.svg";
            public const string Substitution = "~/Icons/switchRed.svg";
            public const string Other = "~/Icons/dollarRed.svg";

            public class crossSell : BqlString.Constant<crossSell> { public crossSell() : base(CrossSell) { } }
            public class substitution : BqlString.Constant<substitution> { public substitution() : base(Substitution) { } }
            public class other : BqlString.Constant<other> { public other() : base(Other) { } }
        }
    }

    [PXImage]
    [PXUIField(DisplayName = Messages.RelatedItemsField, IsReadOnly = true)]
    public class RelatedItemsAttribute : PXAggregateAttribute, IPXRowSelectedSubscriber, IPXRowInsertedSubscriber
    {
        private int _uiFieldAttributeIndex;

        protected Type SuggestRelatedItemsField;
        protected Type RelationField;
        protected Type RequiredField;

        protected Type DocType;
        protected Type DocDateField;

        public Type DocumentDateField
        {
            get { return DocDateField; }
            set 
            {
                DocDateField = value;
                DocType = BqlCommand.GetItemType(value);
            }
        }

        public bool CacheGlobal { get; set; } = true;

        protected PXUIFieldAttribute UIFieldAttribute => (PXUIFieldAttribute)_Attributes[_uiFieldAttributeIndex];

        public RelatedItemsAttribute()
        {
            _uiFieldAttributeIndex = _Attributes.FindIndex(x => x is PXUIFieldAttribute);
        }

        public RelatedItemsAttribute(Type suggestRelatedItemsField, Type relationField, Type requiredField)
            : this()
        {
            SuggestRelatedItemsField = suggestRelatedItemsField;

            RelationField = relationField;
            RequiredField = requiredField;
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);

            var itemType = sender.GetItemType();

            sender.Graph.RowSelecting.AddHandler(itemType, RowSelecting);

            if (DocType != null)
                sender.Graph.RowUpdated.AddHandler(DocType, DocumentUpdated);

            if (typeof(ISubstitutableLine).IsAssignableFrom(itemType))
            {
                sender.Graph.FieldUpdated.AddHandler(itemType, nameof(ISubstitutableLine.InventoryID), RelatedFieldUpdated);
                sender.Graph.FieldUpdated.AddHandler(itemType, nameof(ISubstitutableLine.BranchID), RelatedFieldUpdated);
                sender.Graph.FieldUpdated.AddHandler(itemType, nameof(ISubstitutableLineExt.SuggestRelatedItems), RelatedFieldUpdated);
            }
        }

        #region Event handlers

        protected virtual void DocumentUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            var substitutableDocument = e.Row as ISubstitutableDocument;
            if (substitutableDocument == null)
                return;
            var recalcSuggestRelatedItems = !Equals(substitutableDocument.SuggestRelatedItems, ((ISubstitutableDocument)e.OldRow).SuggestRelatedItems);
            var recalcRelatedItems = recalcSuggestRelatedItems
                || !Equals(sender.GetValue(e.OldRow, DocDateField.Name), sender.GetValue(e.Row, DocDateField.Name));
            
            if(recalcSuggestRelatedItems || recalcRelatedItems)
            {
                var linesCache = sender.Graph.Caches[BqlTable];
                foreach (ISubstitutableLine line in PXParentAttribute.SelectChildren(linesCache, e.Row, sender.GetItemType()))
                {
                    if (recalcSuggestRelatedItems && SuggestRelatedItemsField != null)
                    {
                        var suggestOnLine = PXFormulaAttribute.Evaluate(linesCache, SuggestRelatedItemsField.Name, line);
                        linesCache.SetValue(line, SuggestRelatedItemsField.Name, suggestOnLine);
                    }

                    if(recalcRelatedItems)
                        CalculateRelatedItems(linesCache, line);
                }
            }
        }

        protected virtual void RelatedFieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            var substitutableLine = e.Row as ISubstitutableLine;
            if (substitutableLine == null)
                return;
            CalculateRelatedItems(sender, substitutableLine);
        }

        protected virtual void RowSelecting(PXCache sender, PXRowSelectingEventArgs e)
        {
            var substitutableLine = e.Row as ISubstitutableLine;
            if (substitutableLine == null)
                return;

            using (new PXConnectionScope())
                CalculateRelatedItems(sender, substitutableLine);
        }

        public virtual void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            var substitutableLine = e.Row as ISubstitutableLine;
            if(substitutableLine != null)
                SetRelatedItemsWarning(sender, substitutableLine);
        }

        public virtual void RowInserted(PXCache sender, PXRowInsertedEventArgs e)
        {
            var substitutableLine = e.Row as ISubstitutableLine;
            if (substitutableLine == null)
                return;
            CalculateRelatedItems(sender, substitutableLine);
        }

        #endregion

        #region Images and messages definitions

        protected virtual string RelatedItemsImage(RelationType relation, RelationType required)
        {
            if (relation == RelationType.None)
                return null;

            if (required == RelationType.None)
                return RelatedItemIcons.Any;

            if (required.HasFlag(RelationType.Substitute))
                return RelatedItemIcons.Required.Substitution;
            if (required.HasFlag(RelationType.CrossSell))
                return RelatedItemIcons.Required.CrossSell;
            if (required.HasFlag(RelationType.Other))
                return RelatedItemIcons.Required.Other;
            return null;
        }

        protected virtual PXExceptionInfo RelatedItemsMessage(RelationType relation, RelationType required)
        {
            if (relation == RelationType.None)
                return null;

            if (required == RelationType.None)
            {
                const string relatedItemsAvailableCss = "RelatedItemsAvailable";
                var relations = RelatedItemsMessageArguments(relation);

                string messageFormat = string.Empty;
                switch(relations.Length)
                {
                    case 1:
                        messageFormat = Messages.RelatedItemsAvailable;
                        break;
                    case 2:
                        messageFormat = Messages.RelatedItemsAvailable2;
                        break;
                    case 3:
                        messageFormat = Messages.RelatedItemsAvailable3;
                        break;
                    case 4:
                        messageFormat = Messages.RelatedItemsAvailable4;
                        break;
                }
                return new PXExceptionInfo(PXErrorLevel.Warning, messageFormat, relations) { Css = relatedItemsAvailableCss };
            }
            const string relatedItemsRequiredCss = "RelatedItemsRequired";
            if (required.HasFlag(RelationType.Substitute))
                return new PXExceptionInfo(PXErrorLevel.Warning, Messages.SubstituteItemsRequired) { Css = relatedItemsRequiredCss };
            if (required.HasFlag(RelationType.CrossSell)
                || required.HasFlag(RelationType.Other))
                return new PXExceptionInfo(PXErrorLevel.Warning, Messages.RelatedItemsRequired) { Css = relatedItemsRequiredCss };

            return null;
        }

        public virtual PXExceptionInfo QtyMessage(object inventoryCD, object subItemCD, object siteCD, RelationType relation)
        {
            if (relation == RelationType.None)
                return null;

            var relations = RelatedItemsMessageArguments(relation);

            string messageFormat = string.Empty;
            switch (relations.Length)
            {
                case 1:
                    messageFormat = Messages.QuantityIsNotSufficient;
                    break;
                case 2:
                    messageFormat = Messages.QuantityIsNotSufficient2;
                    break;
                case 3:
                    messageFormat = Messages.QuantityIsNotSufficient3;
                    break;
                case 4:
                    messageFormat = Messages.QuantityIsNotSufficient4;
                    break;
            }
            return new PXExceptionInfo(PXErrorLevel.Warning, messageFormat, new object[] { inventoryCD, siteCD }.Append(relations));
        }

        #endregion

        #region Related Items fields calculations

        private static string[] RelatedItemsMessageArguments(RelationType relation)
        {
            var relations = new List<string>();
            if (relation.HasFlag(RelationType.Substitute))
                relations.Add(InventoryRelation.Desc.Substitute);
            if (relation.HasFlag(RelationType.UpSell))
                relations.Add(InventoryRelation.Desc.UpSell);
            if (relation.HasFlag(RelationType.CrossSell))
                relations.Add(InventoryRelation.Desc.CrossSell);
            if (relation.HasFlag(RelationType.Other))
                relations.Add(InventoryRelation.Desc.Other);
            relations = relations.Select(x => PXMessages.LocalizeNoPrefix(x).ToLower()).ToList();
            relations[0] = relations[0].Substring(0, 1).ToUpper() + relations[0].Substring(1);
            return relations.ToArray();
        }

        public virtual bool FindRelatedItems(PXGraph graph, ISubstitutableLine substitutableLine, bool? showOnlyAvailableItems,
            out RelationType relation, out RelationType required)
        {
            relation = required = RelationType.None;

            if (substitutableLine?.InventoryID == null)
                return false;

            return FindRelatedItems(graph, null, substitutableLine, showOnlyAvailableItems, out relation, out required);
        }

        protected virtual bool FindRelatedItems(PXGraph graph, ISubstitutableDocument substitutableDocument, ISubstitutableLine substitutableLine, 
            bool? showOnlyAvailableItems,
            out RelationType relation, out RelationType required)
        {
            int? inventoryID = substitutableLine.InventoryID;
            int? branchID = substitutableLine.BranchID;
            var date = GetDocumentDate(graph, substitutableDocument);
            showOnlyAvailableItems = showOnlyAvailableItems ?? ShowOnlyAvailableRelatedItems(graph);

            Dictionary<int?, RelatedItemsResult> cachedRelatedItems = null;
            if (CacheGlobal)
            {
                cachedRelatedItems = RelatedItemsCache.Get(branchID, showOnlyAvailableItems, date);

                RelatedItemsResult relatedItemsResult;
                if (cachedRelatedItems.TryGetValue(inventoryID, out relatedItemsResult))
                {
                    relation = relatedItemsResult.Relation;
                    required = relatedItemsResult.Required;
                    return (relation | required) > 0;
                }
            }

            INAvailableRelatedItems[] relatedItems = LoadRelatedItems(graph, substitutableLine, date, showOnlyAvailableItems);

            bool found;
            if (relatedItems.Length == 0)
            {
                relation = RelationType.None;
                required = RelationType.None;

                found = false;
            }
            else
            {
                var newRelation = RelationType.None;
                var newRequired = RelationType.None;

                INAvailableRelatedItems find(string r) => relatedItems.FirstOrDefault(x => x.Relation == r);
                void setType(INAvailableRelatedItems relatedInventory, RelationType type)
                {
                    if (relatedInventory != null)
                    {
                        newRelation |= type;
                        if (relatedInventory.Required == true)
                            newRequired |= type;
                    }
                };

                setType(find(InventoryRelation.Substitute), RelationType.Substitute);
                setType(find(InventoryRelation.UpSell), RelationType.UpSell);
                setType(find(InventoryRelation.CrossSell), RelationType.CrossSell);
                setType(find(InventoryRelation.Other), RelationType.Other);

                relation = newRelation;
                required = newRequired;

                found = true;
            }

            if (cachedRelatedItems != null)
                cachedRelatedItems[inventoryID] = new RelatedItemsResult(inventoryID, relation, required);

            return found;
        }

        protected virtual INAvailableRelatedItems[] LoadRelatedItems(PXGraph graph, ISubstitutableLine substitutableLine, DateTime? documentDate, bool? showOnlyAvailableItems)
        {
            var query = GetRelatedItemsQuery(graph);

            using (new PXFieldScope(query.View, typeof(INAvailableRelatedItems.relation), typeof(INAvailableRelatedItems.required)))
                return query.SelectMain(GetRelatedItemsQueryArguments(graph, substitutableLine, documentDate, showOnlyAvailableItems));
        }

        protected virtual PXSelectBase<INAvailableRelatedItems> GetRelatedItemsQuery(PXGraph graph)
        {
            return new SelectFrom<INAvailableRelatedItems>
                .Where<
                    INAvailableRelatedItems.originalInventoryID.IsEqual<@P.AsInt>
                    .And<@P.AsDateTime.IsNull
                        .Or<Brackets<INAvailableRelatedItems.effectiveDate.IsNull
                                .Or<INAvailableRelatedItems.effectiveDate.IsLessEqual<@P.AsDateTime>>>
                            .And<Brackets<INAvailableRelatedItems.expirationDate.IsNull
                                .Or<INAvailableRelatedItems.expirationDate.IsGreaterEqual<@P.AsDateTime>>>>>>
                    .And<Brackets<
                        @P.AsBool.IsNotEqual<True>
                        .Or<INAvailableRelatedItems.stkItem.IsNotEqual<True>>
                        .Or<INAvailableRelatedItems.relation.IsEqual<InventoryRelation.substitute>.And<INAvailableRelatedItems.required.IsEqual<True>>>
                        .Or<INAvailableRelatedItems.qtyAvail.IsGreater<decimal0>>>>
                    .And<Brackets<INAvailableRelatedItems.siteID.IsNull
                        .Or<FeatureInstalled<FeaturesSet.interBranch>>
						.Or<INAvailableRelatedItems.branchID.IsNull>
						.Or<INAvailableRelatedItems.branchID.IsIn<@P.AsInt>>
						.Or<@P.AsString.IsNotNull
							.And<@P.AsString.IsEqual<SOBehavior.qT>>>>>>
                .AggregateTo<GroupBy<INAvailableRelatedItems.relation>>.View.ReadOnly(graph);
        }

        protected virtual object[] GetRelatedItemsQueryArguments(PXGraph graph, ISubstitutableLine substitutableLine, DateTime? documentDate, bool? showOnlyAvailableItems)
        {
			int? organizationID = PXAccess.GetParentOrganizationID(substitutableLine.BranchID);
			var availBranches = PXAccess.GetChildBranchIDs(organizationID, false);
			if (availBranches.Length == 0)
				availBranches = new[] { 0 };

			return new object[]
            {
                substitutableLine.InventoryID,
                documentDate, documentDate, documentDate,
                showOnlyAvailableItems,
				availBranches,
				null,
                null
            };
        }

        protected virtual bool ShowOnlyAvailableRelatedItems(PXGraph graph) => ((SOSetup)PXSetup<SOSetup>.Select(graph))?.ShowOnlyAvailableRelatedItems == true;

        protected virtual DateTime? GetDocumentDate(PXGraph graph, ISubstitutableDocument substitutableDocument)
        {
            if (DocType == null)
                return null;

            var docCache = graph.Caches[DocType];
            return (DateTime?)docCache.GetValue(substitutableDocument ?? docCache.Current, DocDateField.Name);
        }

        protected virtual void CalculateRelatedItems(PXCache cache, ISubstitutableLine substitutableLine)
        {
            void setRelation(object value)
            {
                if (RelationField != null) 
                    cache.SetValue(substitutableLine, RelationField.Name, value);
            };
            void setRequired(object value)
            {
                if (RequiredField != null)
                    cache.SetValue(substitutableLine, RequiredField.Name, value);
            };

            string relatedItemsImage = null;
            int? relationValue = null;
            int? requiredValue = null;

            if (substitutableLine.InventoryID != null
                && (SuggestRelatedItemsField == null || (bool?)cache.GetValue(substitutableLine, SuggestRelatedItemsField.Name) == true)
                && FindRelatedItems(cache.Graph, null, substitutableLine, null, out var relation, out var required))
            {
                relationValue = (int)relation;
                requiredValue = (int)required;
                relatedItemsImage = RelatedItemsImage(relation, required);
            }

            cache.SetValue(substitutableLine, _FieldName, relatedItemsImage);
            setRelation(relationValue);
            setRequired(requiredValue);
        }

        public virtual bool AnyRequired(PXGraph graph, ISubstitutableDocument substitutableDocument, ISubstitutableLine substitutableLine)
        {
            if (substitutableLine?.InventoryID == null)
                return false;

            RelationType relation;
            RelationType required;
            if(FindRelatedItems(graph, substitutableDocument, substitutableLine, null, out relation, out required))
                return relation > 0 && required > 0;
            return false;
        }

        protected virtual void SetRelatedItemsWarning(PXCache cache, ISubstitutableLine substitutableLine)
        {
            if (RelationField == null
                || RequiredField == null)
                return;

            PXExceptionInfo warning;
            var relationValue = (int?)cache.GetValue(substitutableLine, RelationField.Name);
            if (relationValue == null)
                warning = null;
            else
            {
                var requiredValue = (int?)cache.GetValue(substitutableLine, RequiredField.Name) ?? 0;
                warning = RelatedItemsMessage((RelationType)relationValue, (RelationType)requiredValue);
            }
            var ex = warning?.ToSetPropertyException();
            cache.RaiseExceptionHandling(_FieldName, substitutableLine, cache.GetValue(substitutableLine, _FieldName), ex);
        }

        #endregion

        [PXInternalUseOnly]
        public class RelatedItemsCache : ConcurrentDictionary<Composite, Dictionary<int?, RelatedItemsResult>>
        {
            public static Dictionary<int?, RelatedItemsResult> Get(int? branchID, bool? onlyAvailableItems, DateTime? date)
            {
                var key = Composite.Create(branchID ?? 0, onlyAvailableItems ?? false, date ?? DateTime.Today);
                var cache = GetCache();
                return cache.GetOrAdd(key, (k) => new Dictionary<int?, RelatedItemsResult>());
            }

            private static RelatedItemsCache GetCache()
            {
                var cacheKey = nameof(RelatedItemsCache);
                var relatedItemsCache = PXContext.GetSlot<RelatedItemsCache>(cacheKey);
                if (relatedItemsCache == null)
                    PXContext.SetSlot(cacheKey, relatedItemsCache = PXDatabase.GetLocalizableSlot<RelatedItemsCache>(cacheKey,
                        typeof(INRelatedInventory), typeof(InventoryItem), typeof(INSubItem), typeof(INSiteStatus), typeof(INSite)));
                return relatedItemsCache;
            }
        }

        [PXInternalUseOnly]
        public class RelatedItemsResult
        {
            public RelatedItemsResult(int? inventoryID, RelationType relation, RelationType required)
            {
                InventoryID = inventoryID;
                Relation = relation;
                Required = required;
            }

            public int? InventoryID { get; }

            public RelationType Relation { get; }

            public RelationType Required { get; }
        }
    }
}
