using System;
using System.Collections;
using System.Collections.Generic;
using PX.SM;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.Common.Utility;
using PX.Objects.CR;
using PX.Objects.GL;


namespace PX.Objects.CA
{
    public class CABankTransactionsEnq : PXGraph<CABankTransactionsEnq>
    {
        #region Internal Classes definitions
        [Serializable]
        public partial class Filter : IBqlTable
        {
            #region StartDate
            public abstract class startDate : PX.Data.BQL.BqlDateTime.Field<startDate> { }
            protected DateTime? _StartDate;
            [PXDBDate()]
            [PXDefault()]
            [PXUIField(DisplayName = "From Date", Visibility = PXUIVisibility.Visible, Visible = true, Enabled = true)]
            public virtual DateTime? StartDate
            {
                get
                {
                    return this._StartDate;
                }
                set
                {
                    this._StartDate = value;
                }
            }
            #endregion
            #region CashAccountID
            public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
            protected Int32? _CashAccountID;
            [CashAccount()]
            [PXDefault]
            public virtual int? CashAccountID
            {
                get
                {
                    return this._CashAccountID;
                }
                set
                {
                    this._CashAccountID = value;
                }
            }
            #endregion
            #region EndDate
            public abstract class endDate : PX.Data.BQL.BqlDateTime.Field<endDate> { }
            protected DateTime? _EndDate;
            [PXDBDate()]
            [PXDefault(typeof(AccessInfo.businessDate))]
            [PXUIField(DisplayName = "To Date", Visibility = PXUIVisibility.Visible, Visible = true, Enabled = true)]
            public virtual DateTime? EndDate
            {
                get
                {
                    return this._EndDate;
                }
                set
                {
                    this._EndDate = value;
                }
            }
            #endregion
            #region HeaderRefNbr
            public abstract class headerRefNbr : PX.Data.BQL.BqlString.Field<headerRefNbr> { }
            protected String _HeaderRefNbr;
            [PXString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
            [PXSelector(typeof(Search<CABankTranHeader.refNbr, Where<CABankTranHeader.cashAccountID, Equal<Current<Filter.cashAccountID>>,
                                                                And<CABankTranHeader.tranType, Equal<Current<Filter.tranType>>>>>),
                                                                typeof(CABankTranHeader.docDate))]
            [PXUIField(DisplayName = "Statement Nbr.")]
            public virtual String HeaderRefNbr
            {
                get
                {
                    return this._HeaderRefNbr;
                }
                set
                {
                    this._HeaderRefNbr = value;
                }
            }
            #endregion
            #region TranType
            public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }
            protected String _TranType;
            [PXString(1, IsFixed = true)]
            [PXDefault(typeof(CABankTranType.statement))]
            [CABankTranType.List()]
            [PXUIField(DisplayName = "Statement Type", Visible = false, Visibility = PXUIVisibility.SelectorVisible)]
            public virtual String TranType
            {
                get
                {
                    return this._TranType;
                }
                set
                {
                    this._TranType = value;
                }
            }
            #endregion
        }

		[PXHidden]
		public class CABankTranHistory : IBqlTable
		{
			#region TranID
			public abstract class tranID : PX.Data.BQL.BqlInt.Field<tranID> { }

			/// <summary>
			/// The unique identifier of the CA bank transaction.
			/// This field is the key field.
			/// </summary>
			[PXDBInt(IsKey = true)]
			[PXUIField(DisplayName = "ID", Visible = false)]
			public virtual int? TranID
			{
				get;
				set;
			}
			#endregion
			#region CATranID
			public abstract class cATranID : PX.Data.BQL.BqlLong.Field<cATranID>
			{
			}
			[PXDBLong(IsKey = true)]
			[PXUIField(DisplayName = "Document Number")]
			public virtual long? CATranID
			{
				get;
				set;
			}
			#endregion
			#region CashAccountID
			public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }

			/// <summary>
			/// The cash account specified on the bank statement for which you want to upload bank transactions.
			/// This field is a part of the compound key of the document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="CashAccount.CashAccountID"/> field.
			/// </value>
			[GL.CashAccount]
			public virtual int? CashAccountID
			{
				get;
				set;
			}
			#endregion
			#region TranType
			public abstract class tranType : PX.Data.BQL.BqlString.Field<tranType> { }

