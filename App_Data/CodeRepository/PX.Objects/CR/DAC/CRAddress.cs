﻿using PX.Data;
using PX.Objects.CS;
using System;
using System.Diagnostics;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.CS.Contracts.Interfaces;
using PX.Data.BQL;

namespace PX.Objects.CR
{
    [Serializable]
    [PXCacheName(Messages.ShippingAddress)]
    public partial class CRShippingAddress : CRAddress
    {
        #region Keys
        public new class PK : PrimaryKeyOf<CRShippingAddress>.By<addressID>
        {
            public static CRShippingAddress Find(PXGraph graph, int? addressID) => FindBy(graph, addressID);
        }
        public new static class FK
        {
            public class BusinessAccount : CR.BAccount.PK.ForeignKeyOf<CRShippingAddress>.By<bAccountID> { }
            public class BusinessAccountAddress : CR.Address.PK.ForeignKeyOf<CRShippingAddress>.By<bAccountAddressID> { }
            public class Country : CS.Country.PK.ForeignKeyOf<CRShippingAddress>.By<countryID> { }
            public class State : CS.State.PK.ForeignKeyOf<CRShippingAddress>.By<countryID, state> { }
        }
        #endregion

        #region AddressID
        public new abstract class addressID : BqlInt.Field<addressID> { }
        #endregion

        #region BAccountID
        public new abstract class bAccountID : BqlInt.Field<bAccountID> { }
        #endregion

        #region BAccountAddressID
        public new abstract class bAccountAddressID : BqlInt.Field<bAccountAddressID> { }
        #endregion

        #region IsDefaultAddress
        public new abstract class isDefaultAddress : BqlBool.Field<isDefaultAddress> { }

        [PXDBBool()]
        [PXUIField(DisplayName = "Default Customer Address", Visibility = PXUIVisibility.Visible)]
        [PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
        public override Boolean? IsDefaultAddress
        {
            get
            {
                return base._IsDefaultAddress;
            }
            set
            {
                base._IsDefaultAddress = value;
            }
        }

        #endregion

        #region OverrideAddress
        public new abstract class overrideAddress : PX.Data.IBqlField
        {
        }

		[PXBool()]
		[PXUIField(DisplayName = "Override", Visibility = PXUIVisibility.Visible)]
		public override Boolean? OverrideAddress
		{
			[PXDependsOnFields(typeof(isDefaultAddress))]
			get
			{
				return base.OverrideAddress;
			}
			set
			{
				base.OverrideAddress = value;
			}
		}
		#endregion

		#region RevisionID
		public new abstract class revisionID : BqlInt.Field<revisionID> { }
        #endregion

        #region CountryID
        public new abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
        #endregion

        #region State
        public new abstract class state : PX.Data.BQL.BqlString.Field<state> { }
        #endregion

        #region Latitude
        public new abstract class latitude : PX.Data.BQL.BqlDecimal.Field<latitude> { }
        #endregion

        #region Longitude
        public new abstract class longitude : PX.Data.BQL.BqlDecimal.Field<longitude> { }
        #endregion
    }

    [Serializable]
    [PXCacheName(Messages.BillToAddress)]
    public partial class CRBillingAddress : CRAddress
    {
        #region Keys
        public new class PK : PrimaryKeyOf<CRBillingAddress>.By<addressID>
        {
            public static CRBillingAddress Find(PXGraph graph, int? addressID) => FindBy(graph, addressID);
        }
        public new static class FK
        {
            public class BusinessAccount : CR.BAccount.PK.ForeignKeyOf<CRBillingAddress>.By<bAccountID> { }
            public class BusinessAccountAddress : CR.Address.PK.ForeignKeyOf<CRBillingAddress>.By<bAccountAddressID> { }
            public class Country : CS.Country.PK.ForeignKeyOf<CRBillingAddress>.By<countryID> { }
            public class State : CS.State.PK.ForeignKeyOf<CRBillingAddress>.By<countryID, state> { }
        }
        #endregion

        #region AddressID
        public new abstract class addressID : BqlInt.Field<addressID> { }
        #endregion

        #region BAccountID
        public new abstract class bAccountID : BqlInt.Field<bAccountID> { }
        #endregion

        #region BAccountAddressID
        public new abstract class bAccountAddressID : BqlInt.Field<bAccountAddressID> { }
        #endregion

        #region IsDefaultAddress
        public new abstract class isDefaultAddress : BqlBool.Field<isDefaultAddress> { }

