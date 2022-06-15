using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.EP;
using System;

namespace PX.Objects.PR
{
	/// <summary>
	/// Stores the Other Monies records related to the Record of Employment.
	/// </summary>
	[PXCacheName(Messages.PRROEOtherMonies)]
	[Serializable]
	public class PRROEOtherMonies : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<PRROEOtherMonies>.By<refNbr, lineNbr>
		{
			public static PRROEOtherMonies Find(PXGraph graph, string refNbr, int? lineNbr) => FindBy(graph, refNbr, lineNbr);
		}

		public static class FK
		{
			public class RecordOfEmployment : PRRecordOfEmployment.PK.ForeignKeyOf<PRROEOtherMonies>.By<refNbr> { }
		}
		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(15, IsKey = true, IsUnicode = true)]
		[PXUIField(DisplayName = "Reference Nbr.")]
		[PXDBDefault(typeof(PRRecordOfEmployment.refNbr))]
		public string RefNbr { get; set; }
		#endregion

		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(PRRecordOfEmployment))]
		[PXParent(typeof(Select<PRRecordOfEmployment, Where<PRRecordOfEmployment.refNbr, Equal<Current<refNbr>>>>))]
		[PXUIField(DisplayName = "Line Nbr.", Visible = false)]
		public virtual int? LineNbr { get; set; }
		#endregion

		#region TypeCD
		public abstract class typeCD : PX.Data.BQL.BqlString.Field<typeCD> { }
		[PXDBString(EPEarningType.typeCD.Length, IsUnicode = true, InputMask = EPEarningType.typeCD.InputMask)]
		[PXDefault]
		[PXUIField(DisplayName = "Earning Code")]
		[PXRestrictor(typeof(Where<EPEarningType.isActive.IsEqual<True>>), Messages.EarningTypeIsNotActive, typeof(EPEarningType.typeCD))]
		[PXSelector(typeof(SelectFrom<EPEarningType>.
				Where<PREarningType.isAmountBased.IsEqual<True>>.
				OrderBy<EPEarningType.typeCD.Asc>.
				SearchFor<EPEarningType.typeCD>),
			typeof(EPEarningType.typeCD), typeof(EPEarningType.description),
			SelectorMode = PXSelectorMode.MaskAutocomplete,
			DescriptionField = typeof(EPEarningType.description))]
		[PXForeignReference(typeof(Field<typeCD>.IsRelatedTo<EPEarningType.typeCD>))] //ToDo: AC-142439 Ensure PXForeignReference attribute works correctly with PXCacheExtension DACs.
		public string TypeCD { get; set; }
		#endregion

		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		[PRCurrency]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount")]
		public virtual Decimal? Amount { get; set; }
		#endregion

		#region System Columns
		#region TStamp
		public abstract class tStamp : PX.Data.BQL.BqlByteArray.Field<tStamp> { }
		[PXDBTimestamp]
		public byte[] TStamp { get; set; }
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		[PXDBCreatedByID]
		public Guid? CreatedByID { get; set; }
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		[PXDBCreatedByScreenID]
		public string CreatedByScreenID { get; set; }
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		[PXDBCreatedDateTime]
		public DateTime? CreatedDateTime { get; set; }
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		[PXDBLastModifiedByID]
		public Guid? LastModifiedByID { get; set; }
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		[PXDBLastModifiedByScreenID]
		public string LastModifiedByScreenID { get; set; }
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		[PXDBLastModifiedDateTime]
		public DateTime? LastModifiedDateTime { get; set; }
		#endregion
		#endregion
	}
}
