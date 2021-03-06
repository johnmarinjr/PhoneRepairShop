namespace PX.Objects.TX
{
	using System;
	using PX.Data;
	using PX.Objects.GL;
	using PX.Data.ReferentialIntegrity.Attributes;

	[System.SerializableAttribute()]
	public partial class TXImportSettings : PX.Data.IBqlTable
	{
		#region Keys
		public static class FK
		{
			public class TaxTaxableCategory : TX.TaxCategory.PK.ForeignKeyOf<TXImportSettings>.By<taxableCategoryID> { }
			public class TaxFreightCategory : TX.TaxCategory.PK.ForeignKeyOf<TXImportSettings>.By<freightCategoryID> { }
			public class TaxServiceCategory : TX.TaxCategory.PK.ForeignKeyOf<TXImportSettings>.By<serviceCategoryID> { }
			public class TaxLaborCategory : TX.TaxCategory.PK.ForeignKeyOf<TXImportSettings>.By<laborCategoryID> { }
		}
		#endregion

		#region TaxableCategoryID
		public abstract class taxableCategoryID : PX.Data.BQL.BqlString.Field<taxableCategoryID> { }
		protected String _TaxableCategoryID;
		[PXDBString(TaxCategory.taxCategoryID.Length, IsUnicode = true)]
		[PXSelector(typeof(TaxCategory.taxCategoryID))]
		[PXUIField(DisplayName = "Tax Taxable Category")]
		public virtual String TaxableCategoryID
		{
			get
			{
				return this._TaxableCategoryID;
			}
			set
			{
				this._TaxableCategoryID = value;
			}
		}
		#endregion
		#region FreightCategoryID
		public abstract class freightCategoryID : PX.Data.BQL.BqlString.Field<freightCategoryID> { }
		protected String _FreightCategoryID;
		[PXDBString(TaxCategory.taxCategoryID.Length, IsUnicode = true)]
		[PXSelector(typeof(TaxCategory.taxCategoryID))]
		[PXUIField(DisplayName = "Tax Freight Category")]
		public virtual String FreightCategoryID
		{
			get
			{
				return this._FreightCategoryID;
			}
			set
			{
				this._FreightCategoryID = value;
			}
		}
		#endregion
		#region ServiceCategoryID
		public abstract class serviceCategoryID : PX.Data.BQL.BqlString.Field<serviceCategoryID> { }
		protected String _ServiceCategoryID;
		[PXDBString(TaxCategory.taxCategoryID.Length, IsUnicode = true)]
		[PXSelector(typeof(TaxCategory.taxCategoryID))]
		[PXUIField(DisplayName = "Tax Service Category")]
		public virtual String ServiceCategoryID
		{
			get
			{
				return this._ServiceCategoryID;
			}
			set
			{
				this._ServiceCategoryID = value;
			}
		}
		#endregion
		#region LaborCategoryID
		public abstract class laborCategoryID : PX.Data.BQL.BqlString.Field<laborCategoryID> { }
		protected String _LaborCategoryID;
		[PXDBString(TaxCategory.taxCategoryID.Length, IsUnicode = true)]
		[PXSelector(typeof(TaxCategory.taxCategoryID))]
		[PXUIField(DisplayName = "Tax Labor Category")]
		public virtual String LaborCategoryID
		{
			get
			{
				return this._LaborCategoryID;
			}
			set
			{
				this._LaborCategoryID = value;
			}
		}
		#endregion
	}
}
