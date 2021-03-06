using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Payroll.Data;
using System;

namespace PX.Objects.PR
{
	[PXCacheName(Messages.PRCompanyTaxAttribute)]
	[Serializable]
	public class PRCompanyTaxAttribute : IBqlTable, IPRSetting, IStateSpecific
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRCompanyTaxAttribute>.By<settingName>
		{
			public static PRCompanyTaxAttribute Find(PXGraph graph, string settingName) => FindBy(graph, settingName);
		}

		public static class FK
		{
			public class State : CS.State.PK.ForeignKeyOf<PRCompanyTaxAttribute>.By<countryID, state> { }
			public class Country : CS.Country.PK.ForeignKeyOf<PRCompanyTaxAttribute>.By<countryID> { }
		}
		#endregion

		#region TypeName
		public abstract class typeName : PX.Data.BQL.BqlString.Field<typeName> { }
		[PXDBString(50, IsUnicode = true)]
		[PXUIField(DisplayName = "Type", Visible = false, Enabled = false)]
		public virtual string TypeName { get; set; }
		#endregion
		#region SettingName
		public abstract class settingName : PX.Data.BQL.BqlString.Field<settingName> { }
		[PXDBString(255, IsKey = true, IsUnicode = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Setting", Visible = false, Enabled = false)]
		public virtual string SettingName { get; set; }
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }
		[PXString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Name", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public virtual string Description { get; set; }
		#endregion
		#region State
		public abstract class state : PX.Data.BQL.BqlString.Field<state> { }
		[PXDBString(3, IsUnicode = true)]
		[PXDefault]
		[PXUIField(DisplayName = "State", Visible = true, Enabled = false)]
		public virtual string State { get; set; }
		#endregion
		#region CountryID
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXDBString(2, IsFixed = true)]
		[PXDefault]
		[PRCountry]
		public virtual string CountryID { get; set; }
		#endregion
		#region Value
		public abstract class value : PX.Data.BQL.BqlString.Field<value> { }
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Value")]
		public virtual string Value { get; set; }
		#endregion
		#region AllowOverride
		public abstract class allowOverride : PX.Data.BQL.BqlBool.Field<allowOverride> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Allow Employee Override")]
		public virtual bool? AllowOverride { get; set; }
		#endregion
		#region SortOrder
		public abstract class sortOrder : PX.Data.BQL.BqlInt.Field<sortOrder> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Sort Order")]
		public int? SortOrder { get; set; }
		#endregion
		#region Required
		public abstract class required : PX.Data.BQL.BqlBool.Field<required> { }
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Required")]
		public virtual bool? Required { get; set; }
		#endregion
		#region UseDefault
		public abstract class useDefault : PX.Data.BQL.BqlBool.Field<useDefault> { }
		[PXBool]
		public virtual bool? UseDefault { get; set; }
		#endregion
		#region AatrixMapping
		public abstract class aatrixMapping : PX.Data.BQL.BqlInt.Field<aatrixMapping> { }
		[PXDBInt]
		[PXUIField(Visible = false)]
		public virtual int? AatrixMapping { get; set; }
		#endregion
		#region AdditionalInformation
		[PXDBString(2048, IsUnicode = true)]
		[PXUIField(DisplayName = "Additional Information", Enabled = false)]
		public virtual string AdditionalInformation { get; set; }
		public abstract class additionalInformation : PX.Data.BQL.BqlString.Field<additionalInformation> { }
		#endregion
		#region UsedForTaxCalculation
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Used for Tax Calculation", Enabled = false)]
		public virtual bool? UsedForTaxCalculation { get; set; }
		public abstract class usedForTaxCalculation : PX.Data.BQL.BqlBool.Field<usedForTaxCalculation> { }
		#endregion
		#region UsedForAatrix
		[PXBool]
		[PXUnboundDefault(typeof(aatrixMapping.IsNotNull))]
		[PXUIField(DisplayName = "Used for Government Reporting", Enabled = false)]
		public virtual bool? UsedForAatrix { get; set; }
		public abstract class usedForAatrix : PX.Data.BQL.BqlBool.Field<usedForAatrix> { }
		#endregion
		#region FormBox
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Form/Box", Enabled = false)]
		public virtual string FormBox { get; set; }
		public abstract class formBox : PX.Data.BQL.BqlString.Field<formBox> { }
		#endregion
		#region NoteID
		[PXNote]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : IBqlField { }
		#endregion
		#region ErrorLevel
		public abstract class errorLevel : PX.Data.BQL.BqlInt.Field<errorLevel> { }
		[PXInt]
		public virtual int? ErrorLevel { get; set; }
		#endregion
		#region TaxesInState
		public abstract class taxesInState : PX.Data.BQL.BqlInt.Field<taxesInState> { }
		[PXInt]
		[PXDBScalar(typeof(SearchFor<PRTaxCode.taxID>.Where<PRTaxCode.taxState.IsEqual<state>>))]
		public int? TaxesInState { get; set; }
		#endregion

		#region System Columns
		#region TStamp
		public class tStamp : IBqlField { }
		[PXDBTimestamp()]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public class createdByID : IBqlField { }
		[PXDBCreatedByID()]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public class createdByScreenID : IBqlField { }
		[PXDBCreatedByScreenID()]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public class createdDateTime : IBqlField { }
		[PXDBCreatedDateTime()]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public class lastModifiedByID : IBqlField { }
		[PXDBLastModifiedByID()]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public class lastModifiedByScreenID : IBqlField { }
		[PXDBLastModifiedByScreenID()]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public class lastModifiedDateTime : IBqlField { }
		[PXDBLastModifiedDateTime()]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}