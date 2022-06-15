using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PM.MaterialManagement
{
    public class PMLotSerialNbrSelectorAttribute : PXCustomSelectorAttribute
    {
		protected Type inventoryField;
		protected Type subitemField;
		protected Type locationField;
		protected Type projectField;
		protected Type taskField;
		protected Type master;

		public PMLotSerialNbrSelectorAttribute(Type search,  Type inventory, Type subItem, Type location, Type project, Type task, Type master) 
			: base(search,
				    typeof(PMLotSerialStatusSelectorRecord.lotSerialNbr),
					typeof(PMLotSerialStatusSelectorRecord.siteID),
					typeof(PMLotSerialStatusSelectorRecord.locationID),
					typeof(PMLotSerialStatusSelectorRecord.qtyOnHand),
					typeof(PMLotSerialStatusSelectorRecord.qtyAvail),
					typeof(PMLotSerialStatusSelectorRecord.expireDate))
		{
			this.inventoryField = inventory;
			this.subitemField = subItem;
			this.locationField = location;
			this.projectField = project;
			this.taskField = task;
			this.master = master;
		}

		protected virtual IEnumerable GetRecords()
		{
			object current = null;
			if (PXView.Currents != null && PXView.Currents.Length > 0)
			{
				current = PXView.Currents[0];
			}
			else
			{
				current = _Graph.Caches[_CacheType].Current;
			}

			int? inventoryID = null;
			int? subItemID = null;
			int? locationID = null;
			int? projectID = null;
			int? taskID = null;
			if (PXView.Parameters != null && PXView.Parameters.Length == 3)
            {
				inventoryID = (int?) PXView.Parameters[0];
				subItemID = (int?) PXView.Parameters[1];
				locationID = (int?) PXView.Parameters[2];
			}
            else
            {
				inventoryID = (int?)_Graph.Caches[_CacheType].GetValue(current, inventoryField.Name);
				subItemID = (int?)_Graph.Caches[_CacheType].GetValue(current, subitemField.Name);
				locationID = (int?)_Graph.Caches[_CacheType].GetValue(current, locationField.Name);
			}

			
			if (master != null)
            {
				object parent = PXParentAttribute.SelectParent(_Graph.Caches[_CacheType], current, master);
				if (parent != null)
                {
					projectID = (int?)_Graph.Caches[master].GetValue(parent, projectField.Name);
					taskID = (int?)_Graph.Caches[master].GetValue(parent, taskField.Name);
				}
            }
            else
            {
				projectID = (int?)_Graph.Caches[_CacheType].GetValue(current, projectField.Name);
				taskID = (int?)_Graph.Caches[_CacheType].GetValue(current, taskField.Name);
			}

			return GetSelectorRecords(inventoryID, subItemID, locationID, projectID, taskID);
		}

		protected virtual IList<PMLotSerialStatusSelectorRecord> GetSelectorRecords(int? inventoryID, int? subItemID, int? locationID, int? projectID, int? taskID)
        {
			List<PMLotSerialStatusSelectorRecord> list = new List<PMLotSerialStatusSelectorRecord>();

			PMProject project = PMProject.PK.Find(_Graph, projectID);
			if (project != null && project.NonProject != true && taskID != null)
			{
				if (project.AccountingMode == ProjectAccountingModes.Linked)
				{
					foreach (INLotSerialStatus item in SelectINLotSerial(inventoryID, subItemID, locationID))
					{
						list.Add(ConvertFromStatus(item));
					}
				}
				else
				{
					foreach (PMLotSerialStatus item in SelectPMLotSerial(inventoryID, subItemID, locationID, projectID, taskID))
					{
						list.Add(ConvertFromStatus(item));
					}
				}

			}
			else
			{
				foreach (PMLotSerialStatus item in SelectPMLotSerial(inventoryID, subItemID, locationID, ProjectDefaultAttribute.NonProject(), 0))
				{
					list.Add(ConvertFromStatus(item));
				}
			}

			return list;
		}

		

		private List<INLotSerialStatus> SelectINLotSerial(int? inventoryID, int? subItemID, int? locationID)
        {
			var select = new PXSelectReadonly<INLotSerialStatus,
					Where<INLotSerialStatus.inventoryID, Equal<Required<INLotSerialStatus.inventoryID>>,
					And<INLotSerialStatus.subItemID, Equal<Required<INLotSerialStatus.subItemID>>,
					And<INLotSerialStatus.locationID, Equal<Required<INLotSerialStatus.locationID>>,
					And<INLotSerialStatus.qtyOnHand, Greater<Zero>>>>>>(_Graph);

			using (new PXFieldScope(select.View,
					typeof(INLotSerialStatus.inventoryID),
					typeof(INLotSerialStatus.subItemID),
					typeof(INLotSerialStatus.siteID),
					typeof(INLotSerialStatus.locationID),
					typeof(INLotSerialStatus.lotSerialNbr),
					typeof(INLotSerialStatus.qtyOnHand),
					typeof(INLotSerialStatus.expireDate)))
			{
				return select.Select(inventoryID, subItemID, locationID).RowCast<INLotSerialStatus>().ToList();
			}
		}
		
		private List<PMLotSerialStatus> SelectPMLotSerial(int? inventoryID, int? subItemID, int? locationID, int? projectID, int? taskID)
		{
			var select = new PXSelectReadonly<PMLotSerialStatus,
						Where<PMLotSerialStatus.inventoryID, Equal<Required<PMLotSerialStatus.inventoryID>>,
						And<PMLotSerialStatus.subItemID, Equal<Required<PMLotSerialStatus.subItemID>>,
						And<PMLotSerialStatus.locationID, Equal<Required<PMLotSerialStatus.locationID>>,
						And<PMLotSerialStatus.projectID, Equal<Required<PMLotSerialStatus.projectID>>,
						And<PMLotSerialStatus.taskID, Equal<Required<PMLotSerialStatus.taskID>>,
						And<PMLotSerialStatus.qtyOnHand, Greater<Zero>>>>>>>>(_Graph);

			using (new PXFieldScope(select.View,
					typeof(PMLotSerialStatus.inventoryID),
					typeof(PMLotSerialStatus.subItemID),
					typeof(PMLotSerialStatus.siteID),
					typeof(PMLotSerialStatus.locationID),
					typeof(PMLotSerialStatus.lotSerialNbr),
					typeof(PMLotSerialStatus.qtyOnHand),
					typeof(PMLotSerialStatus.expireDate)))
			{
				return select.Select(inventoryID, subItemID, locationID, projectID, taskID).RowCast<PMLotSerialStatus>().ToList();
			}
		}

		protected virtual PMLotSerialStatusSelectorRecord ConvertFromStatus(INLotSerialStatus item)
        {
			PMLotSerialStatusSelectorRecord record = new PMLotSerialStatusSelectorRecord();
			record.InventoryID = item.InventoryID;
			record.SubItemID = item.SubItemID;
			record.SiteID = item.SiteID;
			record.LocationID = item.LocationID;
			record.LotSerialNbr = item.LotSerialNbr;
			record.QtyOnHand = item.QtyOnHand;
			record.QtyAvail = item.QtyAvail;
			record.ExpireDate = item.ExpireDate;

			return record;
		}

		protected virtual PMLotSerialStatusSelectorRecord ConvertFromStatus(PMLotSerialStatus item)
		{
			PMLotSerialStatusSelectorRecord record = new PMLotSerialStatusSelectorRecord();
			record.InventoryID = item.InventoryID;
			record.SubItemID = item.SubItemID;
			record.SiteID = item.SiteID;
			record.LocationID = item.LocationID;
			record.LotSerialNbr = item.LotSerialNbr;
			record.QtyOnHand = item.QtyOnHand;
			record.QtyAvail = item.QtyAvail;
			record.ExpireDate = item.ExpireDate;

			return record;
		}
	}

	public class PMLotSerialNbrAttribute : INLotSerialNbrAttribute
	{
		protected Type _ProjectType;
		protected Type _TaskType;

        public PMLotSerialNbrAttribute():base() { }

		public PMLotSerialNbrAttribute(Type InventoryType, Type SubItemType, Type LocationType, Type ProjectType, Type TaskType, Type master)
			: base()
		{
			var itemType = BqlCommand.GetItemType(InventoryType);
			if (!typeof(ILSMaster).IsAssignableFrom(itemType))
			{
				throw new PXArgumentException(nameof(itemType), IN.Messages.TypeMustImplementInterface, itemType.GetLongName(), typeof(ILSMaster).GetLongName());
			}

			_InventoryType = InventoryType;
			_SubItemType = SubItemType;
			_LocationType = LocationType;
			_ProjectType = ProjectType;
			_TaskType = TaskType;

			Type SearchType = BqlCommand.Compose(
				typeof(Search<,>),
				typeof(PMLotSerialStatusSelectorRecord.lotSerialNbr),
				typeof(Where<,,>),
				typeof(PMLotSerialStatusSelectorRecord.inventoryID),
				typeof(Equal<>),
				typeof(Optional<>),
				InventoryType,
				typeof(And<,,>),
				typeof(PMLotSerialStatusSelectorRecord.subItemID),
				typeof(Equal<>),
				typeof(Optional<>),
				SubItemType,
				typeof(And<,,>),
				typeof(PMLotSerialStatusSelectorRecord.locationID),
				typeof(Equal<>),
				typeof(Optional<>),
				LocationType,
				typeof(And<,>),
				typeof(PMLotSerialStatusSelectorRecord.qtyOnHand),
				typeof(Greater<>),
				typeof(decimal0)
				);

			PXSelectorAttribute attr = new PMLotSerialNbrSelectorAttribute(SearchType, InventoryType, SubItemType, LocationType, ProjectType, TaskType, master);
			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}

		public PMLotSerialNbrAttribute(Type InventoryType, Type SubItemType, Type LocationType, Type ProjectType, Type TaskType, Type master, Type ParentLotSerialNbrType)
			: this(InventoryType, SubItemType, LocationType, ProjectType, TaskType, master)
		{
			_Attributes[_DefAttrIndex] = new PXDefaultAttribute(ParentLotSerialNbrType) { PersistingCheck = PXPersistingCheck.NullOrBlank };
		}
	}

}
