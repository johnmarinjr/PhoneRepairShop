using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.CS;

namespace PX.Objects.SO
{
	/// <exclude/>
	public class CreateShipmentArgs
	{
		public SOOrderEntry Graph { get; set; }
		public bool MassProcess { get; set; }
		public SOOrder Order { get; set; }
		public int? SiteID { get; set; }
		public DateTime? ShipDate { get; set; }
		public bool? UseOptimalShipDate { get; set; }
		public string Operation { get; set; }
		public DocumentList<SOShipment> ShipmentList { get; set; }
		public PXQuickProcess.ActionFlow QuickProcessFlow { get; set; } = PXQuickProcess.ActionFlow.NoFlow;
	}
}
