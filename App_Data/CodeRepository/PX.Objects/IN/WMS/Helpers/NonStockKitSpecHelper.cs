using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

namespace PX.Objects.IN
{
	public class NonStockKitSpecHelper
	{
		private readonly Dictionary<int, Dictionary<int, decimal>> _nonStockKitSpecs = new Dictionary<int, Dictionary<int, decimal>>();
		private readonly PXGraph _graph;
		public NonStockKitSpecHelper(PXGraph graph) => _graph = graph;

		public bool IsNonStockKit(int? inventoryID) => InventoryItem.PK.Find(_graph, inventoryID).With(i => i.StkItem == false && i.KitItem == true);
		public IReadOnlyDictionary<int, decimal> GetNonStockKitSpec(int kitInventoryID)
		{
			if (_nonStockKitSpecs.TryGetValue(kitInventoryID, out var nonStockKitSpec) == false)
			{
				var stockComponents =
					SelectFrom<INKitSpecStkDet>
					.Where<INKitSpecStkDet.kitInventoryID.IsEqual<@P.AsInt>>
					.View.Select(_graph, kitInventoryID)
					.AsEnumerable()
					.RowCast<INKitSpecStkDet>()
					.Select(r => (item: r.CompInventoryID, qty: ConvertToBaseQty(r.CompInventoryID, r.UOM, r.DfltCompQty)));

				var nonStockComponents =
					SelectFrom<INKitSpecNonStkDet>
					.Where<INKitSpecNonStkDet.kitInventoryID.IsEqual<@P.AsInt>>
					.View.Select(_graph, kitInventoryID)
					.AsEnumerable()
					.RowCast<INKitSpecNonStkDet>()
					.Select(r => (item: r.CompInventoryID, qty: ConvertToBaseQty(r.CompInventoryID, r.UOM, r.DfltCompQty)));

				_nonStockKitSpecs[kitInventoryID] = nonStockKitSpec = stockComponents.Concat(nonStockComponents).GroupBy(r => r.item.Value).ToDictionary(g => g.Key, g => g.Sum(r => r.qty));
			}
			return nonStockKitSpec;
		}

		protected decimal ConvertToBaseQty(int? inventoryID, string uom, decimal? qty) => INUnitAttribute.ConvertToBase(_graph.Caches.Select(p => p.Value).First(), inventoryID, uom, qty ?? 0, INPrecision.NOROUND);
	}
}
