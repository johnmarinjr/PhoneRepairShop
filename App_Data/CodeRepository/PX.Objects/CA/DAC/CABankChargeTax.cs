using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;
using PX.Objects.CS;
using PX.Objects.TX;

namespace PX.Objects.CA
{
	[Serializable]
	[PXCacheName(nameof(CABankChargeTax))]
	public partial class CABankChargeTax : TaxDetail, IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CABankChargeTax>.By<CABankChargeTax.bankTranID, lineNbr, taxID, matchType>
		{
			public static CABankChargeTax Find(PXGraph graph, int? tranID, int? lineNbr, string taxID, string tranType) => FindBy(graph, tranID, lineNbr, taxID, tranType);
		}

		public static class FK
		{
			public class BankTranMatch : CABankTranMatch.PK.ForeignKeyOf<CABankChargeTax>.By<bankTranID, lineNbr, matchType> { }
		}

		#endregion

		#region BankTranID
		public abstract class bankTranID : PX.Data.BQL.BqlInt.Field<bankTranID> { }

		[PXDBInt(IsKey = true)]
		[PXDBDefault(typeof(CABankTran.tranID))]
		[PXParent(typeof(FK.BankTranMatch))]
		public virtual int? BankTranID
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		[PXDBInt(IsKey = true)]
		[PXDefault]

		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region TaxID
		public abstract class taxID : PX.Data.BQL.BqlString.Field<taxID> { }
		[PXDBString(Tax.taxID.Length, IsUnicode = true, IsKey = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Tax ID")]
		[PXSelector(typeof(Tax.taxID), DescriptionField = typeof(Tax.descr))]
		public override String TaxID
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
		#region MatchType
		public abstract class matchType : PX.Data.BQL.BqlString.Field<matchType> { }

		[PXDBString(1, IsFixed = true, IsKey = true)]
		[PXDefault(typeof(CABankTranMatch.matchType.charge))]
		[CABankTranMatch.matchType.List]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = false, Visible = false)]
		public virtual string MatchType
		{
			get;
			set;
		}
		#endregion
		#region TaxRate
		public abstract class taxRate : PX.Data.BQL.BqlDecimal.Field<taxRate> { }
		#endregion
		#region CuryInfoID
		public abstract class curyInfoID : PX.Data.BQL.BqlLong.Field<curyInfoID> { }
		[PXDBLong]
		[CurrencyInfo(typeof(CABankTran.curyInfoID))]
		public override Int64? CuryInfoID
		{
			get
			{
				return this._CuryInfoID;
			}
			set
			{
				this._CuryInfoID = value;
			}
		}
		#endregion
		#region CuryOrigTaxableAmt
		public abstract class curyOrigTaxableAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigTaxableAmt> { }

		[PXDBCurrency(typeof(CABankChargeTax.curyInfoID), typeof(CABankChargeTax.origTaxableAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? CuryOrigTaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region OrigTaxableAmt
		public abstract class origTaxableAmt : PX.Data.BQL.BqlDecimal.Field<origTaxableAmt> { }

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? OrigTaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryTaxableAmt
		public abstract class curyTaxableAmt : PX.Data.BQL.BqlDecimal.Field<curyTaxableAmt> { }

		[PXDBCurrency(typeof(CABankChargeTax.curyInfoID), typeof(CABankChargeTax.taxableAmt))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Taxable Amount", Visibility = PXUIVisibility.Visible)]
		public virtual decimal? CuryTaxableAmt
		{
			get;
			set;
		}
		#endregion
		#region TaxableAmt
		public abstract class taxableAmt : PX.Data.BQL.BqlDecimal.Field<taxableAmt> { }

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Taxable Amount", Visibility = PXUIVisibility.Visible)]
		public virtual decimal? TaxableAmt
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
		[PXDBCurrency(typeof(CABankChargeTax.curyInfoID), typeof(CABankChargeTax.exemptedAmt))]
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

		[PXDBCurrency(typeof(CABankChargeTax.curyInfoID), typeof(CABankChargeTax.taxAmt))]
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

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Tax Amount", Visibility = PXUIVisibility.Visible)]
		public virtual decimal? TaxAmt
		{
			get;
			set;
		}
		#endregion
		#region CuryExpenseAmt
		public abstract class curyExpenseAmt : PX.Data.BQL.BqlDecimal.Field<curyExpenseAmt> { }
		[PXDBCurrency(typeof(CABankChargeTax.curyInfoID), typeof(CABankChargeTax.expenseAmt))]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Expense Amount", Visibility = PXUIVisibility.Visible)]
		public override decimal? CuryExpenseAmt
		{
			get;
			set;
		}
		#endregion
		#region ExpenseAmt
		public abstract class expenseAmt : PX.Data.BQL.BqlDecimal.Field<expenseAmt> { }
		#endregion
		#region NonDeductibleTaxRate
		public abstract class nonDeductibleTaxRate : PX.Data.BQL.BqlDecimal.Field<nonDeductibleTaxRate> { }
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
