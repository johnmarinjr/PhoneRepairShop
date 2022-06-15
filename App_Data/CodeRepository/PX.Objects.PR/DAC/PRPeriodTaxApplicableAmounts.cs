using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRPeriodTaxApplicableAmounts)]
	[Serializable]
	[PeriodTaxApplicableAmountsAccumulator]
	public class PRPeriodTaxApplicableAmounts : IBqlTable, IAggregatePaycheckData
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRPeriodTaxApplicableAmounts>.By<year, employeeID, taxID, wageTypeID, isSupplemental, periodNbr>
		{
			public static PRPeriodTaxApplicableAmounts Find(PXGraph graph, string year, int? employeeID, int? taxID, int? wageTypeID, bool? isSupplemental, int? periodNbr) =>
				FindBy(graph, year, employeeID, taxID, wageTypeID, isSupplemental, periodNbr);
		}

		public static class FK
		{
			public class Employee : PREmployee.PK.ForeignKeyOf<PRPeriodTaxApplicableAmounts>.By<employeeID> { }
			public class Tax : PRTaxCode.PK.ForeignKeyOf<PRPeriodTaxApplicableAmounts>.By<taxID> { }
		}
		#endregion

		#region Year
		[PXDBString(4, IsKey = true, IsFixed = true)]
		public virtual string Year { get; set; }
		public abstract class year : PX.Data.BQL.BqlString.Field<year> { }
		#endregion

		#region EmployeeID
		[PXDBInt(IsKey = true)]
		public virtual int? EmployeeID { get; set; }
		public abstract class employeeID : PX.Data.BQL.BqlInt.Field<employeeID> { }
		#endregion

		#region TaxID
		[PXDBInt(IsKey = true)]
		public virtual int? TaxID { get; set; }
		public abstract class taxID : PX.Data.BQL.BqlInt.Field<taxID> { }
		#endregion

		#region WageTypeID
		[PXDBInt(IsKey = true)]
		public virtual int? WageTypeID { get; set; }
		public abstract class wageTypeID : PX.Data.BQL.BqlInt.Field<wageTypeID> { }
		#endregion

		#region IsSupplemental
		[PXDBBool(IsKey = true)]
		public virtual bool? IsSupplemental { get; set; }
		public abstract class isSupplemental : PX.Data.BQL.BqlBool.Field<isSupplemental> { }
		#endregion

		#region PeriodNbr
		[PXDBInt(IsKey = true)]
		public virtual int? PeriodNbr { get; set; }
		public abstract class periodNbr : PX.Data.BQL.BqlInt.Field<periodNbr> { }
		#endregion

		#region Week
		[PXDBInt]
		public virtual int? Week { get; set; }
		public abstract class week : PX.Data.BQL.BqlInt.Field<week> { }
		#endregion

		#region Month
		[PXDBInt]
		public virtual int? Month { get; set; }
		public abstract class month : PX.Data.BQL.BqlInt.Field<month> { }
		#endregion

		#region AmountAllowed
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? AmountAllowed { get; set; }
		public abstract class amountAllowed : PX.Data.BQL.BqlDecimal.Field<amountAllowed> { }
		#endregion

		#region System Columns
		#region CreatedByID
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		#endregion

		#region CreatedByScreenID
		[PXDBCreatedByScreenID()]
		public virtual string CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		#endregion

		#region CreatedDateTime
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		#endregion

		#region LastModifiedByID
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		#endregion

		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID()]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		#endregion

		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#endregion System Columns
	}

	public class PeriodTaxApplicableAmountsAccumulatorAttribute : PXAccumulatorAttribute
	{
		public PeriodTaxApplicableAmountsAccumulatorAttribute()
		{
			SingleRecord = true;
		}

		protected override bool PrepareInsert(PXCache sender, object row, PXAccumulatorCollection columns)
		{
			if (!base.PrepareInsert(sender, row, columns))
			{
				return false;
			}

			var record = row as PRPeriodTaxApplicableAmounts;
			if (record == null)
			{
				return false;
			}

			columns.Update<PRPeriodTaxApplicableAmounts.amountAllowed>(record.AmountAllowed, PXDataFieldAssign.AssignBehavior.Summarize);

			return true;
		}
	}
}