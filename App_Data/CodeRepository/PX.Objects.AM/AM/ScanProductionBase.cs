using PX.BarcodeProcessing;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AM.Attributes;
using PX.Objects.AM.CacheExtensions;
using PX.Objects.Common.Extensions;
using PX.Objects.EP;
using PX.Objects.IN;
using PX.Objects.IN.WMS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AM
{
	public abstract class ScanProductionBase<TSelf, TGraph, TDocType> : WarehouseManagementSystem<TSelf, TGraph>
		where TSelf : ScanProductionBase<TSelf, TGraph, TDocType>
		where TGraph : AMBatchEntryBase, new()
		where TDocType : IConstant, IBqlOperand, new()
	{
		public PXSetupOptional<AMScanSetup, Where<AMScanSetup.branchID, Equal<Current<AccessInfo.branchID>>>> Setup;
		public PXSetup<AMPSetup> AMSetup;
		public ProdScanHeader ProdHeader => Header.Get<ProdScanHeader>() ?? new ProdScanHeader();
		public ValueSetter<ScanHeader>.Ext<ProdScanHeader> ProdSetter => HeaderSetter.With<ProdScanHeader>();
		protected virtual bool UseDefaultOrderType() => Setup.Current.UseDefaultOrderType == true;
		protected virtual bool IsWarehouseRequired() => Setup.Current.DefaultWarehouse != true || DefaultSiteID == null;
		public abstract bool UseDefaultWarehouse { get; }
		public AMBatch Batch => BatchView.Current;
		public PXSelectBase<AMBatch> BatchView => Graph.AMBatchDataMember;
		public PXSelectBase<AMMTran> Details => Graph.AMMTranDataMember;
		public override bool DocumentLoaded => Batch != null;
		public override bool DocumentIsEditable => base.DocumentIsEditable && AMBatch.PK.Find(Base, Batch)?.Released != true;
		public bool NotReleasedAndHasLines => Batch?.Released != true && Details.SelectMain().Any();
		public abstract class UserSetup : PXUserSetupPerMode<UserSetup, TGraph, ScanHeader, AMScanUserSetup, AMScanUserSetup.userID, AMScanUserSetup.mode, TDocType> { }

		#region Event Handlers
		[PXMergeAttributes]
		[PXUnboundDefault(typeof(AMBatch.batNbr))]
		[PXSelector(typeof(SearchFor<AMBatch.batNbr>.Where<AMBatch.docType.IsEqual<ProdScanHeader.docType.FromCurrent>>))]
		protected virtual void _(Events.CacheAttached<WMSScanHeader.refNbr> e) { }
		protected override void _(Events.RowSelected<ScanHeader> e)
		{
			base._(e);

			if (Batch == null && !string.IsNullOrEmpty(RefNbr))
				RefNbr = null;

			Details.Cache.SetAllEditPermissions(Batch == null || Batch.Released != true);
			Details.Cache.AllowInsert = false;
		}

		protected virtual void _(Events.FieldDefaulting<ScanHeader, ProdScanHeader.docType> e)
			=> e.NewValue = new TDocType().Value;

		protected virtual void _(Events.FieldUpdated<ScanHeader, WMSScanHeader.refNbr> e)
			=> BatchView.Current = e.NewValue == null ? null : BatchView.Search<AMBatch.batNbr>(e.NewValue);

		protected virtual void _(Events.FieldDefaulting<ScanHeader, ProdScanHeader.orderType> e)
			=> e.NewValue = !UseDefaultOrderType() ? null : AMSetup.Current.DefaultOrderType;

		protected virtual void _(Events.RowUpdated<AMScanUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);
		protected virtual void _(Events.RowInserted<AMScanUserSetup> e) => e.Row.IsOverridden = !e.Row.SameAs(Setup.Current);
		#endregion

		#region Overrides
		protected override bool ProcessSingleBarcode(string barcode)
		{
			// just clears the selected document after it got released on the next scan
			if (Header.ProcessingSucceeded == true && Batch?.Released == true)
			{
				RefNbr = null;
				NoteID = null;
			}

			return base.ProcessSingleBarcode(barcode);
		}

		protected override ScanCommand<TSelf> DecorateScanCommand(ScanCommand<TSelf> original)
		{
			var command = base.DecorateScanCommand(original);

			if (command is RemoveCommand remove)
				remove.Intercept.IsEnabled.ByConjoin(basis => basis.NotReleasedAndHasLines);

			if (command is QtySupport.SetQtyCommand setQty)
				setQty.Intercept.IsEnabled.ByConjoin(basis => basis.UseQtyCorrectection.Implies(basis.DocumentIsEditable && basis.NotReleasedAndHasLines));

			return command;
		}

		[PXOverride]
		public virtual void Persist(Action base_Persist)
		{
			base_Persist();

			RefNbr = Batch?.BatNbr;
			NoteID = Batch?.NoteID;

			Details.Cache.Clear();
			Details.Cache.ClearQueryCacheObsolete();
		}

		#endregion

		#region Properties
		public string DocType
		{
			get => ProdHeader.DocType;
			set => ProdSetter.Set(h => h.DocType, value);
		}
		public string OrderType
		{
			get => ProdHeader.OrderType;
			set => ProdSetter.Set(h => h.OrderType, value);
		}
		public string ProdOrdID
		{
			get => ProdHeader.ProdOrdID;
			set => ProdSetter.Set(h => h.ProdOrdID, value);
		}
		public int? OperationID
		{
			get => ProdHeader.OperationID;
			set => ProdSetter.Set(h => h.OperationID, value);
		}
		public string ParentLotSerialNbr
		{
			get => ProdHeader.ParentLotSerialNbr;
			set => ProdSetter.Set(h => h.ParentLotSerialNbr, value);
		}
		public string ParentLotSerialRequired
		{
			get => ProdHeader.ParentLotSerialRequired;
			set => ProdSetter.Set(h => h.ParentLotSerialRequired, value);
		}
		public string LaborType
		{
			get => ProdHeader.LaborType;
			set => ProdSetter.Set(h => h.LaborType, value);
		}
		public string LaborCodeID
		{
			get => ProdHeader.LaborCodeID;
			set => ProdSetter.Set(h => h.LaborCodeID, value);
		}
		public int? EmployeeID
		{
			get => ProdHeader.EmployeeID;
			set => ProdSetter.Set(h => h.EmployeeID, value);
		}
		public int? LaborTime
		{
			get => ProdHeader.LaborTime;
			set => ProdSetter.Set(h => h.LaborTime, value);
		}
		public string ShiftCD
		{
			get => ProdHeader.ShiftCD;
			set => ProdSetter.Set(h => h.ShiftCD, value);
		}
		#endregion

		#region States
		public new sealed class OrderTypeState : EntityState<AMOrderType>
		{
			public const string Value = "ORTY";
			public class value : BqlString.Constant<value> { public value() : base(OrderTypeState.Value) { } }
			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;

			protected override AMOrderType GetByBarcode(string barcode) => AMOrderType.PK.Find(Basis, barcode);
			protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
			protected override void Apply(AMOrderType orderType) {
				Basis.OrderType = orderType.OrderType;
				if (Basis.DocType == AMDocType.Labor)
					Basis.LaborType = AMLaborType.Direct;
			}
			protected override void ClearState() {
				Basis.OrderType = null;
				Basis.LaborType = null;
			}
			protected override void ReportSuccess(AMOrderType selection) => Basis.Reporter.Info(Msg.Ready, selection.OrderType);

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the Order Type.";
				public const string Missing = "The {0} order type is not found.";
				public const string Ready = "The {0} order type is selected.";
			}
			#endregion
		}

		public new sealed class ProdOrdState : EntityState<AMProdItem>
		{
			public const string Value = "PROD";
			public class value : BqlString.Constant<value> { public value() : base(ProdOrdState.Value) { } }
			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;
			protected override AMProdItem GetByBarcode(string barcode)
			{
				if(Basis.OrderType == null && Basis.UseDefaultOrderType() == true)
				{
					Basis.OrderType = Basis.AMSetup.Current.DefaultOrderType;
				}
				return AMProdItem.PK.Find(Basis, Basis.OrderType, barcode);
			} 
			protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
			protected override Validation Validate(AMProdItem proditem)
			{
				if (proditem.Function == OrderTypeFunction.Disassemble)
					return Validation.Fail(Msg.ProdOrdWrongStatus, proditem.OrderType, proditem.ProdOrdID, ProductionOrderStatus.GetStatusDescription(proditem.StatusID));
				else if (!ProductionStatus.IsReleasedTransactionStatus(proditem))
					return Validation.Fail(Msg.ProdOrdWrongType);

				return Validation.Ok;
			}
			protected override void Apply(AMProdItem proditem)
			{
				Basis.ProdOrdID = proditem.ProdOrdID;
				Basis.ParentLotSerialRequired = proditem.ParentLotSerialRequired;				
				if (Basis.DocType == AMDocType.Move || Basis.DocType == AMDocType.Labor)
				{
					Basis.InventoryID = proditem.InventoryID;
					Basis.UOM = proditem.UOM;
					Basis.LaborType = AMLaborType.Direct;
				}
			}
			protected override void ClearState() => Basis.ProdOrdID = null;
			protected override void ReportSuccess(AMProdItem selection) => Basis.Reporter.Info(Msg.Ready, selection.ProdOrdID);


			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the Production Order ID.";
				public const string Missing = "The {0} production order is not found.";
				public const string ProdOrdWrongType = "The production order {0} is a Disassembly type.";
				public const string ProdOrdWrongStatus = "The production order {0}, {1} has a status of {2}";
				public const string Ready = "The {0} production order is selected.";
			}
			#endregion
		}

		public new sealed class OperationState : EntityState<AMProdOper>
		{
			public const string Value = "OPER";
			public class value : BqlString.Constant<value> { public value() : base(OperationState.Value) { } }
			public override string Code => Value;
			protected override string StatePrompt => Msg.Prompt;
			protected override AMProdOper GetByBarcode(string barcode)
			{
				return SelectFrom<AMProdOper>.Where<AMProdOper.orderType.IsEqual<@P.AsString>
					.And<AMProdOper.prodOrdID.IsEqual<@P.AsString>
					.And<AMProdOper.operationCD.IsEqual<@P.AsString>>>>.View.Select(Basis, Basis.OrderType, Basis.ProdOrdID, barcode);
			}
			protected override void ReportMissing(string barcode) => Basis.ReportError(Msg.Missing, barcode);
			protected override void Apply(AMProdOper oper) => Basis.OperationID = oper.OperationID;
			protected override void ClearState() => Basis.OperationID = null;
			protected override void ReportSuccess(AMProdOper selection) => Basis.Reporter.Info(Msg.Ready, selection.OperationCD);

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string Prompt = "Scan the Operation ID.";
				public const string Missing = "The {0} operation is not found.";
				public const string Ready = "The {0} operation is selected.";
			}
			#endregion
		}
		public new sealed class WarehouseState : WarehouseManagementSystem<TSelf, TGraph>.WarehouseState
		{
			protected override bool UseDefaultWarehouse => Basis.UseDefaultWarehouse;
		}
		#endregion

		#region Commands
		public abstract class ReleaseCommand : ScanCommand
		{
			public override string Code => "RELEASE";
			public override string ButtonName => "scanRelease";
			public override string DisplayName => Msg.DisplayName;
			protected override bool IsEnabled => Basis.DocumentIsEditable;

			protected override bool Process()
			{
				if (Basis.Batch != null)
				{
					if (Basis.Batch.Released == true)
					{
						Basis.ReportError(IN.Messages.Document_Status_Invalid);
						return true;
					}

					if (Basis.Batch.Hold != false)
						Basis.BatchView.SetValueExt<AMBatch.hold>(Basis.Batch, false);
					Basis.Save.Press();

					var msg = (DocumentIsReleased, DocumentReleaseFailed);

					Basis
					.WaitFor<AMBatch>((basis, doc) =>
					{
						AMDocumentRelease.ReleaseDoc(new List<AMBatch>() { doc }, false);
						basis.CurrentMode.Commands.OfType<ReleaseCommand>().FirstOrDefault()?.OnAfterRelease(doc);
					})
					.WithDescription(DocumentReleasing, Basis.Batch.BatNbr)
					.ActualizeDataBy((basis, doc) => AMBatch.PK.Find(basis, doc))
					.OnSuccess(x => x.Say(msg.DocumentIsReleased))
					.OnFail(x => x.Say(msg.DocumentReleaseFailed))
					.BeginAwait(Basis.Batch);

					return true;
				}
				return false;
			}

			protected virtual void OnAfterRelease(AMBatch doc) { }

			protected abstract string DocumentReleasing { get; }
			protected abstract string DocumentIsReleased { get; }
			protected abstract string DocumentReleaseFailed { get; }

			#region Messages
			[PXLocalizable]
			public abstract class Msg
			{
				public const string DisplayName = "Release";
			}
			#endregion
		}
		#endregion

	}

	public sealed class ProdScanHeader : PXCacheExtension<WMSScanHeader, QtyScanHeader, ScanHeader>
	{
		#region DocType
		[PXUnboundDefault(typeof(AMBatch.docType))]
		[PXString(1, IsFixed = true)]
		[AMDocType.List]
		public string DocType { get; set; }
		public abstract class docType : BqlString.Field<docType> { }
		#endregion
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }

		[PXString(2, IsFixed = true, InputMask = ">aa")]
		[PXUIField(DisplayName = "Order Type")]
		[PXUnboundDefault(typeof(AMPSetup.defaultOrderType))]
		public String OrderType { get; set; }
		#endregion
		#region ProdOrdID
		public abstract class prodOrdID : PX.Data.BQL.BqlString.Field<prodOrdID> { }

		[PXUnboundDefault]
		[PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Production Nbr", Visibility = PXUIVisibility.SelectorVisible)]
		public String ProdOrdID { get; set; }
		#endregion
		#region OperationID
		public abstract class operationID : PX.Data.BQL.BqlInt.Field<operationID> { }

		[PXInt]
		[PXUIField(DisplayName = "Operation ID")]
		[PXUnboundDefault(typeof(Search<
			AMProdOper.operationID,
			Where<AMProdOper.orderType, Equal<Current<orderType>>,
				And<AMProdOper.prodOrdID, Equal<Current<prodOrdID>>>>,
			OrderBy<
				Asc<AMProdOper.operationCD>>>))]
		[PXSelector(typeof(Search<AMProdOper.operationID,
				Where<AMProdOper.orderType, Equal<Current<orderType>>,
					And<AMProdOper.prodOrdID, Equal<Current<prodOrdID>>>>>),
			SubstituteKey = typeof(AMProdOper.operationCD))]
		[PXFormula(typeof(Validate<AMMTran.prodOrdID>))]
		public int? OperationID { get; set; }
		#endregion
		#region ParentLotSerialNbr
		//[AMLotSerialNbr(typeof(inventoryID), typeof(subItemID), typeof(locationID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDBString(INLotSerialStatus.lotSerialNbr.LENGTH, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Parent Lot/Serial Nbr.", FieldClass = "LotSerial")]
		[PXUnboundDefault("")]
		public string ParentLotSerialNbr { get; set; }
		public abstract class parentLotSerialNbr : PX.Data.BQL.BqlString.Field<parentLotSerialNbr> { }
		#endregion
		#region ParentLotSerialRequired
		/// <summary>
		/// Parent lot number is/isn't required for material transactions
		/// </summary>
		public abstract class parentLotSerialRequired : PX.Data.BQL.BqlBool.Field<parentLotSerialRequired> { }

		/// <summary>
		/// Parent lot number is/isn't required for material transactions
		/// </summary>
		[PXString(1)]
		[PXUIField(DisplayName = "Require Parent Lot/Serial Number")]
		public string ParentLotSerialRequired { get; set; }

		#endregion
		#region LaborType
		public abstract class laborType : PX.Data.BQL.BqlString.Field<laborType> { }

		[PXString(1, IsFixed = true)]
		[AMLaborType.List]
		[PXUnboundDefault(AMLaborType.Direct, PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[PXUIField(DisplayName = "Type")]
		public String LaborType { get; set; }
		#endregion
		#region LaborCodeID

		public abstract class laborCodeID : PX.Data.BQL.BqlString.Field<laborCodeID> { }

		[PXString(15, InputMask = ">AAAAAAAAAAAAAAA")]
		[PXUIField(DisplayName = "Labor Code")]
		public String LaborCodeID { get; set; }
		#endregion
		#region EmployeeID
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }

		[PXInt]
		[ProductionEmployeeSelector]
		[PXDefault(typeof(Search<EPEmployee.bAccountID,
				Where<EPEmployee.userID, Equal<Current<AccessInfo.userID>>,
				And<EPEmployeeExt.amProductionEmployee, Equal<True>,
				And<Current<AMPSetup.defaultEmployee>, Equal<True>>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Employee ID")]
		public Int32? EmployeeID { get; set; }
		#endregion
		#region LaborTime
		public abstract class laborTime : PX.Data.BQL.BqlInt.Field<laborTime> { }

		[PXInt]
		[PXTimeList]
		[PXUIField(DisplayName = "Labor Time")]
		public Int32? LaborTime { get; set; }
		#endregion
		#region ShiftCD
		public abstract class shiftCD : PX.Data.BQL.BqlString.Field<shiftCD> { }

		[PXString(15)]
		[PXUnboundDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[PXUIField(DisplayName = "Shift")]
		[ShiftCodeSelector]
		public String ShiftCD { get; set; }
		#endregion
	}
}
