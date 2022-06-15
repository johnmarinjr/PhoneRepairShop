using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class INSiteMaintExt : PXGraphExtension<INSiteMaint>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<CS.FeaturesSet.projectAccounting>();
        }

		protected virtual void _(Events.RowPersisting<INLocation> e)
		{
			if (e.Row == null) return;

			if (e.Operation.Command() != PXDBOperation.Delete && e.Row.ProjectID != null && e.Row.TaskID == null && e.Row.Active == true)
			{
				INLocation anotherWildcardLocation =
					SelectFrom<INLocation>.
					Where<
						INLocation.locationID.IsNotEqual<P.AsInt>.
						And<INLocation.projectID.IsEqual<P.AsInt>>.
						And<INLocation.taskID.IsNull>.
						And<INLocation.active.IsEqual<True>>>.
					View.Select(Base, e.Row.LocationID, e.Row.ProjectID);

				if (anotherWildcardLocation != null)
				{
					PMProject project = SelectFrom<PMProject>.Where<PMProject.contractID.IsEqual<P.AsInt>>.View.Select(Base, e.Row.ProjectID);
					INSite warehouse = INSite.PK.Find(Base, anotherWildcardLocation.SiteID);
					if (e.Cache.RaiseExceptionHandling<INLocation.projectID>(e.Row, project.ContractCD, new PXSetPropertyException(IN.Messages.ProjectWildcardLocationIsUsedIn, PXErrorLevel.Error, warehouse.SiteCD, anotherWildcardLocation.LocationCD)))
					{
						throw new PXRowPersistingException(PXDataUtils.FieldName<INLocation.projectID>(), e.Row.ProjectID, IN.Messages.ProjectWildcardLocationIsUsedIn, warehouse.SiteCD, anotherWildcardLocation.LocationCD);
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<INLocation, INLocation.projectID> e)
		{
			if (e.Row?.ProjectID != null)
				e.Cache.SetValueExt<INLocation.isCosted>(e.Row, true);
		}

		protected virtual void _(Events.FieldVerifying<INLocation, INLocation.projectID> e)
		{
			//TODO: Redo this using Plans and Status tables once we have them in version 7.0

			if (e.Row == null) return;

			PO.POReceiptLine unreleasedPO =
				SelectFrom<PO.POReceiptLine>.
				Where<
					PO.POReceiptLine.projectID.IsEqual<P.AsInt>.
					And<PO.POReceiptLine.released.IsEqual<False>>.
					And<PO.POReceiptLine.locationID.IsEqual<P.AsInt>>>.
				View.SelectWindowed(Base, 0, 1, e.Row.ProjectID, e.Row.LocationID);

			if (unreleasedPO != null)
			{
				PMProject project = SelectFrom<PMProject>.Where<PMProject.contractID.IsEqual<P.AsInt>>.View.Select(Base, e.Row.ProjectID ?? e.NewValue);
				if (project != null)
					e.NewValue = project.ContractCD;

				throw new PXSetPropertyException(IN.Messages.ProjectUsedInPO);
			}

			SO.SOShipLine unreleasedSO =
				SelectFrom<SO.SOShipLine>.
				Where<
					SO.SOShipLine.projectID.IsEqual<P.AsInt>.
					And<SO.SOShipLine.released.IsEqual<False>>.
					And<SO.SOShipLine.locationID.IsEqual<P.AsInt>>>.
				View.SelectWindowed(Base, 0, 1, e.Row.ProjectID, e.Row.LocationID);

			if (unreleasedSO != null)
			{
				PMProject project = SelectFrom<PMProject>.Where<PMProject.contractID.IsEqual<P.AsInt>>.View.Select(Base, e.Row.ProjectID ?? e.NewValue);
				if (project != null)
					e.NewValue = project.ContractCD;

				throw new PXSetPropertyException(IN.Messages.ProjectUsedInSO);
			}

			INLocationStatus locationStatus =
				SelectFrom<INLocationStatus>.
				Where<
					INLocationStatus.siteID.IsEqual<P.AsInt>.
					And<INLocationStatus.locationID.IsEqual<P.AsInt>>.
					And<INLocationStatus.qtyOnHand.IsNotEqual<decimal0>>>.
				View.SelectWindowed(Base, 0, 1, e.Row.SiteID, e.Row.LocationID);

			if (locationStatus != null)
			{
				PMProject project = SelectFrom<PMProject>.Where<PMProject.contractID.IsEqual<P.AsInt>>.View.Select(Base, e.Row.ProjectID ?? e.NewValue);
				if (project != null)
					e.NewValue = project.ContractCD;

				throw new PXSetPropertyException(IN.Messages.ProjectUsedInIN);
			}
		}

		protected virtual void _(Events.FieldVerifying<INLocation, INLocation.taskID> e)
		{
			//TODO: Redo this using Plans and Status tables once we have them in version 7.0

			if (e.Row == null) return;

			PO.POReceiptLine unreleasedPO =
				SelectFrom<PO.POReceiptLine>.
				Where<
					PO.POReceiptLine.taskID.IsEqual<P.AsInt>.
					And<PO.POReceiptLine.released.IsEqual<False>>.
					And<PO.POReceiptLine.locationID.IsEqual<P.AsInt>>>.
				View.SelectWindowed(Base, 0, 1, e.Row.TaskID, e.Row.LocationID);

			if (unreleasedPO != null)
			{
				PMTask task = PMTask.PK.Find(Base, e.Row.TaskID ?? (int?)e.NewValue);
				if (task != null)
					e.NewValue = task.TaskCD;

				throw new PXSetPropertyException(IN.Messages.TaskUsedInPO);
			}

			SO.SOShipLine unreleasedSO =
				SelectFrom<SO.SOShipLine>.
				Where<
					SO.SOShipLine.taskID.IsEqual<P.AsInt>.
					And<SO.SOShipLine.released.IsEqual<False>>.
					And<SO.SOShipLine.locationID.IsEqual<P.AsInt>>>.
				View.SelectWindowed(Base, 0, 1, e.Row.TaskID, e.Row.LocationID);

			if (unreleasedSO != null)
			{
				PMTask task = PMTask.PK.Find(Base, e.Row.TaskID ?? (int?)e.NewValue);
				if (task != null)
					e.NewValue = task.TaskCD;

				throw new PXSetPropertyException(IN.Messages.TaskUsedInSO);
			}

			INLocationStatus locationStatus =
				SelectFrom<INLocationStatus>.
				Where<
					INLocationStatus.siteID.IsEqual<P.AsInt>.
					And<INLocationStatus.locationID.IsEqual<P.AsInt>>.
					And<INLocationStatus.qtyOnHand.IsNotEqual<decimal0>>>.
				View.SelectWindowed(Base, 0, 1, e.Row.SiteID, e.Row.LocationID);

			if (locationStatus != null)
			{
				PMTask task = PMTask.PK.Find(Base, e.Row.TaskID ?? (int?)e.NewValue);
				if (task != null)
					e.NewValue = task.TaskCD;

				throw new PXSetPropertyException(IN.Messages.TaskUsedInIN);
			}
		}
	}
}