        [PXDBBool]
        [PXUIField(DisplayName = "Default Customer Address", Visibility = PXUIVisibility.Visible)]
        [PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
        public override Boolean? IsDefaultAddress
        {
            get
            {
                return base._IsDefaultAddress;
            }
            set
            {
                base._IsDefaultAddress = value;
            }
        }

        #endregion

        #region OverrideAddress
        public new abstract class overrideAddress : BqlBool.Field<overrideAddress> { }

        [PXBool()]
        [PXUIField(DisplayName = "Override", Visibility = PXUIVisibility.Visible)]
        public override Boolean? OverrideAddress
        {
            [PXDependsOnFields(typeof(isDefaultAddress))]
            get
            {
                return base.OverrideAddress;
            }
            set
            {
                base.OverrideAddress = value;
            }
        }
        #endregion

        #region RevisionID
        public new abstract class revisionID : BqlInt.Field<revisionID> { }
        #endregion

        #region CountryID
        public new abstract class countryID : BqlString.Field<countryID> { }
        #endregion

        #region State
        public new abstract class state : BqlString.Field<state> { }
        #endregion
    }

    [Serializable]
    [PXCacheName(Messages.CRAddress)]
    [DebuggerDisplay("{GetType().Name,nq} ({IsDefaultAddress}): AddressID = {AddressID,nq}, AddressLine1 = {AddressLine1}")]
    public partial class CRAddress : PX.Data.IBqlTable, IAddress, IAddressBase, IAddressLocation
    {
        #region Keys
        public class PK : PrimaryKeyOf<CRAddress>.By<addressID>
        {
            public static CRAddress Find(PXGraph graph, int? addressID) => FindBy(graph, addressID);
        }
        public static class FK
        {
            public class BusinessAccount : CR.BAccount.PK.ForeignKeyOf<CRAddress>.By<bAccountID> { }
            public class BusinessAccountAddress : CR.Address.PK.ForeignKeyOf<CRAddress>.By<bAccountAddressID> { }
            public class Country : CS.Country.PK.ForeignKeyOf<CRAddress>.By<countryID> { }
            public class State : CS.State.PK.ForeignKeyOf<CRAddress>.By<countryID, state> { }
        }
        #endregion
        #region AddressID
        public abstract class addressID : PX.Data.BQL.BqlInt.Field<addressID> { }
        protected Int32? _AddressID;
        [PXDBIdentity(IsKey = true)]
        [PXUIField(DisplayName = "Address ID", Visible = false)]
        public virtual Int32? AddressID
        {
            get
            {
                return this._AddressID;
            }
            set
            {
                this._AddressID = value;
            }
        }
        #endregion
        #region BAccountID
        public abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
        protected Int32? _BAccountID;
        [PXDBInt()]
        [PXDBDefault(typeof(CROpportunity.bAccountID), PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Int32? BAccountID
        {
            get
            {
                return this._BAccountID;
            }
            set
            {
                this._BAccountID = value;
            }
        }

        #endregion
        #region BAccountAddressID
        public abstract class bAccountAddressID : PX.Data.BQL.BqlInt.Field<bAccountAddressID> { }
        protected Int32? _BAccountAddressID;
        [PXDBInt()]
        public virtual Int32? BAccountAddressID
        {
            get
            {
                return this._BAccountAddressID;
            }
            set
            {
                this._BAccountAddressID = value;
            }
        }
        
        #endregion
        #region IsDefaultAddress
        public abstract class isDefaultAddress : PX.Data.BQL.BqlBool.Field<isDefaultAddress> { }
        protected Boolean? _IsDefaultAddress;
        [PXDBBool()]
        [PXUIField(DisplayName = "Default Customer Address", Visibility = PXUIVisibility.Visible)]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Boolean? IsDefaultAddress
        {
            get
            {
                return this._IsDefaultAddress;
            }
            set
            {
                this._IsDefaultAddress = value;
            }
        }
        #endregion
        #region OverrideAddress
        public abstract class overrideAddress : PX.Data.BQL.BqlBool.Field<overrideAddress> { }
        [PXBool()]
        [PXUIField(DisplayName = "Override Address", Visibility = PXUIVisibility.Visible)]
        public virtual Boolean? OverrideAddress
        {
            [PXDependsOnFields(typeof(isDefaultAddress))]
            get
            {
                return (bool?)(this._IsDefaultAddress == null ? this._IsDefaultAddress : this._IsDefaultAddress == false);
            }
            set
            {
                this._IsDefaultAddress = (bool?)(value == null ? value : value == false);
            }
        }
        #endregion
        #region RevisionID
        public abstract class revisionID : PX.Data.BQL.BqlInt.Field<revisionID> { }
        protected Int32? _RevisionID;
        [PXDBInt()]        
        public virtual Int32? RevisionID
        {
            get
            {
                return this._RevisionID;
            }
            set
            {
                this._RevisionID = value;
            }
        }
        #endregion
        #region AddressLine1
        public abstract class addressLine1 : PX.Data.BQL.BqlString.Field<addressLine1> { }
        protected String _AddressLine1;
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Address Line 1", Visibility = PXUIVisibility.SelectorVisible)]
		[PXPersonalDataField]
		public virtual String AddressLine1
        {
            get
            {
                return this._AddressLine1;
            }
            set
            {
                this._AddressLine1 = value;
            }
        }
        #endregion
        #region AddressLine2
        public abstract class addressLine2 : PX.Data.BQL.BqlString.Field<addressLine2> { }
        protected String _AddressLine2;
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Address Line 2")]
		[PXPersonalDataField]
		public virtual String AddressLine2
        {
            get
            {
                return this._AddressLine2;
            }
            set
            {
                this._AddressLine2 = value;
            }
        }
        #endregion
        #region AddressLine3
        public abstract class addressLine3 : PX.Data.BQL.BqlString.Field<addressLine3> { }
        protected String _AddressLine3;
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Address Line 3")]
		[PXPersonalDataField]
		public virtual String AddressLine3
        {
            get
            {
                return this._AddressLine3;
            }
            set
            {
                this._AddressLine3 = value;
            }
        }
        #endregion
        #region City
        public abstract class city : PX.Data.BQL.BqlString.Field<city> { }
        protected String _City;
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "City", Visibility = PXUIVisibility.SelectorVisible)]
		[PXPersonalDataField]
		public virtual String City
        {
            get
            {
                return this._City;
            }
            set
            {
                this._City = value;
            }
        }
        #endregion
        #region CountryID
        public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
        protected String _CountryID;
        [PXDefault(typeof(Search<GL.Branch.countryID, Where<GL.Branch.branchID, Equal<Current<AccessInfo.branchID>>>>))]
		[PXDBString(100)]
        [PXUIField(DisplayName = "Country")]
        [Country]
		public virtual String CountryID
        {
            get
            {
                return this._CountryID;
            }
            set
            {
                this._CountryID = value;
            }
        }
        #endregion
        #region State
        public abstract class state : PX.Data.BQL.BqlString.Field<state> { }
        protected String _State;
        [PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "State")]
        [State(typeof(countryID))]
        public virtual String State
        {
            get
            {
                return this._State;
            }
            set
            {
                this._State = value;
            }
        }
        #endregion
        #region PostalCode
        public abstract class postalCode : PX.Data.BQL.BqlString.Field<postalCode> { }
        protected String _PostalCode;
        [PXDBString(20)]
        [PXUIField(DisplayName = "Postal Code")]
        [PXZipValidation(typeof(Country.zipCodeRegexp), typeof(Country.zipCodeMask), countryIdField: typeof(countryID))]
		[PXPersonalDataField]
		public virtual String PostalCode
        {
            get
            {
                return this._PostalCode;
            }
            set
            {
                this._PostalCode = value;
            }
        }
        #endregion
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }

		[PXDBGuidNotNull]
		public virtual Guid? NoteID { get; set; }
		#endregion
        
        #region IsValidated
        public abstract class isValidated : PX.Data.BQL.BqlBool.Field<isValidated> { }
        protected Boolean? _IsValidated;

        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXDBBool()]
        [CS.ValidatedAddress()]
        [PXUIField(DisplayName = "Validated", FieldClass = CS.Messages.ValidateAddress)]
        public virtual Boolean? IsValidated
        {
            get
            {
                return this._IsValidated;
            }
            set
            {
                this._IsValidated = value;
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
        #region Latitude
        public abstract class latitude : PX.Data.BQL.BqlDecimal.Field<latitude> { }
        [PXDBDecimal(6)]
        [PXUIField(DisplayName = "Latitude", Visible = false)]
        public virtual decimal? Latitude { get; set; }
        #endregion
        #region Longitude
        public abstract class longitude : PX.Data.BQL.BqlDecimal.Field<longitude> { }
        [PXDBDecimal(6)]
        [PXUIField(DisplayName = "Longitude", Visible = false)]
        public virtual decimal? Longitude { get; set; }
        #endregion
    }

}
