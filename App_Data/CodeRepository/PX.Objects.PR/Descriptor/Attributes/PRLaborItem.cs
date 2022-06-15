using PX.Data;
using PX.Objects.PM;
using System;

namespace PX.Objects.PR
{
	public class PRLaborItemAttribute : PMLaborItemAttribute
	{
		public PRLaborItemAttribute(Type project, Type earningType, Type employeeSearch) : base(project, earningType, employeeSearch) { }

		public override void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if(sender.Graph.IsImportFromExcel)
			{
				return;
			}

			sender.Current = e.Row;
			base.FieldDefaulting(sender, e);
		}
	}
}
