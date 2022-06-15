using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.Common;
using PX.Data.BQL;
using PX.Data.EP;
using PX.Objects.GL;
using PX.Objects.AP;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.CR;
using PX.Objects.CM;
using PX.Objects.CA.BankStatementHelpers;
using PX.Objects.Common;
using PX.Objects.Common.Extensions;
using PX.Objects.EP;
using System.Linq;
using PX.Objects.CA.Repositories;

namespace PX.Objects.CA.BankStatementProtoHelpers
{
	[Serializable]
	[PXHidden]
	public partial class CABankTranDocRef : IBqlTable, IBankMatchRelevance
	{
		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		[PXBool]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected
		{
			get;
			set;
		}
		#endregion
		#region TranID
		public abstract class tranID : PX.Data.BQL.BqlInt.Field<tranID> { }
        [PXDBInt]
		public virtual int? TranID
			{
			get;
			set;
		}
		#endregion
		#region TranDate
		public abstract class tranDate : PX.Data.BQL.BqlDateTime.Field<tranDate> { }

		/// <summary>
		/// The transaction date.
		/// </summary>
		[PXDBDate]
		public virtual DateTime? TranDate
		{
			get;
			set;
		}
		#endregion
		#region CATranID
		public abstract class cATranID : PX.Data.BQL.BqlLong.Field<cATranID> { }

		[PXDBLong(IsKey = true)]
		public virtual long? CATranID
			{
			get;
			set;
		}
		#endregion
		#region DocModule
		public abstract class docModule : PX.Data.BQL.BqlString.Field<docModule> { }
		
		[PXDBString(2, IsFixed = true)]
		[PXStringList(new string[] { GL.BatchModule.AP, GL.BatchModule.AR }, new string[] { BatchModule.AP, BatchModule.AR })]
		public virtual string DocModule
			{
			get;
			set;
		}
		#endregion
		#region DocType
		public abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }

		[PXDBString(3, IsFixed = true, InputMask = "")]
		public virtual string DocType
		{
			get;
			set;
		}
		#endregion
		#region DocRefNbr
		public abstract class docRefNbr : PX.Data.BQL.BqlString.Field<docRefNbr> { }

		[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		public virtual string DocRefNbr
		{
			get;
			set;
		}
		#endregion
		#region ReferenceID
		public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID> { }

		[PXDBInt]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? ReferenceID
			{
			get;
			set;
		}
		#endregion
		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }

