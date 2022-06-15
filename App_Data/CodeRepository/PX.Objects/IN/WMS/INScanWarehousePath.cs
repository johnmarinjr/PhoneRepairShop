using System;
using System.Collections;
using System.Collections.Generic;

using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.BarcodeProcessing;
using PX.Objects.Common.Attributes;

namespace PX.Objects.IN.WMS
{
	using WMSBase = WarehouseManagementSystem<INScanWarehousePath, INScanWarehousePath.Host>;

	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class INScanWarehousePath : WMSBase
	{
		public class Host : INSiteMaint { }

		protected override bool UseQtyCorrectection => false;

		#region State
		public ScanPathHeader PathHeader => Header.Get<ScanPathHeader>();
		public ValueSetter<ScanHeader>.Ext<ScanPathHeader> PathSetter => HeaderSetter.With<ScanPathHeader>();

		#region NextPathIndex
		public int? NextPathIndex
		{
			get => PathHeader.NextPathIndex;
			set => PathSetter.Set(h => h.NextPathIndex, value);
		}
		#endregion
		#endregion

		#region DAC overrides
		[BorrowedNote(typeof(INSite), typeof(INSiteMaint))]
		protected virtual void _(Events.CacheAttached<ScanHeader.noteID> e) { }
		#endregion

		#region Views
		protected virtual IEnumerable Location()
		{
			var rows =
				SelectFrom<INLocation>.
				Where<INLocation.siteID.IsEqual<INSite.siteID.FromCurrent>>.
				OrderBy<INLocation.pathPriority.Asc, INLocation.locationCD.Asc>.
				View.Select(Base);

			var result = new PXDelegateResult { IsResultSorted = true };
			result.AddRange(rows);
			return result;
		}
		#endregion

		#region Event Handlers
		protected override void _(Events.RowSelected<ScanHeader> e)
		{
			base._(e);
			Base.location.AllowInsert =
			Base.location.AllowDelete =
			Base.location.AllowUpdate = false;
		}
		#endregion

		protected override IEnumerable<ScanMode<INScanWarehousePath>> CreateScanModes() => new[] { new ScanPathMode() };
		public sealed class ScanPathMode : ScanMode
		{
			public const string Value = "PATH";
			public class value : BqlString.Constant<value> { public value() : base(ScanPathMode.Value) { } }

			public override string Code => Value;
			public override string Description => Msg.Description;

			#region State Machine
			protected override IEnumerable<ScanState<INScanWarehousePath>> CreateStates()
			{
				yield return new WarehouseState();
				yield return new LocationState();
				yield return new ConfirmState();

				// directly set state
				yield return new SetNextIndexState();
			}

			protected override IEnumerable<ScanTransition<INScanWarehousePath>> CreateTransitions()
			{
				return StateFlow(flow => flow
					.From<WarehouseState>()
					.NextTo<LocationState>());
			}

			protected override IEnumerable<ScanCommand<INScanWarehousePath>> CreateCommands()
			{
				yield return new SetNextIndexCommand();
			}

			protected override IEnumerable<ScanRedirect<INScanWarehousePath>> CreateRedirects() => AllWMSRedirects.CreateFor<INScanWarehousePath>();

			protected override void ResetMode(bool fullReset)
			{
				Clear<SetNextIndexState>(when: fullReset);
				Clear<WarehouseState>(when: fullReset);
				Clear<LocationState>();
			}
			#endregion

			#region States
			public sealed new class WarehouseState : WMSBase.WarehouseState
			{
				protected override bool UseDefaultWarehouse => false;
				protected override bool IsStateSkippable() => base.IsStateSkippable() || Basis.SiteID != null;
				protected override void Apply(INSite site)
				{
					base.Apply(site);
					Basis.Graph.site.Current = site;
				}
				protected override void ClearState()
				{
					base.ClearState();
					Basis.Graph.site.Current = null;
				}
			}

			public sealed new class LocationState : WMSBase.LocationState
			{
				protected override void Apply(INLocation location)
				{
					base.Apply(location);
					Basis.Graph.location.Current = location;
				}
				protected override void ClearState()
				{
					base.ClearState();
					Basis.Graph.location.Current = null;
				}
			}

			public sealed class ConfirmState : ConfirmationState
			{
				public override string Prompt => "";

				protected override FlowStatus PerformConfirmation()
				{
					Basis.Graph.location.SetValueExt<INLocation.pathPriority>(Basis.Graph.location.Current, Basis.NextPathIndex);
					Basis.Graph.location.UpdateCurrent();

					Basis.ReportInfo(Msg.PathIndexAssignedToLocation, Basis.NextPathIndex, Basis.Graph.location.Current.LocationCD);
					Basis.NextPathIndex++;

					return FlowStatus.Ok;
				}

				[PXLocalizable]
				public new abstract class Msg : WMSBase.Msg
				{
					public const string PathIndexAssignedToLocation = "The {0} path index is assigned to the {1} location.";
				}
			}

			public sealed class SetNextIndexState : EntityState<ushort?>
			{
				public const string Value = "NIDX";
				public class value : BqlString.Constant<value> { public value() : base(SetNextIndexState.Value) { } }


				public override string Code => Value;
				protected override string StatePrompt => Msg.Prompt;

				protected override ushort? GetByBarcode(string barcode) => ushort.TryParse(barcode, out ushort nextIndex) ? nextIndex : (ushort?)null;
				protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.BadFormat);

				protected override void Apply(ushort? nextIndex) => Basis.NextPathIndex = nextIndex;
				protected override void ClearState() => Basis.NextPathIndex = null;

				protected override void ReportSuccess(ushort? nextIndex) => Basis.ReportInfo(Msg.Ready, nextIndex);

				[PXLocalizable]
				public new abstract class Msg : WMSBase.Msg
				{
					public const string Prompt = "Enter the new next path index.";
					public const string Ready = "The next path index is set to {0}.";
					public const string BadFormat = "The quantity format does not fit the locale settings.";
				}
			}
			#endregion

			#region Commands
			public sealed class SetNextIndexCommand : ScanCommand
			{
				public const string Value = "NEXT";
				public class value : BqlString.Constant<value> { public value() : base(SetNextIndexCommand.Value) { } }

				public override string Code => Value;
				public override string ButtonName => "ScanNextPathIndex";
				public override string DisplayName => Msg.DisplayName;
				protected override bool IsEnabled => !(Basis.CurrentState is SetNextIndexState);
				protected override bool Process()
				{
					if (IsEnabled)
					{
						Basis.SetScanState<SetNextIndexState>();
						return true;
					}
					else
					{
						return false;
					}
				}

				[PXLocalizable]
				public abstract class Msg
				{
					public const string DisplayName = "Set Next Path Index";
				}
			}
			#endregion

			#region Messages
			[PXLocalizable]
			public new abstract class Msg : ScanMode.Msg
			{
				public const string Description = "Scan Path";
			}
			#endregion
		}
	}

	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public sealed class ScanPathHeader : PXCacheExtension<ScanHeader>
	{
		#region NextPathIndex
		[PXInt]
		[PXUnboundDefault(1)]
		[PXUIField(DisplayName = "Next Path Index", Enabled = false)]
		public int? NextPathIndex { get; set; }
		public abstract class nextPathIndex : BqlInt.Field<nextPathIndex> { }
		#endregion
	}
}