			/// <summary>
			/// The type of the bank tansaction.
			///  The field is linked to the <see cref="CABankTranHeader.TranType"/> field.
			/// </summary>
			/// <value>
			/// The field can have one of the following values:
			/// <c>"S"</c>: Bank Statement Import,
			/// <c>"I"</c>: Payments Import
			/// </value>
			[PXDBString(1, IsFixed = true)]
			[CABankTranType.List]
			[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
			public virtual string TranType
			{
				get;
				set;
			}
			#endregion
			#region HeaderRefNbr
			public abstract class headerRefNbr : PX.Data.BQL.BqlString.Field<headerRefNbr> { }

			/// <summary>
			/// The reference number of the imported bank statement (<see cref="CABankTranHeader">CABankTranHeader</see>),
			/// which the system generates automatically in accordance with the numbering sequence assigned to statements on the Cash Management Preferences (CA101000) form.
			/// </summary>
			[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
			[PXUIField(DisplayName = "Statement Nbr.")]
			public virtual string HeaderRefNbr
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
			[PXUIField(DisplayName = "Tran. Date")]
			public virtual DateTime? TranDate
			{
				get;
				set;
			}

			#endregion
			#region Processed
			public abstract class processed : PX.Data.BQL.BqlBool.Field<processed> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that this bank transaction is processed.
			/// </summary>
			[PXDBBool]
			[PXUIField(DisplayName = "Processed")]
			public virtual bool? Processed
			{
				get;
				set;
			}
			#endregion

			#region MatchedModule
			public abstract class matchedModule : PX.Data.BQL.BqlString.Field<matchedModule>
			{
			}
			[PXDBString(2, IsFixed = true)]
			[PXUIField(DisplayName = "Module")]
			[BatchModule.FullList]
			public virtual string MatchedModule
			{
				get;
				set;
			}
			#endregion
			#region MatchedDocType
			public abstract class matchedDocType : PX.Data.BQL.BqlString.Field<matchedDocType>
			{
			}
			[PXDBString(3, IsFixed = true)]
			[PXUIField(DisplayName = Messages.Type)]
			[CAAPARTranType.ListByModule(typeof(matchedModule))]
			public virtual string MatchedDocType
			{
				get;
				set;
			}
			#endregion
			#region MatchedRefNbr
			public abstract class matchedRefNbr : PX.Data.BQL.BqlString.Field<matchedRefNbr>
			{
			}
			[PXDBString(15, IsUnicode = true)]
			[PXUIField(DisplayName = Messages.RefNbr)]
			public virtual string MatchedRefNbr
			{
				get;
				set;
			}
			#endregion
			#region MatchedReferenceID
			public abstract class matchedReferenceID : PX.Data.BQL.BqlInt.Field<matchedReferenceID>
			{
			}
			[PXDBInt]
			[PXSelector(typeof(BAccountR.bAccountID),
				SubstituteKey = typeof(BAccountR.acctCD),
				DescriptionField = typeof(BAccountR.acctName))]
			[PXUIField(DisplayName = "Business Account")]
			public virtual int? MatchedReferenceID
			{
				get;
				set;
			}
			#endregion

			#region CuryMatchedDebitAmt
			public abstract class curyMatchedDebitAmt : PX.Data.BQL.BqlDecimal.Field<curyMatchedDebitAmt>
			{
			}
			[PXDecimal]
			[PXUIField(DisplayName = "Matched Receipt")]
			public virtual decimal? CuryMatchedDebitAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryMatchedCreditAmt
			public abstract class curyMatchedCreditAmt : PX.Data.BQL.BqlDecimal.Field<curyMatchedCreditAmt>
			{
			}
			[PXDecimal]
			[PXUIField(DisplayName = "Matched Disbursement")]
			public virtual decimal? CuryMatchedCreditAmt
			{
				get;
				set;
			}
			#endregion

			#region ExtTranID
			public abstract class extTranID : PX.Data.BQL.BqlString.Field<extTranID> { }

			/// <summary>
			/// The external identifier of the transaction.
			/// </summary>
			[PXDBString(255, IsUnicode = true)]
			[PXUIField(DisplayName = "Ext. Tran. ID", Visible = false)]
			public virtual string ExtTranID
			{
				get;
				set;
			}
			#endregion
			#region ExtRefNbr
			public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }

			/// <summary>
			/// The external reference number of the transaction.
			/// </summary>
			[PXDBString(40, IsUnicode = true)]
			[PXUIField(DisplayName = "Ext. Ref. Nbr.")]
			public virtual string ExtRefNbr
			{
				get;
				set;
			}
			#endregion
			#region RuleID
			public abstract class ruleID : PX.Data.BQL.BqlInt.Field<ruleID> { }

			/// <summary>
			/// The identifier of the rule that was applied to the bank transaction to create a document.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="CABankTranRule.RuleID"/> field.
			/// </value>
			[PXDBInt]
			[PXSelector(typeof(CABankTranRule.ruleID), DescriptionField = typeof(CABankTranRule.description))]
			[PXUIField(DisplayName = "Applied Rule", Enabled = false)]
			public int? RuleID
			{
				get;
				set;
			}
			#endregion
			#region Hidden
			public abstract class hidden : PX.Data.BQL.BqlBool.Field<hidden> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that this bank transaction has been hidden from the statement on the Process Bank Transactions (CA306000) form.
			/// </summary>
			[PXDBBool]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Hidden", Enabled = false)]
			public virtual bool? Hidden
			{
				get;
				set;
			}
			#endregion
			#region CreateDocument
			public abstract class createDocument : PX.Data.BQL.BqlBool.Field<createDocument> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that a new payment will be created for the selected bank transactions. 
			/// </summary>
			[PXDBBool]
			[PXDefault(false)]
			[PXUIField(DisplayName = "Create")]
			public virtual bool? CreateDocument
			{
				get;
				set;
			}
			#endregion
			#region MatchedToInvoice
			public abstract class matchedToInvoice : PX.Data.BQL.BqlBool.Field<matchedToInvoice> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that this bank transaction is matched to the invoice.
			/// This is a virtual field and it has no representation in the database.
			/// </summary>
			[PXBool]
			[PXUIField(DisplayName = "Matched to Invoice", Visible = false, Enabled = false)]
			public virtual bool? MatchedToInvoice
			{
				get;
				set;
			}
			#endregion
			#region MatchedToExpenseReceipt
			public abstract class matchedToExpenseReceipt : PX.Data.BQL.BqlBool.Field<matchedToExpenseReceipt> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that this bank transaction is matched to the Expense Receipt.
			/// This is a virtual field and it has no representation in the database.
			/// </summary>
			[PXBool]
			[PXUIField(DisplayName = "Matched To Expense Receipt", Visible = false, Enabled = false)]
			public virtual bool? MatchedToExpenseReceipt
			{
				get;
				set;
			}
			#endregion
			#region DocumentMatched
			public abstract class documentMatched : PX.Data.BQL.BqlBool.Field<documentMatched> { }

			/// <summary>
			/// Specifies (if set to <c>true</c>) that this bank transaction is matched to the payment and ready to be processed. 
			/// That is, the bank transaction has been matched to an existing transaction in the system, or details of a new document that matches this transaction have been specified.
			/// </summary>
			[PXDBBool]
			[PXUIField(DisplayName = "Matched", Visible = true, Enabled = false)]
			public virtual bool? DocumentMatched
			{
				get;
				set;
			}
			#endregion
			#region Status
			public abstract class status : PX.Data.BQL.BqlString.Field<status> { }

			/// <summary>
			/// The status of the bank transaction.
			/// This is a virtual field and it has no representation in the database.
			/// </summary>
			/// <value>
			/// The field can have one of the following values:
			/// <c>"M"</c>: The bank transaction is matched to the payment and ready to be processed.
			/// <c>"I"</c>: The bank transaction is matched to the invoice.
			/// <c>"C"</c>: The bank transactions will be matched to a new payment.
			/// <c>"H"</c>: The bank transaction is hidden from the statement on the Process Bank Transactions (CA306000) form.
			/// <c>string.Empty</c>: The <see cref="DocumentMatched"/>, <see cref="MatchedToInvoice"/>, <see cref="CreateDocument"/>, and <see cref="Hidden"/> flags are set to <c>false</c>.
			/// </value>
			[PXString(1, IsFixed = true)]
			[CABankTranStatus.List]
			[PXUIField(DisplayName = "Match Type", Visibility = PXUIVisibility.SelectorVisible, Visible = false, Enabled = false)]
			public virtual string Status
			{
				[PXDependsOnFields(typeof(hidden), typeof(createDocument), typeof(matchedToInvoice), typeof(documentMatched), typeof(matchedToExpenseReceipt))]
				get
				{
					if (this.Hidden == true)
					{
						return CABankTranStatus.Hidden;
					}
					if (MatchedToExpenseReceipt == true)
					{
						return CABankTranStatus.ExpenseReceiptMatched;
					}
					if (this.CreateDocument == true)
					{
						return CABankTranStatus.Created;
					}
					if (this.MatchedToInvoice == true)
					{
						return CABankTranStatus.InvoiceMatched;
					}
					if (this.DocumentMatched == true)
					{
						return CABankTranStatus.Matched;
					}
					return string.Empty;
				}
			}
			#endregion
			#region TranDesc
			public abstract class tranDesc : PX.Data.BQL.BqlString.Field<tranDesc> { }

			/// <summary>
			/// The description of the bank transaction.
			/// </summary>
			[PXDBString(512, IsUnicode = true)]
			[PXUIField(DisplayName = "Tran. Desc")]
			public virtual string TranDesc
			{
				get;
				set;
			}
			#endregion
			#region TranCode
			public abstract class tranCode : PX.Data.BQL.BqlString.Field<tranCode> { }

			/// <summary>
			/// The external code from the bank.
			/// </summary>
			[PXDBString(35, IsUnicode = true)]
			[PXUIField(DisplayName = "Tran. Code", Visible = false)]
			public virtual string TranCode
			{
				get;
				set;
			}
			#endregion
			#region CuryID
			public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

			/// <summary>
			/// The identifier of currency of the bank transaction.
			/// </summary>
			[PXDBString(5, IsUnicode = true)]
			[PXUIField(DisplayName = "Currency")]
			public virtual string CuryID
			{
				get;
				set;
			}
			#endregion
			#region CuryDebitAmt
			public abstract class curyDebitAmt : PX.Data.BQL.BqlDecimal.Field<curyDebitAmt> { }

			/// <summary>
			/// The amount of the receipt in the selected currency.
			/// This is a virtual field and it has no representation in the database.
			/// </summary>
			[CM.PXDBCury(typeof(CABankTranHistory.curyID))]
			[PXUIField(DisplayName = "Receipt")]
			public virtual decimal? CuryDebitAmt
			{
				get;
				set;
			}
			#endregion
			#region CuryCreditAmt
			public abstract class curyCreditAmt : PX.Data.BQL.BqlDecimal.Field<curyCreditAmt> { }

			/// <summary>
			/// The amount of the disbursement in the selected currency.
			/// This is a virtual field and it has no representation in the database.
			/// </summary>
			[CM.PXDBCury(typeof(CABankTranHistory.curyID))]
			[PXUIField(DisplayName = "Disbursement")]
			public virtual decimal? CuryCreditAmt
			{
				get;
				set;
			}
			#endregion
			#region InvoiceInfo
			public abstract class invoiceInfo : PX.Data.BQL.BqlString.Field<invoiceInfo> { }

			/// <summary>
			/// The reference number of the document (invoice or bill) generated to match a payment. 
			/// This field is displayed if the <c>"AP"</c> or <c>"AR"</c> option is selected in the <see cref="OrigModule"/> field.
			/// This field is displayed on the Create Payment tab of on the Process Bank Transactions (CA306000) form.
			/// </summary>
			[PXDBString(256, IsUnicode = true)]
			[PXUIField(DisplayName = "Invoice Nbr.", Visible = false)]
			public virtual string InvoiceInfo
			{
				get;
				set;
			}
			#endregion
			#region PayeeName
			public abstract class payeeName : PX.Data.BQL.BqlString.Field<payeeName> { }

			/// <summary>
			/// The payee name, if any, specified for a transaction.
			/// </summary>
			[PXDBString(256, IsUnicode = true)]
			[PXUIField(DisplayName = "Payee Name", Visible = false)]
			public virtual string PayeeName
			{
				get;
				set;
			}
			#endregion
			#region EntryTypeID
			public abstract class entryTypeID : PX.Data.BQL.BqlString.Field<entryTypeID> { }

			/// <summary>
			/// The identifier of an entry type that is used as a template for a new cash transaction to be created to match the selected bank transaction. 
			/// The field is displayed if the <c>CA</c> option is selected in the <see cref="OrigModule"/> field.
			/// This field is displayed on the Create Payment tab of on the Process Bank Transactions (CA306000) form.
			/// </summary>
			/// <value>
			/// Corresponds to the <see cref="CAEntryType.EntryTypeId"/> field.
			/// </value>
			[PXDBString(10, IsUnicode = true)]
			[PXSelector(typeof(Search2<CAEntryType.entryTypeId,
				InnerJoin<CashAccountETDetail, On<CashAccountETDetail.entryTypeID, Equal<CAEntryType.entryTypeId>>>,
				Where<CashAccountETDetail.accountID, Equal<Current<CABankTran.cashAccountID>>,
					And<CAEntryType.module, Equal<GL.BatchModule.moduleCA>,
						And<Where<CAEntryType.drCr, Equal<Current<CABankTran.drCr>>>>>>>),
				DescriptionField = typeof(CAEntryType.descr))]
			[PXUIField(DisplayName = "Entry Type ID", Visibility = PXUIVisibility.SelectorVisible, Visible = false)]
			public virtual string EntryTypeID
			{
				get;
				set;
			}
			#endregion
			#region PayeeLocationID
			public abstract class payeeLocationID : PX.Data.BQL.BqlInt.Field<payeeLocationID> { }

			/// <summary>
			/// The location of the vendor or customer. 
			/// This field is displayed if the <c>"AP"</c> or <c>"AR"</c> option is selected in the <see cref="OrigModule"/> field.
			/// This field is displayed on the Create Payment tab of on the Process Bank Transactions (CA306000) form.
			/// </summary>
			[PXDBInt()]
			[PXUIField(DisplayName = "Location", Visibility = PXUIVisibility.Visible, FieldClass = "LOCATION")]
			public virtual int? PayeeLocationID
			{
				get;
				set;
			}
			#endregion
			#region PaymentMethodID
			public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }

