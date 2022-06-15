using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;

namespace PX.Objects.CR
{
	[Serializable]
	[PXHidden]
	[PXPrimaryGraph(typeof(CRDuplicateValidationSetupMaint))]
	public class CRValidation : IBqlTable
	{
		#region ID
		public abstract class iD : PX.Data.BQL.BqlInt.Field<iD> { }

		[PXDBInt(IsKey = true)]
		public virtual Int32? ID { get; set; }
		#endregion

		#region Type
		public abstract class type : PX.Data.BQL.BqlString.Field<type> { }

		[PXDBString(2)]
		[ValidationTypes]
		[PXUIField(DisplayName = "Type")]
		public virtual String Type { get; set; }
		#endregion

		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }

		[PXDBString(60)]
		[PXUIField(DisplayName = "Description")]
		public virtual String Description { get; set; }
		#endregion

		#region ValidationThreshold
		public abstract class validationThreshold : PX.Data.BQL.BqlDecimal.Field<validationThreshold> { }

		[PXDBDecimal(2)]
		[PXUIField(DisplayName = "Validation Score Threshold")]
		[PXDefault(TypeCode.Decimal, "5.0")]
		public virtual decimal? ValidationThreshold { get; set; }
		#endregion

		#region ValidateOnEntry
		public abstract class validateOnEntry : PX.Data.BQL.BqlBool.Field<validateOnEntry> { }

		[PXDBBool]
		[PXUIField(DisplayName = "Validate on Entry")]
		[PXDefault(false)]
		public virtual Boolean? ValidateOnEntry { get; set; }
		#endregion

		#region GrammValidationDateTime
		public static readonly DateTime DefaultGramValidationDateTime = new DateTime(1901, 01, 01);
		public class defaultGramValidationDateTime : BqlDateTime.Constant<defaultGramValidationDateTime>
		{
			public defaultGramValidationDateTime()
				: base(DefaultGramValidationDateTime)
			{ }
		}

		public abstract class gramValidationDateTime : BqlDateTime.Field<gramValidationDateTime> { }
		[PXDBDateAndTime]
		[PXDefault(typeof(defaultGramValidationDateTime))]
		public virtual DateTime? GramValidationDateTime { get; set; }
		#endregion
	}

	[PXHidden]
	[PXBreakInheritance]
	public class CRValidationTree : CRValidation
	{
		public new abstract class iD : PX.Data.BQL.BqlInt.Field<iD> { }
	}


	[PXHidden]
	public abstract class CRGramValidationDateTime : IBqlTable
	{
		#region GrammValidationDateTime
		public abstract class value : BqlDateTime.Field<value> { }
		[PXDBDateAndTime(BqlField = typeof(CRValidation.gramValidationDateTime))]
		public virtual DateTime? Value { get; set; }
		#endregion

		[PXHidden]
		[PXProjection(typeof(
			SelectFrom<CRValidation>
			.Where<CRValidation.type.IsIn<
				ValidationTypesAttribute.leadToLead,
				ValidationTypesAttribute.leadToAccount,
				ValidationTypesAttribute.leadToContact,
				ValidationTypesAttribute.contactToLead
			>>
			.AggregateTo<Max<CRValidation.gramValidationDateTime>>
		))]
		public class ByLead : CRGramValidationDateTime
		{
			public new abstract class value : BqlDateTime.Field<value> { }
		}

		[PXHidden]
		[PXProjection(typeof(
			SelectFrom<CRValidation>
			.Where<CRValidation.type.IsIn<
				ValidationTypesAttribute.contactToContact,
				ValidationTypesAttribute.contactToLead,
				ValidationTypesAttribute.contactToAccount,
				ValidationTypesAttribute.leadToContact
			>>
			.AggregateTo<Max<CRValidation.gramValidationDateTime>>
		))]
		public class ByContact : CRGramValidationDateTime
		{
			public new abstract class value : BqlDateTime.Field<value> { }
		}

		[PXHidden]
		[PXProjection(typeof(
			SelectFrom<CRValidation>
			.Where<CRValidation.type.IsIn<
				ValidationTypesAttribute.accountToAccount,
				ValidationTypesAttribute.contactToAccount,
				ValidationTypesAttribute.leadToAccount
			>>
			.AggregateTo<Max<CRValidation.gramValidationDateTime>>
		))]
		public class ByBAccount : CRGramValidationDateTime
		{
			public new abstract class value : BqlDateTime.Field<value> { }
		}
	}
}