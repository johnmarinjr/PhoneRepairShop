using System;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;

namespace PX.Objects.CR
{
	/// <summary>
	/// Abstract class is needed to handle the case when we have priorities for sources (e.g. Location -> BAccount -> Contact) and the source is optional
	/// </summary>
	public abstract class CRAddressAttribute : AddressAttribute, IPXRowUpdatedSubscriber
	{
		#region State

		BqlCommand _DuplicateSelect = BqlCommand.CreateInstance(typeof(
			Select<
				CRAddress,
			Where<
				CRAddress.bAccountID, Equal<Required<CRAddress.bAccountID>>,
				And<CRAddress.bAccountAddressID, Equal<Required<CRAddress.bAccountAddressID>>,
				And<CRAddress.revisionID, Equal<Required<CRAddress.revisionID>>,
				And<CRAddress.isDefaultAddress, Equal<boolTrue>>>>>>));

		#endregion

		#region Ctor
		public CRAddressAttribute(Type AddressIDType, Type IsDefaultAddressType, Type SelectType)
			: base(AddressIDType, IsDefaultAddressType, SelectType) { }
		#endregion

		#region Implementation

		public override void DefaultAddress<TAddress, TAddressID>(PXCache sender, object DocumentRow, object AddressRow)
		{
			(PXView view, object[] parms) = GetViewWithParameters(sender, DocumentRow, AddressRow);

			if (view != null)
			{
				// In case when some of the ID fields are filled - BAccountID, LocationID and ContactID
				// Need to get the old ethalon

				int startRow = -1;
				int totalRows = 0;

				bool addressFind = false;
				foreach (PXResult res in view.Select(new object[] { DocumentRow }, parms, null, null, null, null, ref startRow, 1, ref totalRows))
				{
					if (res.GetItem<TAddress>() is TAddress item)
					{
						var tCache = sender.Graph.Caches<TAddress>();
						var status = tCache.GetStatus(item);
						if (status == PXEntryStatus.Deleted)
						{
							// existing crcontact shoun't be changed, it should create new record instead
							tCache.SetStatus(item, PXEntryStatus.Notchanged);
							tCache.ClearQueryCache();
						}
						if (status == PXEntryStatus.InsertedDeleted)
						{
							tCache.SetStatus(item, PXEntryStatus.Inserted);
							tCache.ClearQueryCache();
						}
					}

					addressFind = DefaultAddress<TAddress, TAddressID>(sender, FieldName, DocumentRow, AddressRow, res);
					break;
				}

				if (!addressFind && !_Required)
					this.ClearRecord(sender, DocumentRow);
			}
			else
			{
				// In case when all the ID fields are empty - BAccountID, LocationID and ContactID - it's not clear who should be the "ethalon"
				// Need to delete the previous entity and create new ethalon (with IsDefault = true)

				ClearRecord(sender, DocumentRow);

				if (_Required)
				{
					using (ReadOnlyScope rs = new ReadOnlyScope(sender.Graph.Caches[_RecordType]))
					{
						object record = sender.Graph.Caches[_RecordType].Insert();
						object recordid = sender.Graph.Caches[_RecordType].GetValue(record, _RecordID);

						sender.SetValue(DocumentRow, _FieldOrdinal, recordid);
					}
				}
			}
		}

		protected abstract (PXView, object[]) GetViewWithParameters(PXCache sender, object DocumentRow, object ContactRow);

		public override void RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			bool previsDirty = sender.Graph.Caches[_RecordType].IsDirty;
			base.RowInserted(sender, e);
			sender.Graph.Caches[_RecordType].IsDirty = previsDirty;
		}

