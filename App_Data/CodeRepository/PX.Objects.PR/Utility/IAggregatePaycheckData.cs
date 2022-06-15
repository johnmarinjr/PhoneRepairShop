namespace PX.Objects.PR
{
	public interface IAggregatePaycheckData
	{
		int? PeriodNbr { get; set; }
		int? Week { get; set; }
		int? Month { get; set; }
		string Year { get; set; }
	}
}
