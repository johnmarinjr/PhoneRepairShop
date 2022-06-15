using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Location = PX.Objects.CR.Location;
using RelationType = PX.Objects.IN.RelatedItems.InventoryRelation.RelationType;

namespace PX.Objects.IN.RelatedItems
{
    public abstract class AddRelatedItemExt<TGraph, TSubstitutableDocument, TSubstitutableLine> : PXGraphExtension<TGraph> 
        where TGraph : PXGraph
        where TSubstitutableDocument : class, IBqlTable, ISubstitutableDocument, new()
        where TSubstitutableLine : class, IBqlTable, ISubstitutableLine, new()
    {
        protected virtual bool SplitSerialTrackingItems => false;

        protected abstract DateTime? GetDocumentDate(TSubstitutableDocument document);

        protected PXCache DocumentCache => Base.Caches<TSubstitutableDocument>();

        protected PXCache LinesCache => Base.Caches<TSubstitutableLine>();

        protected virtual ISubstitutableLineExt GetSubstitutableLineExt(TSubstitutableLine substitutableLine)
            => substitutableLine?.GetExtensions()?.OfType<ISubstitutableLineExt>()?.FirstOrDefault();

        protected virtual bool AllowRelatedItems(TSubstitutableDocument document) 
            => document?.SuggestRelatedItems == true;

        protected virtual bool AllowRelatedItems(TSubstitutableLine line)
            => GetSubstitutableLineExt(line)?.RelatedItemsRelation > 0;

        protected virtual TSubstitutableLine FindFocusFor(TSubstitutableLine line) => null;

        #region Views

        public PXFilter<RelatedItemsFilter> RelatedItemsFilter;

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public SelectFrom<RelatedItem>
            .OrderBy<RelatedItem.relation.Asc>
            .View AllRelatedItems;
        public virtual IEnumerable allRelatedItems() => LoadRelatedItems(null);

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public SelectFrom<RelatedItem>.View SubstituteItems;
        public virtual IEnumerable substituteItems() => LoadRelatedItems(InventoryRelation.Substitute);

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public SelectFrom<RelatedItem>.View UpSellItems;
        public virtual IEnumerable upSellItems() => LoadRelatedItems(InventoryRelation.UpSell);

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public SelectFrom<RelatedItem>.View CrossSellItems;
        public virtual IEnumerable crossSellItems() => LoadRelatedItems(InventoryRelation.CrossSell);

        [PXFilterable]
        [PXCopyPasteHiddenView]
        public SelectFrom<RelatedItem>.View OtherRelatedItems;
        public virtual IEnumerable otherRelatedItems() => LoadRelatedItems(InventoryRelation.Other);

        protected virtual BqlCommand RelatedItemsLoadCommand(string relation)
        {
            var select = AllRelatedItems.View.BqlSelect;
            if (!string.IsNullOrEmpty(relation))
                select = select.WhereAnd<Where<RelatedItem.relation.IsEqual<@P.AsString.ASCII>>>();

            return select;
        }

        public virtual RelatedItem[] LoadRelatedItems(string relation)
        {
            var filter = RelatedItemsFilter.Current;
            if (filter.InventoryID == null)
                return Array<RelatedItem>.Empty;            
            
            var relatedItemsCache = AllRelatedItems.Cache;

            var relatedItemFromView = PXView.MaximumRows == 1
                ? relatedItemsCache.CreateInstance<RelatedItem>(PXView.SortColumns, PXView.Searches)
                : null;

            if (relatedItemsCache.Cached.Any_())
            {
                if (relatedItemFromView != null)
                {
                    var find = (RelatedItem)relatedItemsCache.Locate(relatedItemFromView);
                    if (find != null)
                        return new[] { find };
                }
            }

            if (relatedItemFromView != null && relatedItemFromView.InventoryID != ((TSubstitutableLine)LinesCache.Current).InventoryID)
                return Array<RelatedItem>.Empty;

            var parameters = PXView.Parameters;
            var select = RelatedItemsLoadCommand(relation);
            if(!string.IsNullOrEmpty(relation))
                parameters = parameters.Append(relation);

            int startRow = PXView.StartRow;
            int totalRows = 0;

            var relatedItems = new PXView(Base, false, select)
                .Select(
                    PXView.Currents, parameters, PXView.Searches,
                    PXView.SortColumns, PXView.Descendings, PXView.Filters,
                    ref startRow, PXView.MaximumRows, ref totalRows)
                .RowCast<RelatedItem>()
                .ToArray();

            PXView.StartRow = 0;

            foreach (var relatedItem in relatedItems)
            {
                AllRelatedItems.Cache.SetDefaultExt<RelatedItem.curyUnitPrice>(relatedItem);
            }

            return relatedItems;
        }

        [PXCopyPasteHiddenView]
        public SelectFrom<RelatedItemHistory>.View RelatedItemsHistory;

        #endregion

        public override void Initialize()
        {
            base.Initialize();

            PXNoteAttribute.ForcePassThrow<RelatedItem.noteID>(AllRelatedItems.Cache);
        }

        #region Actions

        public PXAction<TSubstitutableDocument> addRelatedItems;
        [PXUIField(DisplayName = "Add Related Items", Visible = false)]
        [PXButton]
        public virtual IEnumerable AddRelatedItems(PXAdapter adapter)
        {
            var document = (TSubstitutableDocument)DocumentCache.Current;
            if (document == null || !AllowRelatedItems(document))
                return adapter.Get();

            var line = (TSubstitutableLine)LinesCache.Current;
            if (line == null || !AllowRelatedItems(line))
                return adapter.Get();

            if(RelatedItemsFilter.AskExt((g, v) => InitializeAddRelatedItemsPanel(document, line)) == WebDialogResult.OK)
            {
                var filter = RelatedItemsFilter.Current;
                var selectedRelatedItems = AllRelatedItems.Cache.Updated
                    .OfType<RelatedItem>()
                    .Where(x => 
                        x.Selected == true
                        && (filter.OnlyAvailableItems != true 
                            || x.AvailableQty == null 
                            || x.Relation == InventoryRelation.Substitute && x.Required == true 
                            || x.AvailableQty > 0))
                    .ToArray();
                AddSelectedRelatedItems(filter, line, selectedRelatedItems);
            }

            ClearAddRelatedPanelCaches();

            return adapter.Get();
        }

        protected virtual void ClearAddRelatedPanelCaches()
        {
            RelatedItemsFilter.Cache.Clear();
            AllRelatedItems.Cache.Clear();
            AllRelatedItems.Cache.ClearQueryCache();
        }

        protected virtual void InitializeAddRelatedItemsPanel(TSubstitutableDocument document, TSubstitutableLine line)
        {
            ClearAddRelatedPanelCaches();
            InitializeFilter(document, line);
        }

        protected virtual RelatedItemsFilter InitializeFilter(TSubstitutableDocument document, TSubstitutableLine line)
        {
            var filter = RelatedItemsFilter.Current;

            filter.DocumentDate = GetDocumentDate(document);
            filter.LineNbr = line.LineNbr;
            filter.BranchID = line.BranchID;

            filter.SiteID = line.SiteID;
            filter.InventoryID = line.InventoryID;
            filter.SubItemID = line.SubItemID;
            filter.UOM = line.UOM;

            var unit = INUnit.UK.ByInventory.Find(Base, filter.InventoryID, filter.UOM);
            if(unit == null)
                throw new PXUnitConversionException();
            filter.BaseUnitMultDiv = unit.UnitMultDiv;
            filter.BaseUnitRate = unit.UnitRate;

            filter.Qty
                = filter.OriginalQty
                = line.Qty;

            filter.CuryID = document.CuryID;
            filter.CuryUnitPrice = line.CuryUnitPrice;
            filter.OriginalCuryExtPrice 
                = filter.CuryExtPrice 
                = line.CuryExtPrice;

            filter.AvailableQty = GetAvailableQty(line);

            filter.RelatedItemsRelation = GetSubstitutableLineExt(line)?.RelatedItemsRelation;

            var relation = (RelationType)(filter.RelatedItemsRelation ?? 0);
            SetTabsVisibility(filter, relation);
            return filter;
        }

        protected virtual decimal? GetAvailableQty(TSubstitutableLine line)
        {
            var siteStatus = INSiteStatus.PK.Find(Base, line.InventoryID, line.SubItemID, line.SiteID);
            var availableQty = INUnitAttribute.ConvertFromBase(RelatedItemsFilter.Cache, line.InventoryID, line.UOM, siteStatus?.QtyAvail ?? 0, INPrecision.QUANTITY);
            return availableQty;
        }

        protected virtual void SetTabsVisibility(RelatedItemsFilter filter, RelationType relation)
        {
            filter.ShowAllRelatedItems = relation == RelationType.None || Convert.ToString((int)relation, 2).Replace("0", "").Length > 1;
            filter.ShowSubstituteItems = relation.HasFlag(RelationType.Substitute);
            filter.ShowUpSellItems = relation.HasFlag(RelationType.UpSell);
            filter.ShowCrossSellItems = relation.HasFlag(RelationType.CrossSell);
            filter.ShowOtherRelatedItems = relation.HasFlag(RelationType.Other);
        }

        protected virtual void AddSelectedRelatedItems(RelatedItemsFilter filter, TSubstitutableLine line, RelatedItem[] relatedItems)
        {
            if (relatedItems == null || !relatedItems.Any())
                return;

            var firstRelatedItem = relatedItems.First();

            if(SingleSelection(firstRelatedItem.Relation))
            {
                var oldOriginal = (TSubstitutableLine)LinesCache.CreateCopy(line);

                if (filter.Qty < oldOriginal.Qty)
                {
                    DecreaseOriginalItemQty(filter, line, firstRelatedItem);

                    AddRelatedItem(filter, oldOriginal, firstRelatedItem);
                }
                else
                {
                    SubstituteOriginalItem(filter, line, firstRelatedItem);
                }
            }
            else
            {
                var activeLine = line;
                foreach (var relatedItem in relatedItems)
                {
                    var focus = FindFocusFor(activeLine);
                    var lines = AddRelatedItem(filter, line, relatedItem, focus);
                    activeLine = lines.Last();
                }
            }
        }

        protected virtual IEnumerable<TSubstitutableLine> AddRelatedItem(RelatedItemsFilter filter, TSubstitutableLine originalLine, RelatedItem relatedItem, TSubstitutableLine focus = null)
        {
            TSubstitutableLine activeLine;
            TSubstitutableLine[] lines;
            if (SplitSerialTrackingItems && IsSerialTrackingItem(relatedItem.RelatedInventoryID))
            {
                var baseQty = INUnitAttribute.ConvertToBase(LinesCache, relatedItem.RelatedInventoryID, relatedItem.UOM, relatedItem.QtySelected ?? 0, INPrecision.NOROUND);
                lines = AddSerialTrackingRelatedItems(filter, originalLine, relatedItem, focus, baseQty).ToArray();
            }
            else
            {
                focus = focus ?? FindFocusFor(originalLine);
                activeLine = AddRelatedItem(filter, originalLine, relatedItem, focus, relatedItem.UOM, relatedItem.QtySelected);
                lines = new[] { activeLine };
            }
            return lines;
        }

        protected virtual IEnumerable<TSubstitutableLine> AddSerialTrackingRelatedItems(RelatedItemsFilter filter, TSubstitutableLine originalLine, RelatedItem relatedItem, TSubstitutableLine focus, decimal? baseQty)
        {
            var lines = new List<TSubstitutableLine>();

            if (baseQty <= 0)
                return lines;

            if (baseQty % 1 > 0)
            {
                var newQty = INUnitAttribute.ConvertFromBase(LinesCache, relatedItem.RelatedInventoryID, relatedItem.UOM, Math.Ceiling(baseQty ?? 0), INPrecision.NOROUND);
                AllRelatedItems.Cache.SetValueExt<RelatedItem.qtySelected>(relatedItem, newQty);
            }

            var inventory = InventoryItem.PK.Find(Base, relatedItem.RelatedInventoryID);
            var activeLine = originalLine;
            while (baseQty > 0)
            {
                focus = focus ?? FindFocusFor(activeLine);
                var relatedItemQty = baseQty > 1 ? 1 : baseQty;//the last item qty will be rounded to 1m
                activeLine = AddRelatedItem(filter, originalLine, relatedItem, focus, inventory?.BaseUnit, relatedItemQty);
                lines.Add(activeLine);
                focus = null;
                baseQty -= relatedItemQty;
            }
            return lines;
        }

        protected virtual TSubstitutableLine AddRelatedItem(RelatedItemsFilter filter, TSubstitutableLine originalLine, RelatedItem relatedItem, 
            TSubstitutableLine focus, string uom, decimal? qty)
        {
            try
            {
                if (focus != null)
                {
                    LinesCache.InsertPositionMode = true;
                    LinesCache.InsertPosition = LinesCache.Keys.ToDictionary(x => x, x => LinesCache.GetValue(focus, x));
                }

                var newLine = new TSubstitutableLine();
                newLine.InventoryID = relatedItem.RelatedInventoryID;
                newLine.SubItemID = relatedItem.SubItemID;
                newLine.SiteID = relatedItem.SiteID;
                newLine.UOM = uom;
                newLine.Qty = qty;

                newLine = (TSubstitutableLine)LinesCache.Insert(newLine);

                var curyExtPrice = CalculateExtPrice(filter, originalLine, relatedItem, newLine);
                if (curyExtPrice != null)
                {
                    newLine.CuryExtPrice = curyExtPrice;
                    newLine = (TSubstitutableLine)LinesCache.Update(newLine);
                    LinesCache
                        .GetSetterFor(newLine)
                        .Set(x => x.ManualPrice, true);
                }

                AddRelatedItemHistory(filter, originalLine, newLine, relatedItem);

                return newLine;
            }
            finally 
            {
                if (focus != null)
                {
                    LinesCache.InsertPositionMode = false;
                    LinesCache.InsertPosition = null;
                }
            }
        }

        protected virtual TSubstitutableLine DecreaseOriginalItemQty(RelatedItemsFilter filter, TSubstitutableLine originalLine, RelatedItem relatedItem)
        {
            var oldOriginal = (TSubstitutableLine)LinesCache.CreateCopy(originalLine);
            var updated = (TSubstitutableLine)LinesCache.CreateCopy(originalLine);

            updated.Qty = originalLine.Qty - filter.Qty;
            updated = (TSubstitutableLine)LinesCache.Update(updated);

            var extPrice = CalculateExtPrice(filter, oldOriginal, relatedItem, updated);

            if (extPrice != null)
            {
                updated.CuryExtPrice = extPrice;
                updated = (TSubstitutableLine)LinesCache.Update(updated);
            }

            return updated;
        }

        protected virtual IEnumerable<TSubstitutableLine> SubstituteOriginalItem(RelatedItemsFilter filter, TSubstitutableLine originalLine, RelatedItem relatedItem)
        {
            TSubstitutableLine activeLine;
            TSubstitutableLine[] lines;
            if (SplitSerialTrackingItems && IsSerialTrackingItem(relatedItem.RelatedInventoryID))
            {
                var oldOriginal = (TSubstitutableLine)LinesCache.CreateCopy(originalLine);
                var inventory = InventoryItem.PK.Find(Base, relatedItem.RelatedInventoryID);

                SubstituteOriginalItem(filter, originalLine, relatedItem, inventory.BaseUnit, 1);

                var baseQty = INUnitAttribute.ConvertToBase(LinesCache, relatedItem.RelatedInventoryID, relatedItem.UOM, relatedItem.QtySelected ?? 0, INPrecision.NOROUND);
                baseQty--;

                lines = AddSerialTrackingRelatedItems(filter, oldOriginal, relatedItem, null, baseQty).ToArray();
            }
            else
            {
                activeLine = SubstituteOriginalItem(filter, originalLine, relatedItem, relatedItem.UOM, relatedItem.QtySelected);
                lines = new[] { activeLine };
            }
            return lines;
        }

        protected virtual TSubstitutableLine SubstituteOriginalItem(RelatedItemsFilter filter, TSubstitutableLine originalLine, RelatedItem relatedItem, string uom, decimal? qty)
        {
            var oldOriginal = (TSubstitutableLine)LinesCache.CreateCopy(originalLine);
            var updated = (TSubstitutableLine)LinesCache.CreateCopy(originalLine);
            
            updated.InventoryID = relatedItem.RelatedInventoryID;
            updated.SubItemID = relatedItem.SubItemID;
            updated.SiteID = relatedItem.SiteID ?? originalLine.SiteID;

            void uomDefaulting(PXCache sender, PXFieldDefaultingEventArgs args)
            {
                args.NewValue = uom;
                args.Cancel = true;
            };

            try
            {
                Base.FieldDefaulting.AddHandler(typeof(TSubstitutableLine), nameof(ISubstitutableLine.UOM), uomDefaulting);
                updated = (TSubstitutableLine)LinesCache.Update(updated);
            }
            finally
            {
                Base.FieldDefaulting.RemoveHandler(typeof(TSubstitutableLine), nameof(ISubstitutableLine.UOM), uomDefaulting);
            }

            updated.Qty = qty;
            updated = (TSubstitutableLine)LinesCache.Update(updated);

            var extPrice = CalculateExtPrice(filter, oldOriginal, relatedItem, updated);

            if (extPrice != null)
            {
                updated.CuryExtPrice = extPrice;
                updated = (TSubstitutableLine)LinesCache.Update(updated);
            }
            else if(updated.ManualPrice == true)
            {
                updated.ManualPrice = false;
                updated = (TSubstitutableLine)LinesCache.Update(updated);
            }

            UpdateRelatedItemHistory(filter, oldOriginal, updated, relatedItem);

            return updated;
        }

        protected virtual decimal? CalculateExtPrice(RelatedItemsFilter filter, TSubstitutableLine originalLine, RelatedItem relatedItem, TSubstitutableLine relatedItemLine)
        {
            decimal? selectedQtyFactor;
            if(originalLine.InventoryID == relatedItemLine.InventoryID)
            {
                if (originalLine.ManualPrice != true)
                    return null;
                selectedQtyFactor = originalLine.Qty - filter.Qty;
            }
            else
            {
                if (filter.KeepOriginalPrice != true)
                    return null;
                selectedQtyFactor = filter.Qty;
            }

            var originalUnitPrice = originalLine.ManualPrice == true
                ? ((originalLine.Qty ?? 0) == 0 ? originalLine.CuryExtPrice : (originalLine.CuryExtPrice / originalLine.Qty))
                : originalLine.CuryUnitPrice;

            var curyExtPrice = originalUnitPrice * selectedQtyFactor;

            if (originalLine.InventoryID == relatedItemLine.InventoryID)
                return curyExtPrice;

            decimal? qtySelected = relatedItem.QtySelected;
            if (relatedItem.UOM != relatedItemLine.UOM)
            {
                qtySelected = INUnitAttribute.ConvertToBase(LinesCache, relatedItemLine.InventoryID, relatedItem.UOM, qtySelected ?? 0, INPrecision.NOROUND);
                qtySelected = INUnitAttribute.ConvertFromBase(LinesCache, relatedItemLine.InventoryID, relatedItemLine.UOM, qtySelected ?? 0, INPrecision.NOROUND);
            }

            var relatedItemLineCuryExtPrice = curyExtPrice * relatedItemLine.Qty / qtySelected;
            return relatedItemLineCuryExtPrice;
        }

        #endregion

        #region Event handlers

        protected virtual void _(Events.RowSelected<RelatedItemsFilter> e)
        {
            if (e.Row == null)
                return;

            PXUIFieldAttribute.SetEnabled<RelatedItemsFilter.keepOriginalPrice>(e.Cache, e.Row, AllowToKeepOriginalPrice());
        }

        protected virtual void _(Events.RowSelected<TSubstitutableDocument> e)
        {
            if (e.Row == null)
                return;
            SetRelatedItemsVisible(AllowRelatedItems(e.Row));
        }

        protected virtual void _(Events.RowInserted<TSubstitutableLine> e)
        {
            ResetSubstitutionRequired(e.Row);
        }

        protected virtual void _(Events.FieldDefaulting<RelatedItem, RelatedItem.curyUnitPrice> e)
        {
            e.NewValue = CalculateUnitPrice(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowSelected<RelatedItem> e)
        {
            if (e.Row == null)
                return;
            RaiseRelatedItemWarning(e.Row);
        }

        protected virtual void _(Events.RowUpdated<RelatedItem> e)
        {
            if (!e.Cache.ObjectsEqual<RelatedItem.selected>(e.OldRow, e.Row))
            {
                if (e.Row.Selected == true)
                {
                    var otherSelectedItems = e.Cache
                        .Updated
                        .OfType<RelatedItem>()
                        .Except(e.Row)
                        .Where(x => x.Selected == true)
                        .ToArray();
                    if (otherSelectedItems.Any() && (SingleSelection(e.Row.Relation) || otherSelectedItems.Any(x => x.Relation != e.Row.Relation)))
                    {
                        foreach (var relatedItem in otherSelectedItems)
                        {
                            e.Cache.SetValue<RelatedItem.selected>(relatedItem, false);
                            e.Cache.SetStatus(relatedItem, PXEntryStatus.Notchanged);
                            GetRelatedItemsView(relatedItem)?.RequestRefresh();
                        }

                        if (RelatedItemsFilter.Current.KeepOriginalPrice == true)
                        {
                            bool newKeepPrice = AllowToKeepOriginalPrice(e.Row.Relation);
                            bool oldKeepPrice = AllowToKeepOriginalPrice(otherSelectedItems[0].Relation);
                            if (newKeepPrice != oldKeepPrice)
                            {
                                RelatedItemsFilter.Cache.SetValueExt<RelatedItemsFilter.keepOriginalPrice>(RelatedItemsFilter.Current, false);
                                RelatedItemsFilter.View.RequestRefresh();
                            }
                        }
                    }
                }
                else
                {
                    if (RelatedItemsFilter.Current.KeepOriginalPrice == true && AllowToKeepOriginalPrice(e.Row.Relation) && !AllowToKeepOriginalPrice())
                    {
                        RelatedItemsFilter.Cache.SetValueExt<RelatedItemsFilter.keepOriginalPrice>(RelatedItemsFilter.Current, false);
                        RelatedItemsFilter.View.RequestRefresh();
                    }
                    e.Cache.SetStatus(e.Row, PXEntryStatus.Notchanged);

                    e.Cache.ClearQueryCache();
                }
            }

            AllRelatedItems.View.RequestRefresh();
            GetRelatedItemsView(e.Row)?.RequestRefresh();
        }

        protected virtual void _(Events.RowUpdated<RelatedItemsFilter> e)
        {
            if (!e.Cache.ObjectsEqual<RelatedItemsFilter.qty>(e.OldRow, e.Row))
            {
                foreach (RelatedItem relatedItem in AllRelatedItems.Select())
                {
                    AllRelatedItems.Cache.SetDefaultExt<RelatedItem.qtySelected>(relatedItem);
                }
            }
            else if(!e.Cache.ObjectsEqual<RelatedItemsFilter.onlyAvailableItems>(e.OldRow, e.Row))
            {
                TSubstitutableLine substitutableLine = (TSubstitutableLine)LinesCache.Current;
                RelationType relation = RelationType.None;
                if (e.Row.OnlyAvailableItems == ((SOSetup)PXSetup<SOSetup>.Select(Base))?.ShowOnlyAvailableRelatedItems)
                    relation = (RelationType)(GetSubstitutableLineExt(substitutableLine)?.RelatedItemsRelation ?? 0);
                else
                {
                    var relatedItemsAttribute = LinesCache.GetAttributesOfType<RelatedItemsAttribute>(substitutableLine, nameof(ISubstitutableLineExt.RelatedItems)).FirstOrDefault();
                    if (relatedItemsAttribute != null)
                        relatedItemsAttribute.FindRelatedItems(Base, substitutableLine, e.Row.OnlyAvailableItems, out relation, out RelationType _);
                }

                SetTabsVisibility(e.Row, relation);

                AllRelatedItems.Cache.ClearQueryCache();
            }
        }

        protected virtual PXView GetRelatedItemsView(RelatedItem relatedItem)
        {
            switch(relatedItem.Relation)
            {
                case InventoryRelation.Substitute:
                    return SubstituteItems.View;
                case InventoryRelation.UpSell:
                    return UpSellItems.View;
                case InventoryRelation.CrossSell:
                    return CrossSellItems.View;
                case InventoryRelation.Other:
                    return OtherRelatedItems.View;
                default:
                    return null;
            }
        }

        protected virtual void _(Events.RowUpdated<TSubstitutableLine> e)
        {
            if (e.OldRow.InventoryID != null && e.Row.InventoryID != e.OldRow.InventoryID)
                RemoveLineFromHistory(e.Row);
            if(e.Row.InventoryID != e.OldRow.InventoryID)
                ResetSubstitutionRequired(e.Row);
        }

        #endregion

        #region Price calculation

        protected virtual decimal? CalculateUnitPrice(PXCache cache, RelatedItem relatedItem)
        {
            var document = (TSubstitutableDocument)DocumentCache.Current;

            var substitutableLine = (TSubstitutableLine)LinesCache.CreateInstance();
            substitutableLine.CustomerID = document.CustomerID;
            substitutableLine.InventoryID = relatedItem.RelatedInventoryID;
            substitutableLine.SiteID = relatedItem.SiteID;
            substitutableLine.UOM = relatedItem.UOM;
            substitutableLine.Qty = relatedItem.QtySelected;

            object curyUnitPrice;
            LinesCache.RaiseFieldDefaulting(nameof(substitutableLine.CuryUnitPrice), substitutableLine, out curyUnitPrice);
            return curyUnitPrice as decimal? ?? 0;
        }

        #endregion

        #region RelatedItemHistory synchronization

        protected virtual RelatedItemHistory UpdateRelatedItemHistory(RelatedItemsFilter filter, TSubstitutableLine originalLine,
            TSubstitutableLine relatedLine, RelatedItem relatedItem)
        {
            var historyLine = FindHistoryLine(relatedLine) ?? new RelatedItemHistory();
            FillRelatedItemHistory(historyLine, filter, originalLine, relatedLine, relatedItem);
            return (RelatedItemHistory)RelatedItemsHistory.Cache.Update(historyLine);
        }

        protected virtual RelatedItemHistory AddRelatedItemHistory(RelatedItemsFilter filter, TSubstitutableLine originalLine, 
            TSubstitutableLine relatedLine, RelatedItem relatedItem)
        {
            var historyLine = new RelatedItemHistory();
            FillRelatedItemHistory(historyLine, filter, originalLine, relatedLine, relatedItem);
            historyLine = (RelatedItemHistory)RelatedItemsHistory.Cache.Insert(historyLine);
            var substitutableLineExt = GetSubstitutableLineExt(relatedLine);
            if (substitutableLineExt != null)
                substitutableLineExt.HistoryLineID = historyLine.LineID;
            return historyLine;
        }

        protected virtual RelatedItemHistory FindHistoryLine(TSubstitutableLine relatedLine)
        {
            RelatedItemHistory historyLine;
            var substitutableLineExt = GetSubstitutableLineExt(relatedLine);
            var historyLineID = substitutableLineExt?.HistoryLineID;
            if (historyLineID != null)
            {
                historyLine = new RelatedItemHistory 
                { 
                    LineID = historyLineID 
                };
                historyLine = (RelatedItemHistory)RelatedItemsHistory.Cache.Locate(historyLine);
                if (historyLine == null)
                    historyLine = RelatedItemHistory.PK.Dirty.Find(Base, historyLineID);
                return historyLine;
            }

            historyLine = FindHistoryLine(relatedLine.LineNbr);
            if (historyLine != null && substitutableLineExt != null)
                substitutableLineExt.HistoryLineID = historyLine.LineID;

            return historyLine;
        }

        protected abstract RelatedItemHistory FindHistoryLine(int? lineNbr);

        protected virtual void FillRelatedItemHistory(RelatedItemHistory historyLine, RelatedItemsFilter filter, TSubstitutableLine originalLine, TSubstitutableLine relatedLine, RelatedItem relatedItem)
        {
            historyLine.OriginalInventoryID = originalLine.InventoryID;
            historyLine.OriginalInventoryUOM = originalLine.UOM;
            decimal? originalQty = filter.Qty < originalLine.Qty
                ? filter.Qty
                : originalLine.Qty;
            originalQty = originalQty * relatedLine.Qty / relatedItem.QtySelected;
            historyLine.OriginalInventoryQty = originalQty;

            historyLine.RelatedInventoryID = relatedLine.InventoryID;
            historyLine.RelatedInventoryUOM = relatedLine.UOM;
            historyLine.RelatedInventoryQty = relatedLine.Qty;

            historyLine.Relation = relatedItem.Relation;
            historyLine.Tag = relatedItem.Tag;

            historyLine.DocumentDate = GetDocumentDate((TSubstitutableDocument)DocumentCache.Current);
        }

        protected virtual void RemoveLineFromHistory(TSubstitutableLine line)
        {
            var historyLine = FindHistoryLine(line);
            if(historyLine != null)
            {
                RelatedItemsHistory.Cache.Delete(historyLine);
                var substitutableLineExt = GetSubstitutableLineExt(line);
                if (substitutableLineExt != null)
                    substitutableLineExt.HistoryLineID = null;
            }
        }

        #endregion

        protected virtual bool IsSerialTrackingItem(int? inventoryID)
        {
            var inventory = InventoryItem.PK.Find(Base, inventoryID);
            var lotSerClass = INLotSerClass.PK.Find(Base, inventory?.LotSerClassID);
            return lotSerClass?.LotSerTrack == INLotSerTrack.SerialNumbered;
        }

        protected virtual void SetRelatedItemsVisible(bool visible)
        {
            PXUIFieldAttribute.SetVisible(LinesCache, null, nameof(ISubstitutableLineExt.RelatedItems), visible);
            PXUIFieldAttribute.SetVisible(LinesCache, null, nameof(ISubstitutableLine.SubstitutionRequired), visible);
        }

        protected virtual bool SingleSelection(string relation) => relation.IsIn(InventoryRelation.Substitute, InventoryRelation.UpSell);

        protected virtual bool AllowToKeepOriginalPrice(string relation) => relation == InventoryRelation.Substitute;

        protected virtual bool AllowToKeepOriginalPrice()
        {
            var selectedRelatedItems = AllRelatedItems.Cache.Updated
                .OfType<RelatedItem>()
                .Where(x => x.Selected == true)
                .ToArray();

            return selectedRelatedItems.Any() 
                && selectedRelatedItems.All(x => AllowToKeepOriginalPrice(x.Relation));
        }

        protected virtual void RaiseRelatedItemWarning(RelatedItem relatedItem)
        {
            PXSetPropertyException error = null;
            if (relatedItem.Selected == true 
                && relatedItem.AvailableQty < relatedItem.QtySelected)
                error = new PXSetPropertyException(Messages.AvailableQtyIsLessThanSelected, PXErrorLevel.Warning);

            AllRelatedItems.Cache.RaiseExceptionHandling<RelatedItem.qtySelected>(relatedItem, relatedItem.QtySelected, error);
        }

        protected virtual void ResetSubstitutionRequired(TSubstitutableLine line)
        {
            var substitutableLineExt = GetSubstitutableLineExt(line);
            if (substitutableLineExt?.SuggestRelatedItems == true)
            {
                line.SubstitutionRequired = ((RelationType)(substitutableLineExt.RelatedItemsRequired ?? 0)).HasFlag(RelationType.Substitute);
            }
        }
    }
}
