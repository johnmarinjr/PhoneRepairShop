// Decompiled
using System;
using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.GL.Attributes;

namespace PX.Objects.Localizations.CA
{
	[PXHidden]
	[Serializable]
	public class T5018MasterTable : IBqlTable
	{
		public abstract class organizationID : IBqlField, IBqlOperand
		{
		}

		public abstract class fromPeriod : IBqlField, IBqlOperand
		{
		}

		public abstract class toPeriod : IBqlField, IBqlOperand
		{
		}

		public abstract class fromDate : IBqlField, IBqlOperand
		{
		}

		public abstract class toDate : IBqlField, IBqlOperand
		{
		}

		public abstract class acctName : IBqlField, IBqlOperand
		{
		}

		public abstract class firstName : IBqlField, IBqlOperand
		{
		}

		public abstract class lastName : IBqlField, IBqlOperand
		{
		}

		public abstract class title : IBqlField, IBqlOperand
		{
		}

		public abstract class phone1 : IBqlField, IBqlOperand
		{
		}

		public abstract class postalCode : IBqlField, IBqlOperand
		{
		}

		public abstract class taxRegistrationID : IBqlField, IBqlOperand
		{
		}

		public abstract class langugae : IBqlField, IBqlOperand
		{
		}

		public abstract class filingType : IBqlField, IBqlOperand
		{
		}

		public abstract class eMail : IBqlField, IBqlOperand
		{
		}

		public abstract class secteMail : IBqlField, IBqlOperand
		{
		}

		public abstract class phone2 : IBqlField, IBqlOperand
		{
		}

		public abstract class contAreaCode : IBqlField, IBqlOperand
		{
		}

		public abstract class submissionNo : IBqlField, IBqlOperand
		{
		}

		public abstract class thersholdAmount : IBqlField, IBqlOperand
		{
		}

		[Organization(true, DisplayName = "Transmitter")]
		public virtual int? OrganizationID
		{
			get;
			set;
		}

		[PXString]
		[APOpenPeriod]
		[PXUIField(DisplayName = "From Period", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public virtual string FromPeriod
		{
			get;
			set;
		}

		[PXString]
		[APOpenPeriod]
		[PXUIField(DisplayName = "To Period", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public virtual string ToPeriod
		{
			get;
			set;
		}

		[PXDate]
		[PXUIField(DisplayName = "From", Visibility = PXUIVisibility.SelectorVisible, Required = true)]
		public virtual DateTime? FromDate
		{
			get;
			set;
		}

		[PXDate]
		[PXUIField(DisplayName = "To", Required = true)]
		public virtual DateTime? ToDate
		{
			get;
			set;
		}

		[PXString(50)]
		[PXUIField(DisplayName = "Company Name", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string AcctName
		{
			get;
			set;
		}

		[PXString(50)]
		[PXUIField(DisplayName = "First Name")]
		public virtual string FirstName
		{
			get;
			set;
		}

		[PXString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "Last Name")]
		public virtual string LastName
		{
			get;
			set;
		}

		[PXString(50, IsUnicode = true)]
		[Titles]
		[PXUIField(DisplayName = "Title")]
		public virtual string Title
		{
			get;
			set;
		}

		[PXString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "Phone", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string Phone1
		{
			get;
			set;
		}

		[PXString(20)]
		[PXUIField(DisplayName = "Postal Code")]
		public virtual string PostalCode
		{
			get;
			set;
		}

		[PXString(50)]
		[PXUIField(DisplayName = "Program Account Number", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string TaxRegistrationID
		{
			get;
			set;
		}

		[PXString]
		[PXStringList(new string[] {"E", "F"}, new string[] {"English", "French"})]
		[PXUIField(DisplayName = "Language")]
		public virtual string Language
		{
			get;
			set;
		}

		[PXString]
		[PXStringList(new string[] {"O", "A"}, new string[] {"Original", "Amendment"})]
		[PXUIField(DisplayName = "Filing Type")]
		public virtual string FilingType
		{
			get;
			set;
		}

		[PXString]
		[PXUIField(DisplayName = "Email", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string EMail
		{
			get;
			set;
		}

		[PXString]
		[PXUIField(DisplayName = "Second Email", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string SecteMail
		{
			get;
			set;
		}

		[PXString(60)]
		[PXUIField(DisplayName = "Extension Number", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string Phone2
		{
			get;
			set;
		}

		[PXString(50)]
		[PXUIField(DisplayName = "Contact Area Code", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string AreaCode
		{
			get;
			set;
		}

		[PXString(IsUnicode = true)]
		[PXUIField(DisplayName = "Submission Number", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string SubmissionNo
		{
			get;
			set;
		}

		[PXDecimal]
		[PXUnboundDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Threshold Amount", Visibility = PXUIVisibility.Visible)]
		public virtual decimal? ThersholdAmount
		{
			get;
			set;
		}
	}
}
