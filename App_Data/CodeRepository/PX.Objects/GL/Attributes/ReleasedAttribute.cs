using System;
using System.Collections;
using PX.Data;
using PX.Objects.GL.DAC;
using PX.Objects.CR;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.PM;

namespace PX.Objects.GL
{
	[PXDBBool]
	public class ReleasedAttribute : PXAggregateAttribute, IPXRowDeletingSubscriber
	{
		public bool PreventDeletingReleased { get; set; } = false;

		public void RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
		{
			if (PreventDeletingReleased
				&& sender.GetValue(e.Row, _FieldName) != null
				&& (bool?)sender.GetValue(e.Row, _FieldName) == true)
			{
				throw new PXException(InfoMessages.ReleasedDocCannotBeDeleted);
			}
		}
	}
}
