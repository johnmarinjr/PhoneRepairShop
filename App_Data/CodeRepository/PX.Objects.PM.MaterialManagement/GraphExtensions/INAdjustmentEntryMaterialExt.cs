using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using System;
using System.Linq;

namespace PX.Objects.PM.MaterialManagement
{
    public class INAdjustmentEntryMaterialExt : CostCenterSupport<INAdjustmentEntry>
    {
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteStatusAccum> projectsitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMSiteSummaryStatusAccum> projectsummarysitestatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLocationStatusAccum> projectlocationstatus;
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatus> dummy; //To Prevent Cache-Inheritance-Clash in LSSelect  
		[PXCopyPasteHiddenView()]
		public PXSelect<PMLotSerialStatusAccum> projectlotserialstatus;

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[INTranSplitForProjectPlanID(typeof(INRegister.noteID), typeof(INRegister.hold), typeof(INRegister.transferType))]
		protected virtual void _(Events.CacheAttached<INTranSplit.planID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXVerifySelector(typeof(Search2<INCostStatus.receiptNbr,
			InnerJoin<INCostSubItemXRef, On<INCostSubItemXRef.costSubItemID, Equal<INCostStatus.costSubItemID>>,
			InnerJoin<INLocation, On<INLocation.locationID, Equal<Optional<INTran.locationID>>>>>,
			Where<INCostStatus.inventoryID, Equal<Optional<INTran.inventoryID>>,
			And<INCostSubItemXRef.subItemID, Equal<Optional<INTran.subItemID>>,
			And<
			Where2<Where<INCostStatus.costSiteID, Equal<Optional<INTran.siteID>>,
				And<Optional<INTran.costCenterID>, IsNull,
				And<INLocation.isCosted, Equal<False>,
				Or<INCostStatus.costSiteID, Equal<Optional<INTran.locationID>>>>>>,
			Or<INCostStatus.costSiteID, Equal<Optional<INTran.costCenterID>>>>				
				>>>>), VerifyField = false)]
		protected virtual void _(Events.CacheAttached<INTran.origRefNbr> e) { }
				
		protected virtual void _(Events.FieldUpdated<INTran, INTran.siteID> e)
        {
			SetCostCenter(e.Row);
		}
		protected virtual void _(Events.FieldUpdated<INTran, INTran.locationID> e)
		{
			SetCostCenter(e.Row);
		}
		protected virtual void _(Events.FieldUpdated<INTran, INTran.projectID> e)
		{
			SetCostCenter(e.Row);
		}
		protected virtual void _(Events.FieldUpdated<INTran, INTran.taskID> e)
		{
			SetCostCenter(e.Row);
		}
	}

	public class INAdjustmentEntrySplitMaterialExt : PXGraphExtension<INAdjustmentEntrySplit, INAdjustmentEntry>
    {
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();
		}
				
		[PXOverride]
		public virtual INTran SplitTransaction(INTran source,
			Func<INTran, INTran> baseMethod)
		{
			if (source.ProjectID == ProjectDefaultAttribute.NonProject() ||
				source.TaskID != null)
			{
				INTran newTran = baseMethod(source);
								
				if (source.Qty < 0)
				{
					IStatus availability = Base.FindImplementation<GraphExtensions.ItemAvailability.INAdjustmentItemAvailabilityProjectExtension>()
						.FetchWithBaseUOMProject(source);
					decimal overflow = source.Qty.GetValueOrDefault() + availability.QtyOnHand.GetValueOrDefault();
					if (overflow < 0)
					{
						source.Qty = -availability.QtyOnHand;
						Base.transactions.Update(source);

						newTran.Qty = overflow;
					}
				}

				return newTran;
			}

			return null;
		}

		protected virtual void _(Events.FieldVerifying<INTran, INTran.projectID> e)
        {
			if (!string.IsNullOrEmpty(e.Row.PIID) && 
				GetAccountingMode((int?)e.NewValue) == ProjectAccountingModes.ProjectSpecific &&
				e.ExternalCall == true)
            {
				var ex = new PXSetPropertyException(Messages.SpecificProjectNotSupported);
				PMProject project = PMProject.PK.Find(Base, (int?)e.NewValue);
				ex.ErrorValue = project.ContractCD;

				throw ex;
            }
        }

		protected virtual void _(Events.RowSelected<INTran> e)
        {
			if (e.Row != null && !string.IsNullOrEmpty(e.Row.PIID))
            {
				string mode = GetAccountingMode(e.Row.ProjectID);
				PXUIFieldAttribute.SetEnabled<INTran.projectID>(e.Cache, e.Row, mode != ProjectAccountingModes.ProjectSpecific);
				PXUIFieldAttribute.SetEnabled<INTran.taskID>(e.Cache, e.Row, mode != ProjectAccountingModes.ProjectSpecific);
			}
        }

		protected string GetAccountingMode(int? projectID)
		{
			if (projectID != null)
			{
				PMProject project = PMProject.PK.Find(Base, projectID);
				if (project != null && project.NonProject != true)
				{
					return project.AccountingMode;
				}
			}

			return ProjectAccountingModes.Valuated;
		}

		[PXOverride]
		public INTran InsertNewSplit(INTran newLine, Func<INTran, INTran> baseMethod)
        {
			INTran tran = baseMethod(newLine);
			if (GetAccountingMode(tran.ProjectID) != ProjectAccountingModes.ProjectSpecific)
			{
				tran.ProjectID = null;
				tran.TaskID = null;
			}

			return tran;
        }
	} 
}
