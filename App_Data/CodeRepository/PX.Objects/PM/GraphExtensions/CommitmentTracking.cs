using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.CM.TemporaryHelpers;
using PX.Objects.CS;
using System;

namespace PX.Objects.PM
{
	public class CommitmentTracking<T> : PXGraphExtension<T> where T : PXGraph
    {
		public PXSelect<PMBudgetAccum> Budget;
		public PXSelect<PMCommitment> InternalCommitments;
		
		[InjectDependency]
		public IBudgetService BudgetService { get; set; }

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.projectAccounting>();
		}

		protected virtual void _(Events.RowInserted<PMCommitment> e)
		{
			RollUpCommitmentBalance(e.Row, 1);
		}

		protected virtual void _(Events.RowUpdated<PMCommitment> e)
		{
			RollUpCommitmentBalance(e.OldRow, -1);
			RollUpCommitmentBalance(e.Row, 1);
		}

		protected virtual void _(Events.RowDeleted<PMCommitment> e)
		{
			RollUpCommitmentBalance(e.Row, -1);
			ClearEmptyRecords();
		}

		public virtual void RollUpCommitmentBalance(PMCommitment row, int sign)
		{
			if (row == null)
				throw new ArgumentNullException();

			if (row.ProjectID == null || row.ProjectTaskID == null || row.AccountGroupID == null)
				return;

			PMAccountGroup ag = PXSelectorAttribute.Select<PMCommitment.accountGroupID>(Base.Caches[typeof(PMCommitment)], row) as PMAccountGroup;
			PMProject project = PMProject.PK.Find(Base, row.ProjectID);

			Lite.PMBudget target = BudgetService.SelectProjectBalance(row, ag, project, out bool isExisting);

			ProjectBalance pb = CreateProjectBalance();
			var rollupOrigQty = pb.CalculateRollupQty(row, target, row.OrigQty);
			var rollupQty = pb.CalculateRollupQty(row, target, row.Qty);
			var rollupOpenQty = pb.CalculateRollupQty(row, target, row.OpenQty);
			var rollupReceivedQty = pb.CalculateRollupQty(row, target, row.ReceivedQty);
			var rollupInvoicedQty = pb.CalculateRollupQty(row, target, row.InvoicedQty);

			PMBudgetAccum ps = Budget.Insert(new PMBudgetAccum
			{
				ProjectID = target.ProjectID,
				ProjectTaskID = target.ProjectTaskID,
				AccountGroupID = target.AccountGroupID,
				Type = target.Type,
				InventoryID = target.InventoryID,
				CostCodeID = target.CostCodeID,
				UOM = target.UOM,
				IsProduction = target.IsProduction,
				Description = target.Description,
				CuryInfoID = project.CuryInfoID
			});

			ps.CommittedOrigQty += sign * rollupOrigQty;
			ps.CommittedQty += sign * rollupQty;
			ps.CommittedOpenQty += sign * rollupOpenQty;
			ps.CommittedReceivedQty += sign * rollupReceivedQty;
			ps.CommittedInvoicedQty += sign * rollupInvoicedQty;
			ps.CuryCommittedOrigAmount += sign * row.OrigAmount.GetValueOrDefault();
			ps.CuryCommittedAmount += sign * row.Amount.GetValueOrDefault();
			ps.CuryCommittedOpenAmount += sign * row.OpenAmount.GetValueOrDefault();
			ps.CuryCommittedInvoicedAmount += sign * row.InvoicedAmount.GetValueOrDefault();

			if (project.CuryID != project.BaseCuryID)
			{
				CurrencyInfo currencyInfo = MultiCurrencyCalculator.GetCurrencyInfo<PMProject.curyInfoID>(Base, project);

				ps.CommittedOrigAmount += sign * currencyInfo.CuryConvBase(row.OrigAmount.GetValueOrDefault());
				ps.CommittedAmount += sign * currencyInfo.CuryConvBase(row.Amount.GetValueOrDefault());
				ps.CommittedOpenAmount += sign * currencyInfo.CuryConvBase(row.OpenAmount.GetValueOrDefault());
				ps.CommittedInvoicedAmount += sign * currencyInfo.CuryConvBase(row.InvoicedAmount.GetValueOrDefault());
			}
			else
			{
				ps.CommittedOrigAmount += sign * row.OrigAmount.GetValueOrDefault();
				ps.CommittedAmount += sign * row.Amount.GetValueOrDefault();
				ps.CommittedOpenAmount += sign * row.OpenAmount.GetValueOrDefault();
				ps.CommittedInvoicedAmount += sign * row.InvoicedAmount.GetValueOrDefault();
			}
		}

		public virtual ProjectBalance CreateProjectBalance()
		{
			return new ProjectBalance(this.Base);
		}

		protected virtual void ClearEmptyRecords()
		{
			foreach (PMBudgetAccum record in Budget.Cache.Inserted)
			{
				if (IsEmptyCommitmentChange(record))
					Budget.Cache.Remove(record);
			}
		}

		private bool IsEmptyCommitmentChange(PMBudgetAccum item)
		{
			if (item.CommittedOrigQty.GetValueOrDefault() != 0) return false;
			if (item.CommittedQty.GetValueOrDefault() != 0) return false;
			if (item.CommittedOpenQty.GetValueOrDefault() != 0) return false;
			if (item.CommittedReceivedQty.GetValueOrDefault() != 0) return false;
			if (item.CommittedInvoicedQty.GetValueOrDefault() != 0) return false;
			if (item.CuryCommittedOrigAmount.GetValueOrDefault() != 0) return false;
			if (item.CuryCommittedAmount.GetValueOrDefault() != 0) return false;
			if (item.CuryCommittedOpenAmount.GetValueOrDefault() != 0) return false;
			if (item.CuryCommittedInvoicedAmount.GetValueOrDefault() != 0) return false;
			if (item.CommittedOrigAmount.GetValueOrDefault() != 0) return false;
			if (item.CommittedAmount.GetValueOrDefault() != 0) return false;
			if (item.CommittedOpenAmount.GetValueOrDefault() != 0) return false;
			if (item.CommittedInvoicedAmount.GetValueOrDefault() != 0) return false;

			return true;
		}
	}
}
