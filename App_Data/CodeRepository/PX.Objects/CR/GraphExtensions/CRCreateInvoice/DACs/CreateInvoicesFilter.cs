using PX.Data;
using PX.Data.BQL;
using PX.Objects.CS;
using PX.Objects.SO;
using System;

namespace PX.Objects.CR.Extensions.CRCreateInvoice
{
	[Serializable]
	[PXHidden]
	public partial class CreateInvoicesFilter : IBqlTable
	{
		#region MakeQuotePrimary
		public abstract class makeQuotePrimary : PX.Data.BQL.BqlBool.Field<makeQuotePrimary> { }
		[PXBool()]
		[PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Set Quote as Primary", Visible = false)]
		public virtual bool? MakeQuotePrimary { get; set; }
		#endregion

		#region RecalculatePrices
		public abstract class recalculatePrices : PX.Data.BQL.BqlBool.Field<recalculatePrices> { }
		[PXBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Recalculate Prices")]
		public virtual bool? RecalculatePrices { get; set; }
		#endregion

		#region Override Manual Prices
		public abstract class overrideManualPrices : PX.Data.BQL.BqlBool.Field<overrideManualPrices> { }
		[PXBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Override Manual Prices")]
		public virtual bool? OverrideManualPrices { get; set; }
		#endregion

		#region Recalculate Discounts
		public abstract class recalculateDiscounts : PX.Data.BQL.BqlBool.Field<recalculateDiscounts> { }
		[PXBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Recalculate Discounts")]
		public virtual bool? RecalculateDiscounts { get; set; }
		#endregion

		#region Override Manual Discounts
		public abstract class overrideManualDiscounts : PX.Data.BQL.BqlBool.Field<overrideManualDiscounts> { }
		[PXBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Override Manual Line Discounts")]
		public virtual bool? OverrideManualDiscounts { get; set; }
		#endregion

		#region OverrideManualDocGroupDiscounts
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Override Manual Group and Document Discounts")]
		public virtual Boolean? OverrideManualDocGroupDiscounts { get; set; }
		public abstract class overrideManualDocGroupDiscounts : PX.Data.BQL.BqlBool.Field<overrideManualDocGroupDiscounts> { }
		#endregion

		#region ConfirmManualAmount
		public abstract class confirmManualAmount : PX.Data.BQL.BqlBool.Field<confirmManualAmount> { }
		[PXBool()]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Create an Invoice for the Specified Manual Amount")]
		public virtual bool? ConfirmManualAmount { get; set; }
		#endregion
	}
}
