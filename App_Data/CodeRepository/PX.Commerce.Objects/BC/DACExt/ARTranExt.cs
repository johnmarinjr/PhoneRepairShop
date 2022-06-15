using PX.Commerce.Core;
using PX.Data;
using PX.Objects.AR;
using System;

namespace PX.Commerce.Objects
{
	[Serializable]
	public sealed class BCARTranExt : PXCacheExtension<ARTran>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		#region AssociatedOrderLineNbr
		public abstract class associatedOrderLineNbr : PX.Data.BQL.BqlInt.Field<associatedOrderLineNbr> { }
		[PXDBInt()]
		[PXUIField(DisplayName = "Associated Order Line Nbr.", Visible = false,  Enabled = false)]
		public int? AssociatedOrderLineNbr { get; set; }
		#endregion

		#region GiftMessage
		public abstract class giftMessage : PX.Data.BQL.BqlString.Field<giftMessage> { }
		[PXDBString(200, IsUnicode = true)]
		[PXUIField(DisplayName = "Gift Message", Visible = false,  Enabled = false)]
		public string GiftMessage { get; set; }
		#endregion
	}
}
