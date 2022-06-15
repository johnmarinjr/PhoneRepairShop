using System;
using PX.Data;

namespace PX.Objects.Localizations.GB.HMRC
{
    // Token: 0x02000011 RID: 17
    [Serializable]
    public class HMRCHeaderData : IBqlTable
    {
        #region Gov Headers

        #region GovClientConnectionMethod
        [PXDBString(255)]
        public virtual string GovClientConnectionMethod { get; set; }
        public abstract class govClientConnectionMethod : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientPublicIP
        [PXDBString(255)]
        public virtual string GovClientPublicIP { get; set; }
        public abstract class govClientPublicIP : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientPublicIPTimestamp
        [PXDBString(255)]
        public virtual string GovClientPublicIPTimestamp { get; set; }
        public abstract class govClientPublicIPTimestamp : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientPublicPort
        [PXDBString(255)]
        public virtual string GovClientPublicPort { get; set; }
        public abstract class govClientPublicPort : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientDeviceID
        [PXDBString(255)]
        public virtual string GovClientDeviceID { get; set; }
        public abstract class govClientDeviceID : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientUserIDs
        [PXDBString(255)]
        public virtual string GovClientUserIDs { get; set; }
        public abstract class govClientUserIDs : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientTimezone
        [PXDBString(255)]
        public virtual string GovClientTimezone { get; set; }
        public abstract class govClientTimezone : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientLocalIPs
        [PXDBString(255)]
        public virtual string GovClientLocalIPs { get; set; }
        public abstract class govClientLocalIPs : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientLocalIPsTimestamp
        [PXDBString(255)]
        public virtual string GovClientLocalIPsTimestamp { get; set; }
        public abstract class govClientLocalIPsTimestamp : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientScreens
        [PXDBString(255)]
        public virtual string GovClientScreens { get; set; }
        public abstract class govClientScreens : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientWindowSize
        [PXDBString(255)]
        public virtual string GovClientWindowSize { get; set; }
        public abstract class govClientWindowSize : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientBrowserPlugins
        [PXDBString(255)]
        public virtual string GovClientBrowserPlugins { get; set; }
        public abstract class govClientBrowserPlugins : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientBrowserJSUserAgent
        [PXDBString(255)]
        public virtual string GovClientBrowserJSUserAgent { get; set; }
        public abstract class govClientBrowserJSUserAgent : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientBrowserDoNotTrack
        [PXDBString(255)]
        public virtual string GovClientBrowserDoNotTrack { get; set; }
        public abstract class govClientBrowserDoNotTrack : IBqlField, IBqlOperand { }
        #endregion
        #region GovClientMultiFactor
        [PXDBString(255)]
        public virtual string GovClientMultiFactor { get; set; }
        public abstract class govClientMultiFactor : IBqlField, IBqlOperand { }
        #endregion
        #region GovVendorProductName
        [PXDBString(255)]
        public virtual string GovVendorProductName { get; set; }
        public abstract class govVendorProductName : IBqlField, IBqlOperand { }
        #endregion
        #region GovVendorVersion
        [PXDBString(255)]
        public virtual string GovVendorVersion { get; set; }
        public abstract class govVendorVersion : IBqlField, IBqlOperand { }
        #endregion
        #region GovVendorLicenseIDs
        [PXDBString(255)]
        public virtual string GovVendorLicenseIDs { get; set; }
        public abstract class govVendorLicenseIDs : IBqlField, IBqlOperand { }
        #endregion
        #region GovVendorPublicIP
        [PXDBString(255)]
        public virtual string GovVendorPublicIP { get; set; }
        public abstract class govVendorPublicIP : IBqlField, IBqlOperand { }
        #endregion
        #region GovVendorForwarded
        [PXDBString(255)]
        public virtual string GovVendorForwarded { get; set; }
        public abstract class govVendorForwarded : IBqlField, IBqlOperand { }
        #endregion

        #endregion
    }
}
