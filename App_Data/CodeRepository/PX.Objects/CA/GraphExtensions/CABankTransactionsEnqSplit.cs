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


namespace PX.Objects.CA.GraphExtensions
{
	public class CABankTransactionsEnqSplit : PXGraphExtension<CABankTransactionsEnq>
	{
		public sealed class CABankTranHistorySplit : PXCacheExtension<CABankTransactionsEnq.CABankTranHistory>
		{
			public static bool IsActive()
			{
				return PXAccess.FeatureInstalled<FeaturesSet.bankTransactionSplits>();
			}

			#region Splitted
			public abstract class splitted : PX.Data.BQL.BqlBool.Field<splitted> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that this bank transaction is splitted. 
			/// That is, the bank transaction has been matched to an existing transaction in the system, or details of a new document that matches this transaction have been specified.
			/// </summary>
			[PXDBBool]
			[PXUIField(DisplayName = "Split", Visible = true, Enabled = false)]
			public bool? Splitted
			{
				get;
				set;
			}
			#endregion
			#region ParentTranID
			public abstract class parentTranID : PX.Data.BQL.BqlInt.Field<parentTranID> { }

			/// <summary>
			/// The unique identifier of the CA bank transaction.
			/// This field is the key field.
			/// </summary>
			[PXUIField(DisplayName = "ID", Visible = false)]
			[PXDBInt]
			public int? ParentTranID
			{
				get;
				set;
			}
			#endregion
			#region SplittedIcon

			public abstract class splittedIcon : PX.Data.BQL.BqlString.Field<splittedIcon> { }

			[PXUIField(DisplayName = "Split", IsReadOnly = true, Visible = false)]
			[PXImage]
			public string SplittedIcon
			{
				get;
				set;
			}
			#endregion
			#region CuryOrigDebitAmt
			public abstract class curyOrigDebitAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDebitAmt> { }

			/// <summary>
			/// The amount of the original receipt in the selected currency.
			/// This is a virtual field and it has no representation in the database.
			/// </summary>
			[PXCury(typeof(CABankTran.curyID))]
			[PXUIField(DisplayName = "Orig. Receipt", Enabled = false, Visible = false)]
			public decimal? CuryOrigDebitAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryOrigCreditAmt
			public abstract class curyOrigCreditAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigCreditAmt> { }

			/// <summary>
			/// The amount of the original disbursement in the selected currency.
			/// This is a virtual field and it has no representation in the database.
			/// </summary>
			[PXCury(typeof(CABankTran.curyID))]
			[PXUIField(DisplayName = "Orig. Disbursement", Enabled = false, Visible = false)]
			public decimal? CuryOrigCreditAmt
			{
				get;
				set;
			}
			#endregion
		}

		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.bankTransactionSplits>();
		}

		public override void Initialize()
		{
			base.Initialize();
			Base.Result.OrderByNew<OrderBy<Asc<CABankTransactionsEnq.CABankTranHistory.extRefNbr,
									Asc<CABankTransactionsEnq.CABankTranHistory.sortOrder,
									Asc<CABankTransactionsEnq.CABankTranHistory.tranID>>>>>();
		}

		public delegate Dictionary<Type, Type> GetMapperDictionaryDelegate();

		[PXOverride]
		public virtual Dictionary<Type, Type> GetMapperDictionary(GetMapperDictionaryDelegate baseMethod)
		{
			Dictionary<Type, Type> result = baseMethod();
			result.Add(typeof(CABankTranHistorySplit.splitted), typeof(CABankTranSplit.splitted));
			result.Add(typeof(CABankTranHistorySplit.parentTranID), typeof(CABankTranSplit.parentTranID));
			result.Add(typeof(CABankTranHistorySplit.splittedIcon), typeof(CABankTranSplit.splittedIcon));
			result.Add(typeof(CABankTranHistorySplit.curyOrigCreditAmt), typeof(CABankTranSplit.curyOrigCreditAmt));
			result.Add(typeof(CABankTranHistorySplit.curyOrigDebitAmt), typeof(CABankTranSplit.curyOrigDebitAmt));

			return result;
		}

		[PXDBCalced(typeof(IsNull<CABankTranSplit.parentTranID, CABankTran.tranID>), typeof(int))]
		public void _(Events.CacheAttached<CABankTran.sortOrder> e) { }

		public void _(Events.RowSelected<CABankTransactionsEnq.CABankTranHistory> e)
		{
			var row = (CABankTransactionsEnq.CABankTranHistory)e.Row;

			CABankTranHistorySplit currentExt = row?.GetExtension<CABankTranHistorySplit>();
			bool isChild = currentExt?.ParentTranID != null;
			if (isChild)
			{
				PXUIFieldAttribute.SetVisible<CABankTranHistorySplit.splittedIcon>(e.Cache, null, isChild);
				PXUIFieldAttribute.SetVisibility<CABankTranHistorySplit.curyOrigCreditAmt>(e.Cache, null, PXUIVisibility.Visible);
				PXUIFieldAttribute.SetVisibility<CABankTranHistorySplit.curyOrigDebitAmt>(e.Cache, null, PXUIVisibility.Visible);
			}
		}
			
	}
}
