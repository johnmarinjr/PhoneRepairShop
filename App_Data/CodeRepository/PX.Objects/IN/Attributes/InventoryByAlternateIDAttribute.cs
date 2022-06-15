using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN
{
    [PXDBInt]
    [PXUIField(DisplayName = "Inventory ID")]
    public class InventoryByAlternateIDAttribute : PXDimensionSelectorAttribute
    {
        public InventoryByAlternateIDAttribute(Type bAccount, Type alternateID, Type alternateType, Type restrictInventoryByAlternateID)
            : base(BaseInventoryAttribute.DimensionName)
        {
            var selector = new CustomSelectorAttribute(bAccount, alternateID, alternateType, restrictInventoryByAlternateID);
            RegisterSelector(selector);
            DescriptionField = selector.DescriptionField;
        }

        public class CustomSelectorAttribute : PXCustomSelectorAttribute
        {
            protected Type BAccountField { get; }
            protected Type AlternateIDField { get; }
            protected Type AlternateTypeField { get; }
            protected Type RestrictByAlternateIDField { get; }

            public CustomSelectorAttribute(Type bAccount, Type alternateID, Type alternateType, Type restrictInventoryByAlternateID)
                : this()
            {
                BAccountField = bAccount;
                AlternateIDField = alternateID;
                AlternateTypeField = alternateType;
                RestrictByAlternateIDField = restrictInventoryByAlternateID;
            }

            protected CustomSelectorAttribute()
                : base(typeof(SearchFor<InventoryItem.inventoryID>
                      .In<
                          SelectFrom<InventoryItem>
                            .LeftJoin<INItemXRef>
                                .On<INItemXRef.FK.InventoryItem>>),
                        typeof(InventoryItem.inventoryCD),
                        typeof(InventoryItem.descr),
                        typeof(InventoryItem.itemClassID),
                        typeof(InventoryItem.itemStatus),
                        typeof(InventoryItem.itemType),
                        typeof(InventoryItem.baseUnit),
                        typeof(InventoryItem.salesUnit),
                        typeof(InventoryItem.purchaseUnit),
                        typeof(InventoryItem.basePrice),
                        typeof(INItemXRef.uOM)
                    )
            {
                ValidateValue = false;
                SuppressUnconditionalSelect = true;

                SubstituteKey = typeof(InventoryItem.inventoryCD);
                DescriptionField = typeof(InventoryItem.descr);   
            }

            public override void CacheAttached(PXCache sender)
            {
                PXUIFieldAttribute.SetDisplayName<INItemXRef.uOM>(sender.Graph.Caches<INItemXRef>(), Messages.AlternateIDUnit);

                base.CacheAttached(sender);
            }

            protected virtual BqlCommand BuildQuery(PXCache cache, object row, out object[] parameters)
            {
                BqlCommand query;

                var restrictByAlternateID = (bool?)cache.GetValue(row, RestrictByAlternateIDField.Name) == true;
                if (restrictByAlternateID)
                {
                    query = new SelectFrom<InventoryItem>
                        .InnerJoin<INItemXRef>
                            .On<INItemXRef.FK.InventoryItem>
                        .Where<INItemXRef.alternateID.IsEqual<@P.AsString>
                        .And<Brackets<
                            INItemXRef.alternateType.IsEqual<@P.AsString>
                                .And<INItemXRef.bAccountID.IsEqual<@P.AsInt>>
                            .Or<INItemXRef.alternateType.IsNotEqual<INAlternateType.cPN>
                                .And<INItemXRef.alternateType.IsNotEqual<INAlternateType.vPN>>>>>>
                        .AggregateTo<
                            GroupBy<InventoryItem.inventoryID>,
                            Max<INItemXRef.uOM>>();
                    parameters = new object[]
                    {
                        cache.GetValue(row, AlternateIDField.Name),
                        cache.GetValue(row, AlternateTypeField.Name),
                        cache.GetValue(row, BAccountField.Name)
                    };
                }
                else
                {
                    query = new SelectFrom<InventoryItem>();
                    parameters = Array<object>.Empty;
                }

                query = query.WhereAnd<Where<
                    Match<Current<AccessInfo.userName>>
                    .And<InventoryItem.itemStatus.IsNotIn<InventoryItemStatus.unknown, InventoryItemStatus.inactive, InventoryItemStatus.markedForDeletion>>>>();               

                return query;
            }

            protected virtual IEnumerable GetRecords()
            {
                var cache = _Graph.Caches[BqlTable];
                var row = cache.Current;
                if (row == null)
                    return Array<InventoryItem>.Empty;

                if (PXView.Filters.Length == 1)
                {
                    var inventoryFilter = PXView.Filters
                        .OfType<PXFilterRow>()
                        .FirstOrDefault(x =>
                            x.DataField.Equals(nameof(InventoryItem.inventoryID), StringComparison.InvariantCultureIgnoreCase)
                            && x.Condition.IsIn(PXCondition.ISNULL, PXCondition.EQ));
                    if (inventoryFilter != null)
                    {
                        if(inventoryFilter.Value == null)
                            return Array<InventoryItem>.Empty;

                        var inventoryID = Convert.ToInt32(inventoryFilter.Value);
                        var inventory = InventoryItem.PK.Find(_Graph, inventoryID);
                        if (inventory != null)
                            return new[] { inventory };
                    }
                }

                object[] queryParams;
                var query = BuildQuery(cache, row, out queryParams);
                var view = cache.Graph.TypedViews.GetView(query, true);

                int startRow = PXView.StartRow;
                int totalRows = 0;

                var rows = view.Select(PXView.Currents, queryParams, PXView.Searches, PXView.SortColumns, PXView.Descendings, PXView.Filters, ref startRow, PXView.MaximumRows, ref totalRows);

                PXView.StartRow = 0;

                return rows;
            }

            public override void SubstituteKeyFieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
            {
                if (e.NewValue == null)
                    return;

                var inventoryItem = (InventoryItem) SelectFrom<InventoryItem>.Where<InventoryItem.inventoryCD.IsEqual<@P.AsString>>.View.ReadOnly.SelectWindowed(sender.Graph, 0, 1, e.NewValue) ??
									(e.NewValue is int? ? InventoryItem.PK.Find(sender.Graph, (int?)e.NewValue) : null);

                if (inventoryItem == null)
                    throw new PXSetPropertyException(PXMessages.LocalizeFormat(ErrorMessages.ValueDoesntExist, _FieldName, e.NewValue));

                e.NewValue = inventoryItem.InventoryID;
            }

            public override void SubstituteKeyFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
            {
                object value = e.ReturnValue;
                e.ReturnValue = null;

                base.FieldSelecting(sender, e);

                var inventoryItem = InventoryItem.PK.Find(sender.Graph, (int?)value);
                if (inventoryItem != null)
                {
                    e.ReturnValue = inventoryItem.InventoryCD;
                }
                else
                {
                    if (e.Row != null)
                        e.ReturnValue = null;
                }
            }
        }
    }
}
