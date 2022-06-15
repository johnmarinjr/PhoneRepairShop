using PX.Common;
using PX.CS.Contracts.Interfaces;
using PX.Data;
using PX.Objects.CR;
using PX.Payroll;
using PX.Payroll.Data;
using PX.Payroll.Proxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using PXDataAppLicenseInfo = PX.Api.Payroll.AppLicenseInfo;

namespace PX.Objects.PR
{
	public class PayrollTaxClient : PayrollClientBase<IPayrollTaxService>, IPayrollClientTaxService
    {
		private static readonly Dictionary<string, string> ServiceNamesByCountry = new Dictionary<string, string>()
		{
			{ LocationConstants.USCountryCode, "Tax" }
		};

		public static bool IsCountrySupported(string countryID) => ServiceNamesByCountry.ContainsKey(countryID);

		public PayrollTaxClient(string countryID) : base(ServiceNamesByCountry[countryID]) { }

        public IEnumerable<PRPayrollCalculation> Calculate(IEnumerable<PRPayroll> payrolls)
        {
			try
			{
				List<PRPayrollCalculation> results = new List<PRPayrollCalculation>();
				AppLicenseInfo license = PayrollServiceLicenseHelper.GetLicenseForPayrollWcf();

				foreach ( var payrollPartition in PartitionListForWcf(license, payrolls.ToList()))
				{
					results.AddRange( base.Channel.Calculate(license, payrollPartition));
				}
				return results;
			}
			catch (Exception ex)
			{
				throw ProcessWebServiceException(ex);
			}
		}

        public PRLocationCodeDescription GetLocationCode(IAddressBase address)
        {
			try
			{ 
				var prAddress = new PRAddress()
				{
					Address1 = address.AddressLine1,
					Address2 = address.AddressLine2,
					City = address.City,
					State = address.State,
					ZipCode = address.PostalCode
				};

				return base.Channel.GetLocationCode(PayrollServiceLicenseHelper.GetLicenseForPayrollWcf(), prAddress);
			}
			catch (Exception ex)
			{
				throw ProcessWebServiceException(ex);
			}
		}

		public Dictionary<int?, PRLocationCodeDescription> GetLocationCodes(IEnumerable<Address> addresses)
		{
			try
			{
				var uniqueAddresses = addresses.Distinct(x => x.AddressID);
				IEnumerable<PRAddress> prAddresses = uniqueAddresses.Select(x => x.ToPRAddress());
				Dictionary<int?, PRLocationCodeDescription> results = new Dictionary<int?, PRLocationCodeDescription>();
				AppLicenseInfo license = PayrollServiceLicenseHelper.GetLicenseForPayrollWcf();
				foreach (List<PRAddress> addressPartitions in PartitionListForWcf(license, prAddresses.ToList()))
				{
					results.AddRange(base.Channel.GetLocationCodes(license, addressPartitions));
				}

				return results;
			}
			catch (Exception ex)
			{
				throw ProcessWebServiceException(ex);
			}
		}

		public IEnumerable<Payroll.Data.PRTaxType> GetTaxTypes(string taxLocationCode, string taxMunicipalCode, string taxSchoolCode, bool includeRailroadTaxes)
        {
			try
			{
				AppLicenseInfo licenseInfo = PayrollServiceLicenseHelper.GetLicenseForPayrollWcf();
				PRLocationCode locationCode = CreateLocationCode(taxLocationCode, taxMunicipalCode, taxSchoolCode);
				if (includeRailroadTaxes)
				{
					return base.Channel.GetTaxTypesIncludeRailroad(licenseInfo, locationCode);
				}
				else
				{
					return base.Channel.GetTaxTypes(licenseInfo, locationCode);
				}
			}
			catch (Exception ex)
			{
				throw ProcessWebServiceException(ex);
			}
		}

		public IEnumerable<Payroll.Data.PRTaxType> GetAllLocationTaxTypes(IEnumerable<Address> addresses, bool includeRailroadTaxes)
		{
			try
			{
				IEnumerable<PRLocationCode> prLocationCodes = addresses.Select(address => CreateLocationCode(address.TaxLocationCode, address.TaxMunicipalCode, address.TaxSchoolCode)).Distinct();

				List<Payroll.Data.PRTaxType> results = new List<Payroll.Data.PRTaxType>();
				AppLicenseInfo license = PayrollServiceLicenseHelper.GetLicenseForPayrollWcf();
				foreach (List<PRLocationCode> locationCodePartition in PartitionListForWcf(license, prLocationCodes.ToList()))
				{
					results.AddRange(includeRailroadTaxes ?
						base.Channel.GetAllLocationTaxTypesIncludeRailroad(license, locationCodePartition) :
						base.Channel.GetAllLocationTaxTypes(license, locationCodePartition));
				}

				return results;
			}
			catch (Exception ex)
			{
				throw ProcessWebServiceException(ex);
			}
		}

		public IEnumerable<Payroll.Data.PRTaxType> GetSpecificTaxTypes(string typeName, string locationSearch)
        {
			try
			{
				return base.Channel.GetSpecificTaxTypes(PayrollServiceLicenseHelper.GetLicenseForPayrollWcf(), typeName, new PRLocationFinder() { LocationSearch = locationSearch });
			}
			catch (Exception ex)
			{
				throw ProcessWebServiceException(ex);
			}
		}

