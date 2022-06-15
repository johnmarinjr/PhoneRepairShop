using PX.Data;
using PX.Objects.PO;

namespace PX.Objects.CN.Subcontracts.PM.Descriptor
{
	public class RelatedDocumentType
	{
		public const string AllCommitmentsType = "ALL";
		public const string PurchaseOrderType = POOrderType.RegularOrder + " + " + POOrderType.ProjectDropShip; //string.Join(" + ", new object[] { POOrderType.RegularOrder, POOrderType.ProjectDropShip })
		public const string SubcontractType = POOrderType.RegularSubcontract;

		public const string AllCommitmentsLabel = "All Commitments";
		public const string PurchaseOrderLabel = "Purchase Order";
		public const string SubcontractLabel = "Subcontract";

		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute() : base(
				new[]
				{
					Pair(AllCommitmentsType, AllCommitmentsLabel),
					Pair(PurchaseOrderType, PurchaseOrderLabel),
					Pair(SubcontractType, SubcontractLabel)
				})
			{ }
		}

		public class all : PX.Data.BQL.BqlString.Constant<all>
		{
			public all() : base(AllCommitmentsType) { }
		}

		public class purchaseOrder : PX.Data.BQL.BqlString.Constant<purchaseOrder>
		{
			public purchaseOrder() : base(PurchaseOrderType) { }
		}

		public class subcontract : PX.Data.BQL.BqlString.Constant<subcontract>
		{
			public subcontract() : base(SubcontractType) { }
		}
	}
}
