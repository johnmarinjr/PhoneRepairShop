using System;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.GL;
using PX.Objects.AP;
using PX.Objects.EP;
using PX.Objects.CM;
using PX.Objects.CS;

namespace PX.Objects.FA
{
	[Serializable]
	[PXProjection(typeof(SelectFrom<FATran>
		.CrossJoin<APAROrd>
		.Where<FATran.released.IsEqual<True>
			.And<FATran.tranType.IsNotEqual<FATran.tranType.reconcilliationPlus>>
			.And<FATran.tranType.IsNotEqual<FATran.tranType.reconcilliationMinus>>>), Persistent = false)]
	[PXCacheName("FA transactions in GL representation")]
	public class FAProjectedGLTran : IBqlTable
	{
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(FATran.refNbr))]
		[PXUIField(DisplayName = "Reference Number", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXSelector(typeof(FARegister.refNbr))]
		public virtual string RefNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true, BqlField = typeof(FATran.lineNbr))]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region AssetID
		public abstract class assetID : PX.Data.BQL.BqlInt.Field<assetID> { }
		[PXDBInt(BqlTable = typeof(FATran))]
		[PXSelector(typeof(Search2<FixedAsset.assetID, LeftJoin<FADetails, On<FADetails.assetID, Equal<FixedAsset.assetID>>,
			LeftJoin<FALocationHistory, On<FALocationHistory.assetID, Equal<FADetails.assetID>,
										And<FALocationHistory.revisionID, Equal<FADetails.locationRevID>>>,
			LeftJoin<Branch, On<Branch.branchID, Equal<FALocationHistory.locationID>>,
			LeftJoin<EPEmployee, On<EPEmployee.bAccountID, Equal<FALocationHistory.employeeID>>>>>>,
			Where<FixedAsset.recordType, Equal<FARecordType.assetType>>>),
			typeof(FixedAsset.assetCD),
			typeof(FixedAsset.description),
			typeof(FixedAsset.classID),
			typeof(FixedAsset.usefulLife),
			typeof(FixedAsset.assetTypeID),
			typeof(FADetails.status),
			typeof(Branch.branchCD),
			typeof(EPEmployee.acctName),
			typeof(FALocationHistory.department),
			Filterable = true,
			SubstituteKey = typeof(FixedAsset.assetCD),
			DescriptionField = typeof(FixedAsset.description))]
		[PXUIField(DisplayName = "Asset", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual int? AssetID
		{
			get;
			set;
		}
		#endregion
		#region BookID
		public abstract class bookID : PX.Data.BQL.BqlInt.Field<bookID> { }
		[PXDBInt(BqlTable = typeof(FATran))]
		[PXSelector(typeof(Search2<FABook.bookID,
							InnerJoin<FABookBalance, On<FABookBalance.bookID, Equal<FABook.bookID>>>,
							Where<FABookBalance.assetID, Equal<Current<FixedAsset.assetID>>>>),
			SubstituteKey = typeof(FABook.bookCode),
			DescriptionField = typeof(FABook.description))]
		[PXUIField(DisplayName = "Book", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual int? BookID
		{
			get;
			set;
		}
		#endregion
		#region FinPeriodID
		public abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[FABookPeriodID(
			assetSourceType: typeof(FATran.assetID),
			bookSourceType: typeof(FATran.bookID),
			BqlTable = typeof(FATran))]
		public virtual string FinPeriodID
		{
			get;
			set;
		}
		#endregion

		#region GLAccountID
		public abstract class gLAccountID : PX.Data.BQL.BqlInt.Field<gLAccountID> { }
		[Account(IsDBField = false)]
		[PXDBCalced(
			typeof(IIf<
				Where<APAROrd.ord.IsEqual<short0>>,
					FATran.creditAccountID,
				FATran.debitAccountID>),
			typeof(int))]
		public virtual int? GLAccountID
		{
			get;
			set;
		}
		#endregion
		#region GLSubID
		public abstract class gLSubID : PX.Data.BQL.BqlInt.Field<gLSubID> { }
		[SubAccount(typeof(gLAccountID), IsDBField = false)]
		[PXDBCalced(
			typeof(IIf<
				Where<APAROrd.ord.IsEqual<short0>>,
					FATran.creditSubID,
				FATran.debitSubID>),
			typeof(int))]
		public virtual int? GLSubID
		{
			get;
			set;
		}
		#endregion
		#region GLBranchID
		public abstract class gLBranchID : PX.Data.BQL.BqlInt.Field<gLBranchID> { }
		[Branch(IsDBField = false)]
		[PXDBCalced(
			typeof(Switch<
				Case<Where<APAROrd.ord.IsEqual<short1>
					.And<FATran.tranType.IsEqual<FATran.tranType.transferDepreciation>>>,
					FATran.srcBranchID,
				Case<Where<APAROrd.ord.IsEqual<short0>
					.And<FATran.tranType.IsEqual<FATran.tranType.transferPurchasing>>>,
					FATran.srcBranchID>>,
				FATran.branchID>),
			typeof(int))]
		public virtual int? GLBranchID
		{
			get;
			set;
		}
		#endregion
		#region SignedAmt
		public abstract class signedAmt : PX.Data.BQL.BqlDecimal.Field<signedAmt> { }
		[PXBaseCury]
		[PXDBCalced(
			typeof(Switch<
				Case<Where<APAROrd.ord.IsEqual<short1>
						.And<FATran.tranType.IsEqual<FATran.tranType.purchasingMinus>>
						.And<FATran.debitAccountID.IsEqual<FATran.creditAccountID>>
						.And<FATran.debitSubID.IsEqual<FATran.creditSubID>>
					.Or<APAROrd.ord.IsEqual<short0>
						.And<FATran.tranType.IsEqual<FATran.tranType.purchasingPlus>>
						.And<FATran.debitAccountID.IsEqual<FATran.creditAccountID>>
						.And<FATran.debitSubID.IsEqual<FATran.creditSubID>>>>,
					decimal0,
				Case<Where<APAROrd.ord.IsEqual<short1>>,
					FATran.tranAmt,
				Case<Where<APAROrd.ord.IsEqual<short0>>,
					Mult<decimal_1, FATran.tranAmt>>>>>),
			typeof(decimal))]
		public virtual decimal? SignedAmt
		{
			get;
			set;
		}
		#endregion
	}
}
