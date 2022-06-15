﻿using PX.Data;
using PX.Objects.CS;
using System;
using PX.TM;
using PX.SM;

namespace PX.Objects.CR
{
	[Serializable]
	[PXCacheName(Messages.DuplicateValidationRules)]
	public class CRValidationRules : IBqlTable
	{
		#region ValidationType

		public abstract class validationType : PX.Data.BQL.BqlString.Field<validationType> { }

		[PXDBString(2, IsFixed = true, IsKey = true)]
		[PXUIField(DisplayName = "Validation Type", Visible = false)]
		[PXDefault(typeof(Current<CRValidation.type>))]
		[ValidationTypes]
		public virtual String ValidationType { get; set; }

		#endregion
		#region MatchingField

		public abstract class matchingField : PX.Data.BQL.BqlString.Field<matchingField> { }

		[PXDBString(60, IsKey = true)]
		[PXDefault("")]
		[PXUIField(DisplayName = "Matching Field", Visibility = PXUIVisibility.Visible)]
		public virtual String MatchingField { get; set; }

		#endregion
		#region ScoreWieght

		public abstract class scoreWeight : PX.Data.BQL.BqlDecimal.Field<scoreWeight> { }

		[PXDBDecimal(4)]
		[PXDefault(TypeCode.Decimal, "1")]
		[PXUIField(DisplayName = "Score Weight")]
		public virtual decimal? ScoreWeight { get; set; }

		#endregion
		#region TransformationRule

		public abstract class transformationRule : PX.Data.BQL.BqlString.Field<transformationRule> { }

		[PXDBString(2)]
		[PXUIField(DisplayName = "Transformation Rule")]
		[PXDefault(TransformationRulesAttribute.None)]
		[TransformationRules]
		public virtual String TransformationRule { get; set; }

		#endregion

		#region CreateOnEntry
		public abstract class createOnEntry : PX.Data.BQL.BqlString.Field<createOnEntry> { }

		[PXDBString(1)]
		[PXUIField(DisplayName = "Create on Entry")]
		[PXDefault(CreateOnEntryAttribute.Allow)]
		[CreateOnEntry]
		public virtual String CreateOnEntry { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		protected DateTime? _CreatedDateTime;
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		protected DateTime? _LastModifiedDateTime;
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }

		[PXNote]
		public virtual Guid? NoteID { get; set; }
		#endregion
	}

	[Serializable]
	[PXBreakInheritance]
	[PXHidden]
	public class CRValidationRulesBuffer : CRValidationRules
	{
		
	}

}
