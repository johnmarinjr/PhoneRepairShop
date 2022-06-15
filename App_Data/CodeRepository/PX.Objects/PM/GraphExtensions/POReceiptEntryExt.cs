using PX.Common;
using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.PO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PM
{
    public class POReceiptEntryExt : CommitmentTracking<POReceiptEntry>
    {
		public PXSetup<PMSetup> Setup;

		public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.projectModule>();
        }

		protected virtual void _(Events.FieldDefaulting<POReceiptLine, POReceiptLine.projectID> e)
		{
			POReceiptLine row = e.Row as POReceiptLine;
			if (row == null) return;
			if (PM.ProjectAttribute.IsPMVisible(GL.BatchModule.PO)) 
			{
				if (Base.apsetup.Current.RequireSingleProjectPerDocument == true && Base.Document.Current != null)
				{
					e.NewValue = Base.Document.Current.ProjectID;
					e.Cancel = true;
                    return;
                }

				if (IsDefaultFromLocation(row))
				{
					INLocation location = INLocation.PK.Find(Base, row.LocationID);
					if (location != null && location.ProjectID != null)
                    {
						e.NewValue = location.ProjectID;

					}
				}
				else
				{
					POLine line = POLine.PK.Find(Base, row.POType, row.PONbr, row.POLineNbr);
					if (line != null && line.ProjectID != null)
					{
						e.NewValue = line.ProjectID;
					}
				}
			}
		}

		private bool IsDefaultFromLocation(POReceiptLine row)
        {
			if (!IsStockItem(row))
				return false;

			if (row.ProjectID == null || ProjectDefaultAttribute.IsNonProject(row.ProjectID))
				return true;

			PMProject project = PMProject.PK.Find(Base, row.ProjectID);
			if (project != null && project.AccountingMode == ProjectAccountingModes.Linked && row.LocationID != null)
			{
				return true;
			}

			return false;
		}

		protected virtual void _(Events.RowSelected<POReceiptLine> e)
        {
			POReceiptLine row = e.Row as POReceiptLine;
			if (row == null) return;

			bool fromPO = !(string.IsNullOrEmpty(row.POType) || string.IsNullOrEmpty(row.PONbr) || row.POLineNbr == null);

			if (row.Released != true)
			{
				bool isPMVisible = ProjectAttribute.IsPMVisible(GL.BatchModule.PO);
				bool requireSingleProject = (Base.apsetup.Current.RequireSingleProjectPerDocument == true);
				bool linkedLocation = false;
				if (IsStockItem(e.Row)) 
				{
					INLocation location = INLocation.PK.Find(Base, row.LocationID);
					if (location != null)
					{
						linkedLocation = location.ProjectID != null;
					}
				}
				
				PXUIFieldAttribute.SetEnabled<POReceiptLine.projectID>(e.Cache, row, isPMVisible && !requireSingleProject && !fromPO && !linkedLocation);
				PXUIFieldAttribute.SetEnabled<POReceiptLine.taskID>(e.Cache, row, isPMVisible && !linkedLocation);
			}
		}

		protected virtual void _(Events.FieldDefaulting<POReceiptLine, POReceiptLine.taskID> e)
		{
			POReceiptLine row = e.Row as POReceiptLine;
			if (row == null) return;
			if (PM.ProjectAttribute.IsPMVisible(GL.BatchModule.PO) && row.SiteID != null && row.ProjectID != null)
			{
				PMProject project = PMProject.PK.Find(Base, row.ProjectID);
				if (IsStockItem(row) && project != null && project.NonProject != null )
				{
					if (project.AccountingMode == ProjectAccountingModes.Linked)
					{
						if (row.LocationID != null && ProjectDefaultAttribute.IsProject(Base, row.ProjectID))
						{
							PXResultset<PMTask> tasks = PXSelectJoin<PMTask,
										LeftJoin<INLocation, On<PMTask.projectID, Equal<INLocation.projectID>, And<INLocation.active, Equal<True>>>>,
										Where<PMTask.projectID, Equal<Required<PMTask.projectID>>, And<PMTask.visibleInPO, Equal<True>>>>.Select(Base, row.ProjectID);

							HashSet<int> validTasks = new HashSet<int>();
							HashSet<int> tasksForLocation = new HashSet<int>();
							foreach (PXResult<PMTask, INLocation> res in tasks)
							{
								PMTask task = (PMTask)res;
								INLocation location = (INLocation)res;

								validTasks.Add(task.TaskID.Value);

								if (task.TaskID == location.TaskID)
								{
									tasksForLocation.Add(task.TaskID.Value);
								}
							}

							POLine poLine = PXSelectReadonly<POLine, Where<POLine.orderType, Equal<Required<POLine.orderType>>,
									And<POLine.orderNbr, Equal<Required<POLine.orderNbr>>,
									And<POLine.lineNbr, Equal<Required<POLine.lineNbr>>>>>>.Select(Base, row.POType, row.PONbr, row.POLineNbr);

							if (poLine != null && poLine.TaskID != null)
							{
								if (tasksForLocation.Contains(poLine.TaskID.Value))
								{
									e.NewValue = poLine.TaskID;
									return;
								}
							}

							if (tasksForLocation.Count > 0)
							{
								e.NewValue = tasksForLocation.First();
								return;
							}

							if (validTasks.Count > 0)
							{
								e.NewValue = validTasks.First();
								return;
							}
						}
					}
					else
					{
						POLine line = POLine.PK.Find(Base, row.POType, row.PONbr, row.POLineNbr);
						if (line != null && line.TaskID != null)
						{
							e.NewValue = line.TaskID;
						}
					}
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<POReceiptLine, POReceiptLine.taskID> e)
		{
			POReceiptLine row = e.Row as POReceiptLine;
			if (row == null) return;
			if (!(e.NewValue is Int32)) return;

			CheckOrderTaskRule(e.Cache, row, (int?)e.NewValue);
		}

		protected virtual void _(Events.FieldVerifying<POReceiptLine, POReceiptLine.locationID> e)
		{
			POReceiptLine row = e.Row as POReceiptLine;
			if (row == null) return;

			CheckLocationTaskRule(e.Cache, row, e.NewValue);
		}

		protected virtual void _(Events.FieldUpdated<POReceiptLine, POReceiptLine.locationID> e)
		{
			if (IsStockItem(e.Row))
			{
				e.Cache.SetDefaultExt<POReceiptLine.projectID>(e.Row); //will set pending value for TaskID to null if project is changed. This is the desired behavior for all other screens.
				if (e.Cache.GetValuePending<POReceiptLine.taskID>(e.Row) == null) //To redefault the TaskID in currecnt screen - set the Pending value from NULL to NOTSET
					e.Cache.SetValuePending<POReceiptLine.taskID>(e.Row, PXCache.NotSetValue);
				e.Cache.SetDefaultExt<POReceiptLine.taskID>(e.Row);
			}
		}

		protected virtual void _(Events.RowPersisting<POReceiptLine> e)
        {
			if (e.Operation.Command() == PXDBOperation.Delete)
				return;

			CheckForSingleLocation(e.Cache, e.Row);
			CheckSplitsForSameTask(e.Cache, e.Row);
			CheckLocationTaskRule(e.Cache, e.Row, e.Row.LocationID);
			CheckOrderTaskRule(e.Cache, e.Row, e.Row.TaskID);
		}

		[PXOverride]
		public virtual void Copy(POReceiptLine aDest, POLine aSrc, decimal aQtyAdj, decimal aBaseQtyAdj, Action<POReceiptLine, POLine, decimal, decimal> baseMethod)
		{
			baseMethod(aDest, aSrc, aQtyAdj, aBaseQtyAdj);

			if (aDest.IsStockItem() && aSrc.TaskID != null && !POLineType.IsProjectDropShip(aDest.LineType))
			{
				DeriveLocationFromSourceForStockItem(aDest, aSrc);
			}
		}

		protected virtual void DeriveLocationFromSourceForStockItem(POReceiptLine target, POLine source)
        {
			PMProject project = PMProject.PK.Find(Base, target.ProjectID);

			if (project?.AccountingMode == ProjectAccountingModes.Linked && project.NonProject != true)
            {
				DeriveLocationFromSource(target, source);
			}
			else
			{
				INItemSite itemSite = INItemSite.PK.Find(Base, target.InventoryID, target.SiteID);
				if (itemSite != null)
				{
					target.LocationID = itemSite.DfltReceiptLocationID;
				}
			}
		}

		protected virtual void CheckLocationTaskRule(PXCache sender, POReceiptLine row, object newLocationID)
		{
			if (newLocationID != null && POLineType.IsStock(row.LineType) && row.LineType != POLineType.GoodsForDropShip
				&& row.SiteID != null && row.ProjectID != null && !POLineType.IsProjectDropShip(row.LineType))
			{
				PMProject project = PMProject.PK.Find(Base, row.ProjectID);
				if (project != null && project.NonProject != true)
				{
					INLocation selLocation = INLocation.PK.Find(Base, (int?)newLocationID);
					if (project.AccountingMode == ProjectAccountingModes.Linked)
					{						
						int? selLocationProject = selLocation?.ProjectID ?? ProjectDefaultAttribute.NonProject();
						if ((Base.apsetup.Current.RequireSingleProjectPerDocument == true)
							&& selLocation != null && !row.ProjectID.IsIn(null, selLocationProject))
						{
							var ex = new PXSetPropertyException(PO.Messages.LocationNotAssignedToProject, PXErrorLevel.Error, sender.GetValueExt<POReceiptLine.projectID>(row));
							ex.ErrorValue = selLocation.LocationCD;
							throw ex;

						}
						else if (row.POType != null && row.PONbr != null && row.POLineNbr != null)
						{
							POLine poLine = PXSelectReadonly<POLine, Where<POLine.orderType, Equal<Required<POLine.orderType>>,
											And<POLine.orderNbr, Equal<Required<POLine.orderNbr>>,
											And<POLine.lineNbr, Equal<Required<POLine.lineNbr>>>>>>.Select(Base, row.POType, row.PONbr, row.POLineNbr);

							if (poLine != null && poLine.TaskID != null &&
								selLocation != null && (selLocation.ProjectID != poLine.ProjectID || (selLocation.TaskID != poLine.TaskID && selLocation.TaskID != null)))
							{
								if (Base.posetup.Current.OrderRequestApproval == true)
								{
									PMProject rowProject = PMProject.PK.Find(Base, row.ProjectID);
									PMTask rowTask = PMTask.PK.Find(Base, row.TaskID);
									var ex = new PXSetPropertyException(PO.Messages.LocationIsNotMappedToTask, PXErrorLevel.Error, rowProject?.ContractCD, rowTask?.TaskCD);
									ex.ErrorValue = selLocation.LocationCD;
									throw ex;
								}
								else
								{
									sender.RaiseExceptionHandling<POReceiptLine.locationID>(row, selLocation.LocationCD,
										new PXSetPropertyException(PO.Messages.LocationIsNotMappedToTask, PXErrorLevel.Warning));
								}
							}
						}
					}
					else
					{
						if (selLocation.ProjectID != null && selLocation.ProjectID != row.ProjectID)
						{
							PMProject locProject = PMProject.PK.Find(Base, selLocation.ProjectID);
							PMProject rowProject = PMProject.PK.Find(Base, row.ProjectID);
							var ex = new PXSetPropertyException(PO.Messages.LocationIsMappedToOtherProject, PXErrorLevel.Error, selLocation.LocationCD, locProject?.ContractCD, rowProject?.ContractCD);
							ex.ErrorValue = selLocation.LocationCD;
							throw ex;
						}
						
					}
				}
			}
		}

		

		protected virtual void CheckOrderTaskRule(PXCache sender, POReceiptLine row, int? newTaskID)
		{
			if (row.POType != null && row.PONbr != null && row.POLineNbr != null && (!POLineType.IsStock(row.LineType) || row.LineType.IsIn(POLineType.GoodsForDropShip, POLineType.GoodsForProject)))
			{
				POLine poLine = PXSelectReadonly<POLine, Where<POLine.orderType, Equal<Required<POLine.orderType>>,
								And<POLine.orderNbr, Equal<Required<POLine.orderNbr>>,
								And<POLine.lineNbr, Equal<Required<POLine.lineNbr>>>>>>.Select(Base, row.POType, row.PONbr, row.POLineNbr);

				if (poLine != null && poLine.TaskID != null && poLine.TaskID != newTaskID)
				{
					PMTask task = PMTask.PK.Find(Base, row.TaskID);
					string taskCd = task != null ? taskCd = task.TaskCD : null;

					if (Base.posetup.Current.OrderRequestApproval == true)
					{
						var ex = new PXSetPropertyException(PO.Messages.TaskDiffersError, PXErrorLevel.Error);
						ex.ErrorValue = taskCd;
						throw ex;
					}
					else
					{
						sender.RaiseExceptionHandling<POReceiptLine.taskID>(row, taskCd,
							new PXSetPropertyException(PO.Messages.TaskDiffersWarning, PXErrorLevel.Warning));
					}
				}
			}
		}

		protected virtual bool CheckForSingleLocation(PXCache sender, POReceiptLine row)
		{
			if (POLineType.IsStock(row.LineType) && row.LineType != POLineType.GoodsForDropShip && !POLineType.IsProjectDropShip(row.LineType) && row.TaskID != null && row.LocationID == null && row.BaseReceiptQty > 0m)
			{
				sender.RaiseExceptionHandling<POReceiptLine.locationID>(row, null, new PXSetPropertyException(IN.Messages.RequireSingleLocation));
				return false;
			}

			return true;
		}

		protected virtual bool CheckSplitsForSameTask(PXCache sender, POReceiptLine row)
		{
			if (POLineType.IsStock(row.LineType))
			{
				if (row.HasMixedProjectTasks == true)
				{
					sender.RaiseExceptionHandling<POReceiptLine.locationID>(row, null, new PXSetPropertyException(IN.Messages.MixedProjectsInSplits));
					return false;
				}

			}

			return true;
		}

		private void DeriveLocationFromSource(POReceiptLine target, POLine source)
		{
			//try no derive Location from Task.
			PXResultset<INLocation> locations = PXSelectReadonly<INLocation, Where<INLocation.siteID, Equal<Required<INLocation.siteID>>,
				And<INLocation.projectID, Equal<Required<INLocation.projectID>>,
					And<INLocation.taskID, Equal<Required<INLocation.taskID>>,
					And<INLocation.active, Equal<True>>>>>>.Select(Base, source.SiteID, source.ProjectID, source.TaskID);

			if (locations.Count == 0)
			{
				INLocation wildcardLocation = PXSelectReadonly<INLocation, Where<INLocation.siteID, Equal<Required<INLocation.siteID>>,
				And<INLocation.projectID, Equal<Required<INLocation.projectID>>,
				And<INLocation.taskID, IsNull, And<INLocation.active, Equal<True>>>>>>.Select(Base, source.SiteID, source.ProjectID);

				if (wildcardLocation != null)
				{
					target.LocationID = wildcardLocation.LocationID;
				}
				else
				{
					target.LocationID = null;
					target.ProjectID = null;
					target.TaskID = null;
				}
			}
			else if (locations.Count == 1)
			{
				target.LocationID = ((INLocation)locations[0]).LocationID;
			}
			else
			{
				target.LocationID = null;
				target.ProjectID = null;
				target.TaskID = null;
			}
		}

		private bool IsStockItem(POReceiptLine row)
		{
			//Note: POReceiptLine.IsStockItem includes the same conditions except for GoodsForDropShip
			return row?.LineType != null && (row.LineType == POLineType.GoodsForInventory ||
											 row.LineType == POLineType.GoodsForSalesOrder ||
											 row.LineType == POLineType.GoodsForServiceOrder ||
											 row.LineType == POLineType.GoodsForReplenishment ||
											 row.LineType == POLineType.GoodsForManufacturing ||
											 row.LineType == POLineType.GoodsForDropShip);
		}

		private bool IsRequired(string poLineType)
		{
			switch (poLineType)
			{
				case PX.Objects.PO.POLineType.NonStock:
				case PX.Objects.PO.POLineType.Freight:
				case PX.Objects.PO.POLineType.Service:
					return true;

				default:
					return false;
			}
		}

		public PXAction<POReceipt> createAPDocument;
		[PXUIField(DisplayName = PO.Messages.CreateAPInvoice, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXProcessButton]
		public virtual System.Collections.IEnumerable CreateAPDocument(PXAdapter adapter)
		{
			if (this.Base.Document.Current != null &&
				this.Base.Document.Current.Released == true)
			{
				POReceipt doc = this.Base.Document.Current;
				if (doc.UnbilledQty != Decimal.Zero)
				{
					ValidateLines();
				}
			}
			return Base.createAPDocument.Press(adapter);
		}

		public virtual void ValidateLines()
		{
			bool validationFailed = false;
			foreach (POReceiptLine line in this.Base.transactions.Select())
			{
				if (line.TaskID != null)
				{
					PMProject project = PXSelectorAttribute.Select<POLine.projectID>(this.Base.transactions.Cache, line) as PMProject;
					if (project.IsActive != true)
					{
						PXUIFieldAttribute.SetError<POLine.projectID>(this.Base.transactions.Cache, line, PO.Messages.ProjectIsNotActive, project.ContractCD);
						validationFailed = true;
					}
					else
					{
						PMTask task = PXSelectorAttribute.Select<POLine.taskID>(this.Base.transactions.Cache, line) as PMTask;
						if (task.IsActive != true)
						{
							PXUIFieldAttribute.SetError<POLine.taskID>(this.Base.transactions.Cache, line, PO.Messages.ProjectTaskIsNotActive, task.TaskCD);
							validationFailed = true;
						}
					}
				}
			}

			if (validationFailed)
			{
				throw new PXException(PO.Messages.LineIsInvalid);
			}
		}
	}
}
