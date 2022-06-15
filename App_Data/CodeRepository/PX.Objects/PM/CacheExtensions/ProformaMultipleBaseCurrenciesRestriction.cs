using System;
using PX.Data;
using PX.Objects.Common;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.CR;
using PX.Objects.AR;

namespace PX.Objects.PM
{
	public sealed class ProformaMultipleBaseCurrenciesRestriction : PXCacheExtension<PMProformaVisibilityRestriction, PMProforma>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		#region BranchID
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFormula(typeof(Switch<
				Case<Where<Current2<PMProforma.branchID>, IsNotNull>,
					Current2<PMProforma.branchID>,
				Case<Where<PMProforma.locationID, IsNull, And<UnattendedMode, Equal<True>>>, Null,
				Case<Where<Location.bAccountID, Equal<Current<PMProforma.customerID>>, And<Location.locationID, Equal<Current<PMProforma.locationID>>>>, Location.cBranchID,
				Case<Where<PMProforma.customerID, IsNotNull,
						And<Not<Selector<PMProforma.customerID, Customer.cOrgBAccountID>, RestrictByBranch<Current2<PMProforma.branchID>>>>>,
					Null,
				Case<Where<PMProforma.customerID, IsNotNull,
						And<Selector<PMProforma.customerID, Customer.baseCuryID>, NotEqual<Current<AccessInfo.baseCuryID>>>>,
					Null>>>>>,
				Current<AccessInfo.branchID>>))]
		public int? BranchID { get; set; }
		#endregion

		#region CustomerID
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRestrictor(typeof(Where<Current2<branchBaseCuryID>, IsNull,
			Or<Customer.baseCuryID, Equal<Current2<branchBaseCuryID>>>>), null)]
		public int? CustomerID { get; set; }
		#endregion

		#region BranchBaseCuryID
		public new abstract class branchBaseCuryID : PX.Data.BQL.BqlString.Field<branchBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<PMProforma.branchID, Branch.baseCuryID>))]
		public string BranchBaseCuryID { get; set; }
		#endregion

		#region CustomerBaseCuryID
		public new abstract class customerBaseCuryID : PX.Data.BQL.BqlString.Field<customerBaseCuryID> { }

		[PXString]
		[PXFormula(typeof(Selector<PMProforma.customerID, Customer.baseCuryID>))]
		public string CustomerBaseCuryID { get; set; }
		#endregion
	}
}