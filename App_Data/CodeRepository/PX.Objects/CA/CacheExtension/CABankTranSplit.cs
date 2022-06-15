using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;

namespace PX.Objects.CA
{
	public sealed class CABankTranSplit : PXCacheExtension<CABankTran>
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
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Split", Visible = true, Enabled = false, IsReadOnly = true)]
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
		[PXDBInt]
		[PXSelector(typeof(Search<CABankTran.tranID, Where<CABankTran.tranID, NotEqual<Current<CABankTran.tranID>>>>))]
		[PXUIField(DisplayName = "ID", Visible = false)]		
		public int? ParentTranID
		{
			get;
			set;
		}
		#endregion
		#region OrigDrCr
		public abstract class origDrCr : PX.Data.BQL.BqlString.Field<origDrCr> { }

		/// <summary>
		/// The balance type of the original bank transaction.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"D"</c>: Receipt,
		/// <c>"C"</c>: Disbursement
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[CADrCr.List]
		[PXUIField(DisplayName = "DrCr")]
		public string OrigDrCr
		{
			get;
			set;
		}
		#endregion
		#region CuryOrigTranAmt
		public abstract class curyOrigTranAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigTranAmt> { }

		/// <summary>
		/// The amount of the original bank transaction in the selected currency.
		/// </summary>
		[PXDBCury(typeof(CABankTran.curyID))]
		[PXUIField(DisplayName = "CuryOrigTranAmt")]
		public decimal? CuryOrigTranAmt
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
			[PXDependsOnFields(typeof(CABankTran.drCr), typeof(curyOrigTranAmt))]
			get
			{
				return (OrigDrCr == CADrCr.CADebit && CuryOrigTranAmt != 0m) ? this.CuryOrigTranAmt : null;
			}
			set
			{
			}
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
			[PXDependsOnFields(typeof(CABankTran.drCr), typeof(curyOrigTranAmt))]
			get
			{
				return (OrigDrCr == CADrCr.CACredit && CuryOrigTranAmt != 0m) ? -this.CuryOrigTranAmt : null;
			}
			set
			{
			}
		}
		#endregion
		#region CuryDisplayDebitAmt
		public abstract class curyDisplayDebitAmt : PX.Data.BQL.BqlDecimal.Field<curyDisplayDebitAmt> { }

		/// <summary>
		/// The amount of the receipt in the selected currency.
		/// This is a virtual field and it has no representation in the database.
		/// </summary>
		[PXCury(typeof(CABankTran.curyID))]
		[PXUIField(DisplayName = "Orig. Receipt", Enabled = true)]
		public decimal? CuryDisplayDebitAmt
		{
			[PXDependsOnFields(typeof(CABankTran.drCr), typeof(CABankTran.curyTranAmt), typeof(origDrCr), typeof(curyOrigTranAmt))]
			get
			{
				if (this.Splitted == true)
				{
					return (OrigDrCr == CADrCr.CADebit) ? CuryOrigTranAmt : decimal.Zero;
				}
				else
				{
					return (Base.DrCr == CADrCr.CADebit) ? Base.CuryTranAmt : decimal.Zero;
				}
			}

			set
			{
				if (value != 0m)
				{
					Base.CuryTranAmt = value;
					Base.DrCr = CADrCr.CADebit;
				}
				else if (Base.DrCr == CADrCr.CADebit)
				{
					Base.CuryTranAmt = 0m;
				}
			}
		}
		#endregion
		#region CuryDisplayCreditAmt
		public abstract class curyDisplayCreditAmt : PX.Data.BQL.BqlDecimal.Field<curyDisplayCreditAmt> { }

		/// <summary>
		/// The amount of the disbursement in the selected currency.
		/// This is a virtual field and it has no representation in the database.
		/// </summary>
		[PXCury(typeof(CABankTran.curyID))]
		[PXUIField(DisplayName = "Orig. Disbursement", Enabled = true)]
		public decimal? CuryDisplayCreditAmt
		{
			[PXDependsOnFields(typeof(CABankTran.drCr), typeof(CABankTran.curyTranAmt), typeof(origDrCr), typeof(curyOrigTranAmt))]
			get
			{
				if (this.Splitted == true)
				{
					return (OrigDrCr == CADrCr.CACredit) ? -CuryOrigTranAmt : decimal.Zero;
				}
				else
				{
					return (Base.DrCr == CADrCr.CACredit) ? -Base.CuryTranAmt : decimal.Zero;
				}
			}

			set
			{
				if (value != 0m)
				{
					Base.CuryTranAmt = -value;
					Base.DrCr = CADrCr.CACredit;
				}
				else if (Base.DrCr == CADrCr.CACredit)
				{
					Base.CuryTranAmt = 0m;
				}
			}
		}
		#endregion
		#region SplittedIcon

		public abstract class splittedIcon : PX.Data.BQL.BqlString.Field<splittedIcon> { }

		[PXUIField(DisplayName = "Split", IsReadOnly = true, Visible = false)]
		[PXImage]
		public string SplittedIcon
		{
			[PXDependsOnFields(typeof(splitted), typeof(parentTranID))]
			get
			{
				if (this.Splitted == true)
				{
					return SplitIcon.Parent;
				}

				if (ParentTranID != null)
				{
					return SplitIcon.Split;
				}
				return null;
			}
			set
			{
			}
		}
		#endregion
		#region ChildsCount
		public abstract class childsCount : PX.Data.BQL.BqlInt.Field<childsCount> { }

		[PXDBInt(MinValue = 0)]
		public int? ChildsCount
		{
			get;
			set;
		}
		#endregion
		#region UnmatchedChilds
		public abstract class unmatchedChilds : PX.Data.BQL.BqlInt.Field<unmatchedChilds> { }

		[PXDBInt(MinValue = 0)]
		public int? UnmatchedChilds
		{
			get;
			set;
		}
		#endregion
		#region UnprocessedChilds
		public abstract class unprocessedChilds : PX.Data.BQL.BqlInt.Field<unprocessedChilds> { }

		[PXDBInt(MinValue = 0)]
		public int? UnprocessedChilds
		{
			get;
			set;
		}
		#endregion

		#region FullProcessed
		public abstract class fullProcessed : PX.Data.BQL.BqlBool.Field<fullProcessed> { }

		/// <summary>
		/// Specifies (if set to <c>true</c>) that this bank transaction is processed.
		/// </summary>
		[PXBool]
		[PXFormula(typeof(Switch<
			Case<Where<unprocessedChilds.IsGreater<Zero>>, False>,
			CABankTran.processed>))]
		[PXUIField(DisplayName = "Processed", Visible = true, Enabled = false, IsReadOnly = true)]
		public bool? FullProcessed
		{
			get;
			set;
		}
		#endregion

		#region FullDocumentMatched
		public abstract class fullDocumentMatched : PX.Data.BQL.BqlBool.Field<fullDocumentMatched> { }

		/// <summary>
		/// Specifies (if set to <c>true</c>) that this bank transaction is matched to the payment and ready to be processed. 
		/// That is, the bank transaction has been matched to an existing transaction in the system, or details of a new document that matches this transaction have been specified.
		/// </summary>
		[PXBool]
		[PXFormula(typeof(Switch<
			Case<Where<unmatchedChilds.IsGreater<Zero>>, False>,
			CABankTran.documentMatched>))]
		[PXUIField(DisplayName = "Matched", Visible = true, Enabled = false, IsReadOnly = true)]
		public bool? FullDocumentMatched
		{
			get;
			set;
		}
		#endregion
	}
}
