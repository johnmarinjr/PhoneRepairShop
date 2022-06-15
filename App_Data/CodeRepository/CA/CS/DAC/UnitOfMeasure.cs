using PX.Data;
using PX.Data.BQL;
using System;

namespace PX.Objects.Localizations.CA.CS
{
    [Serializable]
    [PXCacheName("Unit of Measure")]
    [PXPrimaryGraph(typeof(UnitOfMeasureMaint))]
    public class UnitOfMeasure : IBqlTable
    {
        #region Unit

        public abstract class unit : BqlString.Field<unit> { }

        [PXDBString(6, IsUnicode = true, IsKey = true)]
        [PXDefault()]
        [PXUIField(DisplayName = "Unit ID")]
        [PXSelector(
            typeof(Search<UnitOfMeasure.unit>),
            typeof(UnitOfMeasure.unit),
            typeof(UnitOfMeasure.descr))]
        public virtual string Unit
        {
            get;
            set;
        }

        #endregion

        #region Descr

        public abstract class descr : BqlString.Field<descr> { }

        [PXDBLocalizableString(6, IsUnicode = true)]
        [PXDefault()]
        [PXUIField(DisplayName = "Description for Reports")]
        public virtual string Descr
        {
            get;
            set;
        }

        #endregion

        #region NoteID

        public abstract class noteID : BqlGuid.Field<noteID> { }

        [PXNote()]
        public virtual Guid? NoteID
        {
            get;
            set;
        }

        #endregion

        #region tstamp

        public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }

        [PXDBTimestamp()]
        public virtual byte[] tstamp
        {
            get;
            set;
        }

        #endregion

        #region CreatedByID

        public abstract class createdByID : BqlGuid.Field<createdByID> { }

        [PXDBCreatedByID()]
        public virtual Guid? CreatedByID
        {
            get;
            set;
        }

        #endregion

        #region CreatedByScreenID

        public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }

        [PXDBCreatedByScreenID()]
        public virtual string CreatedByScreenID
        {
            get;
            set;
        }

        #endregion

        #region CreatedDateTime

        public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }

        [PXDBCreatedDateTime()]
        public virtual DateTime? CreatedDateTime
        {
            get;
            set;
        }

        #endregion

        #region LastModifiedByID

        public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }

        [PXDBLastModifiedByID()]
        public virtual Guid? LastModifiedByID
        {
            get;
            set;
        }

        #endregion

        #region LastModifiedByScreenID

        public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }

        [PXDBLastModifiedByScreenID()]
        public virtual string LastModifiedByScreenID
        {
            get;
            set;
        }

        #endregion

        #region LastModifiedDateTime

        public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }

        [PXDBLastModifiedDateTime()]
        public virtual DateTime? LastModifiedDateTime
        {
            get;
            set;
        }

        #endregion
    }
}
