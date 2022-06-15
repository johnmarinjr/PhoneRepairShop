using PX.Data;
using PX.Objects.AP;
using PX.Objects.IN;
using System;
using PX.Objects.CR;

namespace PX.Objects.FS
{
    public class DBoxHeaderAddress
    {
        #region AddressLine1
        public virtual string AddressLine1 { get; set; }
        #endregion
        #region AddressLine2
        public virtual string AddressLine2 { get; set; }
        #endregion
        #region AddressLine3
        public virtual string AddressLine3 { get; set; }
        #endregion
        #region City
        public virtual string City { get; set; }
        #endregion
        #region CountryID
        public virtual string CountryID { get; set; }
        #endregion
        #region State
        public virtual string State { get; set; }
        #endregion
        #region PostalCode
        public virtual string PostalCode { get; set; }
        #endregion

        public static implicit operator DBoxHeaderAddress(CRAddress address)
        {
            if (address == null)
            {
                return null;
            }

            DBoxHeaderAddress ret = new DBoxHeaderAddress();

            ret.AddressLine1 = address.AddressLine1;
            ret.AddressLine2 = address.AddressLine2;
            ret.AddressLine3 = address.AddressLine3;
            ret.City = address.City;
            ret.CountryID = address.CountryID;
            ret.State = address.State;
            ret.PostalCode = address.PostalCode;

            return ret;
        }
    }
}

