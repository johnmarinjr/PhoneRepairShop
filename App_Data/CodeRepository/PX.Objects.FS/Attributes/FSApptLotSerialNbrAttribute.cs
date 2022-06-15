using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using PX.Data;
using PX.Common;
using PX.Objects.TX;
using PX.Objects.CS;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.IN;
using PX.Objects.GL;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;
using LocationStatus = PX.Objects.IN.Overrides.INDocumentRelease.LocationStatus;
using LotSerialStatus = PX.Objects.IN.Overrides.INDocumentRelease.LotSerialStatus;
using ItemLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.ItemLotSerial;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;
using IQtyAllocated = PX.Objects.IN.Overrides.INDocumentRelease.IQtyAllocated;
using System.Globalization;
using PX.Objects.CM;
using PX.Objects.CA;
using System.Linq;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Data.BQL;
using PX.Objects.GL.FinPeriods;
using PX.Objects.Common.Exceptions;
using PX.Objects.SO;

namespace PX.Objects.FS
{
	#region FSApptLotSerialNbrAttribute

	public class FSApptLotSerialNbrAttribute : SOShipLotSerialNbrAttribute
	{
		public FSApptLotSerialNbrAttribute(Type SiteID, Type InventoryType, Type SubItemType, Type LocationType)
			: base(SiteID, InventoryType, SubItemType, LocationType)
		{
			CreateCustomSelector(SiteID, InventoryType, SubItemType, LocationType);
		}

		public FSApptLotSerialNbrAttribute(Type SiteID, Type InventoryType, Type SubItemType, Type LocationType, Type ParentLotSerialNbrType)
			: base(SiteID, InventoryType, SubItemType, LocationType, ParentLotSerialNbrType)
		{
			CreateCustomSelector(SiteID, InventoryType, SubItemType, LocationType);
		}

		protected virtual void CreateCustomSelector(Type SiteID, Type InventoryType, Type SubItemType, Type LocationType)
		{
			var selector = (PXSelectorAttribute)_Attributes[_SelAttrIndex];

			var customSelector = new FSINLotSerialNbrAttribute(SiteID, InventoryType, SubItemType, LocationType, SrvOrdLineID: null);

			_Attributes[_SelAttrIndex] = customSelector;
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldUpdated.AddHandler<FSApptLineSplit.lotSerialNbr>(LotSerialNumberUpdated);
			sender.Graph.FieldVerifying.AddHandler<FSApptLineSplit.lotSerialNbr>(LotSerialNumberFieldVerifying);
		}

		protected override void LotSerialNumberUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
		}

		protected void LotSerialNumberFieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			decimal lotSerialAvailQty;
			decimal lotSerialUsedQty;
			bool foundServiceOrderAllocation;

			FSApptLineSplit apptLineSplit = (FSApptLineSplit)e.Row;
			FSAppointmentDet apptLine = (FSAppointmentDet)PXParentAttribute.SelectParent(sender, apptLineSplit, typeof(FSAppointmentDet));

			GetLotSerialAvailability(sender.Graph,
									 apptLine,
									 (string)e.NewValue,
									 true,
									 out lotSerialAvailQty,
									 out lotSerialUsedQty,
									 out foundServiceOrderAllocation);

			decimal remainingQty = lotSerialAvailQty - lotSerialUsedQty;

