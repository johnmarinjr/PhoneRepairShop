using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Objects.CM;
using PX.Objects.SO;

namespace PX.Objects.SO
{
	[PXHidden]
	public class CarrierLabelHistory : IBqlTable
	{
		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }

		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Justification]
		[PXDBIdentity(IsKey = true)]
		public virtual int? RecordID
		{
			get;
			set;
		}
		#endregion
		#region ShipmentNbr
		public abstract class shipmentNbr : PX.Data.BQL.BqlString.Field<shipmentNbr> { }

		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Justification]
		[PXDBString(15, IsKey = true, IsUnicode = true)]
		public virtual string ShipmentNbr
		{
			get;
			set;
		}
		#endregion
		#region RefNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		// Acuminator disable once PX1055 DacKeyFieldsWithIdentityKeyField [Justification]
		[PXDBInt(IsKey = true)]
		public virtual int? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region PluginTypeName
		public abstract class pluginTypeName : PX.Data.BQL.BqlString.Field<pluginTypeName> { }

		[PXDBString(255)]
		public virtual string PluginTypeName
		{
			get;
			set;
		}
		#endregion
		#region ServiceMethod
		public abstract class serviceMethod : PX.Data.BQL.BqlString.Field<serviceMethod>
		{
			public const int Length = 255;
		}

		[PXDBString(serviceMethod.Length)]
		public virtual string ServiceMethod
		{
			get;
			set;
		}
		#endregion
		#region RateAmount
		public abstract class rateAmount : PX.Data.BQL.BqlDecimal.Field<rateAmount> { }

		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual decimal? RateAmount
		{
			get;
			set;
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID]
		public virtual Guid? CreatedByID
		{
			get;
			set;
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID
		{
			get;
			set;
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime
		{
			get;
			set;
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp(RecordComesFirst = true)]
		public virtual byte[] tstamp
		{
			get;
			set;
		}
		#endregion
	}
}
