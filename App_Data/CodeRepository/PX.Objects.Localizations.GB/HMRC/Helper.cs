using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using System.Web;

namespace PX.Objects.Localizations.GB.HMRC
{
    static class Helper
    {
        private static string _deviceManufacturer;
        private static string _deviceModel;

        private static void _fillDeviceInfo()
        {
            try
            {


                System.Management.SelectQuery query =
                    new System.Management.SelectQuery(@"Select * from Win32_ComputerSystem");
                using (System.Management.ManagementObjectSearcher searcher =
                    new System.Management.ManagementObjectSearcher(query))
                {
                    System.Management.ManagementObject process =
                        searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    process.Get();
                    _deviceManufacturer = process["Manufacturer"].ToString();
                    _deviceModel = process["Model"].ToString();
                }
            }
            catch
            {
                _deviceManufacturer = "Unknown";
                _deviceModel = "Unknown";
            }
        }

        public static string DeviceManufacturer
        {
            get
            {
                if (!String.IsNullOrEmpty(_deviceManufacturer))
                {
                    return _deviceManufacturer;
                }
                else
                {
                    _fillDeviceInfo();
                    return _deviceManufacturer;
                }
            }
        }

        public static string DeviceModel
        {
            get
            {
                if (!String.IsNullOrEmpty(_deviceModel))
                {
                    return _deviceModel;
                }
                else
                {
                    _fillDeviceInfo();
                    return _deviceModel;
                }
            }
        }

        public static string GetOsName()
        {
            OperatingSystem os_info = System.Environment.OSVersion;

            return "os-family=Windows&"+
                $"os-version={os_info.Version.Major.ToString()}.{os_info.Version.Minor.ToString()}&"+
                $"device-manufacturer={Uri.EscapeDataString(DeviceManufacturer)}&"+
                $"device-model={Uri.EscapeDataString(DeviceModel)}";
        }

        public static string GetCompanyNameAsGuid(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            Guid NameSpace_X500 = new Guid("6ba7b814-9dad-11d1-80b4-00c04fd430c8");

            // convert the name to a sequence of octets (as defined by the standard or conventions of its namespace) (step 3)
            // ASSUME: UTF-8 encoding is always appropriate
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);

            // convert the namespace UUID to network order (step 3)
            byte[] namespaceBytes = NameSpace_X500.ToByteArray();
            SwapByteOrder(namespaceBytes);

            // comput the hash of the name space ID concatenated with the name (step 4)
            byte[] hash;
            using (HashAlgorithm algorithm = SHA1.Create())
            {
                algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
                algorithm.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
                hash = algorithm.Hash;
            }

            // most bytes from the hash are copied straight to the bytes of the new GUID (steps 5-7, 9, 11-12)
            byte[] newGuid = new byte[16];
            Array.Copy(hash, 0, newGuid, 0, 16);

            // set the four most significant bits (bits 12 through 15) of the time_hi_and_version field to the appropriate 4-bit version number from Section 4.1.3 (step 8)
            newGuid[6] = (byte)((newGuid[6] & 0x0F) | (5 << 4));

            // set the two most significant bits (bits 6 and 7) of the clock_seq_hi_and_reserved to zero and one, respectively (step 10)
            newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

            // convert the resulting UUID to local byte order (step 13)
            SwapByteOrder(newGuid);
            return new Guid(newGuid).ToString();
        }



        // Converts a GUID (expressed as a byte array) to/from network order (MSB-first).
        internal static void SwapByteOrder(byte[] guid)
        {
            SwapBytes(guid, 0, 3);
            SwapBytes(guid, 1, 2);
            SwapBytes(guid, 4, 5);
            SwapBytes(guid, 6, 7);
        }

        private static void SwapBytes(byte[] guid, int left, int right)
        {
            byte temp = guid[left];
            guid[left] = guid[right];
            guid[right] = temp;
        }

        public static string GetLocalIPAddresses()
        {
            string ips = PX.Data.Update.PXInstanceHelper.IPAddress;
            if (!string.IsNullOrEmpty(ips))
                return ips;
            ips = "";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host?.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (!string.IsNullOrEmpty(ips))
                        ips += ",";
                    ips += ip.ToString();
                }
            }
            return ips;
        }

        public static string GetMacAddress()
        {
            string macAddresses = "";
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet
                    && nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    macAddresses = string.Join(":",
                        nic.GetPhysicalAddress().GetAddressBytes()
                            .Select(x => x.ToString("X2")));
                     break;
                }
            }
            return macAddresses;
        }

        public static string GetUtcTimestamp()
        {
            var now = DateTime.UtcNow;
            return $"{now:yyyy-MM-dd}T{now:HH:mm:ss.fff}Z";
        }

        public static string GetVendorIPAddress()
        {
	        return new WebClient().DownloadString("https://api.ipify.org");
        }

        public static string GetLicenseHash(string licenseId)
        {
	        using (var md5 = new MD5CryptoServiceProvider())
	        {
		        using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(licenseId ?? "")))
		        {
			        return Uri.EscapeDataString(
				        md5.ComputeHash(stream).Aggregate(new StringBuilder(), (acc, b) =>
				        {
					        acc.Append(b.ToString("X2"));
					        return acc;
				        }).ToString());
		        }
	        }
        }

        public static string GetTimeZone()
        {
	        string timezone = "UTC" + (TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours >= 0
		                                ? "+"
		                                : "-")
	                                + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).ToString(@"hh\:mm");

	        if (string.IsNullOrEmpty(timezone))
		        timezone = "UTC+01:00";

	        return timezone;
        }

        public static string GetDeviceId()
        {
	        if (HttpContext.Current?.Request != null)
	        {
		        HttpCookie cookie = HttpContext.Current.Request.Cookies["AcumaticaHMRC"];
		        if (cookie == null)
		        {
			        cookie = new HttpCookie("AcumaticaHMRC");
			        string newId = Guid.NewGuid().ToString();
			        cookie.Values.Add("DeviceID", newId);
		        }
		        cookie.Expires = DateTime.Now.AddYears(1);
		        HttpContext.Current?.Response.Cookies.Add(cookie);
		        return cookie["DeviceID"];
	        }
	        return new Guid().ToString();
        }

        public const string TestSiteURL = "https://test-api.service.hmrc.gov.uk";
        public const string ProductionSiteURL = "https://api.service.hmrc.gov.uk";

        public static string GetSiteUrl(bool isTestEnv)
        {
	        return isTestEnv ? TestSiteURL : ProductionSiteURL;
        }

    }
}
