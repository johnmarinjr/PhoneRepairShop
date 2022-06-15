using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Payroll.Data;
using PX.Payroll.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public static class TaxLocationHelpers
	{
		public static void UpdateAddressLocationCodes(List<Address> addresses)
		{
			GetUpdatedAddressLocationCodes(addresses);
		}

		public static List<Address> GetUpdatedAddressLocationCodes(List<Address> addresses)
		{
			return PXGraph.CreateInstance<UpdateAddressLocationCodesGraph>().GetUpdatedAddressLocationCodes(addresses);
		}

		public static void AddressPersisting(Events.RowPersisting<Address> e)
		{
			if (IsAddressedModified(e.Cache, e.Row))
			{
				UpdateAddressLocationCode(e.Cache.Graph, e.Row);
			}
		}

		public static bool IsAddressedModified(PXCache cache, Address row)
		{
			return !object.Equals(row.AddressLine1, cache.GetValueOriginal<Address.addressLine1>(row)) ||
				!object.Equals(row.AddressLine2, cache.GetValueOriginal<Address.addressLine2>(row)) ||
				!object.Equals(row.City, cache.GetValueOriginal<Address.city>(row)) ||
				!object.Equals(row.State, cache.GetValueOriginal<Address.state>(row)) ||
				!object.Equals(row.PostalCode, cache.GetValueOriginal<Address.postalCode>(row)) ||
				!object.Equals(row.CountryID, cache.GetValueOriginal<Address.countryID>(row));
		}

		public static void UpdateAddressLocationCode(PXGraph graph, Address address)
		{
			PXCache cache = graph?.Caches<Address>();
			try
			{
				PRLocationCodeDescription locationCode = null;
				if (PRWebServiceRestClient.IsCountrySupported(address.CountryID))
				{
					PRWebServiceRestClient restClient = new PRWebServiceRestClient();
					locationCode = restClient.GetLocationCodes(new List<PRAddress>() { address.ToPRAddress() }).FirstOrDefault().Value;
				}
				else if (PayrollTaxClient.IsCountrySupported(address.CountryID))
				{
					var payrollService = new PayrollTaxClient(address.CountryID);
					locationCode = payrollService.GetLocationCode(address);
				}

				if (locationCode != null && locationCode.TaxLocationCode != null)
				{
					address.TaxLocationCode = locationCode.TaxLocationCode;
					address.TaxMunicipalCode = locationCode.TaxMunicipalCode;
					address.TaxSchoolCode = locationCode.TaxSchoolCode;
				}
			}
			catch
			{
				address.TaxLocationCode = null;
				address.TaxMunicipalCode = null;
				address.TaxSchoolCode = null;
			}
		}

		public class AddressEqualityComparer : IEqualityComparer<Address>
		{
			public bool Equals(Address x, Address y)
			{
				return x.AddressID == y.AddressID;
			}

			public int GetHashCode(Address obj)
			{
				return obj.AddressID.GetHashCode();
			}
		}

		public class UpdateAddressLocationCodesGraph : PXGraph<UpdateAddressLocationCodesGraph>
		{
			public SelectFrom<Address>.View Addresses;

			public void UpdateAddressLocationCodes(List<Address> addresses)
			{
				GetUpdatedAddressLocationCodes(addresses);
			}

			public List<Address> GetUpdatedAddressLocationCodes(List<Address> addresses)
			{
				List<Address> updatedAddresses = new List<Address>();
				List<IGrouping<string, Address>> addressesByCountryGroups = addresses.GroupBy(x => x.CountryID).ToList();
				HashSet<string> countriesProcessed = new HashSet<string>();
				Dictionary<int?, PRLocationCodeDescription> locationCodesFromWS = new Dictionary<int?, PRLocationCodeDescription>();
				foreach (IGrouping<string, Address> addressesByCountry in addressesByCountryGroups
					.Where(x => PRWebServiceRestClient.IsCountrySupported(x.Key)))
				{
					PRWebServiceRestClient restClient = new PRWebServiceRestClient();
					locationCodesFromWS = locationCodesFromWS.Concat(restClient.GetLocationCodes(addressesByCountry.Select(x => x.ToPRAddress())))
						.GroupBy(kvp => kvp.Key)
						.ToDictionary(k => k.Key, v => v.First().Value);
					countriesProcessed.Add(addressesByCountry.Key);
				}

				foreach (IGrouping<string, Address> addressesByCountry in addressesByCountryGroups
					.Where(x => PayrollTaxClient.IsCountrySupported(x.Key) && !countriesProcessed.Contains(x.Key)))
				{
					var payrollService = new PayrollTaxClient(addressesByCountry.Key);
					locationCodesFromWS = locationCodesFromWS.Concat(payrollService.GetLocationCodes(addressesByCountry))
						.GroupBy(kvp => kvp.Key)
						.ToDictionary(k => k.Key, v => v.First().Value);
					countriesProcessed.Add(addressesByCountry.Key);
				}

				foreach (KeyValuePair<int?, PRLocationCodeDescription> kvp in locationCodesFromWS)
				{
					if (kvp.Value?.TaxLocationCode != null)
					{
						Address address = Addresses.Search<Address.addressID>(kvp.Key);
						if (address != null)
						{
							Addresses.Cache.RestoreCopy(address, addresses.First(x => x.AddressID == kvp.Key));
							address.TaxLocationCode = kvp.Value.TaxLocationCode;
							address.TaxMunicipalCode = kvp.Value.TaxMunicipalCode;
							address.TaxSchoolCode = kvp.Value.TaxSchoolCode;
							Addresses.Cache.SetValue(address, nameof(PRxAddress.psdCode), kvp.Value.PsdCode);
							updatedAddresses.Add(Addresses.Update(address));
						}
					}
				}

				Persist();
				return updatedAddresses;
			}
		}

		#region Avoid breaking changes in 2021R1
		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R2)]
		public static void UpdateAddressLocationCode(Address address)
		{
			UpdateAddressLocationCode(null, address);
		}
		#endregion Avoid breaking changes in 2021R1
	}

	public static class AddressExtensions
	{
		public static PRAddress ToPRAddress(this Address address)
		{
			return new PRAddress()
			{
				Address1 = address.AddressLine1,
				Address2 = address.AddressLine2,
				City = address.City,
				State = address.State,
				ZipCode = address.PostalCode,
				AddressID = address.AddressID
			};
		}
	}
}
