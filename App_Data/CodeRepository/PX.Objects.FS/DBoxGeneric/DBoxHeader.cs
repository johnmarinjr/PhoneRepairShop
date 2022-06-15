using PX.Data;
using PX.Objects.AP;
using PX.Objects.IN;
using System;

namespace PX.Objects.FS
{
    public class DBoxHeader
    {

        #region CustomerID
        public virtual int? CustomerID { get; set; }
        #endregion
        #region LocationID
        public virtual int? LocationID { get; set; }
        #endregion
        #region CuryID
        public virtual string CuryID { get; set; }
        #endregion
        #region ContactID
        public virtual int? ContactID { get; set; }
        #endregion
        #region SalesPersonID
        public virtual int? SalesPersonID { get; set; }
        #endregion
        #region TaxZoneID
        public virtual string TaxZoneID { get; set; }
        #endregion
        #region SrvOrdType
        public virtual string SrvOrdType { get; set; }
        #endregion
        #region BranchID
        public virtual int? BranchID { get; set; }
        #endregion
        #region BranchLocationID
        public virtual int? BranchLocationID { get; set; }
        #endregion
        #region Description
        public virtual string Description { get; set; }
		#endregion
		#region Details
		public virtual string LongDescr { get; set; }
		#endregion
		#region ProjectID
		public virtual int? ProjectID { get; set; }
        #endregion
        #region ProjectTaskID
        public virtual int? ProjectTaskID { get; set; }
        #endregion
        #region OrderDate
        public virtual DateTime? OrderDate { get; set; }
        #endregion
        #region SLAETA
        public virtual DateTime? SLAETA { get; set; }
        #endregion
        #region AssignedEmpID
        public virtual int? AssignedEmpID { get; set; }
        #endregion
        #region ProblemID
        public virtual int? ProblemID { get; set; }
        #endregion
        #region ScheduledDateTimeBegin
        public virtual DateTime? ScheduledDateTimeBegin { get; set; }
        #endregion
        #region ScheduledDateTimeEnd
        public virtual DateTime? ScheduledDateTimeEnd { get; set; }
        #endregion
        #region HandleManuallyScheduleTime
        public virtual bool? HandleManuallyScheduleTime { get; set; }
        #endregion
        #region CreateAppointment
        public virtual bool? CreateAppointment { get; set; }
        #endregion

        #region CopyNotes
        public virtual bool? CopyNotes { get; set; }
        #endregion
        #region CopyFiles
        public virtual bool? CopyFiles { get; set; }
        #endregion

        #region HeaderContact
        public DBoxHeaderContact Contact;
        #endregion
        #region HeaderAddress
        public DBoxHeaderAddress Address;
        #endregion

        public object sourceDocument;

        public static implicit operator DBoxHeader(DBoxDocSettings docSettings)
        {
            if (docSettings == null)
            {
                return null;
            }

            DBoxHeader ret = new DBoxHeader();

            ret.CustomerID = docSettings.CustomerID;
            ret.SrvOrdType = docSettings.SrvOrdType;
            ret.BranchID = docSettings.BranchID;
            ret.BranchLocationID = docSettings.BranchLocationID;
            ret.Description = docSettings.Description;
			ret.LongDescr = docSettings.LongDescr;
            ret.ProjectID = docSettings.ProjectID;
            ret.ProjectTaskID = docSettings.ProjectTaskID;
            ret.OrderDate = docSettings.OrderDate;
            ret.SLAETA = docSettings.SLAETA;
            ret.AssignedEmpID = docSettings.AssignedEmpID;
            ret.ProblemID = docSettings.ProblemID;
            ret.ScheduledDateTimeBegin = docSettings.ScheduledDateTimeBegin;
            ret.HandleManuallyScheduleTime = docSettings.HandleManuallyScheduleTime;
            ret.CreateAppointment = docSettings.DestinationDocument == ID.PostDoc_EntityType.APPOINTMENT;

            if (docSettings.HandleManuallyScheduleTime == true)
            {
                ret.ScheduledDateTimeEnd = docSettings.ScheduledDateTimeEnd;
            }

            return ret;
        }
    }
}

