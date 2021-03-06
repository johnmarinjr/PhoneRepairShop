using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CS;
using PX.Objects.GL;
using System;


namespace PX.Objects.CA
{
	/// <summary>
	/// Contains the main properties of Clearing Accounts.
	/// Records defines a settings for deposit to the cash account from the clearing account(s).
	/// The presence of this record for the specific Cash Account /Deposit Account pair defines a possibility to post to Cash Account from the specific cash account.
	/// Clearing Accounts are edited on the Cash Accounts (CA202000) form (which corresponds to the <see cref="CashAccountMaint"/> graph) on the tab Clearing Accounts.
	/// </summary>
	[Serializable]
	[PXCacheName(Messages.ClearingAccount)]
	public partial class CashAccountDeposit : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CashAccountDeposit>.By<accountID, depositAcctID, paymentMethodID>
		{
			public static CashAccountDeposit Find(PXGraph graph, int? accountID, int? depositAcctID, string paymentMethodID)
				=> FindBy(graph, accountID, depositAcctID, paymentMethodID);
		}

		public static class FK
		{
			public class ParentCashAccount : CA.CashAccount.PK.ForeignKeyOf<CashAccountDeposit>.By<accountID> { }
			public class DepositeCashAccount : CA.CashAccount.PK.ForeignKeyOf<CashAccountDeposit>.By<depositAcctID> { }
			public class ChargeEntryType : CA.CAEntryType.PK.ForeignKeyOf<CashAccountDeposit>.By<chargeEntryTypeID> { }
			public class PaymentMethod : CA.PaymentMethod.PK.ForeignKeyOf<CashAccountDeposit>.By<paymentMethodID> { }
		}

		#endregion

		#region AccountID
		public abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }

        /// <summary>
        /// The unique identifier of the parent cash account.
        /// This field is the key field.
        /// </summary>
        [PXDBInt(IsKey = true)]
        [PXDBDefault(typeof(CashAccount.cashAccountID))]
		[PXUIField(DisplayName = "Cash Account ID", Visible = false)]
        [PXParent(typeof(Select<CashAccount, Where<CashAccount.cashAccountID, Equal<Current<CashAccountDeposit.accountID>>>>))]
		public virtual int? AccountID
		{
			get;
			set;
		}
		#endregion
		#region DepositAcctID
		public abstract class depositAcctID : PX.Data.BQL.BqlInt.Field<depositAcctID> { }

        /// <summary>
        /// The cash account used to record customer payments that will later be deposited to the bank.
        /// Corresponds to the value of the <see cref="CashAccount.CashAccountID"/> field.
        /// </summary>
		[PXDefault]
        [CashAccount(typeof(CashAccount.branchID), typeof(Search<CashAccount.cashAccountID, Where<CashAccount.curyID, Equal<Current<CashAccount.curyID>>,
                And<CashAccount.cashAccountID, NotEqual<Current<CashAccount.cashAccountID>>,
                And<Where<CashAccount.clearingAccount, Equal<boolTrue>,
                Or<CashAccount.cashAccountID, Equal<Current<CashAccountDeposit.depositAcctID>>>>>>>>), IsKey = true, DisplayName = "Clearing Account")]
		public virtual int? DepositAcctID
		{
			get;
			set;
		}
		#endregion
		#region PaymentMethodID
		public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }

        /// <summary>
        /// The payment method of the deposited payment to which this charge rate should be applied.
        /// If the field is filled by empty string (default), the charge rate is applied to deposited payments, regardless of their payment method.
        /// Corresponds to the value of the <see cref="PaymentMethod.PaymentMethodID"/> field.
        /// </summary>
		[PXDBString(10, IsKey = true, IsUnicode = true)]
		[PXDefault("", PersistingCheck = PXPersistingCheck.Null)]
		[PXUIField(DisplayName = "Payment Method", Required = false)]
		[PXSelector(typeof(PaymentMethod.paymentMethodID))]
		public virtual string PaymentMethodID
		{
			get;
			set;
		}
		#endregion
		#region ChargeEntryTypeID
        public abstract class chargeEntryTypeID : PX.Data.BQL.BqlString.Field<chargeEntryTypeID> { }

        /// <summary>
        /// The entry type of the bank charges that apply to the deposit.
        /// Corresponds to the value of the <see cref="CAEntryType.EntryTypeId"/> field.
        /// </summary>
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Charges Type")]
		[PXSelector(typeof(Search2<CAEntryType.entryTypeId,
			InnerJoin<CashAccountETDetail, On<CashAccountETDetail.entryTypeID, Equal<CAEntryType.entryTypeId>>>,
			Where<CashAccountETDetail.accountID, Equal<Current<depositAcctID>>,
				And<CAEntryType.module, Equal<BatchModule.moduleCA>>>>),
			DescriptionField = typeof(CAEntryType.descr), DirtyRead = false)]
		[PXDefault(typeof(Search2<CAEntryType.entryTypeId, 
			InnerJoin<CashAccountETDetail, On<CashAccountETDetail.entryTypeID, Equal<CAEntryType.entryTypeId>>>,
			Where<CashAccountETDetail.accountID, Equal<Current<depositAcctID>>,
				And<CAEntryType.module, Equal<BatchModule.moduleCA>,
				And<CAEntryType.useToReclassifyPayments, Equal<False>,
				And<CashAccountETDetail.isDefault, Equal<True>>>>>>), 
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXFormula(typeof(Default<depositAcctID>))]
		public virtual string ChargeEntryTypeID
		{
			get;
			set;
		}
		#endregion
		#region ChargeRate
        public abstract class chargeRate : PX.Data.BQL.BqlDecimal.Field<chargeRate> { }

        /// <summary>
        /// The rate of the specified charges (expressed as a percent of the deposit total).
        /// </summary>
        [PXDBDecimal(6)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Charge Rate", Visibility = PXUIVisibility.Visible, Enabled = true)]
		public virtual decimal? ChargeRate
		{
			get;
			set;
		}
		#endregion
	}
}
