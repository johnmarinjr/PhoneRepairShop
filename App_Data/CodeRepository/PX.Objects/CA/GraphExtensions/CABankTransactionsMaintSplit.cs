using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.CA
{
	public class CABankTransactionsMaintSplit : PXGraphExtension<CABankTransactionsMaint>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.bankTransactionSplits>();
		}

		public override void Initialize()
		{
			base.Initialize();
			Base.Details.Cache.AllowUpdate = true;
			Base.Details.AllowUpdate = true;

			Base.Details.Cache.AllowDelete = true;
			Base.Details.AllowDelete = true;
		}

		#region Action
		public delegate IEnumerable HideDelegate(PXAdapter adapter);

		[PXOverride]
		public virtual IEnumerable Hide(PXAdapter adapter, HideDelegate baseMethod)
		{
			CABankTran detail = Base.Details.Current;
			CABankTranSplit detExt = detail.GetExtension<CABankTranSplit>();
			if (detExt?.ParentTranID != null || detExt.Splitted == true)
			{
				throw new PXException(Messages.TransactionCannotBeHiddenBecauseItHasBeenSplit);
			}

			return baseMethod(adapter);
		}

		public PXAction<CABankTranHeader> split;

		[PXUIField(DisplayName = "Split", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable Split(PXAdapter adapter)
		{
			CABankTran parent = null;
			CABankTranSplit currentExt = Base.Details.Current.GetExtension<CABankTranSplit>();

			if (currentExt != null)
			{
				if (currentExt.ParentTranID != null)
				{
					parent = Base.Details.Locate(CABankTran.PK.Find(Base, currentExt.ParentTranID));
				}
				else
				{
					parent = Base.Details.Current;
				}

				var splitRow = SplitTransaction(Base.Details.Cache, parent);
				Base.Details.View.RequestRefresh();
			}

			return adapter.Get();
		}

		protected internal CABankTran SplitTransaction(PXCache cache, CABankTran originalTran)
		{
			if (originalTran.DocumentMatched == true)
			{
				throw new PXException(Messages.TransactionCannotBeSplitBecauseItHasBeenMatched);
			}

			using (PXTransactionScope ts = new PXTransactionScope())
			{
				int? parentKey = originalTran.TranID;

				CABankTran splitTran = (CABankTran)cache.CreateCopy(originalTran);
				splitTran.TranID = null;
				splitTran.CuryTranAmt = 0m;
				splitTran.NoteID = null;

				CABankTranSplit splitTranExt = splitTran.GetExtension<CABankTranSplit>();
				splitTranExt.ParentTranID = parentKey;
				splitTranExt.Splitted = false;
				splitTranExt.CuryOrigTranAmt = 0m;
				splitTranExt.OrigDrCr = null;
				splitTranExt.ChildsCount = null;
				splitTranExt.UnmatchedChilds = null;
				splitTranExt.UnprocessedChilds = null;
				splitTran = (CABankTran)cache.Insert(splitTran);

				var copyOriginalTran = (CABankTran)cache.CreateCopy(originalTran);
				CABankTranSplit origTranExt = copyOriginalTran.GetExtension<CABankTranSplit>();
				if (origTranExt.Splitted != true)
				{
					origTranExt.CuryOrigTranAmt = originalTran.CuryTranAmt;
					origTranExt.OrigDrCr = originalTran.DrCr;
					origTranExt.Splitted = true;
				}

				origTranExt.ChildsCount = (origTranExt.ChildsCount ?? 0) + 1;
				origTranExt.UnmatchedChilds = (origTranExt.UnmatchedChilds ?? 0) + 1;
				origTranExt.UnprocessedChilds = (origTranExt.UnprocessedChilds ?? 0) + 1;

				copyOriginalTran = (CABankTran)cache.Update(copyOriginalTran);

				ts.Complete();

				return splitTran;
			}
		}

		[PXOverride]
		public virtual void ValidateBeforeProcessing(CABankTran det)
		{
			CABankTranSplit detExt = det.GetExtension<CABankTranSplit>();

			if (detExt?.ParentTranID != null || detExt.Splitted == true)
			{
				int? parentId = detExt.Splitted == true ? det.TranID : detExt.ParentTranID;
				foreach (CABankTran child in SelectFrom<CABankTran>
						  .Where<CABankTran.documentMatched.IsEqual<False>.And<Brackets<CABankTranSplit.parentTranID.IsEqual<P.AsInt>
							.Or<CABankTran.tranID.IsEqual<P.AsInt>>>>>.View
						  .Select(Base, new object[] { parentId, parentId }))
				{
					throw new PXSetPropertyException(Messages.TransactionCanBeProcessedBecauseItHasRelatedUnmatchedTransactions);
				}
			}
		}
		#endregion

		[PXOverride]
		public virtual IEnumerable<CABankTran> GetUnprocessedTransactions()
		{
			CABankTransactionsMaint.Filter current = Base.TranFilter.Current;
			if (current == null || current.CashAccountID == null) yield break;

			int order = 0;
			foreach (CABankTran det in PXSelect<
				CABankTran,
				Where<CABankTranSplit.parentTranID, IsNull,
					And<CABankTran.cashAccountID, Equal<Required<CABankTran.cashAccountID>>,
					And<CABankTran.tranType, Equal<Required<CABankTran.tranType>>,
					And<Where<CABankTran.processed, Equal<False>,
						Or<Where<CABankTranSplit.unprocessedChilds, Greater<Zero>,
							And<CABankTranSplit.unprocessedChilds, IsNotNull>>>>>>>>,
				OrderBy<Asc<CABankTran.tranID>>>
				.Select(Base, current.CashAccountID, current.TranType)
				.RowCast<CABankTran>())
			{
				det.SortOrder = order++;
				CABankTranSplit detExt = det.GetExtension<CABankTranSplit>();
				if (detExt?.Splitted == true)
				{
					foreach (CABankTran child in PXSelect<
					CABankTran,
					Where<CABankTran.cashAccountID, Equal<Required<CABankTran.cashAccountID>>,
						And<CABankTran.tranType, Equal<Required<CABankTran.tranType>>,
						And<CABankTranSplit.parentTranID, Equal<Required<CABankTran.tranID>>>>>,
					OrderBy<Asc<CABankTran.sortOrder>>>
					.Select(Base, current.CashAccountID, current.TranType, det.TranID)
					.RowCast<CABankTran>())
					{
						child.SortOrder = order++;

						yield return child;
					}
				}

				yield return det;
			}
		}

		#region Events
		public virtual void _(Events.RowSelected<CABankTran> e)
		{
			var row = (CABankTran)e.Row;

			CABankTranSplit currentExt = row?.GetExtension<CABankTranSplit>();
			bool isChild = currentExt?.ParentTranID != null;
			if (isChild)
			{
				bool isEnable = true;
				if (row.DocumentMatched == true)
				{
					isEnable = false;
				}
				else
				{
					CABankTran parentRow = CABankTran.PK.Find(Base, currentExt.ParentTranID);
					if (parentRow?.DocumentMatched == true)
					{
						isEnable = false;
					}
				}

				var status = Base.Details.Cache.GetStatus(Base.Details.Current);
				bool isNew = (status == PXEntryStatus.Inserted || status == PXEntryStatus.InsertedDeleted);
				if(isNew)
				{
					PXUIFieldAttribute.SetEnabled(e.Cache, row, false);
					PXUIFieldAttribute.SetWarning<CABankTranSplit.splittedIcon>(e.Cache, row, Messages.ChildTransactionNeedsToBeSavedBeforeMatching);
				}

				PXUIFieldAttribute.SetEnabled<CABankTran.curyCreditAmt>(e.Cache, row, isChild && isEnable);
				PXUIFieldAttribute.SetEnabled<CABankTran.curyDebitAmt>(e.Cache, row, isChild && isEnable);
				PXUIFieldAttribute.SetVisible<CABankTranSplit.splittedIcon>(e.Cache, null, isChild);
				PXUIFieldAttribute.SetVisibility<CABankTranSplit.curyOrigCreditAmt>(e.Cache, null, PXUIVisibility.Visible);
				PXUIFieldAttribute.SetVisibility<CABankTranSplit.curyOrigDebitAmt>(e.Cache, null, PXUIVisibility.Visible);
			}

			
			if (row?.Processed == true)
			{
				PXUIFieldAttribute.SetEnabled(e.Cache, row, false);
			}
		}

		public virtual void _(Events.RowUpdating<CABankTran> e)
		{
			CABankTran row = (CABankTran)e.NewRow;

			if (row?.CreateDocument == true)
			{
				bool valid = CABankTransactionsMaint.ValidateTranFields(Base, e.Cache, row, Base.Adjustments);
				Base.Details.Cache.SetValueExt<CABankTran.documentMatched>(row, valid);
			}
		}

		[PXOverride]
		public virtual void CABankTran_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			CABankTran row = (CABankTran)e.Row;
			CABankTran oldRow = (CABankTran)e.OldRow;

			CABankTranSplit rowExt = row.GetExtension<CABankTranSplit>();
			if (rowExt?.ParentTranID == null)
				return;

			if (oldRow?.CuryTranAmt != row?.CuryTranAmt
				|| oldRow?.DocumentMatched != row?.DocumentMatched
				|| oldRow?.Processed != row?.Processed)
			{
				CABankTran parentRow = (CABankTran)sender.Locate(new CABankTran { TranID = rowExt.ParentTranID }) ?? CABankTran.PK.Find(Base, rowExt.ParentTranID);
				CABankTran copyParentRow = (CABankTran)sender.CreateCopy(parentRow);
				if(copyParentRow== null)
					throw new PXSetPropertyException(Messages.DocNotFound);

				CABankTranSplit parentRowExt = copyParentRow.GetExtension<CABankTranSplit>();

				if (oldRow?.CuryTranAmt != row?.CuryTranAmt)
				{
					decimal? newTranAmout = copyParentRow.CuryTranAmt + (oldRow?.CuryTranAmt ?? 0m) - (row?.CuryTranAmt ?? 0m);
					copyParentRow = (CABankTran)sender.CreateCopy(copyParentRow);
					if (newTranAmout > 0m)
					{
						copyParentRow.CuryDebitAmt = newTranAmout;
					}
					else if (newTranAmout < 0m)
					{
						copyParentRow.CuryCreditAmt = -newTranAmout;
					}
				}

				if (parentRowExt != null &&
				(oldRow?.DocumentMatched != row?.DocumentMatched))
				{
					parentRowExt.UnmatchedChilds = SelectFrom<CABankTran>
														.Where<CABankTranSplit.parentTranID.IsEqual<@P.AsInt>
														.And<CABankTran.documentMatched.IsEqual<False>>>
														.View.Select(Base, rowExt.ParentTranID).Count;
				}

				if (parentRowExt != null && oldRow?.Processed != row?.Processed)
				{
					parentRowExt.UnprocessedChilds = parentRowExt.UnprocessedChilds + (row?.Processed == true ? -1 : 1);
				}

				copyParentRow = (CABankTran)sender.Update(copyParentRow);
				sender.Current = row;
				Base.Details.View.RequestRefresh();
			}
		}

		public virtual void _(Events.RowDeleting<CABankTran> e)
		{
			var row = (CABankTran)e.Row;
			var rowExt = row.GetExtension<CABankTranSplit>();
			if (rowExt?.ParentTranID == null)
			{
				e.Cancel = true;
				throw new PXException(Messages.OnlyChildTransactionsCanBeDeleted);
			}
			else if (row?.DocumentMatched == true)
			{
				e.Cancel = true;
				throw new PXException(Messages.ChildTransactionCanBeDeletedbecauseItOrParentHasBeenMatched);
			}
			else
			{
				CABankTran parentRow = CABankTran.PK.Find(Base, rowExt.ParentTranID);
				if (parentRow?.DocumentMatched == true)
				{
					e.Cancel = true;
					throw new PXException(Messages.ChildTransactionCanBeDeletedbecauseItOrParentHasBeenMatched);
				}
			}
		}

		public virtual void _(Events.RowDeleted<CABankTran> e)
		{
			var row = (CABankTran)e.Row;
			PXCache sender = e.Cache;
			var rowExt = row.GetExtension<CABankTranSplit>();
			if (rowExt?.ParentTranID == null)
				return;

			CABankTran parentRow = (CABankTran)sender.Locate(new CABankTran { TranID = rowExt.ParentTranID });
			decimal? newTranAmout = parentRow.CuryTranAmt + (row?.CuryTranAmt ?? 0m);
			parentRow = (CABankTran)sender.CreateCopy(parentRow);

			if (newTranAmout > 0m)
			{
				parentRow.CuryDebitAmt = newTranAmout;
			}
			else if (newTranAmout < 0m)
			{
				parentRow.CuryCreditAmt = -newTranAmout;
			}

			bool hasChild = false;
			foreach (CABankTran child in SelectFrom<CABankTran>
			  .Where<CABankTranSplit.parentTranID.IsEqual<P.AsInt>>.View
			  .Select(Base, new object[] { rowExt.ParentTranID }))
			{
				hasChild = true;
				break;
			}

			CABankTranSplit parentRowExt = parentRow.GetExtension<CABankTranSplit>();
			if (!hasChild)
			{
				parentRowExt.Splitted = false;
				parentRowExt.CuryOrigTranAmt = null;
				parentRowExt.OrigDrCr = null;
			}

			parentRowExt.ChildsCount = (parentRowExt.ChildsCount ?? 0) - 1;
			parentRowExt.UnmatchedChilds = (parentRowExt.UnmatchedChilds ?? 0) - 1;
			parentRowExt.UnprocessedChilds = (parentRowExt.UnprocessedChilds ?? 0) - 1;

			parentRow = (CABankTran)sender.Update(parentRow);
		}

		public virtual void _(Events.FieldVerifying<CABankTran.curyCreditAmt> e)
		{
			CABankTran row = (CABankTran)e.Row;
			PXCache sender = e.Cache;

			var ex = VerifyAmout(sender, row, -(decimal?)e.NewValue, row.CuryTranAmt);
			if (ex != null)
			{
				e.NewValue = e.OldValue;
				throw ex;
			}
		}

		public virtual void _(Events.FieldVerifying<CABankTran.curyDebitAmt> e)
		{
			CABankTran row = (CABankTran)e.Row;
			PXCache sender = e.Cache;

			var ex = VerifyAmout(sender, row, (decimal?)e.NewValue, row.CuryTranAmt);
			if (ex != null)
			{
				e.NewValue = e.OldValue;
				throw ex;
			}
		}

		protected virtual PXSetPropertyException VerifyAmout(PXCache cache, CABankTran row, decimal? newValue, decimal? oldValue)
		{
			CABankTranSplit rowExt = row?.GetExtension<CABankTranSplit>();
			if (rowExt?.ParentTranID == null)
				return null;

			CABankTran parentRow = (CABankTran)cache.Locate(new CABankTran { TranID = rowExt.ParentTranID });
			decimal? newParentTranAmout = parentRow.CuryTranAmt + (oldValue ?? 0m) - (newValue ?? 0m);

			if (newParentTranAmout == 0m)
			{
				return new PXSetPropertyException(Messages.AmountOfOriginalTransactionCannotBeZero);
			}

			return null;
		}

		[PXOverride]
		public void CATranExt_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CABankTran currentBankTran = Base.Details.Current;
			var status = Base.Details.Cache.GetStatus(currentBankTran);
			bool isNew = (status == PXEntryStatus.Inserted || status == PXEntryStatus.InsertedDeleted);
			if (isNew || currentBankTran?.Processed == true)
			{
				PXUIFieldAttribute.SetEnabled(sender, null, false);
			}
		}

		[PXOverride]
		public void CABankTranInvoiceMatch_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			CABankTran currentBankTran = Base.Details.Current;
			var status = Base.Details.Cache.GetStatus(currentBankTran);
			bool isNew = (status == PXEntryStatus.Inserted || status == PXEntryStatus.InsertedDeleted);
			if (isNew || currentBankTran?.Processed == true)
			{
				PXUIFieldAttribute.SetEnabled(sender, null, false);
			}
		}
		#endregion
	}
}
