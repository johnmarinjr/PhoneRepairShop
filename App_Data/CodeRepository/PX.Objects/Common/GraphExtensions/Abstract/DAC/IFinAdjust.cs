namespace PX.Objects.Common.GraphExtensions.Abstract.DAC
{
	public interface IFinAdjust : IAdjustment
	{
		int? AdjdBranchID { get; set; }
		int? AdjgBranchID { get; set; }
		decimal? CuryAdjgPPDAmt { get; set; }
		bool? AdjdHasPPDTaxes { get; set; }
		decimal? AdjdCuryRate { get; set; }
		decimal? AdjPPDAmt { get; set; }
		decimal? CuryAdjdPPDAmt { get; set; }
		bool? VoidAppl { get; set; }
	}
}
