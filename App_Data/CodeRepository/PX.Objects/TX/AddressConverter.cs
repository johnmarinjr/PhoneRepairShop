using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.CS.Contracts.Interfaces;
using PX.Data;
using PX.TaxProvider;

namespace PX.Objects.TX
{
    public static class AddressConverter
    {
        public static TaxAddress ConvertTaxAddress(IAddressLocation address)
        {
            var result = new TaxAddress
            {
                Country = address.CountryID,
                Region = address.State,
                City = address.City,
                PostalCode = address.PostalCode,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                AddressLine3 = address.AddressLine3,
                Latitude = address.Latitude,
                Longitude = address.Longitude
            };

            return result;
        }
    }
}
