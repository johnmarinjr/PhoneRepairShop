namespace PX.Objects.PR
{
	public interface IPaycheckDetail<TKey>
	{
		int? BranchID { get; set; }
		TKey ParentKeyID { set; }
		decimal? Amount { get; set; }
	}

	public interface IPaycheckExpenseDetail<TKey> : IPaycheckDetail<TKey>
	{
		int? ProjectID { get; set; }
		int? ProjectTaskID { get; set; }
		int? CostCodeID { get; set; }
		string EarningTypeCD { get; set; }
		int? LabourItemID { get; set; }
	}
}