			/// <summary>
			/// The payment method used by a customer or vendor for the document. 
			/// This field is displayed if the <c>"AP"</c> or <c>"AR"</c> option is selected in the <see cref="OrigModule"/> field.
			/// This field is displayed on the Create Payment tab of on the Process Bank Transactions (CA306000) form.
			/// </summary>
			[PXDBString(10, IsUnicode = true)]
			[PXSelector(typeof(Search<PaymentMethod.paymentMethodID>), DescriptionField = typeof(PaymentMethod.descr))]
			[PXUIField(DisplayName = "Payment Method", Visible = false)]
			public virtual string PaymentMethodID
			{
				get;
				set;
			}
			#endregion
			#region SortOrder
			public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
			[PXInt]
			public Int32? SortOrder
			{
				get;
				set;
			}
			#endregion
		}
		#endregion
		#region Selects
		public PXFilter<Filter> TranFilter;
        public PXSelect<CABankTranHistory> Result;
        public PXSelect<CATran> Trans;
		public PXSelect<CR.BAccountR> BAccountCache;
		public PXSelectJoin<CABankTran, LeftJoin<CABankTranMatch,
					 On<CABankTranMatch.tranID, Equal<CABankTran.tranID>,
						And<CABankTranMatch.tranType, Equal<CABankTran.tranType>>>,
						LeftJoin<CATran, On<CATran.tranID, Equal<CABankTranMatch.cATranID>>>>,
					 Where<CABankTran.cashAccountID, Equal<Current<Filter.cashAccountID>>,
						And<CABankTran.tranDate, GreaterEqual<Current<Filter.startDate>>,
						And<CABankTran.tranDate, LessEqual<Current<Filter.endDate>>,
						And<CABankTran.tranType, Equal<Current<Filter.tranType>>,
						And<CABankTran.processed, Equal<True>,
						And<Where<CABankTran.headerRefNbr, Equal<Current<Filter.headerRefNbr>>, Or<Current<Filter.headerRefNbr>, IsNull>>>>>>>>> CATran_History;
		#endregion