        public Dictionary<string, SymmetryToAatrixTaxMapping> GetAatrixTaxMapping(IEnumerable<PX.Payroll.Data.PRTaxType> uniqueTaxes)
        {
			try
			{
				Dictionary<string, SymmetryToAatrixTaxMapping> results = new Dictionary<string, SymmetryToAatrixTaxMapping>();
				AppLicenseInfo license = PayrollServiceLicenseHelper.GetLicenseForPayrollWcf();
				foreach (List<PX.Payroll.Data.PRTaxType> uniqueTaxesPartitions in PartitionListForWcf(license, uniqueTaxes.ToList()))
				{
					foreach(var taxMapping in base.Channel.GetAatrixTaxMapping(license, uniqueTaxesPartitions))
					{
						results[taxMapping.Key] = taxMapping.Value;
					}
				}

				return results;
			}
			catch (Exception ex)
			{
				throw ProcessWebServiceException(ex);
			}
		}

        public byte[] GetTaxMappingFile()
        {
			try
			{
				return base.Channel.GetTaxMappingFile(PayrollServiceLicenseHelper.GetLicenseForPayrollWcf());
			}
			catch (Exception ex)
			{
				throw ProcessWebServiceException(ex);
			}
		}

		private PRLocationCode CreateLocationCode(string taxLocationCode, string taxMunicipalCode, string taxSchoolCode)
		{
			return new PRLocationCode()
			{
				TaxLocationCode = taxLocationCode,
				TaxMunicipalCode = taxMunicipalCode,
				TaxSchoolCode = taxSchoolCode
			};
		}

		private List<List<TType>> PartitionListForWcf<TType>(AppLicenseInfo license, List<TType> list)
		{
			List<List<TType>> partitions = new List<List<TType>>() { list };

			DataContractSerializer licenseSerializer = new DataContractSerializer(typeof(AppLicenseInfo));
			MemoryStream memStream = new MemoryStream();
			licenseSerializer.WriteObject(memStream, license);
			long licenseLength = memStream.Length;
			long maxPayloadLength = (long)(PayrollWebServiceConfiguration.ServerMaxReceivedMessageSize * PayrollWebServiceConfiguration.PayloadRatio) - licenseLength;

			bool partitioned;
			do
			{
				partitioned = false;
				List<List<TType>> newPartitions = new List<List<TType>>();
				foreach (List<TType> partition in partitions)
				{
					DataContractSerializer serializer = new DataContractSerializer(typeof(List<TType>));
					memStream = new MemoryStream();
					serializer.WriteObject(memStream, partition);
					if (memStream.Length > maxPayloadLength && partition.Count() > 1)
					{
						partitioned = true;
						newPartitions.Add(partition.Take(partition.Count() / 2).ToList());
						newPartitions.Add(partition.Skip(partition.Count() / 2).ToList());
					}
					else
					{
						newPartitions.Add(partition);
					}

					memStream.Close();
				}

				partitions = newPartitions;
			} while (partitioned);

			return partitions;
		}

		private Exception ProcessWebServiceException(Exception ex)
		{
			if (ex is EndpointNotFoundException)
			{
				return new PXException(Messages.CantContactWebservice, ex);
			}
			else
			{
				Exception inner = ex.InnerException;

				FaultException<ExceptionDetail> faultException = ex as FaultException<ExceptionDetail>;
				if (faultException != null)
				{
					inner = new FaultExceptionWrapper(faultException);
					ex = new PXException(ex.Message, inner);
				}

				if (inner != null)
				{
					WebServiceErrorTracer tracer = null;
					AppDomain parentAppDomain = AppDomain.CurrentDomain.GetData(PXPayrollAssemblyScope.ParentAppDomainProperty) as AppDomain;
					if (parentAppDomain == null)
					{
						tracer = new WebServiceErrorTracer();
					}
					else
					{
						tracer = (WebServiceErrorTracer)parentAppDomain.CreateInstanceAndUnwrap(typeof(WebServiceErrorTracer).Assembly.FullName, typeof(WebServiceErrorTracer).FullName);
					}

					tracer.TraceException(inner);
				} 
			}

			return ex;
		}

		[Serializable]
		private class FaultExceptionWrapper : PXException, ISerializable
		{
			private string _StackTrace;

			public FaultExceptionWrapper(FaultException<ExceptionDetail> fault)
			{
				_Message = fault.Detail.InnerException.Message;
				_StackTrace = fault.Detail.InnerException.StackTrace;
			}

			private FaultExceptionWrapper(SerializationInfo info, StreamingContext context) : base(info, context)
			{
				_StackTrace = (string)info.GetValue(nameof(_StackTrace), typeof(string));
			}

			public override void GetObjectData(SerializationInfo info, StreamingContext context)
			{
				base.GetObjectData(info, context);
				info.AddValue(nameof(_StackTrace), _StackTrace, typeof(string));
			}

			public override string StackTrace => _StackTrace;
		}

		[Serializable]
		private class WebServiceErrorTracer : MarshalByRefObject
		{
			public void TraceException(Exception ex)
			{
				PXTrace.WriteError(ex);
			}
		}
	}
}
