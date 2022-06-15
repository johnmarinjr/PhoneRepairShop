using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.Common.Bql;
using PX.Objects.Common.Extensions;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.IN;
using PX.SM;

namespace PX.Objects.SO
{
	public class SOPickingJobEnq : PXGraph<SOPickingJobEnq>
	{
		#region DACs
		[PXCacheName("Filter")]
		public class HeaderFilter : IBqlTable
		{
			#region WorksheetType
			[PXString(2, IsFixed = true)]
			[SOPickingJobProcess.HeaderFilter.worksheetType.List]
			[PXUnboundDefault(typeof(SOPickingJobProcess.HeaderFilter.worksheetType.all.When<Where<FeatureInstalled<FeaturesSet.wMSAdvancedPicking>>>.Else<SOPickingWorksheet.worksheetType.single>))]
			[PXUIField(DisplayName = "Pick List Type", FieldClass = nameof(FeaturesSet.WMSAdvancedPicking))]
			public virtual String WorksheetType { get; set; }
			public abstract class worksheetType : BqlString.Field<worksheetType> { }
			#endregion
			#region SiteID
			[Site(Required = true)]
			[InterBranchRestrictor(typeof(Where<SameOrganizationBranch<INSite.branchID, Current<AccessInfo.branchID>>>))]
			public virtual Int32? SiteID { get; set; }
			public abstract class siteID : BqlInt.Field<siteID> { }
			#endregion
			#region AssigneeID
			[PXGuid]
			[PXSelector(typeof(Search<Users.pKID, Where<Users.isHidden.IsEqual<False>>>), SubstituteKey = typeof(Users.username))]
			[PXUIField(DisplayName = "Picker")]
			public Guid? AssigneeID { get; set; }
			public abstract class assigneeID : BqlGuid.Field<assigneeID> { }
			#endregion
			#region CustomerID
			[Customer]
			[PXUIVisible(typeof(worksheetType.IsEqual<SOPickingWorksheet.worksheetType.single>))]
			public virtual Int32? CustomerID { get; set; }
			public abstract class customerID : BqlInt.Field<customerID> { }
			#endregion
			#region CarrierPluginID
			[PXDBString(15, IsUnicode = true, InputMask = ">aaaaaaaaaaaaaaa")]
			[PXSelector(typeof(Search<CarrierPlugin.carrierPluginID>))]
			[PXUIField(DisplayName = "Carrier")]
			[PXUIVisible(typeof(worksheetType.IsEqual<SOPickingWorksheet.worksheetType.single>))]
			public virtual String CarrierPluginID { get; set; }
			public abstract class carrierPluginID : BqlString.Field<carrierPluginID> { }
			#endregion
			#region ShipVia
			[PXString(15, IsUnicode = true)]
			[PXSelector(typeof(Search<Carrier.carrierID, Where<carrierPluginID.FromCurrent.IsNull.Or<Carrier.carrierPluginID.IsEqual<carrierPluginID.FromCurrent>>>>), DescriptionField = typeof(Carrier.description), CacheGlobal = true)]
			[PXUIField(DisplayName = "Ship Via")]
			[PXUIVisible(typeof(worksheetType.IsEqual<SOPickingWorksheet.worksheetType.single>))]
			public virtual String ShipVia { get; set; }
			public abstract class shipVia : BqlString.Field<shipVia> { }
			#endregion
			#region ShowPick
			[PXBool]
			[PXUnboundDefault(typeof(SearchFor<SOPickPackShipSetup.showPickTab>.Where<SOPickPackShipSetup.branchID.IsEqual<AccessInfo.branchID.FromCurrent>>))]
			public virtual bool? ShowPick { get; set; }
			public abstract class showPick : BqlBool.Field<showPick> { }
			#endregion
			#region ShowPack
			[PXBool]
			[PXUnboundDefault(typeof(SearchFor<SOPickPackShipSetup.showPackTab>.Where<SOPickPackShipSetup.branchID.IsEqual<AccessInfo.branchID.FromCurrent>>))]
			public virtual bool? ShowPack { get; set; }
			public abstract class showPack : BqlBool.Field<showPack> { }
			#endregion
		}
		#endregion

		#region DAC overrides
		#region SOPickingWorksheet
		public SelectFrom<SOPickingWorksheet>.View DummyWorksheet;

		[PXMergeAttributes]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Pick List Type")]
		protected virtual void _(Events.CacheAttached<SOPickingWorksheet.worksheetType> e) { }

		[PXMergeAttributes]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Pick List Date")]
		protected virtual void _(Events.CacheAttached<SOPickingWorksheet.pickDate> e) { }
		#endregion
		#region SOShipment
		public SelectFrom<SOShipment>.View DummyShipment;