		public CABankTransactionsEnq()
        {
            Result.AllowDelete = false;
            Result.AllowInsert = false;
            Result.AllowUpdate = false;

			PXUIFieldAttribute.SetVisible<CABankTran.invoiceInfo>(Result.Cache, null, true);
			PXUIFieldAttribute.SetVisible<CABankTran.entryTypeID>(Result.Cache, null, true);
			PXUIFieldAttribute.SetVisible<CABankTran.status>(Result.Cache, null, true);

        }

		protected virtual Dictionary<Type, Type> GetMapperDictionary()
		{
			return new Dictionary<Type, Type>
			{
				{typeof(CABankTranHistory.tranID), typeof(CABankTran.tranID)},
				{typeof(CABankTranHistory.cATranID), typeof(CATran.tranID)},
				{typeof(CABankTranHistory.tranType), typeof(CABankTran.tranType)},
				{typeof(CABankTranHistory.headerRefNbr), typeof(CABankTran.headerRefNbr)},
				{typeof(CABankTranHistory.cashAccountID), typeof(CABankTran.cashAccountID)},
				{typeof(CABankTranHistory.tranDate), typeof(CABankTran.tranDate)},
				{typeof(CABankTranHistory.extTranID), typeof(CABankTran.extTranID)},
				{typeof(CABankTranHistory.extRefNbr), typeof(CABankTran.extRefNbr)},
				{typeof(CABankTranHistory.status), typeof(CABankTran.status)},
				{typeof(CABankTranHistory.tranDesc), typeof(CABankTran.tranDesc)},
				{typeof(CABankTranHistory.tranCode), typeof(CABankTran.tranCode)},
				{typeof(CABankTranHistory.curyID), typeof(CABankTran.curyID)},
				{typeof(CABankTranHistory.curyCreditAmt), typeof(CABankTran.curyCreditAmt)},
				{typeof(CABankTranHistory.curyDebitAmt), typeof(CABankTran.curyDebitAmt)},
				{typeof(CABankTranHistory.documentMatched), typeof(CABankTran.documentMatched)},
				{typeof(CABankTranHistory.matchedToInvoice), typeof(CABankTran.matchedToInvoice)},
				{typeof(CABankTranHistory.matchedToExpenseReceipt), typeof(CABankTran.matchedToExpenseReceipt)},
				{typeof(CABankTranHistory.hidden), typeof(CABankTran.hidden)},
				{typeof(CABankTranHistory.processed), typeof(CABankTran.hidden)},
				{typeof(CABankTranHistory.createDocument), typeof(CABankTran.createDocument)},
				{typeof(CABankTranHistory.invoiceInfo), typeof(CABankTran.invoiceInfo)},
				{typeof(CABankTranHistory.payeeName), typeof(CABankTran.payeeName)},
				{typeof(CABankTranHistory.entryTypeID), typeof(CABankTran.entryTypeID)},
				{typeof(CABankTranHistory.paymentMethodID), typeof(CABankTran.paymentMethodID)},
				{typeof(CABankTranHistory.payeeLocationID), typeof(CABankTran.payeeLocationID)},
				{typeof(CABankTranHistory.ruleID), typeof(CABankTran.ruleID)},
				{typeof(CABankTranHistory.curyMatchedDebitAmt), typeof(CATran.curyDebitAmt)},
				{typeof(CABankTranHistory.curyMatchedCreditAmt), typeof(CATran.curyCreditAmt)},
				{typeof(CABankTranHistory.matchedModule), typeof(CATran.origModule)},
				{typeof(CABankTranHistory.matchedDocType), typeof(CATran.origTranType)},
				{typeof(CABankTranHistory.matchedRefNbr), typeof(CATran.origRefNbr)},
				{typeof(CABankTranHistory.matchedReferenceID), typeof(CATran.referenceID)},
				{typeof(CABankTranHistory.sortOrder), typeof(CABankTran.sortOrder)},
			};
		}

