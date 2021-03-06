using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Objects.PR.AUF
{
	public class AleRecord : AufRecord
	{
		public AleRecord() : base(AufRecordType.Ale) { }

		public override string ToString()
		{
			List<object> lineData = new List<object>()
			{
				IsDesignatedGovernmentEntity == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox,
				IsAggregateGroupMember == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox,
				IsSelfInsured == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox,
				UsesCoeQualifyingOfferMethod == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox,
				AufConstants.UnusedField, // COE-Qualifying Offer Method Transition Relief
				AufConstants.UnusedField, // COE-Section 4980H Transition Relief
				UsesCoe98PctMethod == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox
			};

			for (int i = 0; i < 12; i++)
			{
				lineData.Add(MecIndicator == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox);
				lineData.Add(FteCount[i]);
				lineData.Add(EmployeeCount[i]);
				lineData.Add(IsAggregateGroupMember == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox);
				lineData.Add(AufConstants.UnusedField); // Transition Relief Indicator
			}

			lineData.Add(IsAuthoritativeTransmittal == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox);

			return FormatLine(lineData.ToArray());
		}

		public virtual bool? IsDesignatedGovernmentEntity { get; set; }
		public virtual bool? IsAggregateGroupMember { get; set; }
		public virtual bool? IsSelfInsured { get; set; }
		public virtual bool? UsesCoeQualifyingOfferMethod { get; set; }
		public virtual bool? UsesCoe98PctMethod { get; set; }
		public virtual bool? MecIndicator { get; set; }
		public virtual int[] FteCount { get; set; } = Enumerable.Repeat(0, 12).ToArray();
		public virtual int[] EmployeeCount { get; set; } = Enumerable.Repeat(0, 12).ToArray();
		public virtual bool? IsAuthoritativeTransmittal { get; set; }
	}
}