		[PXDBInt]
		[PXUIField(DisplayName = "Cash Account ID", Visible = false)]
		[PXDefault]
		public virtual int? CashAccountID
		{
			get;
			set;
		}
		#endregion
		#region MatchRelevance
		public abstract class matchRelevance : PX.Data.BQL.BqlDecimal.Field<matchRelevance> { }

		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBDecimal(6)]
		[PXUIField(DisplayName = "Match Relevance", Enabled = false)]
		public virtual decimal? MatchRelevance
			{
			get;
			set;
		}
		#endregion
		#region CuryTranAmt
		public abstract class curyTranAmt : PX.Data.BQL.BqlDecimal.Field<curyTranAmt> { }

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount", Visibility = PXUIVisibility.Visible)]
		public virtual decimal? CuryTranAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryDiscAmt
		/// <summary>
		/// The cash discount amount in the currency of the document.
		/// </summary>
		public abstract class curyDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyDiscAmt> { }
		
		/// <summary>
		/// The cash discount amount in the currency of the document.
		/// </summary>
		[PXDBDecimal(4)]
		public virtual decimal? CuryDiscAmt
		{
			get;
			set;
		}
		#endregion
		#region DiscDate
		/// <summary>
		/// The date when the cash discount can be taken in accordance with the credit terms of source document
		/// </summary>
		public abstract class discDate : PX.Data.BQL.BqlDateTime.Field<discDate> { }

		/// <summary>
		/// The date when the cash discount can be taken in accordance with the credit terms of source document
		/// </summary>
		[PXDBDate]
		public virtual DateTime? DiscDate
		{
			get;
			set;
		}
		#endregion
		[Obsolete(Common.Messages.MethodIsObsoleteAndWillBeRemoved2020R1)]
		public void Copy(CABankTranInvoiceMatch aSrc)
		{
			DocModule = aSrc.OrigModule;
			DocType = aSrc.OrigTranType;
			DocRefNbr = aSrc.OrigRefNbr;
			ReferenceID = aSrc.ReferenceID;
		}

		public void Copy(CATran aSrc)
		{
			this.CashAccountID = aSrc.CashAccountID;
			this.CATranID = aSrc.TranID;
			this.CuryTranAmt = aSrc.CuryTranAmt;
			this.ReferenceID = aSrc.ReferenceID;
		}
		public void Copy(CABankTran aSrc)
		{
			this.TranID = aSrc.TranID;
			this.TranDate = aSrc.TranDate;
			this.CashAccountID = aSrc.CashAccountID;
		}

	}

	public abstract class CABankTranDocumentMatch
	{
		#region MatchRelevance
		public abstract class matchRelevance : PX.Data.BQL.BqlDecimal.Field<matchRelevance> { }

		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDecimal(6)]
		[PXUIField(DisplayName = "Match Relevance", Enabled = false)]
		public virtual decimal? MatchRelevance
		{
			get;
			set;
		}
		#endregion
		#region MatchRelevancePercent
		public abstract class matchRelevancePercent : PX.Data.BQL.BqlDecimal.Field<matchRelevancePercent> { }

		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXDecimal(3)]
		[PXUIField(DisplayName = "Match Relevance, %", Enabled = false)]
		[PXFormula(typeof(Mult<CABankTranDocumentMatch.matchRelevance, decimal100>))]
		public virtual Decimal? MatchRelevancePercent
		{
			get;
			set;
		}
		#endregion
		public abstract string GetDocumentKey();

		public abstract void BuildDocRef(CABankTranDocRef docRef);
	}

	[Serializable]
	public partial class CABankTranInvoiceMatch : CABankTranDocumentMatch, IBqlTable
	{
		#region IsMatched
		public abstract class isMatched : PX.Data.BQL.BqlBool.Field<isMatched> { }

		[PXBool]
		[PXUIField(DisplayName = "Matched")]
		public virtual bool? IsMatched
		{
			get;
			set;
		}
		#endregion
        #region IsBestMatch
        public abstract class isBestMatch : PX.Data.BQL.BqlBool.Field<isBestMatch> { }

        [PXBool]
        [PXUIField(DisplayName = "Best Match")]
        public virtual bool? IsBestMatch
        {
            get;
            set;
        }
        #endregion
        #region BranchID
        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

        [Branch(Visibility = PXUIVisibility.SelectorVisible, Visible = false, Enabled = false)]
        public virtual int? BranchID
        {
            get;
            set;
        }
        #endregion
		#region OrigModule
		public abstract class origModule : PX.Data.BQL.BqlString.Field<origModule> { }

		[PXDBString(2, IsFixed = true, IsKey = true)]
		[PXDefault]
		[BatchModule.List]
		[PXUIField(DisplayName = "Module")]
		public virtual string OrigModule
		{
			get;
			set;
		}
		#endregion
		#region OrigTranType
		public abstract class origTranType : PX.Data.BQL.BqlString.Field<origTranType> { }

		[PXDBString(3, IsFixed = true, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Type")]
		[CAAPARTranType.ListByModuleRestricted(typeof(CABankTranInvoiceMatch.origModule))]
		public virtual string OrigTranType
			{
			get;
			set;
		}
		#endregion
		#region OrigRefNbr
		public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Reference Nbr.")]
		public virtual string OrigRefNbr
			{
			get;
			set;
		}
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }

		[PXDBString(40, IsUnicode = true)]
		[PXUIField(DisplayName = "Ext. Ref. Nbr.", Visibility = PXUIVisibility.Visible)]
		public virtual string ExtRefNbr
			{
			get;
			set;
		}
		#endregion
		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }

		[CashAccount(DisplayName = "Cash Account", Visibility = PXUIVisibility.Visible, DescriptionField = typeof(CashAccount.descr))]
		public virtual int? CashAccountID
			{
			get;
			set;
		}
		#endregion
		#region TranDate
		public abstract class tranDate : PX.Data.BQL.BqlDateTime.Field<tranDate> { }

		[PXDBDate]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Doc. Date")]
		public virtual DateTime? TranDate
		{
			get;
			set;
		}
		#endregion
		#region DiscDate
		/// <summary>
		/// The date when the cash discount can be taken in accordance with the credit terms of source document
		/// </summary>
		public abstract class discDate : PX.Data.BQL.BqlDateTime.Field<discDate> { }

		/// <summary>
		/// The date when the cash discount can be taken in accordance with the credit terms of source document
		/// </summary>
		[PXDBDate()]
		[PXUIField(DisplayName = "Discount Date")]
		public virtual DateTime? DiscDate
		{
			get;
			set;
		}
		#endregion
		#region DrCr
		public abstract class drCr : PX.Data.BQL.BqlString.Field<drCr> { }

		[PXDefault]
		[PXDBString(1, IsFixed = true)]
		[CADrCr.List]
		[PXUIField(DisplayName = "Disb. / Receipt")]
		public virtual string DrCr
			{
			get;
			set;
		}
		#endregion
		#region ReferenceID
		public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID> { }

		[PXDBInt]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXSelector(typeof(BAccountR.bAccountID),
						SubstituteKey = typeof(BAccountR.acctCD),
					 DescriptionField = typeof(BAccountR.acctName))]
		[PXUIField(DisplayName = "Business Account", Visibility = PXUIVisibility.Visible)]
		public virtual int? ReferenceID
			{
			get;
			set;
		}
		#endregion
		#region ReferenceName
		public abstract class referenceName : PX.Data.BQL.BqlString.Field<referenceName> { }

		[PXUIField(DisplayName = CR.Messages.BAccountName, Visibility = PXUIVisibility.Visible)]
		[PXString(60, IsUnicode = true)]
		public virtual string ReferenceName
			{
			get;
			set;
		}
		#endregion
		#region TranDesc
		public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }

		[PXDBString(256, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.Visible)]
		[PXFieldDescription]
		public virtual string TranDesc
			{
			get;
			set;
		}
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }

		[FinPeriodSelector(typeof(CATran.tranDate))]
		[PXUIField(DisplayName = "Post Period")]
		public virtual string FinPeriodID
			{
			get;
			set;
		}
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

		[PXDBLong]
		public virtual long? CuryInfoID
			{
			get;
			set;
		}
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
		[PXUIField(DisplayName = "Currency", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXSelector(typeof(Currency.curyID))]
		public virtual string CuryID
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

		[PXDBBool()]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Released", Enabled = false)]
		public virtual bool? Released
			{
			get;
			set;
		}
		#endregion
		#region CuryTranAmt
		public abstract class curyTranAmt : PX.Data.BQL.BqlDecimal.Field<curyTranAmt> { }

		[PXDBCurrency(typeof(CATran.curyInfoID), typeof(CATran.tranAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount", Visibility = PXUIVisibility.Visible)]
		public virtual decimal? CuryTranAmt
			{
			get;
			set;
		}
		#endregion
		#region TranAmt
		public abstract class tranAmt : PX.Data.BQL.BqlDecimal.Field<tranAmt> { }

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tran. Amount")]
		public virtual decimal? TranAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryDiscAmt
		/// <summary>
		/// The cash discount amount of the document.
		/// Given in the <see cref="curyID">currency of the document</see>.
		/// </summary>
		public abstract class curyDiscAmt : PX.Data.BQL.BqlDecimal.Field<curyDiscAmt> { }

		/// <summary>
		/// The cash discount amount of the document.
		/// Given in the <see cref="CuryID">currency of the document</see>.
		/// </summary>
		[PXDBDecimal]
		[PXUIField(DisplayName = "Cash Discount", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? CuryDiscAmt
		{
			get;
			set;
		}
		#endregion
		#region DiscAmt
		/// <summary>
		/// The cash discount amount of the document.
		/// Given in the <see cref="Branch.baseCuryID">base currency of the branch</see>.
		/// </summary>
		public abstract class discAmt : PX.Data.BQL.BqlDecimal.Field<discAmt> { }

		/// <summary>
		/// The cash discount amount of the document.
		/// Given in the <see cref="Company.BaseCuryID">base currency of the company</see>.
		/// </summary>
		[PXDBDecimal]
		public virtual Decimal? DiscAmt
		{
			get;
			set;
		}
		#endregion
		#region DueDate
		public abstract class dueDate : PX.Data.BQL.BqlDateTime.Field<dueDate> { }

		[PXDBDate]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Doc. Date")]
		public virtual DateTime? DueDate
		{
			get;
			set;
		}
		#endregion
		public void Copy(ARInvoice aSrc)
		{
			this.CashAccountID = null;
			this.TranDate = aSrc.DocDate;
			this.TranDesc = aSrc.DocDesc;
			this.ReferenceID = aSrc.CustomerID;
			this.DrCr = aSrc.DrCr == GL.DrCr.Credit ? CADrCr.CADebit : CADrCr.CACredit;
			this.FinPeriodID = aSrc.FinPeriodID;
			this.ExtRefNbr = aSrc.InvoiceNbr;
			this.CuryInfoID = aSrc.CuryInfoID;
			this.CuryID = aSrc.CuryID;
			this.CuryTranAmt = aSrc.CuryDocBal;
			this.TranAmt = aSrc.DocBal;
			this.CuryDiscAmt = aSrc.CuryDiscBal;
			this.DiscAmt = aSrc.DiscBal;
			this.DiscDate = aSrc.DiscDate;
			this.DueDate = aSrc.DueDate;
			this.Released = aSrc.Released;
			this.OrigTranType = aSrc.DocType;
			this.OrigRefNbr = aSrc.RefNbr;
			this.OrigModule = GL.BatchModule.AR;
            this.BranchID = aSrc.BranchID;
		}
		public void Copy(APInvoice aSrc)
		{
			this.CashAccountID = null;
			this.TranDate = aSrc.DocDate;
			this.TranDesc = aSrc.DocDesc;
			this.ReferenceID = aSrc.VendorID;
			this.DrCr = aSrc.DrCr == GL.DrCr.Credit ? CADrCr.CADebit : CADrCr.CACredit;
			this.FinPeriodID = aSrc.FinPeriodID;
			this.ExtRefNbr = aSrc.InvoiceNbr;
			this.CuryInfoID = aSrc.CuryInfoID;
			this.CuryID = aSrc.CuryID;
			this.CuryTranAmt = aSrc.CuryDocBal;
			this.TranAmt = aSrc.DocBal;
			this.CuryDiscAmt = aSrc.CuryDiscBal;
			this.DiscAmt = aSrc.DiscBal;
			this.DiscDate = aSrc.DiscDate;
			this.DueDate = aSrc.DueDate;
			this.Released = aSrc.Released;
			this.OrigTranType = aSrc.DocType;
			this.OrigRefNbr = aSrc.RefNbr;
			this.OrigModule = GL.BatchModule.AP;
            this.BranchID = aSrc.BranchID;
		}
		public void Copy(Light.ARInvoice aSrc)
		{
			this.CashAccountID = null;
			this.TranDate = aSrc.DocDate;
			this.TranDesc = aSrc.DocDesc;
			this.ReferenceID = aSrc.CustomerID;
			this.DrCr = aSrc.DrCr == GL.DrCr.Credit ? CADrCr.CADebit : CADrCr.CACredit;
			this.FinPeriodID = aSrc.FinPeriodID;
			this.ExtRefNbr = aSrc.InvoiceNbr;
			this.CuryInfoID = aSrc.CuryInfoID;
			this.CuryID = aSrc.CuryID;
			this.CuryTranAmt = aSrc.CuryDocBal;
			this.TranAmt = aSrc.DocBal;
			this.CuryDiscAmt = aSrc.CuryDiscBal;
			this.DiscAmt = aSrc.DiscBal;
			this.DiscDate = aSrc.DiscDate;
			this.DueDate = aSrc.DueDate;
			this.Released = aSrc.Released;
			this.OrigTranType = aSrc.DocType;
			this.OrigRefNbr = aSrc.RefNbr;
			this.OrigModule = GL.BatchModule.AR;
            this.BranchID = aSrc.BranchID;
		}
		public void Copy(Light.APInvoice aSrc)
		{
			this.CashAccountID = null;
			this.TranDate = aSrc.DocDate;
			this.TranDesc = aSrc.DocDesc;
			this.ReferenceID = aSrc.VendorID;
			this.DrCr = aSrc.DrCr == GL.DrCr.Credit ? CADrCr.CADebit : CADrCr.CACredit;
			this.FinPeriodID = aSrc.FinPeriodID;
			this.ExtRefNbr = aSrc.InvoiceNbr;
			this.CuryInfoID = aSrc.CuryInfoID;
			this.CuryID = aSrc.CuryID;
			this.CuryTranAmt = aSrc.CuryDocBal;
			this.TranAmt = aSrc.DocBal;
			this.CuryDiscAmt = aSrc.CuryDiscBal;
			this.DiscAmt = aSrc.DiscBal;
			this.DiscDate = aSrc.DiscDate;
			this.DueDate = aSrc.DueDate;
			this.Released = aSrc.Released;
			this.OrigTranType = aSrc.DocType;
			this.OrigRefNbr = aSrc.RefNbr;
			this.OrigModule = GL.BatchModule.AP;
            this.BranchID = aSrc.BranchID;
		}
		public void Copy(CATran aSrc)
		{

		}

		public override string GetDocumentKey()
		{
			return OrigModule + OrigTranType + OrigRefNbr;
		}

		public override void BuildDocRef(CABankTranDocRef docRef)
		{
			docRef.DocModule = OrigModule;
			docRef.DocType = OrigTranType;
			docRef.DocRefNbr = OrigRefNbr;
			docRef.ReferenceID = ReferenceID;
			docRef.CuryTranAmt = CuryTranAmt;
			docRef.CuryDiscAmt = CuryDiscAmt;
			docRef.DiscDate = DiscDate;
		}
	}

    [PXHidden]
    public class CABankTranExpenseDetailMatch : CABankTranDocumentMatch, IBqlTable
    {
        #region IsMatched
        public abstract class isMatched : PX.Data.BQL.BqlBool.Field<isMatched> { }

        [PXBool]
        [PXUIField(DisplayName = "Matched")]
        public virtual bool? IsMatched
        {
            get;
            set;
        }
        #endregion
        #region RefNbr
        public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
        /// <summary>
        /// The user-friendly unique identifier of the receipt.
        /// </summary>
        [PXString(15, IsUnicode = true, InputMask = "", IsKey = true)]
        [PXUIField(DisplayName = "Receipt Number", Visibility = PXUIVisibility.Visible)]
        [EPExpenceReceiptSelector]
        public virtual string RefNbr
        {
            get;
            set;
        }
        #endregion
        #region ExtRefNbr
        public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }

        [PXString(40, IsUnicode = true)]
        [PXUIField(DisplayName = "Ref. Nbr.", Visibility = PXUIVisibility.Visible)]
        public virtual string ExtRefNbr { get; set; }
        #endregion
        #region DocDate
        public abstract class docDate : PX.Data.BQL.BqlDateTime.Field<docDate> { }

        [PXDate]
        [PXUIField(DisplayName = "Doc. Date")]
        public virtual DateTime? DocDate { get; set; }
        #endregion
        #region PaidWith
        public abstract class paidWith : BqlInt.Field<paidWith> { }

        [PXUIField(DisplayName = "Paid With")]
        [PXString(8)]
        [LabelList(typeof(EPExpenseClaimDetails.paidWith.Labels))]
        public string PaidWith { get; set; }
        #endregion
        #region CardNumber
        public abstract class cardNumber : BqlString.Field<cardNumber> { }

        [PXString(20, IsUnicode = true, InputMask = "")]
        [PXUIField(DisplayName = "Card Number")]
        public virtual string CardNumber { get; set; }
        #endregion
        #region CuryInfoID
        public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }

        [PXDBLong]
        public virtual long? CuryInfoID
        {
            get;
            set;
        }
        #endregion
        #region CuryDocAmt
        public abstract class curyDocAmt : PX.Data.BQL.BqlDecimal.Field<curyDocAmt> { }

        [PXCurrency(typeof(curyInfoID), typeof(docAmt))]
        [PXUIField(DisplayName = "Amount in Claim Curr.")]
        public virtual decimal? CuryDocAmt
        {
            get;
            set;
        }
        #endregion
        #region DocAmt
        public abstract class docAmt : PX.Data.BQL.BqlDecimal.Field<docAmt> { }

        [PXDecimal(4)]
        public virtual decimal? DocAmt
        {
            get;
            set;
        }
		#endregion
		#region CuryID
	    public abstract class claimCuryID : PX.Data.BQL.BqlString.Field<claimCuryID> { }

	    [PXString(5, IsUnicode = true, InputMask = ">LLLLL")]
	    [PXUIField(DisplayName = "Claim Currency")]
	    public virtual string ClaimCuryID
	    {
		    get;
		    set;
	    }
	    #endregion
		#region CuryDocAmtDiff
		public abstract class curyDocAmtDiff : PX.Data.BQL.BqlDecimal.Field<curyDocAmtDiff> { }

        [PXCurrency(typeof(curyInfoID), typeof(docAmtDiff))]
        [PXUIField(DisplayName = "Amount Difference")]
        public virtual decimal? CuryDocAmtDiff
        {
            get;
            set;
        }
        #endregion
        #region DocAmtDiff
        public abstract class docAmtDiff : PX.Data.BQL.BqlDecimal.Field<docAmtDiff> { }

        [PXDecimal(4)]
        public virtual decimal? DocAmtDiff
        {
            get;
            set;
        }
        #endregion
        #region ReferenceID
        public abstract class referenceID : PX.Data.BQL.BqlInt.Field<referenceID> { }

        [PXInt]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXSelector(typeof(BAccountR.bAccountID),
            SubstituteKey = typeof(BAccountR.acctCD),
            DescriptionField = typeof(BAccountR.acctName))]
        [PXUIField(DisplayName = "Employee")]
        public virtual int? ReferenceID
        {
            get;
            set;
        }
        #endregion
        #region ReferenceName
        public abstract class referenceName : PX.Data.BQL.BqlString.Field<referenceName> { }

        [PXUIField(DisplayName = "Employee Name")]
        [PXString(60, IsUnicode = true)]
        public virtual string ReferenceName { get; set; }
        #endregion
        #region TranDesc
        public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }

        [PXString(256, IsUnicode = true)]
        [PXUIField(DisplayName = "Description")]
        public virtual string TranDesc
        {
            get;
            set;
        }
        #endregion

	    public override string GetDocumentKey()
	    {
		    return String.Concat(BatchModule.EP, EPExpenseClaimDetails.DocType, RefNbr);
	    }

	    public override void BuildDocRef(CABankTranDocRef docRef)
	    {
			docRef.DocModule = BatchModule.EP;
		    docRef.DocType = EPExpenseClaimDetails.DocType;
		    docRef.DocRefNbr = RefNbr;
		    docRef.ReferenceID = ReferenceID;
		}
    }

    public class PXInvoiceSelectorAttribute : PXCustomSelectorAttribute
	{
		protected Type _BatchModule;

		public PXInvoiceSelectorAttribute(Type BatchModule)
			: base(typeof(GeneralInvoice.refNbr),
				typeof(GeneralInvoice.refNbr),
				typeof(GeneralInvoice.docDate),
				typeof(GeneralInvoice.finPeriodID),
				typeof(GeneralInvoice.vendorBAccountID),
				typeof(GeneralInvoice.customerBAccountID),
				typeof(GeneralInvoice.locationID),
				typeof(GeneralInvoice.curyID),
				typeof(GeneralInvoice.curyOrigDocAmt),
				typeof(GeneralInvoice.curyDocBal),
				typeof(GeneralInvoice.dueDate),
				typeof(GeneralInvoice.branchID),
				typeof(GeneralInvoice.apExtRefNbr),
				typeof(GeneralInvoice.arExtRefNbr),
				typeof(GeneralInvoice.description))
		{
			this._BatchModule = BatchModule;
		}

		protected virtual IEnumerable GetRecords()
		{
			PXCache cache = this._Graph.Caches[BqlCommand.GetItemType(this._BatchModule)];
			PXCache adjustments = this._Graph.Caches[typeof(CABankTranAdjustment)];
			PXCache bankTrans = this._Graph.Caches[typeof(CABankTran)];
			CABankTran currentBankTran = (CABankTran)bankTrans.Current;
			object current = null;
			foreach (object item in PXView.Currents)
			{
				if (item != null && (item.GetType() == typeof(CABankTranAdjustment) || item.GetType().IsSubclassOf(typeof(CABankTranAdjustment))))
				{
					current = item;
					break;
				}
			}
			if (current == null)
			{
				current = adjustments.Current;
			}

			CABankTranAdjustment currentAdj = current as CABankTranAdjustment;
			if (cache.Current == null) yield break;
			string tranModule = (string)cache.GetValue(cache.Current, this._BatchModule.Name);
			switch (tranModule)
			{
				case GL.BatchModule.AP:
					foreach (APAdjust.APInvoice apInvoice in GetRecordsAP(currentAdj, currentBankTran, adjustments, this._Graph))
					{
						GeneralInvoice gInvoice = new GeneralInvoice();
						gInvoice.RefNbr = apInvoice.RefNbr;
						gInvoice.BranchID = apInvoice.BranchID;
						gInvoice.OrigModule = apInvoice.OrigModule;
						gInvoice.DocType = apInvoice.DocType;
						gInvoice.DocDate = apInvoice.DocDate;
						gInvoice.FinPeriodID = apInvoice.FinPeriodID;
						gInvoice.BAccountID = apInvoice.VendorID;
						gInvoice.LocationID = apInvoice.VendorLocationID;
						gInvoice.CuryID = apInvoice.CuryID;
						gInvoice.CuryOrigDocAmt = apInvoice.CuryOrigDocAmt;
						gInvoice.CuryDocBal = apInvoice.CuryDocBal;
						gInvoice.Status = apInvoice.Status;
						gInvoice.DueDate = apInvoice.DueDate;
						gInvoice.Description = apInvoice.DocDesc;
						gInvoice.ExtRefNbr = apInvoice.InvoiceNbr;
						yield return gInvoice;
					}
					break;
				case GL.BatchModule.AR:
					foreach (ARAdjust.ARInvoice arInvoice in GetRecordsAR(currentAdj, currentBankTran, adjustments, this._Graph))
					{
						GeneralInvoice gInvoice = new GeneralInvoice();
						gInvoice.RefNbr = arInvoice.RefNbr;
						gInvoice.BranchID = arInvoice.BranchID;
						gInvoice.OrigModule = arInvoice.OrigModule;
						gInvoice.DocType = arInvoice.DocType;
						gInvoice.DocDate = arInvoice.DocDate;
						gInvoice.FinPeriodID = arInvoice.FinPeriodID;
						gInvoice.BAccountID = arInvoice.CustomerID;
						gInvoice.LocationID = arInvoice.CustomerLocationID;
						gInvoice.CuryID = arInvoice.CuryID;
						gInvoice.CuryOrigDocAmt = arInvoice.CuryOrigDocAmt;
						gInvoice.CuryDocBal = arInvoice.CuryDocBal;
						gInvoice.Status = arInvoice.Status;
						gInvoice.DueDate = arInvoice.DueDate;
						gInvoice.Description = arInvoice.DocDesc;
						gInvoice.ExtRefNbr = arInvoice.InvoiceNbr;
						yield return gInvoice;
					}
					break;
			}
		}

		public static IEnumerable<ARAdjust.ARInvoice> GetRecordsAR(CABankTranAdjustment currentAdj, CABankTran currentBankTran, PXCache adjustments, PXGraph graph)
		{
			foreach (ARAdjust.ARInvoice result in GetRecordsAR(currentAdj.AdjdDocType, currentAdj.TranID, currentAdj.AdjNbr, currentBankTran,adjustments,graph))
			{
				yield return result;
			}
		}
		public static IEnumerable<APAdjust.APInvoice> GetRecordsAP(CABankTranAdjustment currentAdj, CABankTran currentBankTran, PXCache adjustments, PXGraph graph)
		{
			foreach (APAdjust.APInvoice result in GetRecordsAP(currentAdj.AdjdDocType, currentAdj.TranID, currentAdj.AdjNbr, currentBankTran, adjustments, graph))
			{
				yield return result;
			}
		}
		public static IEnumerable<ARAdjust.ARInvoice> GetRecordsAR(string AdjdDocType, int? TranID, int? AdjNbr, CABankTran currentBankTran, PXCache adjustments, PXGraph graph)
		{
			foreach (PXResult<ARAdjust.ARInvoice, CABankTranMatch> result in PXSelectJoin<ARAdjust.ARInvoice,
									LeftJoin<CABankTranMatch, On<CABankTranMatch.docModule, Equal<GL.BatchModule.moduleAR>,
										And<CABankTranMatch.docType, Equal<ARAdjust.ARInvoice.docType>,
										And<CABankTranMatch.docRefNbr, Equal<ARAdjust.ARInvoice.refNbr>,
										And<CABankTranMatch.tranID, NotEqual<Required<CABankTran.tranID>>>>>>,
									LeftJoin<ARAdjust, On<ARAdjust.adjdDocType, Equal<ARAdjust.ARInvoice.docType>, And<ARAdjust.adjdRefNbr, Equal<ARAdjust.ARInvoice.refNbr>,
										And<ARAdjust.released, Equal<boolFalse>, And<ARAdjust.voided, Equal<boolFalse>, And<Where<ARAdjust.adjgDocType, NotEqual<Required<CABankTranAdjustment.adjdDocType>>>>>>>>,
									LeftJoin<CABankTranAdjustment, On<CABankTranAdjustment.adjdModule, Equal<BatchModule.moduleAR>, 
									And<CABankTranAdjustment.adjdDocType, Equal<ARAdjust.ARInvoice.docType>,
										And<CABankTranAdjustment.adjdRefNbr, Equal<ARAdjust.ARInvoice.refNbr>,
										And<CABankTranAdjustment.released, Equal<boolFalse>,
										And<Where<CABankTranAdjustment.tranID,
											NotEqual<Required<CABankTranAdjustment.tranID>>,
											Or<Required<CABankTranAdjustment.adjNbr>, IsNull, 
											Or<CABankTranAdjustment.adjNbr, NotEqual<Required<CABankTranAdjustment.adjNbr>>>>>>>>>>,
									LeftJoin<CABankTran, On<CABankTran.tranID, Equal<CABankTranAdjustment.tranID>>>>>>,
									Where<ARAdjust.ARInvoice.customerID, In2<Search<PX.Objects.AR.Override.BAccount.bAccountID,
										Where<PX.Objects.AR.Override.BAccount.bAccountID, Equal<Required<CABankTran.payeeBAccountID>>, 
										Or<PX.Objects.AR.Override.BAccount.consolidatingBAccountID, Equal<Required<CABankTran.payeeBAccountID>>>>>>,
									And<ARAdjust.ARInvoice.docType, Equal<Required<CABankTranAdjustment.adjdDocType>>,
									And<ARAdjust.ARInvoice.released, Equal<boolTrue>,
									And<ARAdjust.ARInvoice.openDoc, Equal<boolTrue>,
									And<ARAdjust.adjgRefNbr, IsNull,
									And<ARAdjust.ARInvoice.pendingPPD, NotEqual<True>,
									And<Where<CABankTranAdjustment.adjdRefNbr, IsNull, Or<CABankTran.origModule, NotEqual<BatchModule.moduleAR>>>>>>>>>>>
									.Select(graph, currentBankTran.TranID, AdjdDocType, TranID, AdjNbr, AdjNbr, currentBankTran.PayeeBAccountID, currentBankTran.PayeeBAccountID, AdjdDocType))
			{
				ARAdjust.ARInvoice aRInvoice = result;
				CABankTranMatch match = result;

				if (ShouldSkipRecord(match, aRInvoice, adjustments, TranID, AdjNbr, graph, BatchModule.AR))
					{
						continue;
					}

				yield return aRInvoice;
			}
		}
		public static IEnumerable<APAdjust.APInvoice> GetRecordsAP(string AdjdDocType, int? TranID, int? AdjNbr, CABankTran currentBankTran, PXCache adjustments, PXGraph graph)
		{
			foreach (PXResult<APAdjust.APInvoice, CABankTranMatch> result in PXSelectJoin<APAdjust.APInvoice,
										LeftJoin<CABankTranMatch, On<CABankTranMatch.docModule, Equal<GL.BatchModule.moduleAP>,
											And<CABankTranMatch.docType, Equal<APAdjust.APInvoice.docType>,
											And<CABankTranMatch.docRefNbr, Equal<APAdjust.APInvoice.refNbr>,
											And<CABankTranMatch.tranID, NotEqual<Required<CABankTran.tranID>>>>>>,
									   LeftJoin<APAdjust, On<APAdjust.adjdDocType, Equal<APAdjust.APInvoice.docType>,
										   And<APAdjust.adjdRefNbr, Equal<APAdjust.APInvoice.refNbr>, And<APAdjust.released, Equal<boolFalse>>>>,
									   LeftJoin<CABankTranAdjustment, On<CABankTranAdjustment.adjdModule, Equal<BatchModule.moduleAP>, 
										And<CABankTranAdjustment.adjdDocType, Equal<APAdjust.APInvoice.docType>,
										And<CABankTranAdjustment.adjdRefNbr, Equal<APAdjust.APInvoice.refNbr>, And<CABankTranAdjustment.released, Equal<boolFalse>,
											   And<Where<CABankTranAdjustment.tranID,
													NotEqual<Required<CABankTranAdjustment.tranID>>,
													Or<Required<CABankTranAdjustment.adjNbr>, IsNull, 
													Or<CABankTranAdjustment.adjNbr, NotEqual<Required<CABankTranAdjustment.adjNbr>>>>>>>>>>,
									   LeftJoin<AP.Standalone.APPayment, On<AP.Standalone.APPayment.docType, Equal<APAdjust.APInvoice.docType>,
										   And<AP.Standalone.APPayment.refNbr, Equal<APAdjust.APInvoice.refNbr>, And<
										   Where<AP.Standalone.APPayment.docType, Equal<APDocType.prepayment>, Or<AP.Standalone.APPayment.docType, Equal<APDocType.debitAdj>>>>>>,
									   LeftJoin<CABankTran, On<CABankTran.tranID, Equal<CABankTranAdjustment.tranID>>>>>>>,
									   Where<APAdjust.APInvoice.vendorID, Equal<Optional<CABankTran.payeeBAccountID>>, And<APAdjust.APInvoice.docType, Equal<Optional<CABankTranAdjustment.adjdDocType>>,
									   And2<Where<APAdjust.APInvoice.released, Equal<True>, Or<APAdjust.APInvoice.prebooked, Equal<True>>>, And<APAdjust.APInvoice.openDoc, Equal<boolTrue>,
									   And2<Where<CABankTranAdjustment.adjdRefNbr, IsNull, Or<CABankTran.origModule, NotEqual<BatchModule.moduleAP>>>, And<APAdjust.adjgRefNbr, IsNull,
										  And2<Where<AP.Standalone.APPayment.refNbr, IsNull, And<Required<CABankTran.docType>, NotEqual<APDocType.refund>,
										   Or<AP.Standalone.APPayment.refNbr, IsNotNull, And<Required<CABankTran.docType>, Equal<APDocType.refund>,
										   Or<AP.Standalone.APPayment.docType, Equal<APDocType.debitAdj>, And<Required<CABankTran.docType>, Equal<APDocType.check>,
										   Or<AP.Standalone.APPayment.docType, Equal<APDocType.debitAdj>, And<Required<CABankTran.docType>, Equal<APDocType.voidCheck>>>>>>>>>,
										And<Where<APAdjust.APInvoice.docDate, LessEqual<Required<CABankTran.matchingPaymentDate>>, 
											And<APAdjust.APInvoice.finPeriodID, LessEqual<Required<CABankTran.matchingfinPeriodID>>,
											Or<Current<APSetup.earlyChecks>, Equal<True>, 
												And<Required<CABankTran.docType>, NotEqual<APDocType.refund>>>>>>>>>>>>>>
								 .Select(graph, currentBankTran.TranID, TranID, AdjNbr, AdjNbr, currentBankTran.PayeeBAccountID, AdjdDocType, currentBankTran.DocType, currentBankTran.DocType, currentBankTran.DocType, currentBankTran.DocType, currentBankTran.MatchingPaymentDate, currentBankTran.MatchingFinPeriodID, currentBankTran.DocType))
			{
				APAdjust.APInvoice aPInvoice = result;
				CABankTranMatch match = result;

				if(ShouldSkipRecord(match, aPInvoice, adjustments, TranID, AdjNbr, graph, BatchModule.AP))
				{
					continue;
				}

				yield return aPInvoice;
			}
		}
		protected static bool ShouldSkipRecord(CABankTranMatch match, CM.IRegister invoice, PXCache adjustments, int? TranID, int? AdjNbr, PXGraph graph, string module)
		{
			bool skipRecord = IsAlreadyMatched(match, invoice, graph, module) || IsAlreadySelected(adjustments, invoice, TranID, AdjNbr);
			return skipRecord;
		}

		protected static bool IsAlreadyMatched(CABankTranMatch match, CM.IRegister invoice, PXGraph graph, string module)
		{
			if (match.TranID != null && match.LineNbr != null)
			{
				return true;
			}

			foreach (CABankTranMatch cachedMatch in graph.Caches[typeof(CABankTranMatch)].Inserted)
			{
				if (cachedMatch.DocModule == module && cachedMatch.DocType == invoice.DocType && cachedMatch.DocRefNbr == invoice.RefNbr)
				{
					return true;
				}
			}

			return false;
		}

		protected static bool IsAlreadySelected(PXCache adjustments, CM.IRegister invoice, int? TranID, int? AdjNbr)
		{
			foreach (CABankTranAdjustment adj in adjustments.Inserted)
			{
				if ((adj.AdjdDocType == invoice.DocType && adj.AdjdRefNbr == invoice.RefNbr && (adj.TranID != TranID || adj.AdjNbr != AdjNbr)
				|| (adj.AdjdDocType == invoice.DocType && adj.AdjdRefNbr == invoice.RefNbr)))
				{
					return true;
				}
			}

			return false;
		}

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R1)]
		protected static bool ShouldSkipRecord(string docType, string refNbr, int? TranID, int? AdjNbr, CABankTran currentBankTran, PXCache adjustments, PXGraph graph)
		{
			foreach (CABankTranAdjustment adj in adjustments.Inserted)
			{
				if ((adj.AdjdDocType == docType && adj.AdjdRefNbr == refNbr && (adj.TranID != TranID || adj.AdjNbr != AdjNbr)
				|| (adj.AdjdDocType == docType && adj.AdjdRefNbr == refNbr)))
				{
					return true;
				}
			}
			CABankTranMatch match = PXSelect<CABankTranMatch, Where<CABankTranMatch.docModule, Equal<GL.BatchModule.moduleAR>,
				And<CABankTranMatch.docType, Equal<Required<CABankTranMatch.docType>>,
				And<CABankTranMatch.docRefNbr, Equal<Required<CABankTranMatch.docRefNbr>>,
				And<CABankTranMatch.tranID, NotEqual<Required<CABankTran.tranID>>>>>>>.Select(graph, docType, refNbr, currentBankTran.TranID);
			if (match != null) return true;
			return false;
		}
	}

	public static class StatementsMatchingProto
	{
		public static IEnumerable<PXResult<CATranExt, Light.BAccount>> FindDetailMatches<T>(T graph, CABankTran aDetail, IMatchSettings aSettings, decimal aRelevanceTreshold, CATranExt[] aBestMatches)
			where T : PXGraph, ICABankTransactionsDataProvider
		{
			var matchList = new List<PXResult<CATranExt, Light.BAccount>>();
			bool hasBaccount = aDetail.PayeeBAccountID.HasValue;
			bool hasLocation = aDetail.PayeeLocationID.HasValue;
			if (!aDetail.TranEntryDate.HasValue && !aDetail.TranDate.HasValue) return matchList;
			Pair<DateTime, DateTime> tranDateRange = GetDateRangeForMatch(aDetail, aSettings);
			CashAccount cashAcct = PXSelect<CashAccount, Where<CashAccount.cashAccountID, Equal<Required<CashAccount.cashAccountID>>>>.Select(graph, aDetail.CashAccountID);
			CASetup setup = PXSelect<CASetup>.Select(graph);
			string curyID = cashAcct.CuryID;
			CATranExt bestMatch = null;
			int bestMatchesNumber = aBestMatches != null ? aBestMatches.Length : 0;

			if (cashAcct.MatchToBatch == true && cashAcct.ClearingAccount != true)
			{
				bestMatch = MatchToBatch(graph, aDetail, aSettings, aRelevanceTreshold, aBestMatches, matchList, tranDateRange, curyID, bestMatch, bestMatchesNumber, setup.AllowMatchingToUnreleasedBatch ?? false);
			}
			
			foreach (PXResult<CATranExt, Light.BAccount, CATran2, CABankTranMatch2, CABatchDetail> iRes in graph.SearchForMatchingTransactions(aDetail, aSettings, tranDateRange, curyID))
			{
				CABatchDetail batchDetail = iRes;
				if (cashAcct.MatchToBatch == true && batchDetail != null && batchDetail.BatchNbr != null)//exclude transaction included to the batch
				{
					PXResultset<CABatchDetail> matches = graph.SearchForMatchesInCABatches(aDetail.TranType, batchDetail.BatchNbr);
					if (matches == null || matches.Count < 1)//if there is no already matched transaction in that batch
					{
						continue;
					}
				}
				CATranExt iTran = iRes;
				Light.BAccount iPayee = iRes;
				iTran.ReferenceName = iPayee.AcctName;
				//Check updated in cache
				bool matched = false;

				CABankTranMatch match = (CABankTranMatch2)iRes;//existing match to CATran
				match = CheckMatchInCache(graph.Caches[typeof(CABankTranMatch)], iTran, match);
				if (match == null || match.TranID == null)
				{
					match = (CABankTranMatch)iRes;//existing match to batch
					match = CheckBatchMatchInCache(graph.Caches[typeof(CABankTranMatch)], batchDetail, match);
				}
				if (match != null && match.TranID != null)
				{
					if (match.TranID != aDetail.TranID)
					{
						continue;
					}
					matched = true;
				}
				iTran.MatchRelevance = graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail, iTran, aSettings);
				iTran.IsMatched = matched;
				if (iTran.IsMatched == false && iTran.MatchRelevance < aRelevanceTreshold)
					continue;

				if (bestMatchesNumber > 0)
				{
					for (int i = 0; i < bestMatchesNumber; i++)
					{
						if ((aBestMatches[i] == null || aBestMatches[i].MatchRelevance < iTran.MatchRelevance))
						{
							for (int j = bestMatchesNumber - 1; j > i; j--)
							{
								aBestMatches[j] = aBestMatches[j - 1];
							}
							aBestMatches[i] = iTran;
							break;
						}
					}
				}
				else
				{
					if (bestMatch == null || bestMatch.MatchRelevance < iTran.MatchRelevance)
					{
						bestMatch = iTran;
					}
				}

				iTran.IsBestMatch = false;
				matchList.Add(new PXResult<CATranExt, Light.BAccount>(iTran, iPayee));
			}

			//adding matched transactions if they fell out of filters
			foreach (PXResult<CABankTranMatch, CATranExt, Light.BAccount> matches in PXSelectJoin<CABankTranMatch,
				LeftJoin<CATranExt, On<CABankTranMatch.cATranID, Equal<CATranExt.tranID>>,
				LeftJoin<Light.BAccount, On<Light.BAccount.bAccountID, Equal<CATranExt.referenceID>>>>,
				Where<CABankTranMatch.tranID, Equal<Required<CABankTranMatch.tranID>>>>.Select(graph, aDetail.TranID))
			{
				CATranExt matchedTran = matches;
				CABankTranMatch match = matches;
				Light.BAccount bAccount = matches;
				if (matchedTran != null && matchedTran.TranID != null)
				{
					if (matchList.Find((PXResult<CATranExt, Light.BAccount> result) => { CATranExt tran = result;  return tran.TranID == matchedTran.TranID; }) == null)
					{
						matchedTran.MatchRelevance = graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail, matchedTran, aSettings);
						matchedTran.IsMatched = true;
						matchList.Add(new PXResult<CATranExt, Light.BAccount>(matchedTran, bAccount));
					}
				}
				else if (match.DocModule == BatchModule.AP && match.DocType == CATranType.CABatch)
				{
					CABatch batch = PXSelect<CABatch, Where<CABatch.batchNbr, Equal<Required<CABatch.batchNbr>>>>.Select(graph, match.DocRefNbr);
					if (batch != null && batch.BatchNbr != null)
					{
						if (matchList.Find((PXResult<CATranExt, Light.BAccount> result) => { CATranExt tran = result;
							return tran.OrigModule == BatchModule.AP && tran.OrigRefNbr == batch.BatchNbr && tran.OrigTranType == CATranType.CABatch; }) == null)
						{
							matchedTran = new CATranExt();
							batch.CopyTo(matchedTran);
							matchedTran.MatchRelevance = graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail, matchedTran, aSettings);
							matchedTran.IsMatched = true;
							matchList.Add(new PXResult<CATranExt, Light.BAccount>(matchedTran, bAccount));
						}
					}
				}
			}

			if (bestMatchesNumber > 0)
				bestMatch = aBestMatches[0];
			if (bestMatch != null)
			{
				bestMatch.IsBestMatch = true;
			}
			aDetail.CountMatches = matchList.Count;
			return matchList;
		}

		private static CABankTranMatch CheckBatchMatchInCache(PXCache cache, CABatchDetail detail, CABankTranMatch match)
		{
			if (match != null && match.TranID != null)
			{
				var status = cache.GetStatus(match);
				if (status == PXEntryStatus.Deleted) 
					match = null;
				else if (status == PXEntryStatus.Updated)
				{
					CABankTranMatch updatedMatch = (CABankTranMatch)cache.Locate(match);
					if(updatedMatch.DocRefNbr!=match.DocRefNbr || updatedMatch.DocType!=match.DocType || updatedMatch.DocModule!=match.DocModule)
					match = null;
				}
			}
			if ((match == null || match.TranID == null) && detail != null && detail.BatchNbr != null)
			{
				foreach (CABankTranMatch insertedMatch in cache.Inserted)
				{
					if (insertedMatch.DocRefNbr == detail.BatchNbr && insertedMatch.DocType == CATranType.CABatch && insertedMatch.DocModule == BatchModule.AP)
					{
						return insertedMatch;
					}
				}
				foreach (CABankTranMatch insertedMatch in cache.Updated)
				{
					if (insertedMatch.DocRefNbr == detail.BatchNbr && insertedMatch.DocType == CATranType.CABatch && insertedMatch.DocModule == BatchModule.AP)
					{
						return insertedMatch;
					}
				}
			}
			return match;
		}

		private static CABankTranMatch CheckMatchInCache(PXCache cache, CATranExt catran, CABankTranMatch match)
		{
			if (match != null && match.TranID != null)
			{
				var status=cache.GetStatus(match);
				if (status == PXEntryStatus.Deleted || (status == PXEntryStatus.Updated && ((CABankTranMatch)cache.Locate(match)).CATranID != catran.TranID))
					match = null;
			}
			if(match==null || match.TranID==null)
			{
				foreach (CABankTranMatch insertedMatch in cache.Inserted)
				{
					if (insertedMatch.CATranID == catran.TranID)
					{
						return insertedMatch;
					}
				}
				foreach (CABankTranMatch insertedMatch in cache.Updated)
				{
					if (insertedMatch.CATranID == catran.TranID)
					{
						return insertedMatch;
					}
				}
			}
			return match;
		}

		class CABatchWithBaccount
		{
			public CABatch Batch { get; set; }
			public Int32? BaccountID { get; set; }
			public Light.BAccount BAccount { get; set; }
		}
		private static CATranExt MatchToBatch<T>(T graph, CABankTran aDetail, IMatchSettings aSettings, decimal aRelevanceTreshold, CATranExt[] aBestMatches, List<PXResult<CATranExt, Light.BAccount>> matchList, Pair<DateTime, DateTime> tranDateRange, string curyID, CATranExt bestMatch, int bestMatchesNumber, bool allowUnreleased)
			where T : PXGraph, ICABankTransactionsDataProvider
		{
			List<CABatchWithBaccount> batches = new List<CABatchWithBaccount>();
			bool matchFound = false;
			bool referenceNotEqual = false;
			foreach (PXResult<CABatch, CABatchDetail, Light.APPayment, Light.BAccount, CABankTranMatch> iRes in graph.SearchForMatchingCABatches(aDetail, tranDateRange, curyID, allowUnreleased))
			{
				CABankTranMatch existingMatch = iRes;
				CABatch batch = iRes;
				Light.APPayment payment = iRes;
				Light.BAccount bAccount = iRes;
				if (batches.Count == 0 || batches[batches.Count - 1].Batch.BatchNbr != batch.BatchNbr)
				{
					if (batches.Count > 0)
					{
						if (matchFound)
						{
							batches.RemoveAt(batches.Count - 1);
						}
						if (referenceNotEqual)
						{
							batches[batches.Count - 1].BaccountID = null;
						}
					}
					matchFound = false;
					referenceNotEqual = false;
					batches.Add(new CABatchWithBaccount() { Batch = batch, BaccountID = payment.VendorID, BAccount = bAccount });

				}
				if (existingMatch != null && existingMatch.TranID.HasValue)
				{
					matchFound = true;
				}
				if (batches.Count == 0 || batches[batches.Count - 1].BaccountID != payment.VendorID)
				{
					referenceNotEqual = true;
				}
			}
			if (batches.Count > 0)
			{
				if (referenceNotEqual)
				{
					batches[batches.Count - 1].BaccountID = null;
				}
				if (matchFound)
				{
					batches.RemoveAt(batches.Count - 1);
				}
			}
			foreach (CABatchWithBaccount batch in batches)
			{
				CATranExt iTran = new CATranExt();
				batch.Batch.CopyTo(iTran);
				iTran.ReferenceID = batch.BaccountID;
				//Check updated in cache
				bool matched = false;
				var matchedRows = graph.SearchForTranMatchForCABatch(batch.Batch.BatchNbr);
				if (matchedRows.Count != 0)
				{
					CABankTranMatch match = matchedRows;
					if (match.TranID != aDetail.TranID)
					{
						continue;
					}
					matched = true;
				}
				iTran.MatchRelevance = graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail, iTran, aSettings);
				iTran.IsMatched = matched;
				if (iTran.IsMatched == false && iTran.MatchRelevance < aRelevanceTreshold)
					continue;

				if (bestMatchesNumber > 0)
				{
					for (int i = 0; i < bestMatchesNumber; i++)
					{
						if ((aBestMatches[i] == null || aBestMatches[i].MatchRelevance < iTran.MatchRelevance))
						{
							for (int j = bestMatchesNumber - 1; j > i; j--)
							{
								aBestMatches[j] = aBestMatches[j - 1];
							}
							aBestMatches[i] = iTran;
							break;
						}
					}
				}
				else
				{
					if (bestMatch == null || bestMatch.MatchRelevance < iTran.MatchRelevance)
					{
						bestMatch = iTran;
					}
				}

				iTran.IsBestMatch = false;
				matchList.Add(new PXResult<CATranExt, Light.BAccount>(iTran, batch.BAccount));
			}
			return bestMatch;
		}
		public static decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CATran aTran, IMatchSettings aSettings)
		{
			decimal relevance = Decimal.Zero;
			decimal[] weights = { 0.1m, 0.7m, 0.2m };
			double sigma = 50.0;
			double meanValue = -7.0;
			if (aSettings != null)
			{
				if (aSettings.DateCompareWeight.HasValue && aSettings.RefNbrCompareWeight.HasValue && aSettings.PayeeCompareWeight.HasValue)
				{
					Decimal totalWeight = (aSettings.DateCompareWeight.Value + aSettings.RefNbrCompareWeight.Value + aSettings.PayeeCompareWeight.Value);
					if (totalWeight != Decimal.Zero)
					{
						weights[0] = aSettings.DateCompareWeight.Value / totalWeight;
						weights[1] = aSettings.RefNbrCompareWeight.Value / totalWeight;
						weights[2] = aSettings.PayeeCompareWeight.Value / totalWeight;
					}
				}
				if (aSettings.DateMeanOffset.HasValue)
					meanValue = (double)aSettings.DateMeanOffset.Value;
				if (aSettings.DateSigma.HasValue)
					sigma = (double)aSettings.DateSigma.Value;
			}
			bool looseCompare = false;
			relevance += graph.GetService<ICABankTransactionsRepository>().CompareDate(graph, aDetail, aTran, meanValue, sigma) * weights[0];
			relevance += graph.GetService<ICABankTransactionsRepository>().CompareRefNbr(graph, aDetail, aTran, looseCompare, aSettings) * weights[1];
			relevance += graph.GetService<ICABankTransactionsRepository>().ComparePayee(graph, aDetail, aTran) * weights[2];
			return relevance;
		}

		public static decimal CompareDate(CABankTran aDetail, CATran aTran, Double meanValue, Double sigma)
		{
		    return CompareDate(aDetail, aTran.TranDate, meanValue, sigma);
		}

		public static decimal CompareDate(CABankTran aDetail, DateTime? tranDate, Double meanValue, Double sigma)
	    {
			if (tranDate == null) return 0m;

	        TimeSpan diff1 = (aDetail.TranDate.Value - tranDate.Value);
	        TimeSpan diff2 = aDetail.TranEntryDate.HasValue ? (aDetail.TranEntryDate.Value - tranDate.Value) : diff1;
	        TimeSpan diff = diff1.Duration() < diff2.Duration() ? diff1 : diff2;
	        Double sigma2 = (sigma * sigma);
	        if (sigma2 < 1.0)
	        {
	            sigma2 = 0.25; //Corresponds to 0.5 day
	        }
	        decimal res = (decimal)Math.Exp(-(Math.Pow(diff.TotalDays - meanValue, 2.0) / (2 * sigma2))); //Normal Distribution 
	        return res > 0 ? res : 0.0m;
	    }

        public static decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, CATran aTran, bool looseCompare, IMatchSettings matchSettings)
		{
		    return CompareRefNbr(graph, aDetail, aTran.ExtRefNbr, looseCompare, matchSettings);
		}

	    public static decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, string extRefNbr, bool looseCompare, IMatchSettings matchSettings)
	    {
	        if (looseCompare)
	            return graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail.ExtRefNbr, extRefNbr, false, matchSettings.EmptyRefNbrMatching ?? true);
	        else
	            return graph.GetService<ICABankTransactionsRepository>().EvaluateTideMatching(graph, aDetail.ExtRefNbr, extRefNbr, false, matchSettings.EmptyRefNbrMatching ?? true);
	    }

        public static decimal ComparePayee(PXGraph graph, CABankTran aDetail, CATran aTran)
		{
			return graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail.PayeeName, aTran.ReferenceName, false);
		}

		public static Pair<DateTime, DateTime> GetDateRangeForMatch(CABankTran aDetail, IMatchSettings aSettings)
		{
			DateTime tranDateStart = aDetail.TranEntryDate ?? aDetail.TranDate.Value;
			DateTime tranDateEnd = aDetail.TranEntryDate ?? aDetail.TranDate.Value;
			bool isReceipt = (aDetail.DrCr == CADrCr.CADebit);
			tranDateStart = tranDateStart.AddDays(-(isReceipt ? aSettings.ReceiptTranDaysBefore.Value : aSettings.DisbursementTranDaysBefore.Value));
			tranDateEnd = tranDateEnd.AddDays((isReceipt ? aSettings.ReceiptTranDaysAfter.Value : aSettings.DisbursementTranDaysAfter.Value));
			if (tranDateEnd < tranDateStart)
			{
				DateTime swap = tranDateStart;
				tranDateStart = tranDateEnd;
				tranDateEnd = swap;
			}
			return new Pair<DateTime, DateTime>(tranDateStart, tranDateEnd);
		}

		/// <summary>
		/// Return dates for invoice matching:
		///	 1. first = Start of Due Date;
		///	 2. second = End of Due Date;
		///  3. third = Last Discout Date;
		/// </summary>
		/// <param name="aDetail"></param>
		/// <param name="aSettings"></param>
		/// <returns></returns>
		public static Triplet<DateTime, DateTime, DateTime> GetInvoiceDateRangeForMatch(CABankTran aDetail, IMatchSettings aSettings)
		{
			DateTime invDueDateStart = aDetail.TranEntryDate ?? aDetail.TranDate.Value;
			DateTime invDueDateEnd = aDetail.TranEntryDate ?? aDetail.TranDate.Value;
			DateTime invLastDiscountDate = aDetail.TranEntryDate ?? aDetail.TranDate.Value;

			invDueDateStart = invDueDateStart.AddDays(-aSettings.DaysAfterInvoiceDueDate ?? 0);
			invDueDateEnd = invDueDateEnd.AddDays(aSettings.DaysBeforeInvoiceDueDate ?? 0);
			invLastDiscountDate = invLastDiscountDate.AddDays(aSettings.DaysBeforeInvoiceDiscountDate ?? 0);
			if (invDueDateEnd < invDueDateStart)
			{
				DateTime swap = invDueDateStart;
				invDueDateStart = invDueDateEnd;
				invDueDateEnd = swap;
			}
			return new Triplet<DateTime, DateTime, DateTime>(invDueDateStart, invDueDateEnd, invLastDiscountDate);
		}

		public static void SetDocTypeList(PXCache cache, CABankTran Row)
		{
			CABankTran detail = Row;

			List<string> AllowedValues = new List<string>();
			List<string> AllowedLabels = new List<string>();

			if (detail.OrigModule == GL.BatchModule.AP)
			{
				if (detail.DocType == APDocType.Refund)
				{
					PXDefaultAttribute.SetDefault<CABankTranAdjustment.adjdDocType>(cache, APDocType.DebitAdj);
					PXStringListAttribute.SetList<CABankTranAdjustment.adjdDocType>(cache, null, new string[] { APDocType.DebitAdj, APDocType.Prepayment }, new string[] { AP.Messages.DebitAdj, AP.Messages.Prepayment });
				}
				else if (detail.DocType == APDocType.Prepayment)
				{
					PXDefaultAttribute.SetDefault<CABankTranAdjustment.adjdDocType>(cache, APDocType.Invoice);
					PXStringListAttribute.SetList<CABankTranAdjustment.adjdDocType>(cache, null, new string[] { APDocType.Invoice, APDocType.CreditAdj }, new string[] { AP.Messages.Invoice, AP.Messages.CreditAdj });
				}
				else if (detail.DocType == APDocType.Check)
				{
					PXDefaultAttribute.SetDefault<CABankTranAdjustment.adjdDocType>(cache, APDocType.Invoice);
					PXStringListAttribute.SetList<CABankTranAdjustment.adjdDocType>(cache, null, new string[] { APDocType.Invoice, APDocType.DebitAdj, APDocType.CreditAdj, APDocType.Prepayment }, new string[] { AP.Messages.Invoice, AP.Messages.DebitAdj, AP.Messages.CreditAdj, AP.Messages.Prepayment });
				}
				else
				{
					PXDefaultAttribute.SetDefault<CABankTranAdjustment.adjdDocType>(cache, APDocType.Invoice);
					PXStringListAttribute.SetList<CABankTranAdjustment.adjdDocType>(cache, null, new string[] { APDocType.Invoice, APDocType.CreditAdj, APDocType.Prepayment }, new string[] { AP.Messages.Invoice, AP.Messages.CreditAdj, AP.Messages.Prepayment });
				}
			}
			else if (detail.OrigModule == GL.BatchModule.AR)
			{

				if (detail.DocType == ARDocType.Refund)
				{
					PXDefaultAttribute.SetDefault<CABankTranAdjustment.adjdDocType>(cache, ARDocType.CreditMemo);
					PXStringListAttribute.SetList<CABankTranAdjustment.adjdDocType>(cache, null, new string[] { ARDocType.CreditMemo, ARDocType.Payment, ARDocType.Prepayment }, new string[] { AR.Messages.CreditMemo, AR.Messages.Payment, AR.Messages.Prepayment });
				}
				else if (detail.DocType == ARDocType.Payment || detail.DocType == ARDocType.VoidPayment)
				{
					PXDefaultAttribute.SetDefault<CABankTranAdjustment.adjdDocType>(cache, ARDocType.Invoice);
					PXStringListAttribute.SetList<CABankTranAdjustment.adjdDocType>(cache, null,
						new string[] { ARDocType.Invoice, ARDocType.DebitMemo, ARDocType.CreditMemo, ARDocType.FinCharge },
						new string[] { AR.Messages.Invoice, AR.Messages.DebitMemo, AR.Messages.CreditMemo, AR.Messages.FinCharge });
				}
				else
				{
					PXDefaultAttribute.SetDefault<CABankTranAdjustment.adjdDocType>(cache, ARDocType.Invoice);
					PXStringListAttribute.SetList<CABankTranAdjustment.adjdDocType>(cache, null,
						new string[] { ARDocType.Invoice, ARDocType.DebitMemo, ARDocType.FinCharge },
						new string[] { AR.Messages.Invoice, AR.Messages.DebitMemo, AR.Messages.FinCharge });
				}
			}
		}

		public static decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran, IMatchSettings aSettings)
		{
			decimal relevance = Decimal.Zero;
			decimal[] weights = { 0.1m, 0.7m, 0.2m };
			double sigma = 50.0;
			double meanValue = -7.0;
			if (aSettings != null)
			{
				if (aSettings.InvoiceDateCompareWeight.HasValue && aSettings.InvoiceRefNbrCompareWeight.HasValue && aSettings.InvoicePayeeCompareWeight.HasValue)
				{
					Decimal totalWeight = (aSettings.InvoiceDateCompareWeight.Value + 
										aSettings.InvoiceRefNbrCompareWeight.Value + aSettings.InvoicePayeeCompareWeight.Value);
					if (totalWeight != Decimal.Zero)
					{
						weights[0] = aSettings.InvoiceDateCompareWeight.Value / totalWeight;
						weights[1] = aSettings.InvoiceRefNbrCompareWeight.Value / totalWeight;
						weights[2] = aSettings.InvoicePayeeCompareWeight.Value / totalWeight;
					}
				}
				if (aSettings.AveragePaymentDelay.HasValue)
					meanValue = (double)aSettings.AveragePaymentDelay.Value;
				if (aSettings.InvoiceDateSigma.HasValue)
					sigma = (double)aSettings.InvoiceDateSigma.Value;
			}
			bool looseCompare = false;
			relevance += graph.GetService<ICABankTransactionsRepository>().CompareDate(graph, aDetail, aTran, meanValue, sigma) * weights[0];
			relevance += graph.GetService<ICABankTransactionsRepository>().CompareRefNbr(graph, aDetail, aTran, looseCompare) * weights[1];
			relevance += graph.GetService<ICABankTransactionsRepository>().ComparePayee(graph, aDetail, aTran) * weights[2];
			return relevance;
		}

	    public static decimal CompareRefNbr(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran, bool looseCompare)
	    {
	        decimal relevance1 = Decimal.Zero;
	        decimal relevance2 = Decimal.Zero;
	        if (looseCompare)
	        {
	            relevance1 = String.IsNullOrEmpty(aDetail.InvoiceInfo) && String.IsNullOrEmpty(aTran.ExtRefNbr) ? Decimal.Zero : graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail.InvoiceInfo, aTran.ExtRefNbr, false);
	            relevance2 = String.IsNullOrEmpty(aDetail.InvoiceInfo) && String.IsNullOrEmpty(aTran.OrigRefNbr) ? Decimal.Zero : graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail.InvoiceInfo, aTran.OrigRefNbr, false);
	        }
	        else
	        {
	            relevance1 = String.IsNullOrEmpty(aDetail.InvoiceInfo) && String.IsNullOrEmpty(aTran.ExtRefNbr) ? Decimal.Zero : graph.GetService<ICABankTransactionsRepository>().EvaluateTideMatching(graph, aDetail.InvoiceInfo, aTran.ExtRefNbr, false);
	            relevance2 = String.IsNullOrEmpty(aDetail.InvoiceInfo) && String.IsNullOrEmpty(aTran.OrigRefNbr) ? Decimal.Zero : graph.GetService<ICABankTransactionsRepository>().EvaluateTideMatching(graph, aDetail.InvoiceInfo, aTran.OrigRefNbr, false);
	        }
	        return (relevance1 > relevance2 ? relevance1 : relevance2);
	    }

	    public static decimal EvaluateMatching(PXGraph graph, CABankTran aDetail, CABankTranExpenseDetailMatch expenseMath, IMatchSettings aSettings)
        {
	        decimal relevance = Decimal.Zero;
			decimal[] weights = { 0.2m, 0.7m, 0.1m };
			double dateSigma = 50.0;
	        double dateMeanValue = -7.0;
	        if (aSettings != null)
	        {
	            if (aSettings.DateCompareWeight.HasValue && aSettings.RefNbrCompareWeight.HasValue && aSettings.AmountWeight.HasValue)
	            {
	                Decimal totalWeight = (aSettings.DateCompareWeight.Value + aSettings.RefNbrCompareWeight.Value + aSettings.AmountWeight.Value);
	                if (totalWeight != Decimal.Zero)
	                {
	                    weights[0] = aSettings.DateCompareWeight.Value / totalWeight;
	                    weights[1] = aSettings.RefNbrCompareWeight.Value / totalWeight;
		                weights[2] = aSettings.AmountWeight.Value / totalWeight;
					}
	            }

	            if (aSettings.DateMeanOffset.HasValue)
		            dateMeanValue = (double)aSettings.DateMeanOffset.Value;
	            if (aSettings.DateSigma.HasValue)
		            dateSigma = (double)aSettings.DateSigma.Value;
			}
	        bool looseCompare = false;

	        relevance += graph.GetService<ICABankTransactionsRepository>().CompareDate(graph, aDetail, expenseMath, dateMeanValue, dateSigma) * weights[0];
	        relevance += graph.GetService<ICABankTransactionsRepository>().CompareRefNbr(graph, aDetail, expenseMath, looseCompare, aSettings) * weights[1];
	        relevance += graph.GetService<ICABankTransactionsRepository>().CompareExpenseReceiptAmount(graph, aDetail, expenseMath, aSettings) * weights[2];

			return relevance;
	    }

		public static decimal ComparePayee(PXGraph graph, CABankTran aDetail, CABankTranInvoiceMatch aTran)
		{
			return graph.GetService<ICABankTransactionsRepository>().EvaluateMatching(graph, aDetail.PayeeName, aTran.ReferenceName, false);
		}

		public static void UpdateSourceDoc(PXGraph graph, CATran aTran, DateTime? clearDate)
        {
            aTran.Cleared = true;
            aTran.ClearDate = clearDate ?? aTran.TranDate;
            graph.Caches[typeof(CATran)].Update(aTran);

            switch (aTran.OrigModule)
            {
                case GL.BatchModule.AP:
                    if (aTran.OrigTranType != GLTranType.GLEntry)
                    {
                        APPayment payment = PXSelect<APPayment, Where<APPayment.docType, Equal<Required<APPayment.docType>>,
                                            And<APPayment.refNbr, Equal<Required<APPayment.refNbr>>>>>.Select(graph, aTran.OrigTranType, aTran.OrigRefNbr);
                        payment.ClearDate = aTran.ClearDate;
                        payment.Cleared = aTran.Cleared;
                        graph.Caches[typeof(APPayment)].Update(payment);
                    }
                    break;
                case GL.BatchModule.AR:
                    if (aTran.OrigTranType != GLTranType.GLEntry)
                    {
                        ARPayment payment = PXSelect<ARPayment, Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
                                            And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>.Select(graph, aTran.OrigTranType, aTran.OrigRefNbr);
                        payment.ClearDate = aTran.ClearDate;
                        payment.Cleared = aTran.Cleared;
                        graph.Caches[typeof(ARPayment)].Update(payment);
                    }
                    break;
                case GL.BatchModule.CA:
                    {
                        if (aTran.OrigTranType == CA.CATranType.CADeposit)
                        {
                            CATran notClearedTran = PXSelect<CATran, Where<CATran.origModule, Equal<Required<CATran.origModule>>,
                                                                And<CATran.origTranType, Equal<Required<CATran.origTranType>>,
                                                                And<CATran.origRefNbr, Equal<Required<CATran.origRefNbr>>,
                                                                And<CATran.cleared, Equal<False>,
                                                                And<CATran.tranID, NotEqual<Required<CATran.tranID>>>>>>>>.Select(graph, aTran.OrigModule, aTran.OrigTranType, aTran.OrigRefNbr, aTran.TranID);
                            if (notClearedTran == null)
                            {
                                CADeposit payment = PXSelect<CADeposit, Where<CADeposit.tranType, Equal<Required<CADeposit.tranType>>,
                                            And<CADeposit.refNbr, Equal<Required<CADeposit.refNbr>>>>>.Select(graph, aTran.OrigTranType, aTran.OrigRefNbr);

                                payment.ClearDate = aTran.ClearDate;
                                payment.Cleared = aTran.Cleared;
                                graph.Caches[typeof(CADeposit)].Update(payment);
                            }
                        }

                        if (aTran.OrigTranType == CA.CATranType.CAAdjustment)
                        {
                            CATran notClearedTran = PXSelect<CATran, Where<CATran.origModule, Equal<CATran.origModule>,
                                                                And<CATran.origTranType, Equal<CATran.origTranType>,
                                                                And<CATran.origRefNbr, Equal<CATran.origRefNbr>,
                                                                And<CATran.cleared, Equal<False>,
                                                                And<CATran.tranID, NotEqual<CATran.tranID>>>>>>>.Select(graph, aTran.OrigModule, aTran.OrigTranType, aTran.OrigRefNbr, aTran.TranID);
                            if (notClearedTran == null)
                            {
                                CAAdj payment = PXSelect<CAAdj, Where<CAAdj.adjTranType, Equal<Required<CAAdj.adjTranType>>,
                                                And<CAAdj.adjRefNbr, Equal<Required<CAAdj.adjRefNbr>>>>>.Select(graph, aTran.OrigTranType, aTran.OrigRefNbr);

                                payment.ClearDate = aTran.ClearDate;
                                payment.Cleared = aTran.Cleared;
                                graph.Caches[typeof(CAAdj)].Update(payment);
                            }
                        }

                        if (aTran.OrigTranType == CA.CATranType.CATransferIn || aTran.OrigTranType == CA.CATranType.CATransferOut)
                        {
                            CATransfer payment = PXSelect<CATransfer, Where<CATransfer.transferNbr, Equal<Required<CATransfer.transferNbr>>>>.Select(graph, aTran.OrigRefNbr);

                            if (payment.TranIDIn == aTran.TranID)
                            {
                                payment.ClearDateIn = aTran.ClearDate;
                                payment.ClearedIn = aTran.Cleared;
                            }

                            if (payment.TranIDOut == aTran.TranID)
                            {
                                payment.ClearDateOut = aTran.ClearDate;
                                payment.ClearedOut = aTran.Cleared;
                            }
                            graph.Caches[typeof(CATransfer)].Update(payment);
                        }
                        if (aTran.OrigTranType == CA.CATranType.CATransferExp)
                        {
							CAExpense expense = PXSelect<CAExpense, Where<CAExpense.cashTranID, Equal<Required<CATran.tranID>>, And<CAExpense.cleared, NotEqual<True>>>>.Select(graph, aTran.TranID);
							expense.Cleared = aTran.Cleared;
							expense.ClearDate = aTran.ClearDate;
							graph.Caches[typeof(CAExpense)].Update(expense);
                        }
                    }
                    break;
            }
        }

		public static object FindInvoiceByInvoiceInfo<T>(T graph, CABankTran aRow, out string Module)
			where T : PXGraph, ICABankTransactionsDataProvider
		{
			object res = null;
			Module = String.Empty;
			//ignore cases when there are more than 1 invoice found, for now
			if (aRow.DrCr == CA.CADrCr.CACredit)
			{
				PXResult<APInvoice, APAdjust, APPayment> invResultAP = graph.FindAPInvoiceByInvoiceInfo(aRow);
				if (invResultAP != null)
				{
					APInvoice invoiceToApply = invResultAP;
					APAdjust unreleasedAjustment = invResultAP;
					APPayment invoicePrepayment = invResultAP;
					var apSetup = (APSetup)graph.Caches[typeof(APSetup)].Current;

					if (invoiceToApply.Released == false && invoiceToApply.Prebooked == false)
					{
						throw new PXSetPropertyException(Messages.ApplicationInvoiceIsNotReleased, aRow.InvoiceInfo);
					}

					if (invoiceToApply.OpenDoc == false)
					{
						throw new PXSetPropertyException(Messages.APPaymentApplicationInvoiceIsClosed, aRow.InvoiceInfo);
					}

					if (apSetup.EarlyChecks == false && (invoiceToApply.DocDate > aRow.TranDate))
					{
						throw new PXSetPropertyException(Messages.APPaymentApplicationInvoiceDateIsGreaterThenPaymentDate, aRow.InvoiceInfo);
					}

					if (unreleasedAjustment != null && string.IsNullOrEmpty(unreleasedAjustment.AdjgRefNbr) == false)
					{
						throw new PXSetPropertyException(Messages.APPaymentApplicationInvoiceUnrealeasedApplicationExist, aRow.InvoiceInfo);
					}

					if (aRow.DrCr == CADrCr.CACredit && invoicePrepayment != null && string.IsNullOrEmpty(invoicePrepayment.RefNbr) == false)
					{
						throw new PXSetPropertyException(Messages.APPaymentApplicationInvoiceIsPartOfPrepaymentOrDebitAdjustment, aRow.InvoiceInfo);
					}

					res = invoiceToApply;

					Module = GL.BatchModule.AP;
				}
			}

			if (aRow.DrCr == CA.CADrCr.CADebit)
			{
				PXResult<ARInvoice, ARAdjust> invResultAR = graph.FindARInvoiceByInvoiceInfo(aRow);
				if (invResultAR != null)
				{
					ARInvoice invoiceToApply = invResultAR;
					ARAdjust unreleasedAjustment = invResultAR;
					if (invoiceToApply.Released == false)
					{
						throw new PXSetPropertyException(Messages.ApplicationInvoiceIsNotReleased, aRow.InvoiceInfo);
					}

					if (invoiceToApply.OpenDoc == false)
					{
						throw new PXSetPropertyException(Messages.ARPaymentApplicationInvoiceIsClosed, aRow.InvoiceInfo);
					}

					if (invoiceToApply.DocDate > aRow.TranDate)
					{
						throw new PXSetPropertyException(Messages.ARPaymentApplicationInvoiceDateIsGreaterThenPaymentDate, aRow.InvoiceInfo);
					}

					if (unreleasedAjustment != null && string.IsNullOrEmpty(unreleasedAjustment.AdjgRefNbr) == false)
					{
						throw new PXSetPropertyException(Messages.ARPaymentApplicationInvoiceUnrealeasedApplicationExist, aRow.InvoiceInfo);
					}

					res = invoiceToApply;

					Module = GL.BatchModule.AR;
				}
			}

			return res;
		}

		public static bool CheckRuleMatches(CABankTran transaction, CABankTranRule rule)
		{
			if (rule.BankDrCr != transaction.DrCr)
				return false;

			bool match = true;

			decimal tranAmout = Math.Abs(transaction.CuryTranAmt ?? 0.0m);
			switch (rule.AmountMatchingMode)
			{
				case MatchingMode.Equal:
					match &= tranAmout == rule.CuryTranAmt;
					break;
				case MatchingMode.Between:
					match &= rule.CuryTranAmt <= tranAmout && tranAmout <= rule.MaxCuryTranAmt;
					break;
			}

			if (rule.BankTranCashAccountID != null && match)
			{
				match &= transaction.CashAccountID == rule.BankTranCashAccountID;
			}

			if (rule.TranCuryID != null && match)
			{
				match &= (transaction.CuryID ?? "").Trim() == rule.TranCuryID.Trim();
			}

			if (String.IsNullOrWhiteSpace(rule.TranCode) == false && match)
			{
				match &= String.Equals(transaction.TranCode ?? "", rule.TranCode, StringComparison.CurrentCultureIgnoreCase);
			}

			if (String.IsNullOrEmpty(rule.BankTranDescription) == false && match)
			{
				match &= rule.Regex.IsMatch(transaction.TranDesc ?? "");
			}

			return match;
		}
	}
}
