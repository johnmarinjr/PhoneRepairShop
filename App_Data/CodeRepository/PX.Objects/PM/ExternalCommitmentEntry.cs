using PX.Data;
using PX.Objects.CS;
using PX.Objects.IN;
using System;

namespace PX.Objects.PM
{
	[Serializable]
	public class ExternalCommitmentEntry : PXGraph<ExternalCommitmentEntry, PMCommitment>
	{
		#region DAC Attributes Override

		[PXDefault]
		[PXDBGuid(true)] //!!! IsKey=FALSE
		protected virtual void _(Events.CacheAttached<PMCommitment.commitmentID> e)
		{
		}

		[PXDBString(1)]
		[PXDefault(PMCommitmentType.External)]
		[PMCommitmentType.List()]
		[PXUIField(DisplayName = "Type", Enabled = false)]
		protected virtual void _(Events.CacheAttached<PMCommitment.type> e)
		{
		}

		[PXDefault]
		[PXDBString(15, IsUnicode = true, IsKey=true)]//!!! IsKey=TRUE
		[PXUIField(DisplayName = "External Ref. Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<PMCommitment.extRefNbr, Where<PMCommitment.type, Equal<PMCommitmentType.externalType>>>))]
		protected virtual void _(Events.CacheAttached<PMCommitment.extRefNbr> e)
		{
		}
				
		[PXDefault(typeof(Search<InventoryItem.baseUnit, Where<InventoryItem.inventoryID, Equal<Current<PMCommitment.inventoryID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PMUnit(typeof(PMCommitment.inventoryID))]
		protected virtual void _(Events.CacheAttached<PMCommitment.uOM> e)
		{
		}
				
		[PXFormula(typeof(Selector<PMCommitment.projectID, PMProject.curyID>))]
		[PXString(5, IsUnicode = true)]
		[PXSelector(typeof(CM.CurrencyList.curyID))]
		[PXUIField(DisplayName = "Project Currency", Enabled = false, FieldClass = nameof(CS.FeaturesSet.ProjectMultiCurrency))]
		protected virtual void _(Events.CacheAttached<PMCommitment.projectCuryID> e) { }

				
		#endregion

		[PXViewName(Messages.Commitments)]
		public PXSelect<PMCommitment, Where<PMCommitment.type, Equal<PMCommitmentType.externalType>>> Commitments;
				

		#region Event Handlers

		protected virtual void _(Events.FieldDefaulting<PMCommitment, PMCommitment.costCodeID> e)
		{
			if (!CostCodeAttribute.UseCostCode())
				e.NewValue = CostCodeAttribute.GetDefaultCostCode();
		}

		#endregion
	}

    public class ExternalCommitmentEntryExt : CommitmentTracking<ExternalCommitmentEntry>
	{
		public static new bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>();
		}
	}
}
