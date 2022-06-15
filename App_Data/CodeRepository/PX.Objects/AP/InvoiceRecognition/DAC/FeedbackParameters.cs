using PX.Common;
using PX.Data;
using PX.Objects.AP.InvoiceRecognition.Feedback;
using System;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition.DAC
{
	[PXInternalUseOnly]
	[PXHidden]
	public class FeedbackParameters : IBqlTable
	{
		internal DocumentFeedbackBuilder FeedbackBuilder { get; set; }

		internal Dictionary<string, Uri> Links { get; set; }
	}
}
