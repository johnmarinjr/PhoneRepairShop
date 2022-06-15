﻿using System;
using PX.Data;
using PX.Objects.GL;
using PX.Objects.CR;

namespace PX.Objects.Localizations.GB.HMRC.DAC
{
	[PXProjection(typeof(Select2<Branch,
		InnerJoin<BAccountR, On<BAccountR.bAccountID, Equal<Branch.bAccountID>>,
			LeftJoin<BAccountMTDApplication, On<BAccountMTDApplication.bAccountID, Equal<Branch.bAccountID>>>>,
		Where<True, Equal<True>>>))]
	[PXHidden]
	[Serializable]
	public partial class BranchTaxInfo : PX.Data.IBqlTable
	{
		#region BAccountID
		public abstract class bAccountID : IBqlField { }
		[PXDBInt(BqlField = typeof(Branch.bAccountID), IsKey = true)]
		public virtual Int32? BAccountID { get; set; }
		#endregion
		#region BranchID
		public abstract class branchID : IBqlField { }
		[PXDBInt(BqlField = typeof(Branch.branchID))]
		public virtual Int32? BranchID { get; set; }
		#endregion
		#region TaxRegistrationID
		public abstract class taxRegistrationID : PX.Data.IBqlField { }
		[PXDBString(50, IsUnicode = true, BqlField = typeof(BAccountR.taxRegistrationID))]
		[PXUIField(DisplayName = "Tax Registration ID")]
		public virtual String TaxRegistrationID { get; set; }
		#endregion
		#region OrganizationID
		public abstract class organizationID : PX.Data.IBqlField { }
		[PXDBInt(BqlField = typeof(Branch.organizationID))]
		public virtual Int32? OrganizationID { get; set; }
		#endregion
		#region ApplicationID
		[PXDBInt(BqlField = typeof(BAccountMTDApplication.applicationID))]
		public int? ApplicationID { get; set; }
		public abstract class applicationID : PX.Data.BQL.BqlInt.Field<applicationID> { }
		#endregion
	}
}
