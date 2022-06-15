using PX.Commerce.Core;
using PX.Common;
using PX.Data;
using PX.Data.Process;
using PX.Objects.AR;
using PX.Objects.GDPR;
using PX.Objects.SO;
using PX.SM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Commerce.Objects
{

	#region DACs
	[Serializable]
	[PXHidden]
	public partial class PIIEntity : IBqlTable
	{
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }

		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected { get; set; }

		[PXUIField(DisplayName = "Type", Enabled = false)]
		[PXDefault]
		[PXString(IsKey = true)]
		public virtual string Type { get; set; } // ordertype or Invoice

		[PXUIField(DisplayName = "Ref Nbr", Enabled = false)]
		[PXDefault]
		[PXString(IsKey = true)]
		public virtual string Reference { get; set; } //orderNbr or Invoice Ref Nbr

		[PIIMasterEntityAttribute]
		[PXUIField(DisplayName = "Master Entity Type", Enabled = false)]
		[PXDefault]
		[PXString(IsKey = true)]
		public virtual String MasterEntityType { get; set; }

		#region Ship To Fields

		[PXString]
		[PXUIField(DisplayName = "Ship-To Company Name")]
		public virtual String ShipToCompanyName { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To Attention", Visible = false)]
		public virtual String ShipToAttention { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To Email")]
		public virtual String ShipToEmail { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To Phone 1")]
		public virtual String ShipToPhone1 { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To Address Line 1")]
		public virtual String ShipToAddressLine1 { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To Address Line 2", Visible = false)]
		public virtual String ShipToAddressLine2 { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To City")]
		public virtual String ShipToCity { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To State")]
		public virtual String ShipToState { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To Country")]
		public virtual String ShipToCountry { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Ship-To Postal Code")]
		public virtual String ShipToPostalCode { get; set; }

		#endregion

		#region Bill To Fields

		[PXString]
		[PXUIField(DisplayName = "Bill-To Company Name")]
		public virtual String BillToCompanyName { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To Attention", Visible = false)]
		public virtual String BillToAttention { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To Email")]
		public virtual String BillToEmail { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To Phone 1")]
		public virtual String BillToPhone1 { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To Address Line 1")]
		public virtual String BillToAddressLine1 { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To Address Line 2", Visible = false)]
		public virtual String BillToAddressLine2 { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To City")]
		public virtual String BillToCity { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To State")]
		public virtual String BillToState { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To Country")]
		public virtual String BillToCountry { get; set; }

		[PXString]
		[PXUIField(DisplayName = "Bill-To Postal Code")]
		public virtual String BillToPostalCode { get; set; }

		#endregion

		[PXBool]
		[PXUIField(DisplayName = "Pseudonymized", Visible = false)]
		public virtual bool? Pseudonymized { get; set; }

		[PXGuid]
		public virtual Guid? NoteID { get; set; }

		[PXInt]
		public virtual int? BillingAddressID { get; set; }

		[PXInt]
		public virtual int? ShippingAddressID { get; set; }

		[PXInt]
		public virtual int? BillingContactID { get; set; }

		[PXInt]
		public virtual int? ShippingContactID { get; set; }
	}

	[Serializable]
	[PXHidden]
	public class PIIFilter : IBqlTable
	{

		#region MasterEntity
		public abstract class masterEntity : PX.Data.BQL.BqlString.Field<masterEntity> { }

		[PIIMasterEntityAttribute]
		[PXUIField(DisplayName = "Master Entity")]
		[PXDefault(PIIMasterEntityAttribute.SalesOrder)]
		[PXString]
		public virtual String MasterEntity { get; set; }
		#endregion

		#region Action
		public abstract class action : PX.Data.BQL.BqlString.Field<action> { }

		[PXString]
		[PIIAction]
		[PXDefault(PIIActionAttribute.Pseudonymize)]
		[PXUIField(DisplayName = "Action")]
		public virtual String Action { get; set; }
		#endregion

		#region Document Date Within X Days
		public abstract class documentDateWithinXDays : PX.Data.BQL.BqlInt.Field<documentDateWithinXDays> { }

		[PXInt]
		[PXUIField(DisplayName = "Document Date Within")]
		[PXDefault(30)]
		public virtual int? DocumentDateWithinXDays { get; set; }


		[PXString]
		[PXDefault(BCCaptions.Days, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual string Days { get; set; }
		#endregion
	}

	#endregion

	#region Handlers

	public class ErasePIIEntityProcess : GDPRPseudonymizeProcessBase
	{
		protected override void ChildLevelProcessor(PXGraph processingGraph, Type childTable, IEnumerable<PXPersonalDataFieldAttribute> fields, IEnumerable<object> childs, Guid? topParentNoteID)
		{
			PseudonymizeChilds(processingGraph, childTable, fields, childs);
			ErasePseudonymizedData(processingGraph, childTable, childs);
		}

		private void ErasePseudonymizedData(PXGraph processingGraph, Type childTable, IEnumerable<object> childs)
		{
			foreach (object child in childs)
			{
				List<PXDataFieldParam> assignsDB = new List<PXDataFieldParam>();

				assignsDB.Add(new PXDataFieldAssign(nameof(IPseudonymizable.PseudonymizationStatus), PXPseudonymizationStatusListAttribute.Erased));

				List<PXDataFieldParam> restricts = new List<PXDataFieldParam>();

				foreach (string key in processingGraph.Caches[childTable].Keys)
				{
					restricts.Add(new PXDataFieldRestrict(key, processingGraph.Caches[childTable].GetValue(child, key)));
				}

				PXDatabase.Update(
					childTable,
					assignsDB
						.Union(restricts)
						.ToArray()
				);

				var entityNoteID = processingGraph.Caches[childTable].GetValue(child, nameof(INotable.NoteID)) as Guid?;

				PXDatabase.Delete<SMPersonalData>(
					new PXDataFieldRestrict<SMPersonalData.table>(childTable.FullName),
					new PXDataFieldRestrict<SMPersonalData.entityID>(entityNoteID));
			}
		}

	}

	public class RestorePIIEntityProcess : GDPRRestoreProcessBase
	{
		protected override void ChildLevelProcessor(PXGraph processingGraph, Type childTable, IEnumerable<PXPersonalDataFieldAttribute> fields, IEnumerable<object> childs, Guid? topParentNoteID)
		{
			RestoreObfuscatedEntries(processingGraph, childTable, fields, childs);
		}

	}

	public class PseudonymizePIIEntityProcess : GDPRPseudonymizeProcessBase
	{
		protected override void ChildLevelProcessor(PXGraph processingGraph, Type childTable, IEnumerable<PXPersonalDataFieldAttribute> fields, IEnumerable<object> childs, Guid? topParentNoteID)
		{
			StoreChildsValues(processingGraph, childTable, fields, childs, topParentNoteID);
			PseudonymizeChilds(processingGraph, childTable, fields, childs);
			WipeAudit(processingGraph, childTable, fields, childs);
		}
		private void StoreChildsValues(PXGraph processingGraph, Type childTable, IEnumerable<PXPersonalDataFieldAttribute> fields, IEnumerable<object> childs, Guid? topParentNoteID)
		{
			foreach (object child in childs)
			{
				var childNoteID = processingGraph.Caches[childTable].GetValue(child, nameof(INotable.NoteID)) as Guid?;

				// TODO: merge into single INSERT
				foreach (PXPersonalDataFieldAttribute field in fields)
				{
					string value = (string)processingGraph.Caches[childTable].GetValue(child, field.FieldName);
					if (value == null)
						continue;

					if (field.DefaultValue != null && (!value.GetType().IsAssignableFrom(field.DefaultValue.GetType()) || value.Equals(field.DefaultValue)))
						continue;
					var encryptedvalue = !string.IsNullOrEmpty(value)
					?
						PXRSACryptStringAttribute.Encrypt(value)
					:
						null;
					List<PXDataFieldAssign> assigns = new List<PXDataFieldAssign>();

					assigns.Add(new PXDataFieldAssign<SMPersonalData.table>(childTable.FullName));
					assigns.Add(new PXDataFieldAssign<SMPersonalData.field>(field.FieldName));
					assigns.Add(new PXDataFieldAssign<SMPersonalData.entityID>(childNoteID));
					assigns.Add(new PXDataFieldAssign<SMPersonalData.topParentNoteID>(topParentNoteID));
					assigns.Add(new PXDataFieldAssign<SMPersonalData.value>(encryptedvalue));
					assigns.Add(new PXDataFieldAssign<SMPersonalData.createdDateTime>(PXTimeZoneInfo.UtcNow));

					PXDatabase.Insert<SMPersonalData>(assigns.ToArray());
				}

			}
		}

	}

	#endregion

	public class ProcessPII : PXGraph<ProcessPII>
	{

		#region Actions

		public PXCancel<PIIFilter> Cancel;

		public PXAction<PIIFilter> OpenEntity;

		[PXButton]
		[PXUIField(Visible = false)]
		public virtual IEnumerable openentity(PXAdapter adapter)
		{
			var entity = this.Caches[typeof(PIIEntity)].Current as PIIEntity;

			if (entity == null)
				return adapter.Get();
			if (entity.MasterEntityType == PIIMasterEntityAttribute.SalesOrder)
			{
				var extGraph = PXGraph.CreateInstance<SOOrderEntry>();
				var order = SOOrder.PK.Find(extGraph, entity.Type, entity.Reference);
				if (order != null)
				{
					EntityHelper helper = new EntityHelper(extGraph);
					helper.NavigateToRow(extGraph.GetPrimaryCache().GetItemType().FullName, order.NoteID, PXRedirectHelper.WindowMode.NewWindow);
				}
			}
			else
			{
				var extGraph = PXGraph.CreateInstance<ARInvoiceEntry>();
				var invoice = ARInvoice.PK.Find(extGraph, entity.Type, entity.Reference);
				if (invoice != null)
				{
					EntityHelper helper = new EntityHelper(extGraph);
					helper.NavigateToRow(extGraph.GetPrimaryCache().GetItemType().FullName, invoice.NoteID, PXRedirectHelper.WindowMode.NewWindow);
				}
			}
			return adapter.Get();
		}

		#endregion

		#region Selects

		public PXFilter<PIIFilter> Filter;

		[PXFilterable]
		public PXFilteredProcessing<PIIEntity, PIIFilter>
			SelectedItems;

		public ProcessPII()
		{
			SelectedItems.SetSelected<PIIEntity.selected>();
		}

		#region SOCommands
		public PXSelectJoin<SOOrder,
			InnerJoin<SOAddress,
			On<SOAddress.addressID, Equal<SOOrder.billAddressID>,
			And2<Where<PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.notPseudonymized>, Or<PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus, IsNull>>,
				And<SOAddress.isEncrypted, Equal<True>>>>,
			InnerJoin<SOAddress2,
			On<SOAddress2.addressID, Equal<SOOrder.shipAddressID>,
			And2<Where<PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.notPseudonymized>, Or<PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus, IsNull>>, And<SOAddress2.isEncrypted, Equal<True>>>>,
			InnerJoin<SOContact,
			On<SOContact.contactID, Equal<SOOrder.billContactID>,
			And2<Where<PX.Objects.GDPR.SOContactExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.notPseudonymized>, Or<PX.Objects.GDPR.SOContactExt.pseudonymizationStatus, IsNull>>, And<SOContact.isEncrypted, Equal<True>>>>,
			InnerJoin<SOContact2,
			On<SOContact2.contactID, Equal<SOOrder.shipContactID>,
			And2<Where<PX.Objects.GDPR.SOContactExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.notPseudonymized>, Or<PX.Objects.GDPR.SOContactExt.pseudonymizationStatus, IsNull>>, And<SOContact2.isEncrypted, Equal<True>>>>>>>>,
			Where<SOOrder.completed, Equal<True>, Or<SOOrder.cancelled, Equal<True>>>> SOPseudonymize;

		public PXSelectJoin<SOOrder,
			InnerJoin<SOAddress,
			On<SOAddress.addressID, Equal<SOOrder.billAddressID>,
			And<PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.pseudonymized>,
			And<SOAddress.isEncrypted, Equal<True>>>>,
			InnerJoin<SOAddress2,
			On<SOAddress2.addressID, Equal<SOOrder.shipAddressID>,
			And<PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.pseudonymized>,
				And<SOAddress2.isEncrypted, Equal<True>>>>,
			InnerJoin<SOContact,
			On<SOContact.contactID, Equal<SOOrder.billContactID>,
			And<PX.Objects.GDPR.SOContactExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.pseudonymized>, And<SOContact.isEncrypted, Equal<True>>>>,
			InnerJoin<SOContact2,
			On<SOContact2.contactID, Equal<SOOrder.shipContactID>,
			And<PX.Objects.GDPR.SOContactExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.pseudonymized>, And<SOContact2.isEncrypted, Equal<True>>>>>>>>,
			Where<SOOrder.completed, Equal<True>, Or<SOOrder.cancelled, Equal<True>>>> SORestore;

		public PXSelectJoin<SOOrder,
			InnerJoin<SOAddress,
			On<SOAddress.addressID, Equal<SOOrder.billAddressID>,
			And<SOAddress.isEncrypted, Equal<True>, And<PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus, NotEqual<PXPseudonymizationStatusListAttribute.erased>>>>,
			InnerJoin<SOAddress2, On<SOAddress2.addressID, Equal<SOOrder.shipAddressID>,
			And<SOAddress2.isEncrypted, Equal<True>, And<PX.Objects.GDPR.SOAddressExt.pseudonymizationStatus, NotEqual<PXPseudonymizationStatusListAttribute.erased>>>>,
			InnerJoin<SOContact,
			On<SOContact.contactID, Equal<SOOrder.billContactID>,
			And<SOContact.isEncrypted, Equal<True>, And<PX.Objects.GDPR.SOContactExt.pseudonymizationStatus, NotEqual<PXPseudonymizationStatusListAttribute.erased>>>>,
			InnerJoin<SOContact2,
			On<SOContact2.contactID, Equal<SOOrder.shipContactID>,
			And<SOContact2.isEncrypted, Equal<True>, And<PX.Objects.GDPR.SOContactExt.pseudonymizationStatus, NotEqual<PXPseudonymizationStatusListAttribute.erased>>>>>>>>,
			Where<SOOrder.completed, Equal<True>, Or<SOOrder.cancelled, Equal<True>>>> SOErase;
		#endregion

		#region InvoiceCommands
		public PXSelectJoin<ARInvoice,
			InnerJoin<ARAddress,
			On<ARAddress.addressID, Equal<ARInvoice.billAddressID>,
			And2<Where<PX.Objects.GDPR.ARAddressExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.notPseudonymized>, Or<PX.Objects.GDPR.ARAddressExt.pseudonymizationStatus, IsNull>>,
				And<ARAddress.isEncrypted, Equal<True>>>>,
			InnerJoin<ARAddress2,
			On<ARAddress2.addressID, Equal<ARInvoice.shipAddressID>,
			And2<Where<PX.Objects.GDPR.ARAddressExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.notPseudonymized>, Or<PX.Objects.GDPR.ARAddressExt.pseudonymizationStatus, IsNull>>, And<ARAddress2.isEncrypted, Equal<True>>>>,
			InnerJoin<ARContact,
			On<ARContact.contactID, Equal<ARInvoice.billContactID>,
			And2<Where<PX.Objects.GDPR.ARContactExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.notPseudonymized>, Or<PX.Objects.GDPR.ARContactExt.pseudonymizationStatus, IsNull>>, And<ARContact.isEncrypted, Equal<True>>>>,
			InnerJoin<ARContact2,
			On<ARContact2.contactID, Equal<ARInvoice.shipContactID>,
			And2<Where<PX.Objects.GDPR.ARContactExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.notPseudonymized>, Or<PX.Objects.GDPR.ARContactExt.pseudonymizationStatus, IsNull>>, And<ARContact2.isEncrypted, Equal<True>>>>>>>>,
			Where<ARInvoice.released, Equal<True>, Or<ARInvoice.closedDate, IsNotNull>>> INPseudonymize;

		public PXSelectJoin<ARInvoice,
			InnerJoin<ARAddress,
			On<ARAddress.addressID, Equal<ARInvoice.billAddressID>,
			And<PX.Objects.GDPR.ARAddressExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.pseudonymized>,
			And<ARAddress.isEncrypted, Equal<True>>>>,
			InnerJoin<ARAddress2,
			On<ARAddress2.addressID, Equal<ARInvoice.shipAddressID>,
			And<PX.Objects.GDPR.ARAddressExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.pseudonymized>,
				And<ARAddress2.isEncrypted, Equal<True>>>>,
			InnerJoin<ARContact,
			On<ARContact.contactID, Equal<ARInvoice.billContactID>,
			And<PX.Objects.GDPR.ARContactExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.pseudonymized>, And<ARContact.isEncrypted, Equal<True>>>>,
			InnerJoin<ARContact2,
			On<ARContact2.contactID, Equal<ARInvoice.shipContactID>,
			And<PX.Objects.GDPR.ARContactExt.pseudonymizationStatus, Equal<PXPseudonymizationStatusListAttribute.pseudonymized>, And<ARContact2.isEncrypted, Equal<True>>>>>>>>,
			Where<ARInvoice.released, Equal<True>, Or<ARInvoice.closedDate, IsNotNull>>> INRestore;

		public PXSelectJoin<ARInvoice,
			InnerJoin<ARAddress,
			On<ARAddress.addressID, Equal<ARInvoice.billAddressID>,
			And<ARAddress.isEncrypted, Equal<True>, And<PX.Objects.GDPR.ARAddressExt.pseudonymizationStatus, NotEqual<PXPseudonymizationStatusListAttribute.erased>>>>,
			InnerJoin<ARAddress2, On<ARAddress2.addressID, Equal<ARInvoice.shipAddressID>,
			And<ARAddress2.isEncrypted, Equal<True>, And<PX.Objects.GDPR.ARAddressExt.pseudonymizationStatus, NotEqual<PXPseudonymizationStatusListAttribute.erased>>>>,
			InnerJoin<ARContact,
			On<ARContact.contactID, Equal<ARInvoice.billContactID>,
			And<ARContact.isEncrypted, Equal<True>, And<PX.Objects.GDPR.ARContactExt.pseudonymizationStatus, NotEqual<PXPseudonymizationStatusListAttribute.erased>>>>,
			InnerJoin<ARContact2,
			On<ARContact2.contactID, Equal<ARInvoice.shipContactID>,
			And<ARContact2.isEncrypted, Equal<True>, And<PX.Objects.GDPR.ARContactExt.pseudonymizationStatus, NotEqual<PXPseudonymizationStatusListAttribute.erased>>>>>>>>,
			Where<ARInvoice.released, Equal<True>, Or<ARInvoice.closedDate, IsNotNull>>> INErase;
		#endregion
		public virtual IEnumerable selectedItems()
		{
			PIIFilter filter = Filter.Current;
			List<PIIEntity> entities = new List<PIIEntity>();
			var parameters = new List<object>();

			if (filter.DocumentDateWithinXDays != null)
			{
				DateTime asOfDate = DateTime.Now.AddDays(-filter.DocumentDateWithinXDays.Value);
				parameters.Add(asOfDate);
			}

			if (string.IsNullOrEmpty(filter.Action)) yield break;
			if (filter.MasterEntity == PIIMasterEntityAttribute.SalesOrder)
			{
				SelectSalesOrder(filter, entities, parameters);
			}
			else if (filter.MasterEntity == PIIMasterEntityAttribute.SalesInvoice)
			{
				SelectInvoice(filter, entities, parameters);
			}
			else
			{
				SelectSalesOrder(filter, entities, parameters);
				SelectInvoice(filter, entities, parameters);
			}

			foreach (PIIEntity detail in entities)
			{
				var located = SelectedItems.Cache.Locate(detail);

				if (located != null)
				{
					yield return located;
				}
				else
				{
					SelectedItems.Cache.SetStatus(detail, PXEntryStatus.Held);
					yield return detail;
				}
			}

			SelectedItems.Cache.IsDirty = false;
		}

		protected virtual void _(PX.Data.Events.RowSelected<PIIFilter> e)
		{
			PIIFilter filter = e.Row;
			if (filter == null) return;
			if (filter.Action == PIIActionAttribute.Erase)
			{
				SelectedItems.SetProcessDelegate(delegate (List<PIIEntity> entries)
				{
					var graph = PXGraph.CreateInstance<ErasePIIEntityProcess>();
					graph.GetPseudonymizationStatus = typeof(PXPseudonymizationStatusListAttribute.pseudonymized);
					graph.SetPseudonymizationStatus = PXPseudonymizationStatusListAttribute.Erased;
					Process(entries, graph, true);
				});

			}
			else if (filter.Action == PIIActionAttribute.Restore)
			{

				SelectedItems.SetProcessDelegate(delegate (List<PIIEntity> entries)
				{
					var graph = PXGraph.CreateInstance<RestorePIIEntityProcess>();
					graph.GetPseudonymizationStatus = typeof(PXPseudonymizationStatusListAttribute.pseudonymized);
					graph.SetPseudonymizationStatus = PXPseudonymizationStatusListAttribute.NotPseudonymized;
					Process(entries, graph);
				});
			}
			else
			{
				SelectedItems.SetProcessDelegate(delegate (List<PIIEntity> entries)
				{
					var graph = PXGraph.CreateInstance<PseudonymizePIIEntityProcess>();
					graph.GetPseudonymizationStatus = typeof(PXPseudonymizationStatusListAttribute.notPseudonymized);
					graph.SetPseudonymizationStatus = PXPseudonymizationStatusListAttribute.Pseudonymized;
					Process(entries, graph);
				});
			}
		}
		protected virtual void SelectInvoice(PIIFilter filter, List<PIIEntity> entities, List<object> parameters)
		{
			var startRow = PXView.StartRow;
			int totalRows = 0;

			PXView view;
			if (filter.Action == PIIActionAttribute.Pseudonymize)
			{
				view = new PXView(this, true, INPseudonymize.View.BqlSelect);
			}
			else if (filter.Action == PIIActionAttribute.Erase)
			{
				view = new PXView(this, true, INErase.View.BqlSelect);
			}
			else
			{
				view = new PXView(this, true, INRestore.View.BqlSelect);
			}
			if (parameters.Count > 0)
				view.WhereAnd<Where<ARInvoice.docDate, GreaterEqual<Required<ARInvoice.docDate>>>>();
			List<PXFilterRow> newFilter = new List<PXFilterRow>();
			foreach (PXFilterRow gridFilter in PXView.Filters)
			{
				if (gridFilter.DataField.Equals("Reference"))
				{
					gridFilter.DataField = "RefNbr";
					newFilter.Add(gridFilter);
				}else
					newFilter.Add(gridFilter);

			}
			var list = view.Select(PXView.Currents, parameters.ToArray(), PXView.Searches, null, null,newFilter.ToArray(), ref startRow, PXView.MaximumRows, ref totalRows);
			PXView.StartRow = 0;
			foreach (PXResult<ARInvoice, ARAddress, ARAddress2, ARContact, ARContact2> record in list)
			{
				///map to PIIEntity
				ARInvoice order = record.GetItem<ARInvoice>();
				ARAddress billToAddress = record.GetItem<ARAddress>();
				ARAddress2 shipToAddress = record.GetItem<ARAddress2>();
				ARContact billToContact = record.GetItem<ARContact>();
				ARContact2 shipToCOntact = record.GetItem<ARContact2>();
				PIIEntity entity = new PIIEntity();
				entity.NoteID = order.NoteID;
				entity.MasterEntityType = PIIMasterEntityAttribute.SalesInvoice;
				entity.Type = order.DocType;
				entity.Reference = order.RefNbr;
				entity.BillingAddressID = billToAddress.AddressID;
				entity.BillingContactID = billToContact.ContactID;
				entity.BillToAddressLine1 = billToAddress.AddressLine1;
				entity.BillToAddressLine2 = billToAddress.AddressLine2;
				entity.BillToAttention = billToContact.Attention;
				entity.BillToCity = billToAddress.City;
				entity.BillToCompanyName = billToContact.FullName;
				entity.BillToCountry = billToAddress.CountryID;
				entity.BillToEmail = billToContact.Email;
				entity.BillToPhone1 = billToContact.Phone1;
				entity.BillToPostalCode = billToAddress.PostalCode;
				entity.BillToState = billToAddress.State;

				entity.ShippingAddressID = shipToAddress.AddressID;
				entity.ShippingContactID = shipToCOntact.ContactID;
				entity.ShipToAddressLine1 = shipToAddress.AddressLine1;
				entity.ShipToAddressLine2 = shipToAddress.AddressLine2;
				entity.ShipToAttention = shipToCOntact.Attention;
				entity.ShipToCity = shipToAddress.City;
				entity.ShipToCompanyName = shipToCOntact.FullName;
				entity.ShipToCountry = shipToAddress.CountryID;
				entity.ShipToEmail = shipToCOntact.Email;
				entity.ShipToPhone1 = shipToCOntact.Phone1;
				entity.ShipToPostalCode = shipToAddress.PostalCode;
				entity.ShipToState = shipToAddress.State;
				entity.Pseudonymized = shipToAddress.GetExtension<PX.Objects.GDPR.ARAddressExt>().PseudonymizationStatus == PXPseudonymizationStatusListAttribute.Pseudonymized;
				entities.Add(entity);
			}

		}

		protected virtual void SelectSalesOrder(PIIFilter filter, List<PIIEntity> entities, List<object> parameters)
		{
			var startRow = PXView.StartRow;
			int totalRows = 0;

			PXView view;
			if (filter.Action == PIIActionAttribute.Pseudonymize)
			{
				view = new PXView(this, true, SOPseudonymize.View.BqlSelect);
			}
			else if (filter.Action == PIIActionAttribute.Erase)
			{
				view = new PXView(this, true, SOErase.View.BqlSelect);
			}
			else
			{
				view = new PXView(this, true, SORestore.View.BqlSelect);
			}
			if (parameters.Count > 0)
				view.WhereAnd<Where<SOOrder.orderDate, GreaterEqual<Required<SOOrder.orderDate>>>>();

			List<PXFilterRow> newFilter = new List<PXFilterRow>();
			foreach (PXFilterRow gridFilter in PXView.Filters)
			{
				if (gridFilter.DataField.Equals("Reference"))
				{
					gridFilter.DataField = "OrderNbr";
					newFilter.Add(gridFilter);
				}
				else newFilter.Add(gridFilter);

			}
			var list = view.Select(PXView.Currents, parameters.ToArray(), PXView.Searches, null, null, newFilter.ToArray(), ref startRow, PXView.MaximumRows, ref totalRows);
			PXView.StartRow = 0;
			foreach (PXResult<SOOrder, SOAddress, SOAddress2, SOContact, SOContact2> record in list)
			{
				///map to PIIEntity
				SOOrder order = record.GetItem<SOOrder>();
				SOAddress billToAddress = record.GetItem<SOAddress>();
				SOAddress2 shipToAddress = record.GetItem<SOAddress2>();
				SOContact billToContact = record.GetItem<SOContact>();
				SOContact2 shipToCOntact = record.GetItem<SOContact2>();
				PIIEntity entity = new PIIEntity();
				entity.NoteID = order.NoteID;
				entity.MasterEntityType = PIIMasterEntityAttribute.SalesOrder;
				entity.Type = order.OrderType;
				entity.Reference = order.OrderNbr;
				entity.BillingAddressID = billToAddress.AddressID;
				entity.BillingContactID = billToContact.ContactID;
				entity.BillToAddressLine1 = billToAddress.AddressLine1;
				entity.BillToAddressLine2 = billToAddress.AddressLine2;
				entity.BillToAttention = billToContact.Attention;
				entity.BillToCity = billToAddress.City;
				entity.BillToCompanyName = billToContact.FullName;
				entity.BillToCountry = billToAddress.CountryID;
				entity.BillToEmail = billToContact.Email;
				entity.BillToPhone1 = billToContact.Phone1;
				entity.BillToPostalCode = billToAddress.PostalCode;
				entity.BillToState = billToAddress.State;

				entity.ShippingAddressID = shipToAddress.AddressID;
				entity.ShippingContactID = shipToCOntact.ContactID;
				entity.ShipToAddressLine1 = shipToAddress.AddressLine1;
				entity.ShipToAddressLine2 = shipToAddress.AddressLine2;
				entity.ShipToAttention = shipToCOntact.Attention;
				entity.ShipToCity = shipToAddress.City;
				entity.ShipToCompanyName = shipToCOntact.FullName;
				entity.ShipToCountry = shipToAddress.CountryID;
				entity.ShipToEmail = shipToCOntact.Email;
				entity.ShipToPhone1 = shipToCOntact.Phone1;
				entity.ShipToPostalCode = shipToAddress.PostalCode;
				entity.ShipToState = shipToAddress.State;
				entity.Pseudonymized = shipToAddress.GetExtension<PX.Objects.GDPR.SOAddressExt>().PseudonymizationStatus == PXPseudonymizationStatusListAttribute.Pseudonymized;
				entities.Add(entity);
			}

		}

		#endregion

		#region Public Methods

		public static void Process(IEnumerable<PIIEntity> entities, GDPRPersonalDataProcessBase graph, bool eraseAll = false)
		{
			using (PXTransactionScope ts = new PXTransactionScope())
			{
				using (new PXReadDeletedScope())
				{
					graph.ProcessImpl(RemapToPrimary(entities), true, null, eraseAll);
					ts.Complete();
				}
			}
		}

		#endregion
		protected static IEnumerable<IBqlTable> RemapToPrimary(IEnumerable<PIIEntity> entities)
		{
			foreach (PIIEntity entity in entities)
			{
				switch (entity.MasterEntityType)
				{
					case PIIMasterEntityAttribute.SalesOrder:
						yield return new SOOrder()
						{
							OrderType = entity.Type,
							OrderNbr = entity.Reference,
							BillAddressID = entity.BillingAddressID,
							ShipAddressID = entity.ShippingAddressID,
							BillContactID = entity.BillingContactID,
							ShipContactID = entity.ShippingContactID,
							NoteID = entity.NoteID
						};
						break;
					case PIIMasterEntityAttribute.SalesInvoice:
						yield return new ARInvoice()
						{
							DocType = entity.Type,
							RefNbr = entity.Reference,
							BillAddressID = entity.BillingAddressID,
							ShipAddressID = entity.ShippingAddressID,
							BillContactID = entity.BillingContactID,
							ShipContactID = entity.ShippingContactID,
							NoteID = entity.NoteID
						};
						break;
				}
			}
		}

	}
}
