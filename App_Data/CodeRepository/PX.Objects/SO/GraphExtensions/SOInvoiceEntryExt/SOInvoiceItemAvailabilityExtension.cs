using System;
using System.Linq;

using PX.Common;
using PX.Data;

using PX.Objects.Common;
using PX.Objects.Common.Exceptions;
using PX.Objects.AR;
using PX.Objects.IN;

using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.SO.GraphExtensions.SOInvoiceEntryExt
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class SOInvoiceItemAvailabilityExtension : SOBaseItemAvailabilityExtension<SOInvoiceEntry, ARTran, ARTranAsSplit>
	{
		protected override ARTranAsSplit EnsureSplit(ILSMaster row) => Base.FindImplementation<SOInvoiceLineSplittingExtension>().EnsureSplit(row);

		protected override decimal GetUnitRate(ARTran line) => GetUnitRate<ARTran.inventoryID, ARTran.uOM>(line);

		protected override string GetStatus(ARTran line) => string.Empty;

		public override SOInvoicedRecords SelectInvoicedRecords(string arDocType, string arRefNbr)
			=> SelectInvoicedRecords(arDocType, arRefNbr, includeDirectLines: true);


		public virtual void MemoOrderCheck(ARTran line)
		{
			var result = MemoCheckQty(line);
			if (!result.Success)
			{
				var documents = result.ReturnRecords?
					.Select(x => x.DocumentNbr)
					.Where(nbr => nbr != line.RefNbr);

				RaiseErrorOn<ARTran.qty>(true, line, Messages.InvoiceCheck_DecreaseQty,
					LineCache.GetValueExt<ARTran.origInvoiceNbr>(line),
					LineCache.GetValueExt<ARTran.inventoryID>(line),
					documents.With(ds => string.Join(", ", ds)) ?? string.Empty);
			}

			OrderCheck(line, onPersist: true);
		}

		protected virtual ReturnedQtyResult MemoCheckQty(ARTran line)
		{
			bool directInvoiceLine = string.IsNullOrEmpty(line.SOOrderNbr);
			bool
				isEmptyOrigInvoiceType = string.IsNullOrEmpty(line.OrigInvoiceType),
				isEmptyOrigInvoiceNbr = string.IsNullOrEmpty(line.OrigInvoiceNbr),
				isEmptyOrigInvoiceLineNbr = line.OrigInvoiceLineNbr == null;
			if (directInvoiceLine && (!isEmptyOrigInvoiceType || !isEmptyOrigInvoiceNbr || !isEmptyOrigInvoiceLineNbr))
			{
				if (isEmptyOrigInvoiceType)
					RaiseEmptyFieldException<ARTran.origInvoiceType>();
				else if (isEmptyOrigInvoiceNbr)
					RaiseEmptyFieldException<ARTran.origInvoiceNbr>();
				else if (isEmptyOrigInvoiceLineNbr)
					RaiseEmptyFieldException<ARTran.origInvoiceLineNbr>();

				void RaiseEmptyFieldException<TField>() where TField : IBqlField
					=> RaiseErrorOn<TField>(true, line, Messages.IncompleteLinkToOrigInvoiceLine,
						PXUIFieldAttribute.GetDisplayName(LineCache, typeof(TField).Name));
			}

			return line.InvtMult == 0
				? new ReturnedQtyResult(true)
				: MemoCheckQty(line.InventoryID, line.OrigInvoiceType, line.OrigInvoiceNbr, line.OrigInvoiceLineNbr, null, null, null);
		}

		public virtual void OrderCheck(ARTran line, bool onPersist = false)
		{
			if (line.InvtMult == 0 || line.SOOrderType == null && line.SOOrderNbr == null && line.SOOrderLineNbr == null || line.Released == true)
				return;

			if (line.LineType == SOLineType.MiscCharge)
				RaiseErrorOn<ARTran.inventoryID>(onPersist, line, Messages.NonStockNoShipCantBeInvoicedDirectly, LineCache.GetValueExt<ARTran.inventoryID>(line));

			var soLine = SOLine.PK.Find(Base, line.SOOrderType, line.SOOrderNbr, line.SOOrderLineNbr);
			if (soLine == null)
			{
				RaiseErrorOn<ARTran.sOOrderNbr>(onPersist, line, Messages.SOLineNotFound);
				return;
			}

			SOOrderType orderType = SOOrderType.PK.Find(Base, soLine.OrderType);
			if (orderType.OrderType == null || orderType.RequireShipping == false || orderType.ARDocType == ARDocType.NoUpdate)
				RaiseErrorOn<ARTran.sOOrderType>(onPersist, line, Messages.SOTypeCantBeInvoicedDirectly, LineCache.GetValueExt<ARTran.sOOrderType>(line));

			if (soLine.Completed == true)
				RaiseErrorOn<ARTran.sOOrderNbr>(onPersist, line, Messages.CompletedSOLineCantBeInvoicedDirectly);

			if (soLine.CustomerID != line.CustomerID)
				RaiseErrorOn<ARTran.sOOrderNbr>(onPersist, line, Messages.CustomerDiffersInvoiceAndSO);

			if (soLine.POCreate == true)
				RaiseErrorOn<ARTran.sOOrderNbr>(onPersist, line, Messages.SOLineMarkedForPOCantBeInvoicedDirectly);

			if (soLine.InventoryID != line.InventoryID)
				RaiseErrorOn<ARTran.inventoryID>(onPersist, line, Messages.InventoryItemDiffersInvoiceAndSO);

			int arTranInvtMult = Math.Sign((line.InvtMult * line.Qty) ?? 0m);
			if (arTranInvtMult != 0)
			{
				int soLineInvtMult = (soLine.Operation == SOOperation.Receipt) ? 1 : -1;
				if (soLineInvtMult != arTranInvtMult)
					RaiseErrorOn<ARTran.qty>(onPersist, line, Messages.OperationDiffersInvoiceAndSO);
			}

			decimal absQty = Math.Abs(line.BaseQty ?? 0m);
			if (PXDBQuantityAttribute.Round(soLine.BaseOrderQty.Value * soLine.CompleteQtyMax.Value / 100m - soLine.BaseShippedQty.Value - absQty) < 0m)
				RaiseErrorOn<ARTran.qty>(onPersist, line, Messages.OrderCheck_QtyNegative,
					LineCache.GetValueExt<ARTran.inventoryID>(line),
					LineCache.GetValueExt<ARTran.subItemID>(line),
					LineCache.GetValueExt<ARTran.sOOrderType>(line),
					LineCache.GetValueExt<ARTran.sOOrderNbr>(line));
		}


		public virtual IStatus FetchSite(ARTran line, bool excludeCurrent = false)
		{
			ARTranAsSplit split = EnsureSplit(line);

			var availability = FetchSite(split, excludeCurrent);

			if (excludeCurrent)
				availability = ExcludeAllocated(line, availability);

			return availability;
		}

		protected virtual IStatus ExcludeAllocated(ARTran line, IStatus availability)
		{
			if (availability == null)
				return null;

			INItemPlanIDAttribute.GetInclQtyAvail<SiteStatus>(LineCache, line, out decimal signQtyAvail, out decimal signQtyHardAvail);

			decimal? lineQtyAvail = 0m;
			if (signQtyAvail != 0m)
				lineQtyAvail -= signQtyAvail * (line.BaseQty ?? 0m);

			decimal? lineQtyHardAvail = 0m;
			if (signQtyHardAvail != 0m)
				lineQtyHardAvail -= signQtyHardAvail * (line.BaseQty ?? 0m);

			availability.QtyAvail += lineQtyAvail;
			availability.QtyHardAvail += lineQtyHardAvail;
			availability.QtyNotAvail = -lineQtyAvail;

			return availability;
		}


		protected override void RaiseQtyExceptionHandling(ARTran line, PXExceptionInfo ei, decimal? newValue)
		{
			LineCache.RaiseExceptionHandling<ARTran.qty>(line, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					LineCache.GetStateExt<ARTran.inventoryID>(line),
					LineCache.GetStateExt<ARTran.subItemID>(line),
					LineCache.GetStateExt<ARTran.siteID>(line),
					LineCache.GetStateExt<ARTran.locationID>(line),
					LineCache.GetValue<ARTran.lotSerialNbr>(line)));
		}

		protected override void RaiseQtyExceptionHandling(ARTranAsSplit split, PXExceptionInfo ei, decimal? newValue)
		{
			SplitCache.RaiseExceptionHandling<ARTranAsSplit.qty>(split, null,
				new PXSetPropertyException(ei.MessageFormat, PXErrorLevel.Warning,
					SplitCache.GetStateExt<ARTranAsSplit.inventoryID>(split),
					SplitCache.GetStateExt<ARTranAsSplit.subItemID>(split),
					SplitCache.GetStateExt<ARTranAsSplit.siteID>(split),
					SplitCache.GetStateExt<ARTranAsSplit.locationID>(split),
					SplitCache.GetValue<ARTranAsSplit.lotSerialNbr>(split)));
		}


		protected virtual void RaiseErrorOn<TField>(bool onPersist, ARTran line, string errorMessage, params object[] args)
			where TField : IBqlField
		{
			var propertyException = new PXSetPropertyException(errorMessage, args);
			if (onPersist)
			{
				object value = LineCache.GetValueExt(line, typeof(TField).Name);
				if (LineCache.RaiseExceptionHandling(typeof(TField).Name, line, value, propertyException))
					throw new PXRowPersistingException(typeof(TField).Name, value, errorMessage, args);
			}
			else
			{
				throw propertyException;
			}
		}
	}
}
