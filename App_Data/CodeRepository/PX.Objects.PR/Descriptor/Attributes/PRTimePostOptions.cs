using PX.Data;
using PX.Objects.EP;
using System;
using System.Collections.Generic;

namespace PX.Objects.PR
{
	public class PRTimePostOptions : EPPostOptions
	{
		public new class ListAttribute : PXStringListAttribute, IPXRowSelectedSubscriber
		{
			public ListAttribute() : base(GetValuesAndLabels())
			{
			}

			public void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
			{
				PRSetup row = e.Row as PRSetup;

				SetList(sender, row, FieldName, GetValuesAndLabels(row?.ProjectCostAssignment, false));
			}

			private static Tuple<string, string>[] GetValuesAndLabels(string projectCostAssignmentType = null, bool addAllValues = true)
			{
				List<Tuple<string, string>> valuesAndLabels = new List<Tuple<string, string>>();

				if (addAllValues || projectCostAssignmentType == ProjectCostAssignmentType.NoCostAssigned)
				{
					valuesAndLabels.Add(new Tuple<string, string>(DoNotPost, Messages.DoNotPost));
					valuesAndLabels.Add(new Tuple<string, string>(PostToOffBalance, Messages.PostFromTime));
				}
				if (addAllValues || projectCostAssignmentType == ProjectCostAssignmentType.WageCostAssigned || projectCostAssignmentType == ProjectCostAssignmentType.WageLaborBurdenAssigned)
				{
					valuesAndLabels.Add(new Tuple<string, string>(OverridePMInPayroll, Messages.OverridePMInPayroll));
					valuesAndLabels.Add(new Tuple<string, string>(OverridePMAndGLInPayroll, Messages.OverridePMAndGLInPayroll));
					valuesAndLabels.Add(new Tuple<string, string>(PostPMAndGLFromPayroll, Messages.PostPMandGLFromPayroll));
				}

				return valuesAndLabels.ToArray();
			}
		}
	}

	[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
	public class PRxEPPostOptions : EPPostOptions
	{
		public new class ListAttribute : EPPostOptions.ListAttribute, IPXRowSelectedSubscriber
		{
			public void RowSelected(PXCache sender, PXRowSelectedEventArgs e) { }
		}
	}
}