		public override void Record_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && ((CRAddress)e.Row).IsDefaultAddress == true)
			{
				PXView view = sender.Graph.TypedViews.GetView(_DuplicateSelect, true);
				view.Clear();

				CRAddress prev_address = (CRAddress)view.SelectSingle(((CRAddress)e.Row).BAccountID, ((CRAddress)e.Row).BAccountAddressID, ((CRAddress)e.Row).RevisionID);
				if (prev_address != null)
				{
					_KeyToAbort = sender.GetValue(e.Row, _RecordID);
					object newkey = sender.Graph.Caches[typeof(CRAddress)].GetValue(prev_address, _RecordID);

					PXCache cache = sender.Graph.Caches[_ItemType];

					foreach (object data in cache.Updated)
					{
						object datakey = cache.GetValue(data, _FieldOrdinal);
						if (Equals(_KeyToAbort, datakey))
						{
							cache.SetValue(data, _FieldOrdinal, newkey);
						}
					}

					_KeyToAbort = null;
					e.Cancel = true;
					return;
				}
			}
			else if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Delete)
			{
				var mainCache = sender.Graph.Caches[_BqlTable];
				var id = sender.GetValue(e.Row, _RecordID);
				var select = SelectFrom<Standalone.CROpportunityRevision>
					.Where<Standalone.CROpportunityRevision.noteID.IsNotEqual<@P.AsGuid>
						.And<Brackets<
							Standalone.CROpportunityRevision.shipAddressID.IsEqual<@P.AsInt>>
							.Or<Standalone.CROpportunityRevision.billAddressID.IsEqual<@P.AsInt>>
							.Or<Standalone.CROpportunityRevision.opportunityAddressID.IsEqual<@P.AsInt>>>>
					.View
					.ReadOnly
					.Select(sender.Graph, mainCache.GetValue<Standalone.CROpportunityRevision.noteID>(mainCache.Current), id, id, id);

				if (select.Any())
				{
					// something else is using it
					e.Cancel = true;
					sender.Hold(e.Row);
				}
			}
			base.Record_RowPersisting(sender, e);
		}

		public override void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			object key = sender.GetValue(e.Row, _FieldOrdinal);
			if (key != null)
			{
				PXCache cache = sender.Graph.Caches[_RecordType];
				if (Convert.ToInt32(key) < 0)
				{
					foreach (object data in cache.Inserted)
					{
						object datakey = cache.GetValue(data, _RecordID);
						if (Equals(key, datakey))
						{
							if (((CRAddress)data).IsDefaultAddress == true)
							{
								PXView view = sender.Graph.TypedViews.GetView(_DuplicateSelect, true);
								view.Clear();

								CRAddress prev_address = (CRAddress)view.SelectSingle(((CRAddress)data).BAccountID, ((CRAddress)data).BAccountAddressID, ((CRAddress)data).RevisionID);

								if (prev_address != null)
								{
									_KeyToAbort = sender.GetValue(e.Row, _FieldOrdinal);
									object id = sender.Graph.Caches[typeof(CRAddress)].GetValue(prev_address, _RecordID);
									sender.SetValue(e.Row, _FieldOrdinal, id);
								}
							}
							break;
						}
					}
				}
			}
			base.RowPersisting(sender, e);
		}

		public virtual void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (_Required && sender.GetValue(e.Row, _FieldOrdinal) == null)
			{
				using (ReadOnlyScope rs = new ReadOnlyScope(sender.Graph.Caches[_RecordType]))
				{
					object record = sender.Graph.Caches[_RecordType].Insert();
					object recordid = sender.Graph.Caches[_RecordType].GetValue(record, _RecordID);

					sender.SetValue(e.Row, _FieldOrdinal, recordid);
				}
			}
		}


		protected override void Record_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (e.Row != null)
			{
				bool? isDefault = (bool?)sender.GetValue(e.Row, _IsDefault);

				PXUIFieldAttribute.SetVisible(sender, null, _RecordID, false);

				var alreadyDisabled = sender.GetAttributes<CRAddress.overrideAddress>(e.Row)
					.OfType<PXUIFieldAttribute>()
					.Any(field => !field.Enabled);

				if (!alreadyDisabled)
				{
					PXUIFieldAttribute.SetEnabled(sender, e.Row, isDefault == false && sender.AllowUpdate);
					PXUIFieldAttribute.SetEnabled(sender, e.Row, _IsDefault, sender.AllowUpdate);
					PXUIFieldAttribute.SetEnabled<CRAddress.overrideAddress>(sender, e.Row, true);
				}
			}
		}

		public override void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
		}

		#endregion
	}
}