			if (remainingQty < 1m)
			{
				if (foundServiceOrderAllocation == false)
				{
					throw new PXSetPropertyException(TX.Error.LotSerialNotAvailable);
				}
				else
				{
					throw new PXSetPropertyException(TX.Error.LotSerialNbrOnOtherAppointment);
				}
			}
		}

		public override void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.RowSelected(sender, e);
		}

		public override void FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
		}

		public virtual void GetLotSerialAvailability(PXGraph graphToQuery, FSAppointmentDet apptLine, string lotSerialNbr, bool ignoreUseByApptLine, out decimal lotSerialAvailQty, out decimal lotSerialUsedQty, out bool foundServiceOrderAllocation)
		=> GetLotSerialAvailabilityInt(graphToQuery, apptLine, lotSerialNbr, ignoreUseByApptLine, out lotSerialAvailQty, out lotSerialUsedQty, out foundServiceOrderAllocation);

		// TODO: Rename this method to GetLotSerialAvailabilityStatic
		public static void GetLotSerialAvailabilityInt(PXGraph graphToQuery, FSAppointmentDet apptLine, string lotSerialNbr, bool ignoreUseByApptLine, out decimal lotSerialAvailQty, out decimal lotSerialUsedQty, out bool foundServiceOrderAllocation)
		{
			GetLotSerialAvailabilityStatic(graphToQuery, apptLine, lotSerialNbr, null, ignoreUseByApptLine, out lotSerialAvailQty, out lotSerialUsedQty, out foundServiceOrderAllocation);
		}

		public static void GetLotSerialAvailabilityStatic(PXGraph graphToQuery, FSAppointmentDet apptLine, string lotSerialNbr, int? splitLineNbr, bool ignoreUseByApptLine, out decimal lotSerialAvailQty, out decimal lotSerialUsedQty, out bool foundServiceOrderAllocation)
		{
			lotSerialAvailQty = 0m;
			lotSerialUsedQty = 0m;
			foundServiceOrderAllocation = false;

			if (string.IsNullOrEmpty(lotSerialNbr) == true && splitLineNbr == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(lotSerialNbr) == false)
			{
				splitLineNbr = null;
			}

			bool searchINAvailQty = true;
			FSSODetSplit soDetSplit = null;

			if (apptLine.SODetID != null && apptLine.SODetID > 0)
			{
				FSSODet fsSODetRow = FSSODet.UK.Find(graphToQuery, apptLine.SODetID);

				if (fsSODetRow != null)
				{
					BqlCommand bqlCommand = new Select<FSSODetSplit,
									Where<
										FSSODetSplit.srvOrdType, Equal<Required<FSSODetSplit.srvOrdType>>,
										And<FSSODetSplit.refNbr, Equal<Required<FSSODetSplit.refNbr>>,
									And<FSSODetSplit.lineNbr, Equal<Required<FSSODetSplit.lineNbr>>>>>>();

					List<object> parameters = new List<object>();
					parameters.Add(fsSODetRow.SrvOrdType);
					parameters.Add(fsSODetRow.RefNbr);
					parameters.Add(fsSODetRow.LineNbr);

					if (splitLineNbr == null)
					{
						bqlCommand = bqlCommand.WhereAnd(typeof(Where<FSSODetSplit.lotSerialNbr, Equal<Required<FSSODetSplit.lotSerialNbr>>>));
						parameters.Add(lotSerialNbr);
					}
					else
					{
						bqlCommand = bqlCommand.WhereAnd(typeof(Where<FSSODetSplit.splitLineNbr, Equal<Required<FSSODetSplit.splitLineNbr>>>));
						parameters.Add(splitLineNbr);
					}

					soDetSplit = (FSSODetSplit)new PXView(graphToQuery, false, bqlCommand).SelectSingle(parameters.ToArray());

					if (soDetSplit != null)
					{
						searchINAvailQty = false;
					}
				}
			}

			if (searchINAvailQty == true && string.IsNullOrEmpty(lotSerialNbr) == false)
			{
				INLotSerialStatus lotSerialStatus = IN.INLotSerialStatus.PK.Find(graphToQuery, apptLine.InventoryID, apptLine.SubItemID, apptLine.SiteID, apptLine.LocationID, lotSerialNbr);

				if (lotSerialStatus != null)
				{
					lotSerialAvailQty = (decimal)lotSerialStatus.QtyAvail;
				}
			}
			else if (soDetSplit != null)
			{
				lotSerialAvailQty = (decimal)soDetSplit.Qty;

				BqlCommand bqlCommand = new Select4<FSApptLineSplit,
													 Where<
														 FSApptLineSplit.origSrvOrdType, Equal<Required<FSApptLineSplit.origSrvOrdType>>,
														 And<FSApptLineSplit.origSrvOrdNbr, Equal<Required<FSApptLineSplit.origSrvOrdNbr>>,
								And<FSApptLineSplit.origLineNbr, Equal<Required<FSApptLineSplit.origLineNbr>>>>>,
													 Aggregate<
														 Sum<FSApptLineSplit.qty>>>();

				List<object> parameters = new List<object>();
				parameters.Add(soDetSplit.SrvOrdType);
				parameters.Add(soDetSplit.RefNbr);
				parameters.Add(soDetSplit.LineNbr);

				if (splitLineNbr == null)
				{
					bqlCommand = bqlCommand.WhereAnd(typeof(Where<FSApptLineSplit.lotSerialNbr, Equal<Required<FSApptLineSplit.lotSerialNbr>>>));
					parameters.Add(soDetSplit.LotSerialNbr);
				}
				else
				{
					bqlCommand = bqlCommand.WhereAnd(typeof(Where<FSApptLineSplit.origSplitLineNbr, Equal<Required<FSApptLineSplit.origSplitLineNbr>>>));
					parameters.Add(soDetSplit.SplitLineNbr);
				}

				if (ignoreUseByApptLine == true)
				{
					bqlCommand = bqlCommand.WhereAnd(typeof(Where<
																 FSApptLineSplit.srvOrdType, NotEqual<Required<FSApptLineSplit.srvOrdType>>,
																 Or<FSApptLineSplit.apptNbr, NotEqual<Required<FSApptLineSplit.apptNbr>>,
																 Or<FSApptLineSplit.lineNbr, NotEqual<Required<FSApptLineSplit.lineNbr>>>>>));
					parameters.Add(apptLine.SrvOrdType);
					parameters.Add(apptLine.RefNbr);
					parameters.Add(apptLine.LineNbr);
				}

				FSApptLineSplit otherSplitsSum = (FSApptLineSplit)new PXView(graphToQuery, false, bqlCommand).SelectSingle(parameters.ToArray());

				decimal? usedQty = otherSplitsSum != null ? otherSplitsSum.Qty : 0m;
				lotSerialUsedQty = usedQty != null ? (decimal)usedQty : 0m;
				foundServiceOrderAllocation = true;
			}
		}
	}
	#endregion
}
