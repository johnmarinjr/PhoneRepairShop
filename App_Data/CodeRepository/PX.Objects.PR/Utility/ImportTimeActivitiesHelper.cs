using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.EP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PR
{
	public static class ImportTimeActivitiesHelper
	{
		#region Data View Delegate Helper
		public static IEnumerable TimeActivitiesDelegate(PXGraph graph, PXView timeActivitiesView, PXCache earningDetailsCache, ImportTimeActivitiesFilter filter, out bool selectedTimeActivitiesExist)
		{
			int startRow = PXView.StartRow;
			int totalRows = 0;

			PXView query = new PXView(graph, true, timeActivitiesView.BqlSelect);
			List<PXView.PXSearchColumn> searchColumns = timeActivitiesView.GetContextualExternalSearchColumns();
			PXFilterRow[] externalFilters = timeActivitiesView.GetExternalFilters();

			IEnumerable<PXResult<PMTimeActivityExt, PREmployee, GL.Branch, PREarningDetail, PRBatch, PRPayment>> filteredTimeActivities = query.Select(PXView.Currents, PXView.Parameters,
				searchColumns.GetSearches(), searchColumns.GetSortColumns(), searchColumns.GetDescendings(),
				externalFilters, ref startRow, PXView.MaximumRows, ref totalRows).
				Select(x => (PXResult<PMTimeActivityExt, PREmployee, GL.Branch, PREarningDetail, PRBatch, PRPayment>)x);

			HashSet<Guid?> processedTimeActivities = new HashSet<Guid?>();
			var result = new List<PXResult<PMTimeActivityExt, PREmployee, GL.Branch, PREarningDetail, PRBatch, PRPayment>>();
			var importedTimeActivities = new List<PXResult<PMTimeActivityExt, PREmployee, GL.Branch, PREarningDetail, PRBatch, PRPayment>>();
			selectedTimeActivitiesExist = false;

			foreach (var record in filteredTimeActivities)
			{
				PMTimeActivityExt timeActivity = record;
				timeActivity = (PMTimeActivityExt)graph.Caches[typeof(PMTimeActivityExt)].Locate(timeActivity) ?? record;
				PREmployee employee = record;
				PREarningDetail earningDetail = record;
				PRBatch payrollBatch = record;
				PRPayment payment = record;

				// Because of Left Join with PREarningDetail there could be Time Activities that were added to more than one Earning Detail
				if (processedTimeActivities.Contains(timeActivity.NoteID))
					continue;

				if (timeActivity.Selected == true || PXView.MaximumRows == 1)
					selectedTimeActivitiesExist = true;

				timeActivity.EmployeeID = employee.BAccountID;
				timeActivity.ActivityDate = timeActivity.Date.Value.Date;

				if (string.IsNullOrEmpty(payrollBatch?.BatchNbr) &&
					string.IsNullOrEmpty(payment?.PaymentDocAndRef) &&
					earningDetailsCache.Cached.Cast<PREarningDetail>().FirstOrDefault(x => 
						x.SourceType == EarningDetailSourceType.TimeActivity && 
						x.SourceNoteID == timeActivity.NoteID &&
						earningDetailsCache.GetStatus(x) != PXEntryStatus.Deleted) == null)
				{
					// "null, null, null" is needed to ensure correct sorting of Time Activities that were imported to a Paycheck that was later voided.
					// The OrderBy clause (OrderBy<PREarningDetail.sourceNoteID.Asc) can be found in PRPayBatchEntry.TimeActivities and PRPayChecksAndAdjustments.TimeActivities BQL queries.
					result.Add(new PXResult<PMTimeActivityExt, PREmployee, GL.Branch, PREarningDetail, PRBatch, PRPayment>(timeActivity, record, record, null, null, null));
				}
				else
				{
					importedTimeActivities.Add(new PXResult<PMTimeActivityExt, PREmployee, GL.Branch, PREarningDetail, PRBatch, PRPayment>(timeActivity, record, record, record, record, record));
				}

				processedTimeActivities.Add(timeActivity.NoteID);
			}

			PXView.StartRow = 0;

			filter.SelectedTimeActivityExist = selectedTimeActivitiesExist;

			if (filter.ShowImportedActivities == true)
				result.AddRange(importedTimeActivities);

			return result;
		}
		#endregion

		#region Event Handler Helpers
		public static void EarningDetailSelected(PXCache cache, PREarningDetail row, bool warningOnNegativeHours, out bool warningOnHoursField)
		{
			warningOnHoursField = false;
			if (row == null)
			{
				return;
			}

			bool isEnabled = row.SourceNoteID == null || row.SourceType != EarningDetailSourceType.TimeActivity;
			PXUIFieldAttribute.SetEnabled<PREarningDetail.date>(cache, row, isEnabled);
			PXUIFieldAttribute.SetEnabled<PREarningDetail.typeCD>(cache, row, isEnabled);
			PXUIFieldAttribute.SetEnabled<PREarningDetail.projectID>(cache, row, isEnabled);
			PXUIFieldAttribute.SetEnabled<PREarningDetail.projectTaskID>(cache, row, isEnabled);
			PXUIFieldAttribute.SetEnabled<PREarningDetail.labourItemID>(cache, row, isEnabled);
			PXUIFieldAttribute.SetEnabled<PREarningDetail.unionID>(cache, row, isEnabled);
			PXUIFieldAttribute.SetEnabled<PREarningDetail.costCodeID>(cache, row, isEnabled);
			PXUIFieldAttribute.SetEnabled<PREarningDetail.workCodeID>(cache, row, isEnabled);

			string timeWarningMessage = null;
			if (warningOnNegativeHours && row.Hours < 0 && row.PaymentDocType != PayrollType.Adjustment && row.PaymentDocType != PayrollType.VoidCheck)
			{
				timeWarningMessage = Messages.InvalidNegative;
			}

			bool isCreatedFromTimeActivity = row.SourceNoteID != null && row.SourceType == EarningDetailSourceType.TimeActivity;
			if (isCreatedFromTimeActivity)
			{
				decimal timeActivityHours = Math.Round((decimal)row.TimeCardMinutes.GetValueOrDefault() / 60, 2);
				if (timeActivityHours != row.Hours)
					timeWarningMessage = string.Format(Messages.TimeActivityTimeSpentChanged, timeActivityHours);
			}
			warningOnHoursField = timeWarningMessage != null;
			PXUIFieldAttribute.SetWarning<PREarningDetail.hours>(cache, row, timeWarningMessage);
		}

		public static void TimeActivitySelected(Events.RowSelected<PMTimeActivityExt> e, PXCache timeActivitiesCache)
		{
			if (e.Row == null)
			{
				return;
			}

			PMTimeActivityExt currentTimeActivity = e.Row;
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.selected>(timeActivitiesCache, currentTimeActivity, string.IsNullOrWhiteSpace(currentTimeActivity.ErrorMessage));
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.ownerID>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.date>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.timeSpent>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.earningTypeID>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.projectID>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.projectTaskID>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.certifiedJob>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.unionID>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.labourItemID>(timeActivitiesCache, currentTimeActivity, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.workCodeID>(timeActivitiesCache, currentTimeActivity, false);

			PXUIFieldAttribute.SetWarning<PMTimeActivity.ownerID>(timeActivitiesCache, currentTimeActivity, currentTimeActivity.ErrorMessage);
		}

		public static void TimeActivitySelectionUpdated(Events.FieldUpdated<PMTimeActivity.selected> e, PXResultset<PMTimeActivityExt> timeActivities, ImportTimeActivitiesFilter filter)
		{
			if (!e.ExternalCall)
			{
				return;
			}

			filter.SelectedTimeActivityExist =
				e.NewValue as bool? == true ||
				timeActivities.FirstTableItems.Any(item => item.Selected == true);
		}

		#endregion

		#region Action Helpers
		public static void ViewTimeActivity(PXGraph graph, PREarningDetail earningDetail)
		{
			if (earningDetail?.SourceType != EarningDetailSourceType.TimeActivity || earningDetail?.SourceNoteID == null)
				return;

			EmployeeActivitiesEntry timeActivitiesGraph = PXGraph.CreateInstance<EmployeeActivitiesEntry>();

			EPEmployee employee = SelectFrom<EPEmployee>.Where<EPEmployee.bAccountID.IsEqual<P.AsInt>>.View.Select(graph, earningDetail.EmployeeID).TopFirst;

			timeActivitiesGraph.Filter.Current.OwnerID = employee.DefContactID;
			timeActivitiesGraph.Filter.Current.FromWeek = null;
			timeActivitiesGraph.Filter.Current.TillWeek = null;
			timeActivitiesGraph.Filter.Current.ProjectID = null;
			timeActivitiesGraph.Filter.Current.ProjectTaskID = null;
			timeActivitiesGraph.Filter.Current.NoteID = earningDetail.SourceNoteID;

			throw new PXRedirectRequiredException(timeActivitiesGraph, true, "Employee Time Activity");
		}

		public static void ToggleSelectedTimeActivities(PXResultset<PMTimeActivityExt> timeActivities, PXCache timeActivitiesCache, ImportTimeActivitiesFilter filter)
		{
			bool timeActivitySelected = !(filter.SelectedTimeActivityExist == true);
			bool selectedTimeActivityExist = false;

			foreach (PMTimeActivityExt timeActivity in timeActivities.FirstTableItems)
			{
				if (!string.IsNullOrWhiteSpace(timeActivity.ErrorMessage))
					continue;

				timeActivity.Selected = timeActivitySelected;
				if (timeActivitySelected)
					selectedTimeActivityExist = true;
				timeActivitiesCache.Update(timeActivity);
			}

			filter.SelectedTimeActivityExist = selectedTimeActivityExist;
		}
		#endregion

		#region Common Helpers
		public static bool CheckTimeActivityForImport(PMTimeActivityExt timeActivity, out string errorMessage)
		{
			errorMessage = null;

			if (timeActivity.ApprovalStatus == ActivityStatusListAttribute.Open)
			{
				errorMessage = Messages.ActivityOnHold;
				return false;
			}

			if (timeActivity.ApprovalStatus == ApprovalStatusListAttribute.PendingApproval)
			{
				errorMessage = Messages.ActivityPendingApproval;
				return false;
			}

			if (timeActivity.ApprovalStatus != ActivityStatusListAttribute.Released)
			{
				errorMessage = Messages.ActivityNotReleased;
				return false;
			}

			return true;
		}

		public static Dictionary<Guid?, PMTimeActivity> GetTimeActivitiesToReverse(PXGraph graph, string refNbr)
		{
			Dictionary<Guid?, PMTimeActivity> timeActivitiesToReverse = new Dictionary<Guid?, PMTimeActivity>();

			PMTimeActivity[] timeActivities = SelectFrom<PMTimeActivity>
				.InnerJoin<PREarningDetail>
					.On<PREarningDetail.sourceType.IsEqual<EarningDetailSourceType.timeActivity>
						.And<PREarningDetail.paymentDocType.IsNotEqual<PayrollType.voidCheck>>
						.And<PMTimeActivity.noteID.IsEqual<PREarningDetail.sourceNoteID>>>
				.Where<PREarningDetail.paymentRefNbr.IsEqual<P.AsString>>
				.View.Select(graph, refNbr).FirstTableItems.ToArray();

			while (timeActivities.Length > 0)
			{
				List<Guid?> originalNoteIds = new List<Guid?>();
				foreach (PMTimeActivity timeActivity in timeActivities)
				{
					timeActivitiesToReverse[timeActivity.NoteID] = timeActivity;

					if (timeActivity.OrigNoteID != null)
					{
						originalNoteIds.Add(timeActivity.OrigNoteID);
					}
				}

				timeActivities = originalNoteIds.Count > 0 ?
					SelectFrom<PMTimeActivity>.Where<PMTimeActivity.noteID.IsIn<P.AsGuid>>.View.Select(graph, originalNoteIds.ToArray()).FirstTableItems.ToArray() :
					new PMTimeActivity[0];
			}

			return timeActivitiesToReverse;
		}
		#endregion
	}

	[Serializable]
	[PXHidden]
	public class PMTimeActivityExt : PMTimeActivity
	{
		public new abstract class date : PX.Data.BQL.BqlDateTime.Field<date> { }
		public new abstract class reportedInTimeZoneID : PX.Data.BQL.BqlString.Field<reportedInTimeZoneID> { }

		#region EmployeeID
		public abstract class employeeID : BqlInt.Field<employeeID> { }
		[PXInt]
		public int? EmployeeID { get; set; }
		#endregion
		#region ActivityDate
		public abstract class activityDate : BqlDateTime.Field<activityDate> { }
		[PXDate]
		[PXUIField(DisplayName = "Date")]
		public DateTime? ActivityDate { get; set; }
		#endregion
		#region ErrorMessage
		public abstract class errorMessage : BqlString.Field<errorMessage> { }
		[PXString(IsUnicode = true)]
		public virtual string ErrorMessage { get; set; }
		#endregion

		#region TimeZoneAdjustedDate
		public abstract class timeZoneAdjustedDate : BqlDateTime.Field<timeZoneAdjustedDate> { }
		[PXDate]
		[PXUIField(DisplayName = "Date")]
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public DateTime? TimeZoneAdjustedDate { get; set; }
		#endregion
	}

	[Serializable]
	[PXHidden]
	public class ImportTimeActivitiesFilter : IBqlTable
	{
		#region ShowImportedActivities
		public abstract class showImportedActivities : BqlBool.Field<showImportedActivities> { }
		[PXBool]
		[PXUnboundDefault(true)]
		[PXUIField(DisplayName = "Show Imported Activities")]
		public bool? ShowImportedActivities { get; set; }
		#endregion
		#region SelectedTimeActivityExist
		public abstract class selectedTimeActivityExist : BqlBool.Field<selectedTimeActivityExist> { }
		[PXBool]
		[PXUnboundDefault(false)]
		public virtual bool? SelectedTimeActivityExist { get; set; }
		#endregion
	}
}