		[PXMergeAttributes]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Quantity")]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<SOShipment.shipmentQty> e) { }

		[PXMergeAttributes]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Volume")]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<SOShipment.shipmentVolume> e) { }

		[PXMergeAttributes]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Weight")]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<SOShipment.shipmentWeight> e) { }

		[PXMergeAttributes]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<SOShipment.shipDate> e) { }
		#endregion
		#region Carrier
		public SelectFrom<Carrier>.View DummyCarrier;

		[PXMergeAttributes]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<Carrier.carrierID> e) { }

		[PXMergeAttributes]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), Messages.CarrierDescr)]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<Carrier.description> e) { }
		#endregion
		#region Customer
		public SelectFrom<Customer>.View DummyCustomer;

		[PXMergeAttributes]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<Customer.acctCD> e) { }

		[PXMergeAttributes]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<Customer.acctName> e) { }
		#endregion
		#region Location
		public SelectFrom<CR.Location>.View DummyLocation;

		[PXMergeAttributes]
		[PXCustomizeBaseAttribute(typeof(CS.LocationRawAttribute), nameof(CS.LocationRawAttribute.DisplayName), "Customer Location ID")]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<CR.Location.locationCD> e) { }

		[PXMergeAttributes]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Customer Location Name")]
		[PXUIVisible(typeof(HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingWorksheet.worksheetType.single>))]
		protected virtual void _(Events.CacheAttached<CR.Location.descr> e) { }
		#endregion
		#endregion

		#region Attached fields
		#region SOPickingJob
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		[PXUIField(DisplayName = "Time in Queue", Enabled = false)]
		public class timeInQueue : PXFieldAttachedTo<SOPickingJob>.By<SOPickingJobEnq>.AsString.Named<timeInQueue>
		{
			public override string GetValue(SOPickingJob row) => row?.EnqueuedAt.With(date => ServerTime.Value - date.Value).With(diff => (int)diff.TotalHours + diff.ToString(@"\:mm\:ss"));

			private Lazy<DateTime> ServerTime { get; } = Lazy.By(GetServerTime);
			private static DateTime GetServerTime()
			{
				DateTime dbNow;
				PXDatabase.SelectDate(out DateTime _, out dbNow);
				dbNow = PXTimeZoneInfo.ConvertTimeFromUtc(dbNow, LocaleInfo.GetTimeZone());
				return dbNow;
			}
		}
		#endregion
		#endregion

		#region Views
		public PXFilter<HeaderFilter> Filter;

		[PXFilterable]
		public
			SelectPickingJobs<
				Where<SOPickingJob.status.IsNotIn<SOPickingJob.status.onHold, SOPickingJob.status.picked, SOPickingJob.status.completed>>
			> PickingJobs;

		[PXFilterable]
		public
			SelectPickingJobs<
				Where<
					SOPickingJob.status.IsEqual<SOPickingJob.status.picked>.
					Or<
						HeaderFilter.showPick.FromCurrent.IsEqual<False>.
						And<SOPickingJob.status.IsNotIn<SOPickingJob.status.onHold, SOPickingJob.status.completed>>>>
			> PackingJobs;

		public class SelectPickingJobs<TWhere> :
			SelectFrom<SOPickingJob>.
			InnerJoin<SOPicker>.On<SOPickingJob.FK.Picker>.
			InnerJoin<SOPickingWorksheet>.On<SOPicker.FK.Worksheet>.
			InnerJoin<INSite>.On<SOPickingWorksheet.FK.Site>.
			LeftJoin<SOPickerToShipmentLink>.On<
				SOPickerToShipmentLink.FK.Picker.
				And<SOPickingWorksheet.worksheetType.IsEqual<SOPickingWorksheet.worksheetType.single>>>.
			LeftJoin<SOShipment>.On<SOPickerToShipmentLink.FK.Shipment>.
			LeftJoin<Carrier>.On<SOShipment.FK.Carrier>.
			LeftJoin<Customer>.On<SOShipment.FK.Customer>.
			LeftJoin<Location>.On<SOShipment.FK.CustomerLocation>.
			Where<
				Match<INSite, AccessInfo.userName.FromCurrent>.
				And<SOPickingWorksheet.siteID.IsEqual<HeaderFilter.siteID.FromCurrent>>.
				And<
					HeaderFilter.assigneeID.FromCurrent.IsNull.
					Or<SOPickingJob.preferredAssigneeID.IsEqual<HeaderFilter.assigneeID.FromCurrent>>.
					Or<SOPickingJob.actualAssigneeID.IsEqual<HeaderFilter.assigneeID.FromCurrent>>>.
				And<
					HeaderFilter.worksheetType.FromCurrent.IsEqual<SOPickingJobProcess.HeaderFilter.worksheetType.all>.
					Or<SOPickingWorksheet.worksheetType.IsEqual<HeaderFilter.worksheetType.FromCurrent>>>.
				And<
					HeaderFilter.worksheetType.FromCurrent.IsNotEqual<SOPickingWorksheet.worksheetType.single>.
					Or<
						Brackets<
							HeaderFilter.customerID.FromCurrent.IsNull.
							Or<SOShipment.customerID.IsEqual<HeaderFilter.customerID.FromCurrent>>>.
						And<
							HeaderFilter.carrierPluginID.FromCurrent.IsNull.
							Or<Carrier.carrierPluginID.IsEqual<HeaderFilter.carrierPluginID.FromCurrent>>>.
						And<
							HeaderFilter.shipVia.FromCurrent.IsNull.
							Or<Carrier.carrierID.IsEqual<HeaderFilter.shipVia.FromCurrent>>>>>.
				And<TWhere>>.
			OrderBy<
				SOPickingJob.priority.Desc,
				SOPickingJob.enqueuedAt.Asc>.
			View
			where TWhere : IBqlWhere, new()
		{
			public SelectPickingJobs(PXGraph graph) : base(graph) { }
			public SelectPickingJobs(PXGraph graph, Delegate handler) : base(graph, handler) { }
		}
		#endregion

		#region Actions
		public PXCancel<HeaderFilter> Cancel;

		public PXAction<HeaderFilter> HoldJob;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Remove from Queue")]
		protected virtual void holdJob()
		{
			if (PickingJobs.Current != null && PickingJobs.Current.Status.IsIn(SOPickingJob.status.Enqueued, SOPickingJob.status.Reenqueued, SOPickingJob.status.Assigned))
			{
				PickingJobs.Cache.SetValueExt<SOPickingJob.status>(PickingJobs.Current, SOPickingJob.status.OnHold);
				PickingJobs.UpdateCurrent();
			}
		}

		public PXAction<HeaderFilter> StartWatching;
		// Acuminator disable once PX1013 PXActionHandlerInvalidReturnType [No mass processing]
		[PXButton, PXUIField(DisplayName = "Start Watching")]
		protected virtual void startWatching() => PXLongOperation.StartOperation(this, Watching);
		#endregion

		#region Popups
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class ShowPickListPopup : GraphExtensions.
			ShowPickListPopup.
			On<SOPickingJobEnq, HeaderFilter>.
			FilteredBy<Where<SOPickingJob.jobID.IsEqual<SOPickingJob.jobID.FromCurrent>>>
		{ }
		#endregion

		#region Event handlers
		protected virtual void _(Events.RowSelected<HeaderFilter> args)
		{
			PickingJobs.Cache.AllowInsert = false;
			PickingJobs.Cache.AllowDelete = false;
			if (PXLongOperation.Exists(this))
			{
				Filter.Cache.AllowUpdate = false;
				PickingJobs.Cache.AllowUpdate = false;
				Cancel.SetEnabled(false);
				HoldJob.SetEnabled(false);
			}
		}

		protected virtual void _(Events.RowUpdated<SOPickingJob> args)
		{
			var diff = PickingJobs.Cache
				.GetDifference(args.OldRow, args.Row)
				.OrderByDescending(p => p.Key.IsIn(nameof(SOPickingJob.Status), nameof(SOPickingJob.Priority), nameof(SOPickingJob.PreferredAssigneeID), nameof(SOPickingJob.AutomaticShipmentConfirmation)))
				.ToArray();
			if (diff.Length == 0)
				return;

			try
			{
				// Acuminator disable once PX1045 PXGraphCreateInstanceInEventHandlers [concurrency update]
				var processor = CreateInstance<SOPickingJobEnq>();
				var job = SOPickingJob.PK.Find(processor, args.Row);
				foreach (var d in diff)
					processor.PickingJobs.Cache.SetValue(job, d.Key, d.Value.RightValue);
				processor.PickingJobs.Cache.SetStatus(job, PXEntryStatus.Updated);
				// Acuminator disable once PX1043 SavingChangesInEventHandlers [concurrency update]
				processor.Persist();
			}
			catch (Exception ex)
			{
				if (diff.Length > 0)
				{
					var pair = diff[0];
					PXCache<SOPickingJob>.RestoreCopy(args.Row, args.OldRow);
					PickingJobs.Cache.RaiseExceptionHandling(pair.Key, args.Row, pair.Value.LeftValue, ex);
				}
			}

			PickingJobs.Cache.SetStatus(args.Row, PXEntryStatus.Notchanged);
			PickingJobs.Cache.IsDirty = false;
		}
		#endregion

		private static void Watching()
		{
			while (true)
				System.Threading.Thread.Yield();
		}
	}
}