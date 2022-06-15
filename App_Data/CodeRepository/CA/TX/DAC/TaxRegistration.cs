using PX.Data;
using PX.Data.BQL;
using PX.Objects.CR;
using PX.Objects.TX;
using System;

namespace PX.Objects.Localizations.CA.TX
{
    [Serializable]
    [PXCacheName("Tax Registration")]
    public class TaxRegistration : IBqlTable
    {
        #region BAccountID

        public abstract class bAccountID : BqlInt.Field<bAccountID> { }

        [PXDBInt(IsKey = true)]
        [PXDBDefault(typeof(BAccount.bAccountID))]
        [PXParent(typeof(
            Select<BAccount,
                Where<BAccount.bAccountID,
                    Equal<Current<TaxRegistration.bAccountID>>>>))]
        public virtual int? BAccountID
        {
            get;
            set;
        }

        #endregion

        #region TaxID

        public abstract class taxID : BqlString.Field<taxID> { }

        [PXDBString(Tax.taxID.Length, IsKey = true, IsUnicode = true)]
        [PXDefault()]
        [PXUIField(DisplayName = "Tax ID")]
        [PXSelector(typeof(
                Search<Tax.taxID,
                    Where<Tax.isExternal, Equal<False>>>),
            DescriptionField = typeof(Tax.descr))]
        public virtual string TaxID
        {
            get;
            set;
        }

        #endregion

        #region TaxRegistrationNumber

        public abstract class taxRegistrationNumber : BqlString.Field<taxRegistrationNumber> { }

        [PXDBString(50, IsUnicode = true)]
        [PXDefault()]
        [PXUIField(DisplayName = "Tax Registration Number", Required = true)]
        public virtual string TaxRegistrationNumber
        {
            get;
            set;
        }

        #endregion

        #region System Columns

        #region Tstamp

        public abstract class tstamp : BqlByteArray.Field<tstamp> { }

        [PXDBTimestamp()]
        public virtual byte[] Tstamp
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

        [PXDBCreatedDateTime]
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

        #endregion
    }
}
