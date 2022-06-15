using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.Common;
using PX.Objects.Common.Bql;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.RelatedItems;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Location = PX.Objects.CR.Location;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
    public class AddRelatedItemsToOrder : AddRelatedItemExt<SOOrderEntry, SOOrder, SOLine>
    {
        public static bool IsActive() => PXAccess.FeatureInstalled<FeaturesSet.relatedItems>();

        protected override DateTime? GetDocumentDate(SOOrder document) => document.OrderDate.Value;

        protected override SOLine FindFocusFor(SOLine line) => Base.Transactions.Search<SOLine.sortOrder>(line.SortOrder + 1);

        protected override RelatedItemHistory FindHistoryLine(int? lineNbr) => RelatedItemsHistory.Search<RelatedItemHistory.relatedOrderLineNbr>(lineNbr);

        public override void Initialize()
        {
            base.Initialize();

            RelatedItemsHistory.WhereAnd<Where<
                RelatedItemHistory.orderType.IsEqual<SOOrder.orderType.FromCurrent> 
                .And<RelatedItemHistory.orderNbr.IsEqual<SOOrder.orderNbr.FromCurrent>>>>();
        }

        #region Fields CacheAttached

        [PXMergeAttributes]
        [PXFormula(typeof(
            IsImport.IsEqual<False>
            .And<
                Brackets<SOOrder.dontApprove.IsEqual<True>
                    .And<SOOrder.hold.IsEqual<True>
                        .Or<SOOrder.openShipmentCntr.IsEqual<Zero>
                            .And<Brackets<SOOrder.orderQty.IsEqual<decimal0>.Or<SOOrder.openLineCntr.IsGreater<Zero>>>>>>>
                .Or<SOOrder.dontApprove.IsEqual<False>.And<SOOrder.hold.IsEqual<True>>>>
            .And<Use<Selector<SOOrder.customerID, Customer.suggestRelatedItems>>.AsBool.IsEqual<True>>
            .And<Use<Selector<SOOrder.defaultOperation, SOOrderTypeOperation.iNDocType>>.AsString.IsNotEqual<INTranType.transfer>>
            .And<SOOrder.behavior.IsNotEqual<SOBehavior.bL>>))]
        protected virtual void _(Events.CacheAttached<SOOrder.suggestRelatedItems> e) { }

        [PXMergeAttributes]
        [PXFormula(typeof(
            SOOrder.suggestRelatedItems.FromCurrent.IsEqual<True>
            .And<SOLine.operation.IsEqual<SOOperation.issue>>
            .And<SOLine.completed.IsNotEqual<True>>))]
        protected virtual void SOLine_SuggestRelatedItems_CacheAttached(PXCache cache) { }

        [SOLineRelatedItems(typeof(SubstitutableSOLine.suggestRelatedItems), typeof(SubstitutableSOLine.relatedItemsRelation), typeof(SubstitutableSOLine.relatedItemsRequired),
            DocumentDateField = typeof(SOOrder.orderDate))]
        protected virtual void SOLine_RelatedItems_CacheAttached(PXCache cache) { }

        [PXMergeAttributes]
        [PXDefault(typeof(SOOrder.orderType))]
        protected virtual void _(Events.CacheAttached<RelatedItemHistory.orderType> e) { }

        [PXMergeAttributes]
        [PXRemoveBaseAttribute(typeof(PXSelectorAttribute))]
        [PXDBDefault(typeof(SOOrder.orderNbr))]
        protected virtual void _(Events.CacheAttached<RelatedItemHistory.orderNbr> e) { }

        #endregion

		protected override RelatedItemsFilter InitializeFilter(SOOrder document, SOLine line)
		{
			var filter = base.InitializeFilter(document, line);
			filter.OrderBehavior = document.Behavior;
			return filter;
		}

        protected override decimal? GetAvailableQty(SOLine line)
        {
            var availability = Base.ItemAvailabilityExt.FetchWithBaseUOM(line, excludeCurrent: true);
            var availableQty = INUnitAttribute.ConvertFromBase(Base.Transactions.Cache, line.InventoryID, line.UOM, availability?.QtyAvail ?? 0, INPrecision.QUANTITY);
            return availableQty;
        }

        protected override void FillRelatedItemHistory(RelatedItemHistory historyLine, RelatedItemsFilter filter, SOLine originalLine, SOLine relatedLine, RelatedItem relatedItem)
        {
            base.FillRelatedItemHistory(historyLine, filter, originalLine, relatedLine, relatedItem);
            historyLine.OriginalOrderLineNbr = originalLine.LineNbr;
            historyLine.RelatedOrderLineNbr = relatedLine.LineNbr;
        }

        protected virtual void _(Events.RowUpdated<SOOrder> e)
        {
            if(!e.Cache.ObjectsEqual<SOOrder.orderDate>(e.OldRow, e.Row))
            {
                foreach (var line in Base.Transactions.Select())
                    ResetSubstitutionRequired(line);
            }
        }
    }

    public sealed class SubstitutableSOLine : PXCacheExtension<SOLine>, ISubstitutableLineExt
    {
        public static bool IsActive() => AddRelatedItemsToOrder.IsActive();

        #region SuggestRelatedItems
        [PXBool]
        public bool? SuggestRelatedItems { get; set; }
        public abstract class suggestRelatedItems : BqlBool.Field<suggestRelatedItems> { }
        #endregion

        #region RelatedItems
        [PXString]
        public string RelatedItems { get; set; }
        public abstract class relatedItems : BqlString.Field<relatedItems> { }
        #endregion

        #region RelatedItemsRelation
        [PXInt]
        public int? RelatedItemsRelation { get; set; }
        public abstract class relatedItemsRelation : BqlInt.Field<relatedItemsRelation> { }
        #endregion

        #region RelatedItemsRequired
        [PXInt]
        public int? RelatedItemsRequired { get; set; }
        public abstract class relatedItemsRequired : BqlInt.Field<relatedItemsRequired> { }
        #endregion

        #region HistoryLineID
        [PXInt]
        [PXDBDefault(typeof(RelatedItemHistory.lineID), PersistingCheck = PXPersistingCheck.Nothing)]
        public int? HistoryLineID { get; set; }
        public abstract class historyLineID : BqlInt.Field<historyLineID> { }
        #endregion
    }

    public class SOLineRelatedItemsAttribute : RelatedItemsAttribute
    {
        public SOLineRelatedItemsAttribute() : base() 
        { }

        public SOLineRelatedItemsAttribute(Type suggestRelatedItemsField, Type relationField, Type requiredField)
            : base(suggestRelatedItemsField, relationField, requiredField)
        { }

        protected override object[] GetRelatedItemsQueryArguments(PXGraph graph, ISubstitutableLine substitutableLine, DateTime? documentDate, bool? showOnlyAvailableItems)
        {
            var args = base.GetRelatedItemsQueryArguments(graph, substitutableLine, documentDate, showOnlyAvailableItems);
            var behavior = ((SOLine)substitutableLine).Behavior;
            args[args.Length - 2] = behavior;
            args[args.Length - 1] = behavior;
            return args;
        }
    }
}
