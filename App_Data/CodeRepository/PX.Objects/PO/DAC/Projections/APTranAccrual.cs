using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.GL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
    [PXHidden]
    [PXProjection(typeof(
        SelectFrom<POAccrualSplit>
            .InnerJoin<POReceiptLine>
                .On<POReceiptLine.iNReleased.IsEqual<True>
                .And<POAccrualSplit.FK.ReceiptLine>>
        .Where<POAccrualSplit.finPeriodID.IsLessEqual<POAccrualInquiryFilter.finPeriodID.FromCurrent.Value>>
        .AggregateTo<
            GroupBy<POAccrualSplit.aPDocType>,
            GroupBy<POAccrualSplit.aPRefNbr>,
            GroupBy<POAccrualSplit.aPLineNbr>,
            Sum<POAccrualSplit.accruedCost>,
            Sum<POAccrualSplit.pPVAmt>,
            Sum<POAccrualSplit.taxAccruedCost>>
        ), Persistent = false)]
    public class APTranAccrual : IBqlTable
    {
		#region APDocType
		[APDocType.List]
		[PXDBString(3, IsKey = true, IsFixed = true, BqlField = typeof(POAccrualSplit.aPDocType))]
		public virtual string APDocType { get; set; }
		public abstract class aPDocType : BqlString.Field<aPDocType> { }
		#endregion

		#region APRefNbr
		[PXDBString(15, IsUnicode = true, IsKey = true, BqlField = typeof(POAccrualSplit.aPRefNbr))]
		public virtual string APRefNbr { get; set; }
        public abstract class aPRefNbr : BqlString.Field<aPRefNbr> { }
        #endregion

        #region APLineNbr
		[PXDBInt(IsKey = true, BqlField = typeof(POAccrualSplit.aPLineNbr))]
		public virtual int? APLineNbr { get; set; }
        public abstract class aPLineNbr : BqlInt.Field<aPLineNbr> { }
		#endregion

		#region AccruedCost
		public abstract class accruedCost : BqlDecimal.Field<accruedCost> { }
		[PXDBDecimal(4, BqlField = typeof(POAccrualSplit.accruedCost))]
		public virtual decimal? AccruedCost { get; set; }
		#endregion

		#region PPVAmt
		public abstract class pPVAmt : BqlDecimal.Field<pPVAmt> { }
		[PXDBDecimal(4, BqlField = typeof(POAccrualSplit.pPVAmt))]
		public virtual decimal? PPVAmt { get; set; }
        #endregion

        #region TaxAccruedCost
        [PXDBDecimal(4, BqlField = typeof(POAccrualSplit.taxAccruedCost))]
        public virtual decimal? TaxAccruedCost { get; set; }
        public abstract class taxAccruedCost : BqlDecimal.Field<taxAccruedCost> { }
        #endregion
    }
}
