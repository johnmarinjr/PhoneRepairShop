using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PX.Objects.PR.AUF
{
	public class EcvRecord : AufRecord
	{
		public EcvRecord() : base(AufRecordType.Ecv) { }

		public override string ToString()
		{
			List<object> lineData = new List<object>()
			{
				ElectronicOnly == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox
			};

			for (int i = 0; i < 12; i++)
			{
				lineData.Add(string.IsNullOrEmpty(OfferOfCoverageCode[i]) ? AcaOfferOfCoverage.GetDefault() : OfferOfCoverageCode[i]);
				lineData.Add(MinimumIndividualContribution[i] == 0 ? new decimal?() : MinimumIndividualContribution[i]);
				lineData.Add(SafeHarborCode[i]);
			}

			lineData.Add(PolicyOriginCode);
			lineData.Add(SelfInsuredEmployee == true ? AufConstants.SelectedBox : AufConstants.NotSelectedBox);
			lineData.Add(PlanStartMonth);

			return FormatLine(lineData.ToArray());
		}

		public virtual bool? ElectronicOnly { get; set; }
		public virtual string[] OfferOfCoverageCode { get; set; } = new string[12];
		public virtual decimal?[] MinimumIndividualContribution { get; set; } = new decimal?[12];
		public virtual string[] SafeHarborCode { get; set; } = new string[12];
		public virtual char? PolicyOriginCode { get; set; }
		public virtual bool? SelfInsuredEmployee { get; set; }
		public virtual int? PlanStartMonth { get; set; }
	}
}
