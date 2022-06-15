using PX.Commerce.Core;
using PX.Data;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.GL;
using System;

namespace PX.Commerce.Objects
{
	[Serializable]
	[PXCacheName("BCPaymentMethods")]
	public class BCPaymentMethods : IBqlTable
	{
		#region Keys

		public static class FK
		{
			public class Binding : BCBinding.BindingIndex.ForeignKeyOf<BCPaymentMethods>.By<bindingID> { }
			public class CashAcc : CashAccount.PK.ForeignKeyOf<BCPaymentMethods>.By<cashAccountID> { }
		}
		#endregion

		#region BindingID
		[PXDBInt()]
		[PXUIField(DisplayName = "Store")]
		[PXSelector(typeof(BCBinding.bindingID),
					typeof(BCBinding.bindingName),
					SubstituteKey = typeof(BCBinding.bindingName))]
		[PXParent(typeof(Select<BCBinding,
			Where<BCBinding.bindingID, Equal<Current<BCPaymentMethods.bindingID>>>>))]
		[PXDBDefault(typeof(BCBinding.bindingID),
			PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual int? BindingID { get; set; }
		public abstract class bindingID : PX.Data.BQL.BqlInt.Field<bindingID> { }
		#endregion

		#region PaymentMappingID
		[PXDBIdentity(IsKey = true)]
		public int? PaymentMappingID { get; set; }
		public abstract class paymentMappingID : PX.Data.BQL.BqlInt.Field<paymentMappingID> { }
		#endregion

		#region StorePaymentMethod
		[PXDBString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Store Payment Method")]
		[PXDefault]
		[BCCapitalLettersAttribute]
		public virtual string StorePaymentMethod { get; set; }
		public abstract class storePaymentMethod : PX.Data.BQL.BqlString.Field<storePaymentMethod> { }
		#endregion

		#region StoreCurrency
		[PXDBString(IsUnicode = true)]
		[PXUIField(DisplayName = "Store Currency")]
		[PXSelector(typeof(Currency.curyID))]
		[PXDefault]
		public virtual string StoreCurrency { get; set; }
		public abstract class storeCurrency : PX.Data.BQL.BqlString.Field<storeCurrency> { }
		#endregion

		#region StoreOrderPaymentMethod
		[PXDBString(IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Store Order Payment Method", Visible = false)]
		[BCCapitalLettersAttribute]
		public virtual string StoreOrderPaymentMethod { get; set; }
		public abstract class storeOrderPaymentMethod : PX.Data.BQL.BqlString.Field<storeOrderPaymentMethod> { }
		#endregion

		#region PaymentMethodID
		[PXDBString(IsUnicode = true)]
		[PXUIField(DisplayName = "ERP Payment Method")]
		[PXSelector(typeof(Search4<PaymentMethod.paymentMethodID,
			Where<PaymentMethod.isActive, Equal<True>,
				And<PaymentMethod.useForAR, Equal<True>>>,
			Aggregate< GroupBy<PaymentMethod.paymentMethodID, GroupBy<PaymentMethod.useForAR, GroupBy<PaymentMethod.useForAP>>>>>),
			DescriptionField = typeof(PaymentMethod.descr))]
		public virtual string PaymentMethodID { get; set; }
		public abstract class paymentMethodID : PX.Data.BQL.BqlString.Field<paymentMethodID> { }
		#endregion

		#region CashAcccount
		[PXDBInt]
		[PXUIField(DisplayName = "Cash Account")]
		[PXSelector(typeof(Search2<CashAccount.cashAccountID,
						InnerJoin<PaymentMethodAccount, On<PaymentMethodAccount.cashAccountID, Equal<CashAccount.cashAccountID>,
							And<PaymentMethodAccount.paymentMethodID, Equal<Current<BCPaymentMethods.paymentMethodID>>,
							And<PaymentMethodAccount.useForAR, Equal<True>>>>>,
						Where<CashAccount.active, Equal<True>,
							And<CashAccount.curyID, Equal<Current<BCPaymentMethods.storeCurrency>>,
							And<CashAccount.branchID, Equal<Current<BCBinding.branchID>>>>>>),
				 DescriptionField = typeof(CashAccount.descr),
					SubstituteKey = typeof(CashAccount.cashAccountCD)
			)]
		public virtual int? CashAccountID { get; set; }
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
		#endregion

		#region ProcessingCenterID
		[PXDBString(10, IsUnicode = true)]
		[PXUIField(DisplayName = "Proc. Center ID")]
		[PXSelector(typeof(Search2<CCProcessingCenter.processingCenterID,
			InnerJoin<CCProcessingCenterPmntMethod,
				On<CCProcessingCenterPmntMethod.processingCenterID, Equal<CCProcessingCenter.processingCenterID>>>,
			Where<CCProcessingCenter.isActive, Equal<True>,
				And<CCProcessingCenterPmntMethod.isActive, Equal<True>, And<CCProcessingCenterPmntMethod.paymentMethodID, Equal<Current<BCPaymentMethods.paymentMethodID>>,
				And<CCProcessingCenter.cashAccountID, Equal<Current<BCPaymentMethods.cashAccountID>>>>>>>))]
		public virtual string ProcessingCenterID { get; set; }
		public abstract class processingCenterID : PX.Data.BQL.BqlString.Field<processingCenterID> { }
		#endregion

		#region ReleasePayments
		[PXDBBool]
		[PXUIField(DisplayName = "Release Payments")]
		[PXDefault(false)]
		public virtual bool? ReleasePayments { get; set; }
		public abstract class releasePayments : PX.Data.BQL.BqlBool.Field<releasePayments> { }
		#endregion

		#region Active
		[PXDBBool]
		[PXUIField(DisplayName = "Active")]
		[PXDefault(false)]
		public virtual bool? Active { get; set; }
		public abstract class active : PX.Data.BQL.BqlBool.Field<active> { }
		#endregion

		#region ProcessRefunds
		[PXDBBool]
		[PXUIField(DisplayName = "Process Refunds")]
		[PXDefault(false)]
		public virtual bool? ProcessRefunds { get; set; }
		public abstract class processRefunds : PX.Data.BQL.BqlBool.Field<processRefunds> { }
		#endregion

		#region CreatePaymentFromOrder
		[PXDBBool]
		[PXUIField(DisplayName = "Create Payment from Order", Visible = false)]
		[PXDefault(false)]
		public virtual bool? CreatePaymentFromOrder { get; set; }
		public abstract class createPaymentFromOrder : PX.Data.BQL.BqlBool.Field<createPaymentFromOrder> { }
		#endregion
	}

	[Serializable]
	[PXHidden]
	public class BCBigCommercePayment : IBqlTable
	{
		#region Name
		[PXDBString(IsUnicode = true, IsKey = true)]
		[PXUIField(DisplayName = "Name")]
		public virtual string Name { get; set; }
		public abstract class name : PX.Data.BQL.BqlString.Field<name> { }
		#endregion

		#region Currency
		[PXDBString(IsUnicode = true, IsKey = true)]
		[PXUIField(DisplayName = "Currency")]
		public virtual string Currency { get; set; }
		public abstract class currency : PX.Data.BQL.BqlString.Field<currency> { }
		#endregion

		#region Create Payment from Order
		[PXDBBool]
		[PXUIField(DisplayName = "Create Payment from Order")]
		[PXDefault(false)]
		public virtual bool? CreatePaymentfromOrder { get; set; }
		public abstract class createPaymentfromOrder : PX.Data.BQL.BqlBool.Field<createPaymentfromOrder> { }
		#endregion
	}
}
