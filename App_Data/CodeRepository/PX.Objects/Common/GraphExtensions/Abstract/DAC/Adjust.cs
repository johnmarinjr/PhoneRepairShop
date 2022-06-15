using System;
using PX.Data;

namespace PX.Objects.Common.GraphExtensions.Abstract.DAC
{
	public class Adjust : PXMappedCacheExtension, IFinAdjust
	{
		#region AdjgBranchID
		public abstract class adjgBranchID : IBqlField { }

		public virtual int? AdjgBranchID { get; set; }
		#endregion
		#region AdjgFinPeriodID
		public abstract class adjgFinPeriodID : IBqlField { }

		public virtual String AdjgFinPeriodID { get; set; }
		#endregion
		#region AdjgTranPeriodID
		public abstract class adjgTranPeriodID : IBqlField { }

		public virtual String AdjgTranPeriodID { get; set; }
		#endregion
		#region AdjdBranchID
		public abstract class adjdBranchID : IBqlField { }

		public virtual int? AdjdBranchID { get; set; }
		#endregion
		#region AdjdFinPeriodID
		public abstract class adjdFinPeriodID : IBqlField { }

		public virtual String AdjdFinPeriodID { get; set; }
		#endregion
		#region AdjdTranPeriodID
		public abstract class adjdTranPeriodID : IBqlField { }

		public virtual String AdjdTranPeriodID { get; set; }

		#endregion
		#region IAdjustment
		public abstract class adjdCuryInfoID : IBqlField { }
		public long? AdjdCuryInfoID { get; set; }

		public abstract class adjdOrigCuryInfoID : IBqlField { }
		public long? AdjdOrigCuryInfoID { get; set; }

		public abstract class adjgCuryInfoID : IBqlField { }
		public long? AdjgCuryInfoID { get; set; }

		public abstract class adjgDocDate : IBqlField { }
		public DateTime? AdjgDocDate { get; set; }

		public abstract class adjdDocDate : IBqlField { }
		public DateTime? AdjdDocDate { get; set; }

		public abstract class curyAdjgAmt : IBqlField { }
		public decimal? CuryAdjgAmt { get; set; }

		public abstract class curyAdjgDiscAmt : IBqlField { }
		public decimal? CuryAdjgDiscAmt { get; set; }

		public abstract class curyAdjdAmt : IBqlField { }
		public decimal? CuryAdjdAmt { get; set; }

		public abstract class curyAdjdDiscAmt : IBqlField { }
		public decimal? CuryAdjdDiscAmt { get; set; }

		public abstract class adjAmt : IBqlField { }
		public decimal? AdjAmt { get; set; }

		public abstract class adjDiscAmt : IBqlField { }
		public decimal? AdjDiscAmt { get; set; }

		public abstract class rGOLAmt : IBqlField { }
		public decimal? RGOLAmt { get; set; }

		public abstract class released : IBqlField { }
		public bool? Released { get; set; }

		public abstract class voided : IBqlField { }
		public bool? Voided { get; set; }

		public abstract class reverseGainLoss : IBqlField { }
		public bool? ReverseGainLoss { get; set; }

		public abstract class curyDocBal : IBqlField { }
		public decimal? CuryDocBal { get; set; }

		public abstract class docBal : IBqlField { }
		public decimal? DocBal { get; set; }

		public abstract class curyDiscBal : IBqlField { }
		public decimal? CuryDiscBal { get; set; }

		public abstract class discBal : IBqlField { }
		public decimal? DiscBal { get; set; }

		public abstract class curyAdjgWhTaxAmt : IBqlField { }
		public decimal? CuryAdjgWhTaxAmt { get; set; }

		public abstract class curyAdjdWhTaxAmt : IBqlField { }
		public decimal? CuryAdjdWhTaxAmt { get; set; }

		public abstract class adjWhTaxAmt : IBqlField { }
		public decimal? AdjWhTaxAmt { get; set; }

		public abstract class curyWhTaxBal : IBqlField { }
		public decimal? CuryWhTaxBal { get; set; }

		public abstract class whTaxBal : IBqlField { }
		public decimal? WhTaxBal { get; set; }

		public abstract class curyAdjgPPDAmt : IBqlField { }
		public decimal? CuryAdjgPPDAmt { get; set; }

		public abstract class adjdHasPPDTaxes : IBqlField { }
		public bool? AdjdHasPPDTaxes { get; set; }

		public abstract class adjdCuryRate : IBqlField { }
		public decimal? AdjdCuryRate { get; set; }

		public abstract class adjPPDAmt : IBqlField { }
		public decimal? AdjPPDAmt { get; set; }

		public abstract class curyAdjdPPDAmt : IBqlField { }
		public decimal? CuryAdjdPPDAmt { get; set; }

		public abstract class voidAppl : IBqlField { }

		public bool? VoidAppl { get; set; }
		#endregion
	}
}