		protected virtual IEnumerable result()
		{
			var map = GetMapperDictionary();

			var mapper = new PXResultMapper(this, map, typeof(CABankTranHistory));
			PXDelegateResult delegateResult = mapper.CreateDelegateResult();
			
			int startRow = PXView.StartRow;
			int totalRows = 0;
			
			foreach (PXResult<CABankTran, CABankTranMatch, CATran> res in Views[nameof(CATran_History)].Select(PXView.Currents,null, mapper.Searches,mapper.SortColumns,mapper.Descendings, mapper.Filters, ref startRow, PXView.MaximumRows, ref totalRows))
			{
				CABankTran tran = (CABankTran)res;
				CABankTranMatch match = (CABankTranMatch)res;
				tran.MatchedToInvoice = CABankTransactionsMaint.IsMatchedToInvoice(tran, match);
				tran.MatchedToExpenseReceipt = CABankTransactionsMaint.IsMatchedToExpenseReceipt(match);
				CATran catran = (CATran)res;
				if (catran.OrigModule == null)
				{
					catran.OrigModule = match.DocModule;
				}
				if (catran.OrigTranType == null)
				{
					catran.OrigTranType = match.DocType;
				}
				if (catran.OrigRefNbr == null)
				{
					catran.OrigRefNbr = match.DocRefNbr;
				}
				if (catran.ReferenceID == null)
				{
					catran.ReferenceID = match.ReferenceID ?? tran.BAccountID;
				}

				delegateResult.Add(mapper.CreateResult(res));
			}
			PXView.StartRow = 0;
			return delegateResult;
		}
        
