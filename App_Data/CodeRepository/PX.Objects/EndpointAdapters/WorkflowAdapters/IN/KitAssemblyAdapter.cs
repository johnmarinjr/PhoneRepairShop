using System;
using System.Linq;
using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Adapters;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.EndpointAdapters.WorkflowAdapters.IN
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	[PXVersion("20.200.001", "Default")]
	internal class KitAssemblyAdapter
	{
		[FieldsProcessed(new[] {
			"ReferenceNbr",
			"Type",
			"Hold"
		})]
		protected virtual void KitAssembly_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var kitGraph = (KitAssemblyEntry)graph;

			var documentCache = kitGraph.Document.Cache;
			var nbrField = targetEntity.Fields.SingleOrDefault(f => f.Name == "ReferenceNbr") as EntityValueField;
			var typeField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Type") as EntityValueField;
			var holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;

			var kit = (INKitRegister)kitGraph.Document.Cache.CreateInstance();

			if (typeField != null)
				kitGraph.Document.Cache.SetValueExt<INKitRegister.docType>(kit, typeField.Value);

			if (nbrField != null)
				kit.RefNbr = nbrField.Value;

			kitGraph.Document.Current = kitGraph.Document.Insert(kit);

			var documentCurrent = documentCache.Current as INKitRegister;
			if (documentCache.Current == null)
				throw new InvalidOperationException("Cannot insert Kit Assembly.");

			var allocations = (targetEntity.Fields.SingleOrDefault(f => string.Equals(f.Name, "Allocations")) as EntityListField)?.Value ?? new EntityImpl[0];

			if (allocations.Any(a => a.Fields != null && a.Fields.Length > 0))
			{
				var inventory = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("KitInventoryID"));
				var revision = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("Revision"));
				var warehouse = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("WarehouseID"));
				var location = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("LocationID"));
				var qty = (EntityValueField)targetEntity.Fields.SingleOrDefault(f => f.Name.OrdinalEquals("Qty"));

				if (inventory != null)
				{
					documentCache.SetValueExt<INKitRegister.kitInventoryID>(documentCurrent, inventory.Value);
					if (revision != null)
						documentCache.SetValueExt<INKitRegister.kitRevisionID>(documentCurrent, revision.Value);
					if (warehouse != null)
						documentCurrent.LocationID = null;
					documentCache.SetValueExt<INKitRegister.siteID>(documentCurrent, warehouse.Value);
					if (location != null)
						documentCache.SetValueExt<INKitRegister.locationID>(documentCurrent, location.Value);
					if (qty != null)
						documentCache.SetValueExt<INKitRegister.qty>(documentCurrent, qty.Value);

					documentCache.Update(documentCurrent);

					// Delete auto-created allocation
					PXCache allocationsCache = kitGraph.Caches<INKitTranSplit>();
					foreach (INKitTranSplit split in allocationsCache.Inserted)
					{
						allocationsCache.Delete(split);
					}
				}
			}

			kitGraph.SubscribeToPersistDependingOnBoolField(holdField, kitGraph.putOnHold, kitGraph.releaseFromHold);
		}

		[FieldsProcessed(new[] {
			"Hold"
		})]
		protected virtual void KitAssembly_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var kitEntry = (KitAssemblyEntry)graph;
			var holdField = targetEntity.Fields.SingleOrDefault(f => f.Name == "Hold") as EntityValueField;
			kitEntry.SubscribeToPersistDependingOnBoolField(holdField, kitEntry.putOnHold, kitEntry.releaseFromHold);
		}
	}
}
