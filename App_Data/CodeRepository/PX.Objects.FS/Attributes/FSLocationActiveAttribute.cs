using PX.Data;
using PX.Objects.CS;
using PX.Objects.CR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.FS
{
	public class FSLocationActiveAttribute : LocationActiveAttribute
	{
		public FSLocationActiveAttribute(Type WhereType)
			: base(WhereType, typeof(
				LeftJoin<Address,
					On<Address.bAccountID, Equal<Location.bAccountID>,
						And<Address.addressID, Equal<Location.defAddressID>>>,
				LeftJoin<Country,
					On<Country.countryID, Equal<Address.countryID>>,
				LeftJoin<Contact,
					On<Contact.contactID, Equal<Location.defContactID>>>>
			>))
		{
		}

		protected override PXDimensionSelectorAttribute GetSelectorAttribute(Type searchType)
		{
			return new PXDimensionSelectorAttribute(
				DimensionName,
				searchType,
				typeof(Location.locationCD),
				typeof(Location.locationCD),
				typeof(Location.descr),
				typeof(Address.addressLine1),
				typeof(Address.addressLine2),
				typeof(Address.city),
				typeof(Address.state),
				typeof(Address.postalCode),
				typeof(Country.description),
				typeof(Contact.phone1)
			);
		}

	}
}
