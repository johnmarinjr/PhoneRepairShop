using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods;
using PX.Objects.TX;

namespace PX.Objects.CA
{
	/*#region TaxZoneID
		public abstract new class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXDBDefault(typeof(CABankTran.chargeTaxZoneID))]
		public override string TaxZoneID
		{
			get;
			set;
		}
		#endregion*/


	[Serializable]
	[PXBreakInheritance]
	[PXCacheName(nameof(CABankTaxTranMatch))]
	public class CABankTaxTranMatch : TaxDetail, IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CABankTaxTran>.By<module, tranDate, recordID>
		{
			public static CABankTaxTran Find(PXGraph graph, string module, DateTime? tranDate, int? recordID) => FindBy(graph, module, tranDate, recordID);
		}
		public static class FK
		{
			public class BankTransaction : CABankTran.PK.ForeignKeyOf<CABankTax>.By<bankTranID> { }
		}
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

		/// <summary>
		/// The reference to the <see cref="Branch"/> record to which the record belongs.
		/// </summary>
		/// <value>The value is copied from the document from which the record is created.</value>
		[Branch(Enabled = false)]
		public virtual Int32? BranchID
		{
			get;
			set;
		}
		#endregion
		#region Module
		public abstract class module : PX.Data.BQL.BqlString.Field<module> { }
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Justification]
		[PXDBString(2, IsFixed = true, IsKey = true)]
		[PXDefault(BatchModule.CA)]
		[PXUIField(DisplayName = "Module", Enabled = false, Visible = false)]
		public virtual string Module
		{
			get;
			set;
		}
		#endregion
		#region BankTranType
		public abstract class bankTranType : PX.Data.BQL.BqlString.Field<bankTranType> { }

		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Justification]
		[PXDBString(1, IsFixed = true, IsKey = true)]
		[PXDefault(typeof(CABankTran.tranType))]
		[CABankTranType.List]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = false, Visible = false)]
		[PXParent(typeof(FK.BankTransaction))]
		public virtual string BankTranType
		{
			get;
			set;
		}
		#endregion
		#region BankTranID
		public abstract class bankTranID : PX.Data.BQL.BqlInt.Field<bankTranID> { }
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Justification]
		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(CABankTran.tranID))]

		public virtual int? BankTranID
		{
			get;
			set;
		}
		#endregion
		#region TranDate
		public abstract class tranDate : PX.Data.BQL.BqlDateTime.Field<tranDate> { }
		[PXDBDate]
		[PXDBDefault(typeof(CABankTran.tranDate))]
		public virtual DateTime? TranDate
		{
			get;
			set;
		}
		#endregion
		#region TaxID
		public new abstract class taxID : PX.Data.BQL.BqlString.Field<taxID> { }
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Justification]
		[PXDBString(Tax.taxID.Length, IsUnicode = true, IsKey = true)]
		[PXDefault]
		[PXUIField(DisplayName = "Tax ID", Visibility = PXUIVisibility.Visible)]
		[PXSelector(typeof(Tax.taxID), DescriptionField = typeof(Tax.descr), DirtyRead = true)]
		public override string TaxID
		{
			get
			{
				return this._TaxID;
			}

			set
			{
				this._TaxID = value;
			}
		}
		#endregion
		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }

		/// <summary>
		/// This is an auto-numbered field, which is a part of the primary key.
		/// </summary>
		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Justification]
		[PXDBIdentity(IsKey = true)]
		public virtual Int32? RecordID
		{
			get;
			set;
		}
		#endregion
		#region VendorID
		public abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt]
		[PXDefault(typeof(Search<Tax.taxVendorID, Where<Tax.taxID, Equal<Current<CABankTaxTranMatch.taxID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? VendorID
		{
			get;
			set;
		}
		#endregion
		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[Account]
		[PXDefault]
		public virtual int? AccountID
		{
			get;
			set;
		}
		#endregion
		#region SubID
		public abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[SubAccount]
		[PXDefault]
		public virtual int? SubID
		{
			get;
			set;
		}
		#endregion
		#region TaxPeriodID
		public abstract class taxPeriodID : PX.Data.BQL.BqlString.Field<taxPeriodID> { }
		[FinPeriodID]
		public virtual string TaxPeriodID
		{
			get;
			set;
		}
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[FinPeriodID(branchSourceType: typeof(CABankTaxTranMatch.branchID),
			headerMasterFinPeriodIDType: typeof(CABankTran.tranPeriodID))]
		// Acuminator disable once PX1030 PXDefaultIncorrectUse [Incorrect validation]
		[PXDefault]
		public virtual String FinPeriodID
		{
			get;
			set;
		}
		#endregion
		#region FinDate
		public abstract class finDate : PX.Data.BQL.BqlDateTime.Field<finDate> { }

		/// <summary>
		/// The last day (<see cref="PX.Objects.GL.Obsolete.FinPeriod.FinDate"/>) of the financial period of the document to which the record belongs.
		/// </summary>
		[PXDBDate()]
		[PXDBDefault(typeof(Search2<OrganizationFinPeriod.finDate,
			InnerJoin<Branch,
				On<OrganizationFinPeriod.organizationID, Equal<Branch.organizationID>>>,
			Where<Branch.branchID, Equal<Current2<branchID>>,
				And<OrganizationFinPeriod.finPeriodID, Equal<Current2<finPeriodID>>>>>))]
		public virtual DateTime? FinDate
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }

		/// <summary>
		/// Indicates (if set to <c>true</c>) that the record has been released.
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		public virtual Boolean? Released
		{
			get;
			set;
		}
		#endregion
		#region Voided
		public abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }

		/// <summary>
		/// Indicates (if set to <c>true</c>) that the record has been voided.
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		public virtual Boolean? Voided
		{
			get;
			set;
		}
		#endregion
		#region JurisType
		public abstract class jurisType : PX.Data.BQL.BqlString.Field<jurisType> { }

		/// <summary>
		/// The tax jurisdiction type. The field is used for the taxes from Avalara.
		/// </summary>
		[PXDBString(9, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Jurisdiction Type")]
		public virtual String JurisType
		{
			get;
			set;
		}
		#endregion
		#region JurisName
		public abstract class jurisName : PX.Data.BQL.BqlString.Field<jurisName> { }
		protected String _JurisName;

		/// <summary>
		/// The tax jurisdiction name. The field is used for the taxes from Avalara.
		/// </summary>
		[PXDBString(200, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Jurisdiction Name")]
		public virtual String JurisName
		{
			get
			{
				return this._JurisName;
			}
			set
			{
				this._JurisName = value;
			}
		}
		#endregion
		#region CuryInfoID
		public new abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXDBLong]
		[CurrencyInfo(typeof(CABankTran.curyInfoID))]
		public override long? CuryInfoID
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxDiscountAmt
		public abstract class curyTaxDiscountAmt : PX.Data.BQL.BqlDecimal.Field<curyTaxDiscountAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(taxDiscountAmt))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual decimal? CuryTaxDiscountAmt
		{
			get;
			set;
		}
		#endregion
		#region TaxableDiscountAmt
		public abstract class taxDiscountAmt : PX.Data.BQL.BqlDecimal.Field<taxDiscountAmt> { }
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual decimal? TaxDiscountAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryOrigTaxableAmt
		public abstract class curyOrigTaxableAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigTaxableAmt> { }

		/// <summary>
		/// The original taxable amount (before truncation by minimal or maximal value) in the record currency.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(origTaxableAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Orig. Taxable Amount")]
		public virtual Decimal? CuryOrigTaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region OrigTaxableAmt
		public abstract class origTaxableAmt : PX.Data.BQL.BqlDecimal.Field<origTaxableAmt> { }

		/// <summary>
		/// The original taxable amount (before truncation by minimal or maximal value) in the base currency.
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Orig. Taxable Amount")]
		public virtual Decimal? OrigTaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxableAmt
		public abstract class curyTaxableAmt : PX.Data.BQL.BqlDecimal.Field<curyTaxableAmt> { }

		/// <summary>
		/// The taxable amount in the record currency.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(taxableAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Taxable Amount", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? CuryTaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region TaxableAmt
		public abstract class taxableAmt : PX.Data.BQL.BqlDecimal.Field<taxableAmt> { }

		/// <summary>
		/// The taxable amount in the base currency.
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Taxable Amount", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? TaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryExemptedAmt
		public abstract class curyExemptedAmt : IBqlField { }

		/// <summary>
		/// The exempted amount in the record currency.
		/// </summary>
		[PXDBCurrency(typeof(curyInfoID), typeof(exemptedAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Exempted Amount", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.ExemptedTaxReporting))]
		public virtual decimal? CuryExemptedAmt
		{
			get;
			set;
		}
		#endregion
		#region ExemptedAmt
		public abstract class exemptedAmt : IBqlField { }

		/// <summary>
		/// The exempted amount in the base currency.
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Exempted Amount", Visibility = PXUIVisibility.Visible, FieldClass = nameof(FeaturesSet.ExemptedTaxReporting))]
		public virtual decimal? ExemptedAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxAmt
		public abstract class curyTaxAmt : PX.Data.BQL.BqlDecimal.Field<curyTaxAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(taxAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Amount", Visibility = PXUIVisibility.Visible)]
		public virtual decimal? CuryTaxAmt
		{
			get;
			set;
		}
		#endregion
		#region TaxAmt
		public abstract class taxAmt : PX.Data.BQL.BqlDecimal.Field<taxAmt> { }

		/// <summary>
		/// The tax amount in the base currency.
		/// </summary>
		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Amount", Visibility = PXUIVisibility.Visible)]
		public virtual Decimal? TaxAmt
		{
			get;
			set;
		}
		#endregion
		#region BAccountID
		public abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }

		/// <summary>
		/// The reference to the vendor record (<see cref="Vendor.BAccountID"/>) or customer record (<see cref="Customer.BAccountID"/>).
		/// The field is used for the records that have been created in the AP or AR module.
		/// </summary>
		[PXDBInt]
		public virtual Int32? BAccountID
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxAmtSumm
		public abstract class curyTaxAmtSumm : PX.Data.BQL.BqlDecimal.Field<curyTaxAmtSumm> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(taxAmtSumm))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CuryTaxAmtSumm
		{
			get;
			set;
		}
		#endregion
		#region TaxAmtSumm
		public abstract class taxAmtSumm : PX.Data.BQL.BqlDecimal.Field<taxAmtSumm> { }

		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? TaxAmtSumm
		{
			get;
			set;
		}
		#endregion
		#region NonDeductibleTaxRate
		public new abstract class nonDeductibleTaxRate : PX.Data.BQL.BqlDecimal.Field<nonDeductibleTaxRate> { }
		#endregion
		#region CuryExpenseAmt
		public new abstract class curyExpenseAmt : PX.Data.BQL.BqlDecimal.Field<curyExpenseAmt> { }
		[PXDBCurrency(typeof(curyInfoID), typeof(expenseAmt))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Expense Amount", Visibility = PXUIVisibility.Visible)]
		public override decimal? CuryExpenseAmt
		{
			get; set;
		}
		#endregion
		#region ExpenseAmt
		public new abstract class expenseAmt : PX.Data.BQL.BqlDecimal.Field<expenseAmt> { }
		#endregion
		#region CuryID
		public abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }

		/// <summary>
		/// The reference to the currency (<see cref="Currency.CuryID"/>) of the document to which the record belongs.
		/// </summary>
		[PXDBString(5, IsUnicode = true, InputMask = ">LLLLL")]
		[PXUIField(DisplayName = "Currency")]
		[PXSelector(typeof(Currency.curyID))]
		public virtual String CuryID
		{
			get;
			set;
		}
		#endregion
		#region TaxType
		public abstract class taxType : PX.Data.BQL.BqlString.Field<taxType> { }
		[PXDBString(1, IsFixed = true)]
		[PXDefault]
		public virtual string TaxType
		{
			get;
			set;
		}
		#endregion
		#region TaxZoneID
		public abstract class taxZoneID : PX.Data.BQL.BqlString.Field<taxZoneID> { }
		[PXDBString(10, IsUnicode = true)]
		[PXDBDefault(typeof(CABankTran.chargeTaxZoneID))]
		public virtual string TaxZoneID
		{
			get;
			set;
		}
		#endregion
		#region TaxBucketID
		public abstract class taxBucketID : PX.Data.BQL.BqlInt.Field<taxBucketID> { }
		[PXDBInt()]
		[PXDefault(typeof(Search<TaxRev.taxBucketID,
			Where<TaxRev.taxID, Equal<Current<CABankTaxTranMatch.taxID>>,
				And<Current<CABankTaxTranMatch.tranDate>, Between<TaxRev.startDate, TaxRev.endDate>,
				And2<Where<TaxRev.taxType, Equal<Current<CABankTaxTranMatch.taxType>>,
						Or<TaxRev.taxType, Equal<TaxType.sales>,
					And<Current<CABankTaxTranMatch.taxType>, Equal<TaxType.pendingSales>,
						Or<TaxRev.taxType, Equal<TaxType.purchase>,
						And<Current<CABankTaxTranMatch.taxType>, Equal<TaxType.pendingPurchase>>>>>>,
				And<TaxRev.outdated, Equal<False>>>>>>))]
		public virtual Int32? TaxBucketID
		{
			get;
			set;
		}
		#endregion
		#region TaxRate
		public abstract class taxRate : PX.Data.BQL.BqlDecimal.Field<taxRate> { }

		/// <summary>
		/// The tax rate of the relevant <see cref="Tax"/> record.
		/// </summary>
		[PXDBDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Rate", Visibility = PXUIVisibility.Visible, Enabled = false)]
		public override Decimal? TaxRate
		{
			get;
			set;
		}
		#endregion
		#region TaxInvoiceNbr
		public abstract class taxInvoiceNbr : PX.Data.BQL.BqlString.Field<taxInvoiceNbr> { }

		/// <summary>
		/// The reference number of the tax invoice. The field is used for recognized SVAT records.
		/// </summary>
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Tax Invoice Nbr.")]
		public virtual String TaxInvoiceNbr
		{
			get;
			set;
		}
		#endregion
		#region TaxInvoiceDate
		public abstract class taxInvoiceDate : PX.Data.BQL.BqlDateTime.Field<taxInvoiceDate> { }

		/// <summary>
		/// The date of the tax invoice. The field is used for recognized SVAT records.
		/// </summary>
		[PXDBDate(InputMask = "d", DisplayMask = "d")]
		[PXUIField(DisplayName = "Tax Invoice Date")]
		public virtual DateTime? TaxInvoiceDate
		{
			get;
			set;
		}
		#endregion
		#region OrigTranType
		public abstract class origTranType : PX.Data.BQL.BqlString.Field<origTranType> { }

		/// <summary>
		/// The original document type for which the tax amount has been entered.
		/// The field is used for the records that are created on the Tax Bills and Adjustments (TX303000) form.
		/// </summary>
		[PXDBString(3, IsFixed = true)]
		[PXUIField(DisplayName = "Orig. Tran. Type")]
		[PXDefault("")]
		public virtual String OrigTranType
		{
			get;
			set;
		}
		#endregion
		#region OrigRefNbr
		public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

		/// <summary>
		/// The original document reference number for which the tax amount has been entered.
		/// The field is used for the records that are created on the Tax Bills and Adjustments (TX303000) form.
		/// </summary>
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Orig. Doc. Number")]
		[PXDefault("")]
		public virtual String OrigRefNbr
		{
			get;
			set;
		}
		#endregion
		#region LineRefNbr
		public abstract class lineRefNbr : PX.Data.BQL.BqlString.Field<lineRefNbr> { }

		/// <summary>
		/// The reference number of the transaction to which the record is related.
		/// The field is used for the records that are created from GL.
		/// </summary>
		[PXDBString(15, IsUnicode = true)]
		[PXUIField(DisplayName = "Line Ref. Number")]
		[PXDefault("")]
		public virtual String LineRefNbr
		{
			get;
			set;
		}
		#endregion
		#region RevisionID
		public abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }

		/// <summary>
		/// The revision of the tax report to which the record was included.
		/// </summary>
		[PXDBInt]
		public virtual Int32? RevisionID
		{
			get;
			set;
		}
		#endregion
		#region AdjdDocType
		public abstract class adjdDocType : PX.Data.BQL.BqlString.Field<adjdDocType> { }

		/// <summary>
		/// Link to <see cref="APPayment"/> (Check) application. Used for withholding taxes.
		/// </summary>
		[PXDBString(3)]
		public virtual String AdjdDocType
		{
			get;
			set;
		}
		#endregion
		#region AdjdRefNbr
		public abstract class adjdRefNbr : PX.Data.BQL.BqlString.Field<adjdRefNbr> { }

		/// <summary>
		/// Link to <see cref="APPayment"/> (Check) application. Used for withholding taxes.
		/// </summary>
		[PXDBString(15)]
		public virtual String AdjdRefNbr
		{
			get;
			set;
		}
		#endregion
		#region AdjNbr
		public abstract class adjNbr : PX.Data.BQL.BqlInt.Field<adjNbr> { }

		/// <summary>
		/// Link to <see cref="APPayment"/> (Check) application. Used for withholding taxes.
		/// </summary>
		[PXDBInt]
		public virtual Int32? AdjNbr
		{
			get;
			set;
		}
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }

		/// <summary>
		/// The description of the transaction.
		/// </summary>
		[PXDBString(256, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.Visible)]
		public virtual String Description
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get;
			set;
		}
		#endregion
	}
}
