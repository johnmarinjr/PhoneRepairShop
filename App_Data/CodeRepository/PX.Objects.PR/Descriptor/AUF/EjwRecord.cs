using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR.AUF
{
	public class EjwRecord : AufRecord
	{
		public EjwRecord(int jobID, int laborItemID) : base(AufRecordType.Ejw)
		{
			JobID = jobID;
			LaborItemID = laborItemID;
		}

		public override string ToString()
		{
			object[] lineData =
			{
				JobID,
				WeekEndDate,
				WorkClassification,
				JobGross,
				TotalGross,
				AufConstants.UnusedField,
				FederalWithholding,
				StateWithholding,
				Sui,
				Sdi,
				OtherDeductions,
				JobNet,
				RegularHourlyRate,
				CashFringeRegularHourlyRate,
				RegularHoursDay1,
				RegularHoursDay2,
				RegularHoursDay3,
				RegularHoursDay4,
				RegularHoursDay5,
				RegularHoursDay6,
				RegularHoursDay7,
				OvertimeHourlyRate,
				OvertimeHoursDay1,
				OvertimeHoursDay2,
				OvertimeHoursDay3,
				OvertimeHoursDay4,
				OvertimeHoursDay5,
				OvertimeHoursDay6,
				OvertimeHoursDay7,
				AufConstants.UnusedField, // Double Time Hourly Rate
				AufConstants.UnusedField, // Doube Time Hours Day 1
				AufConstants.UnusedField, // Doube Time Hours Day 2
				AufConstants.UnusedField, // Doube Time Hours Day 3
				AufConstants.UnusedField, // Doube Time Hours Day 4
				AufConstants.UnusedField, // Doube Time Hours Day 5
				AufConstants.UnusedField, // Doube Time Hours Day 6
				AufConstants.UnusedField, // Doube Time Hours Day 7
				SSWithholding,
				MedicareWithholding,
				ClassLevel,
				ClassPercent,
				CashFringeOvertimeHourlyRate,
				AufConstants.UnusedField, // Cash Fringe Double Time Hourly Rate
				AufConstants.ManualInput, // Fringe Benefits Hours Day 1
				AufConstants.ManualInput, // Fringe Benefits Hours Day 2
				AufConstants.ManualInput, // Fringe Benefits Hours Day 3
				AufConstants.ManualInput, // Fringe Benefits Hours Day 4
				AufConstants.ManualInput, // Fringe Benefits Hours Day 5
				AufConstants.ManualInput, // Fringe Benefits Hours Day 6
				AufConstants.ManualInput, // Fringe Benefits Hours Day 7
				CheckNumber,
				WorkClassificationCode,
				AufConstants.UnusedField,
				IsPrevailingWage == false ? AufConstants.SelectedBox : AufConstants.NotSelectedBox,
				CheckDate,
				TotalHoursDay1,
				TotalHoursDay2,
				TotalHoursDay3,
				TotalHoursDay4,
				TotalHoursDay5,
				TotalHoursDay6,
				TotalHoursDay7,
				UnionDues
			};

			return FormatLine(lineData);
		}

		public virtual int JobID { get; set; }
		public virtual int LaborItemID { get; set; }
		public virtual DateTime WeekEndDate { get; set; }
		public virtual string WorkClassification { get; set; }
		public virtual decimal? JobGross { get; set; }
		public virtual decimal? TotalGross { get; set; }
		public virtual decimal? FederalWithholding { get; set; }
		public virtual decimal? StateWithholding { get; set; }
		public virtual decimal? Sui { get; set; }
		public virtual decimal? Sdi { get; set; }
		public virtual decimal? OtherDeductions { get; set; }
		public virtual decimal? JobNet { get; set; }
		public virtual decimal? CashFringeRegularHourlyRate
		{
			get => JobTotalRegularHours != 0 ? JobTotalRegularFringe / JobTotalRegularHours : new decimal?();
			set { } // No-op, kept to avoid breaking changes in 2021R1
		}
		public virtual decimal? RegularHourlyRate
		{
			get => JobTotalRegularHours != 0 ? JobTotalRegularWages / JobTotalRegularHours :
					JobTotalOvertimeHours != 0 ? (OvertimeMultiplier != 0 ? (JobTotalOvertimeWages / JobTotalOvertimeHours) / OvertimeMultiplier : (JobTotalOvertimeWages / JobTotalOvertimeHours) / 1.5m) : new decimal?();
			set { } // No-op, kept to avoid breaking changes in 2021R1
		}
		public virtual decimal? RegularHoursDay1 { get; set; }
		public virtual decimal? RegularHoursDay2 { get; set; }
		public virtual decimal? RegularHoursDay3 { get; set; }
		public virtual decimal? RegularHoursDay4 { get; set; }
		public virtual decimal? RegularHoursDay5 { get; set; }
		public virtual decimal? RegularHoursDay6 { get; set; }
		public virtual decimal? RegularHoursDay7 { get; set; }
		public virtual decimal? OvertimeHourlyRate
		{
			get => JobTotalOvertimeHours != 0 ? JobTotalOvertimeWages / JobTotalOvertimeHours : new decimal?();			
			set { } // No-op, kept to avoid breaking changes in 2021R1
		}
		public virtual decimal? OvertimeHoursDay1 { get; set; }
		public virtual decimal? OvertimeHoursDay2 { get; set; }
		public virtual decimal? OvertimeHoursDay3 { get; set; }
		public virtual decimal? OvertimeHoursDay4 { get; set; }
		public virtual decimal? OvertimeHoursDay5 { get; set; }
		public virtual decimal? OvertimeHoursDay6 { get; set; }
		public virtual decimal? OvertimeHoursDay7 { get; set; }
		public virtual decimal? SSWithholding { get; set; }
		public virtual decimal? MedicareWithholding { get; set; }
		public virtual char? ClassLevel { get; set; }
		public virtual int? ClassPercent { get; set; }
		public virtual decimal? CashFringeOvertimeHourlyRate
		{
			get => JobTotalOvertimeHours != 0 ? JobTotalOvertimeFringe / JobTotalOvertimeHours : new decimal?();
			set { } // No-op, kept to avoid breaking changes in 2021R1
		}
		public virtual string CheckNumber { get; set; }
		public virtual string WorkClassificationCode { get; set; }
		public virtual bool? IsPrevailingWage { get; set; }
		public virtual DateTime? CheckDate { get; set; }
		public virtual decimal? TotalHoursDay1 { get; set; }
		public virtual decimal? TotalHoursDay2 { get; set; }
		public virtual decimal? TotalHoursDay3 { get; set; }
		public virtual decimal? TotalHoursDay4 { get; set; }
		public virtual decimal? TotalHoursDay5 { get; set; }
		public virtual decimal? TotalHoursDay6 { get; set; }
		public virtual decimal? TotalHoursDay7 { get; set; }
		public virtual decimal? UnionDues { get; set; }
		public virtual decimal JobTotalRegularHours => (RegularHoursDay1 + RegularHoursDay2 + RegularHoursDay3 + RegularHoursDay4 +
			RegularHoursDay5 + RegularHoursDay6 + RegularHoursDay7).GetValueOrDefault();
		public virtual decimal JobTotalOvertimeHours => (OvertimeHoursDay1 + OvertimeHoursDay2 + OvertimeHoursDay3 + OvertimeHoursDay4 +
			OvertimeHoursDay5 + OvertimeHoursDay6 + OvertimeHoursDay7).GetValueOrDefault();
		public virtual decimal JobTotalRegularWages { get; set; } = 0;
		public virtual decimal JobTotalRegularFringe { get; set; } = 0;
		public virtual decimal JobTotalOvertimeWages { get; set; } = 0;
		public virtual decimal JobTotalOvertimeFringe { get; set; } = 0;
		public virtual decimal OvertimeMultiplier { get; set; } = 0;

		#region Avoid breaking chages in 2021R1
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public EjwRecord(int jobID, DateTime weekEndDate) : base(AufRecordType.Ejw)
		{
			JobID = jobID;
			WeekEndDate = weekEndDate;
		}
		#endregion Avoid breaking chages in 2021R1
	}

	public static class EjwRecordExtensions
	{
		public static IEnumerable<EjwRecord> Aggregate(this IEnumerable<EjwRecord> allRecords, List<GenRecord> genList)
		{
			foreach (IGrouping<CertifiedJobKey, EjwRecord> group in allRecords.GroupBy(x => new CertifiedJobKey(x.JobID, x.LaborItemID)))
			{
				GenRecord referencePayment = genList.FirstOrDefault(x => x.IsRegularPaycheck) ?? genList.First();
				yield return new EjwRecord(group.Key.ProjectID, group.Key.LaborItemID)
				{
					WeekEndDate = referencePayment.PeriodEnd.Value,
					WorkClassification = group.First().WorkClassification,
					JobGross = group.Sum(x => x.JobGross.GetValueOrDefault()),
					TotalGross = genList.Sum(x => x.GrossPay.GetValueOrDefault()),
					FederalWithholding = genList.Sum(x => x.FederalWithheld.GetValueOrDefault()),
					StateWithholding = genList.Sum(x => x.StateWithheld.GetValueOrDefault()),
					Sui = genList.Sum(x => x.SuiWithheld.GetValueOrDefault()),
					Sdi = genList.Sum(x => x.SdiWithheld.GetValueOrDefault()),
					OtherDeductions = group.Sum(x => x.OtherDeductions.GetValueOrDefault()),
					JobNet = genList.Sum(x => x.NetPay.GetValueOrDefault()),
					RegularHoursDay1 = group.Sum(x => x.RegularHoursDay1.GetValueOrDefault()),
					RegularHoursDay2 = group.Sum(x => x.RegularHoursDay2.GetValueOrDefault()),
					RegularHoursDay3 = group.Sum(x => x.RegularHoursDay3.GetValueOrDefault()),
					RegularHoursDay4 = group.Sum(x => x.RegularHoursDay4.GetValueOrDefault()),
					RegularHoursDay5 = group.Sum(x => x.RegularHoursDay5.GetValueOrDefault()),
					RegularHoursDay6 = group.Sum(x => x.RegularHoursDay6.GetValueOrDefault()),
					RegularHoursDay7 = group.Sum(x => x.RegularHoursDay7.GetValueOrDefault()),
					OvertimeHoursDay1 = group.Sum(x => x.OvertimeHoursDay1.GetValueOrDefault()),
					OvertimeHoursDay2 = group.Sum(x => x.OvertimeHoursDay2.GetValueOrDefault()),
					OvertimeHoursDay3 = group.Sum(x => x.OvertimeHoursDay3.GetValueOrDefault()),
					OvertimeHoursDay4 = group.Sum(x => x.OvertimeHoursDay4.GetValueOrDefault()),
					OvertimeHoursDay5 = group.Sum(x => x.OvertimeHoursDay5.GetValueOrDefault()),
					OvertimeHoursDay6 = group.Sum(x => x.OvertimeHoursDay6.GetValueOrDefault()),
					OvertimeHoursDay7 = group.Sum(x => x.OvertimeHoursDay7.GetValueOrDefault()),
					SSWithholding = genList.Sum(x => x.SSWithheld.GetValueOrDefault()),
					MedicareWithholding = genList.Sum(x => x.MedicareWithheld.GetValueOrDefault()),
					ClassLevel = group.First().ClassLevel,
					ClassPercent = group.First().ClassPercent,
					CheckNumber = referencePayment.CheckNumber,
					WorkClassificationCode = group.First().WorkClassificationCode,
					IsPrevailingWage = group.All(x => x.IsPrevailingWage == true),
					CheckDate = referencePayment.CheckDate,
					TotalHoursDay1 = group.Sum(x => x.TotalHoursDay1.GetValueOrDefault()),
					TotalHoursDay2 = group.Sum(x => x.TotalHoursDay2.GetValueOrDefault()),
					TotalHoursDay3 = group.Sum(x => x.TotalHoursDay3.GetValueOrDefault()),
					TotalHoursDay4 = group.Sum(x => x.TotalHoursDay4.GetValueOrDefault()),
					TotalHoursDay5 = group.Sum(x => x.TotalHoursDay5.GetValueOrDefault()),
					TotalHoursDay6 = group.Sum(x => x.TotalHoursDay6.GetValueOrDefault()),
					TotalHoursDay7 = group.Sum(x => x.TotalHoursDay7.GetValueOrDefault()),
					UnionDues = group.Sum(x => x.UnionDues.GetValueOrDefault()),
					JobTotalRegularWages = group.Sum(x => x.JobTotalRegularWages),
					JobTotalRegularFringe = group.Sum(x => x.JobTotalRegularFringe),
					JobTotalOvertimeWages = group.Sum(x => x.JobTotalOvertimeWages),
					JobTotalOvertimeFringe = group.Sum(x => x.JobTotalOvertimeFringe)
				};
			}
		}
	}
}
