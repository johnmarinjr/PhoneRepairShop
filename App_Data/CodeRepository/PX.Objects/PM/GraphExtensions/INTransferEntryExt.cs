using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class INTransferEntryExt : PXGraphExtension<INTransferEntry> 
    {
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.projectModule>();
		}

		protected virtual void _(Events.RowSelected<INRegister> e)
		{
			if (e.Row != null)
			{
				PXUIFieldAttribute.SetVisible<INTran.toProjectID>(Base.transactions.Cache, null, e.Row.TransferType == INTransferType.OneStep);
				PXUIFieldAttribute.SetVisible<INTran.toTaskID>(Base.transactions.Cache, null, e.Row.TransferType == INTransferType.OneStep);
			}
		}

		protected virtual void _(Events.RowSelected<INTran> e)
        {
			if ( e.Row != null)
            {
				LinkedInfo info = GetLinkedInfo(e.Row.LocationID);
				PXUIFieldAttribute.SetEnabled<INTran.projectID>(e.Cache, e.Row, !info.IsLinked);
				PXUIFieldAttribute.SetEnabled<INTran.taskID>(e.Cache, e.Row, !info.IsTaskRestricted);
				LinkedInfo toInfo = GetLinkedInfo(e.Row.ToLocationID);
				PXUIFieldAttribute.SetEnabled<INTran.toProjectID>(e.Cache, e.Row, !toInfo.IsLinked);
				PXUIFieldAttribute.SetEnabled<INTran.toTaskID>(e.Cache, e.Row, !toInfo.IsTaskRestricted);
			}
        }

		protected virtual void _(Events.RowPersisting<INTran> e)
		{
			if (e.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				CheckSplitsForSameTask(e.Cache, e.Row);
			}
		}

		
		
		protected virtual void _(Events.FieldDefaulting<INTran, INTran.projectID> e)
		{
			INTran row = e.Row as INTran;
			if (row == null) return;

			if (row.LocationID != null)
			{
				INLocation location = INLocation.PK.Find(Base, row.LocationID);
				if (location.ProjectID != null)
				{
					e.NewValue = location.ProjectID;
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<INTran, INTran.projectID> e)
		{
			if (e.NewValue != null && e.NewValue is int?)
			{
				PMProject project = PMProject.PK.Find(Base, (int?)e.NewValue);
				if (project != null && project.NonProject != true && project.AccountingMode == ProjectAccountingModes.Linked)
				{
					if (e.Row.LocationID != null)
					{
						INLocation location = INLocation.PK.Find(Base, e.Row.LocationID);
						if (location != null)
						{
							if (location.ProjectID != project.ContractID &&
								(location.ProjectID != null || PXAccess.FeatureInstalled<FeaturesSet.materialManagement>()))
							{
								var ex = new PXSetPropertyException(Messages.LinkedProjectNotValid, PXErrorLevel.Error, project.ContractCD);
								ex.ErrorValue = project.ContractCD;

								throw ex;
							}
						}
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<INTran, INTran.taskID> e)
		{
			INTran row = e.Row as INTran;
			if (row == null) return;

			if (row.LocationID != null)
			{
				INLocation location = INLocation.PK.Find(Base, row.LocationID);
				if (location.TaskID != null)
				{
					e.NewValue = location.TaskID;
				}
				else if (location.ProjectID != null)
                {
					PMTask firstTask = PXSelect<PMTask, 
						Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
						And<PMTask.isActive, Equal<True>>>>.SelectWindowed(Base, 0, 1, location.ProjectID);
					if (firstTask != null)
                    {
						e.NewValue = firstTask.TaskID;
                    }
                }
			}
		}

		protected virtual void _(Events.FieldUpdated<INTran, INTran.locationID> e)
		{
			INTran row = e.Row as INTran;
			if (row == null) return;

			if (Base.CurrentDocument.Current != null)
			{
				e.Cache.SetDefaultExt<INTran.projectID>(e.Row); //will set pending value for TaskID to null if project is changed. This is the desired behavior for all other screens.
				if (e.Cache.GetValuePending<INTran.taskID>(e.Row) == null) //To redefault the TaskID in currecnt screen - set the Pending value from NULL to NOTSET
					e.Cache.SetValuePending<INTran.taskID>(e.Row, PXCache.NotSetValue);
				e.Cache.SetDefaultExt<INTran.taskID>(e.Row);
			}
		}

		protected virtual void _(Events.FieldDefaulting<INTran, INTran.toProjectID> e)
		{
			INTran row = e.Row as INTran;
			if (row == null) return;

			if (row.ToLocationID != null)
			{
				INLocation location = INLocation.PK.Find(Base, row.ToLocationID);
				if (location.ProjectID != null)
				{
					e.NewValue = location.ProjectID;
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<INTran, INTran.toProjectID> e)
		{
			if (e.NewValue != null && e.NewValue is int?)
			{
				PMProject project = PMProject.PK.Find(Base, (int?)e.NewValue);
				if (project != null && project.NonProject != true && project.AccountingMode == ProjectAccountingModes.Linked)
				{
					if (e.Row.ToLocationID != null)
					{
						INLocation location = INLocation.PK.Find(Base, e.Row.ToLocationID);
						if (location != null)
						{
							if (location.ProjectID != project.ContractID &&
								(location.ProjectID != null || PXAccess.FeatureInstalled<FeaturesSet.materialManagement>()))
							{
								var ex = new PXSetPropertyException(Messages.LinkedProjectNotValid, PXErrorLevel.Error, project.ContractCD);
								ex.ErrorValue = project.ContractCD;

								throw ex;
							}
						}
					}
				}
			}
		}

		protected virtual void _(Events.FieldDefaulting<INTran, INTran.toTaskID> e)
		{
			INTran row = e.Row as INTran;
			if (row == null) return;

			if (row.ToLocationID != null)
			{
				INLocation location = INLocation.PK.Find(Base, row.ToLocationID);
				if (location.TaskID != null)
				{
					e.NewValue = location.TaskID;
				}
				else if (location.ProjectID != null)
				{
					PMTask firstTask = PXSelect<PMTask,
						Where<PMTask.projectID, Equal<Required<PMTask.projectID>>,
						And<PMTask.isActive, Equal<True>>>>.SelectWindowed(Base, 0, 1, location.ProjectID);
					if (firstTask != null)
					{
						e.NewValue = firstTask.TaskID;
					}
				}
			}
		}

		protected virtual void _(Events.FieldUpdated<INTran, INTran.toLocationID> e)
		{
			INTran row = e.Row as INTran;
			if (row == null) return;

            if (Base.CurrentDocument.Current != null && !Base.IsImportFromExcel)
            {
                e.Cache.SetDefaultExt<INTran.toProjectID>(e.Row); //will set pending value for TaskID to null if project is changed. This is the desired behavior for all other screens.
                if (e.Cache.GetValuePending<INTran.toTaskID>(e.Row) == null) //To redefault the TaskID in currecnt screen - set the Pending value from NULL to NOTSET
                    e.Cache.SetValuePending<INTran.toTaskID>(e.Row, PXCache.NotSetValue);
                e.Cache.SetDefaultExt<INTran.toTaskID>(e.Row);
            }
        }

		protected virtual void _(Events.FieldDefaulting<INTranSplit, INTranSplit.projectID> e)
		{
			INTran parent = PXParentAttribute.SelectParent(e.Cache, e.Row, typeof(INTran)) as INTran;
			if (parent != null)
			{
				e.NewValue = parent.ProjectID;
			}
		}

		protected virtual void _(Events.FieldDefaulting<INTranSplit, INTranSplit.taskID> e)
		{
			INTran parent = PXParentAttribute.SelectParent(e.Cache, e.Row, typeof(INTran)) as INTran;
			if (parent != null)
			{
				e.NewValue = parent.TaskID;
			}
		}

		protected virtual bool CheckSplitsForSameTask(PXCache sender, INTran row)
		{
			if (row.HasMixedProjectTasks == true)
			{
				sender.RaiseExceptionHandling<INTran.locationID>(row, null, new PXSetPropertyException(IN.Messages.MixedProjectsInSplits));
				return false;
			}

			return true;
		}

		private LinkedInfo GetLinkedInfo(int? locationID)
		{
			LinkedInfo result = new LinkedInfo();
			if (locationID != null)
			{
				INLocation location = INLocation.PK.Find(Base, locationID);
				if (location != null && location.ProjectID != null)
				{
					PMProject project = PMProject.PK.Find(Base, location.ProjectID);
					if (project != null)
					{
						result.IsLinked = project.AccountingMode == ProjectAccountingModes.Linked;
						result.IsTaskRestricted = location.TaskID != null;
					}
				}
			}

			return result;
		}

		private struct LinkedInfo
		{
			public bool IsLinked;
			public bool IsTaskRestricted;
		}
	}
}
