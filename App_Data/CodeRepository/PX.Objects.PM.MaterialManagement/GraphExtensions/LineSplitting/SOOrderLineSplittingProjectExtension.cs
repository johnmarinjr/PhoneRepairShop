using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.SO.GraphExtensions.SOOrderEntryExt;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.LineSplitting
{
	[PXProtectedAccess(typeof(SOOrderLineSplittingExtension))]
	public abstract class SOOrderLineSplittingProjectExtension
		: LineSplittingProjectExtension<SOOrderEntry, SOOrderLineSplittingExtension, SOOrder, SOLine, SOLineSplit>
	{
		public static bool IsActive() => UseProjectAvailability;

		#region Select LotSerial Status
		protected override PXSelectBase<PMLotSerialStatus> GetSerialStatusCmdBaseProject(SOLine line, PXResult<InventoryItem, INLotSerClass> item)
		{
			if (LSBase.IsLocationEnabled || !LSBase.IsLotSerialRequired)
				return base.GetSerialStatusCmdBaseProject(line, item);

			return new
				SelectFrom<PMLotSerialStatus>.
				InnerJoin<INLocation>.On<PMLotSerialStatus.FK.Location>.
				InnerJoin<INSiteLotSerial>.On<
					INSiteLotSerial.inventoryID.IsEqual<PMLotSerialStatus.inventoryID>.
					And<INSiteLotSerial.siteID.IsEqual<PMLotSerialStatus.siteID>>.
					And<INSiteLotSerial.lotSerialNbr.IsEqual<PMLotSerialStatus.lotSerialNbr>>>.
				Where<
					PMLotSerialStatus.inventoryID.IsEqual<PMLotSerialStatus.inventoryID.FromCurrent>.
					And<PMLotSerialStatus.siteID.IsEqual<PMLotSerialStatus.siteID.FromCurrent>>.
					And<PMLotSerialStatus.projectID.IsEqual<PMLotSerialStatus.projectID.FromCurrent>>.
					And<PMLotSerialStatus.taskID.IsEqual<PMLotSerialStatus.taskID.FromCurrent>>.
					And<PMLotSerialStatus.qtyOnHand.IsGreater<decimal0>>.
					And<INSiteLotSerial.qtyHardAvail.IsGreater<decimal0>>>.
				View(Base);
		}

		protected override void AppendSerialStatusCmdWhereProject(PXSelectBase<PMLotSerialStatus> cmd, SOLine line, INLotSerClass lotSerClass)
		{
			if (line.SubItemID != null)
				cmd.WhereAnd<Where<PMLotSerialStatus.subItemID.IsEqual<PMLotSerialStatus.subItemID.FromCurrent>>>();

			if (line.LocationID != null)
			{
				cmd.WhereAnd<Where<PMLotSerialStatus.locationID.IsEqual<PMLotSerialStatus.locationID.FromCurrent>>>();
			}
			else
			{
				switch (line.TranType)
				{
					case INTranType.Transfer:
						cmd.WhereAnd<Where<INLocation.transfersValid.IsEqual<True>>>();
						break;
					default:
						cmd.WhereAnd<Where<INLocation.salesValid.IsEqual<True>>>();
						break;
				}
			}

			if (lotSerClass.IsManualAssignRequired == true)
			{
				if (string.IsNullOrEmpty(line.LotSerialNbr))
					cmd.WhereAnd<Where<True.IsEqual<False>>>();
				else
					cmd.WhereAnd<Where<PMLotSerialStatus.lotSerialNbr.IsEqual<PMLotSerialStatus.lotSerialNbr.FromCurrent>>>();
			}
		}
		#endregion
	}
}