		#region Actions
        public PXAction<Filter> viewDoc;
        [PXUIField(DisplayName = "View Document", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXLookupButton]
        public virtual IEnumerable ViewDoc(PXAdapter adapter)
        {
			CABankTranHistory bankTran = Result.Current;

			RedirectionToOrigDoc.TryRedirect(bankTran.MatchedDocType, bankTran.MatchedRefNbr, bankTran.MatchedModule);
	        
            return adapter.Get();
        }
        public PXAction<Filter> viewStatement;
		[PXUIField(DisplayName = "View Statement", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable ViewStatement(PXAdapter adapter)
		{
			CABankTranHeader header = PXSelect<CABankTranHeader,
					Where<CABankTranHeader.cashAccountID, Equal<Current<Filter.cashAccountID>>,
					And<CABankTranHeader.refNbr, Equal<Current<CABankTranHistory.headerRefNbr>>,
					And<CABankTranHeader.tranType, Equal<Current<CABankTranHistory.tranType>>>>>>.Select(this);
			CABankTransactionsImport graph;
			if (Result.Current.TranType == CABankTranType.PaymentImport)
			{
				graph = PXGraph.CreateInstance<CABankTransactionsImportPayments>();
			}
			else
			{
				graph = PXGraph.CreateInstance<CABankTransactionsImport>();
			}
			graph.Header.Current = header;
			graph.SelectedDetail.Current = graph.SelectedDetail.Search<CABankTranHistory.tranID>(Result.Current.TranID);
			throw new PXRedirectRequiredException(graph, true, "Import") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
		}
		#endregion

		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), "DisplayName", CR.Messages.BAccountName)]
		protected virtual void BAccountR_AcctName_CacheAttached(PXCache sender) { }
	}
    public class CABankTransactionsEnqPayments : CABankTransactionsEnq
    {
        [PXString(1, IsFixed = true)]
        [PXDefault(typeof(CABankTranType.paymentImport))]
        [CABankTranType.List()]
        [PXUIField(DisplayName = "Statement Type", Visible = false, Visibility = PXUIVisibility.SelectorVisible)]
        protected virtual void Filter_TranType_CacheAttached(PXCache sender) { }
    }
}
