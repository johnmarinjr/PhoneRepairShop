using PX.Data;
using PX.Objects.AR;
using System;
using PX.Objects.GL;
using PX.Objects.PM;

namespace PX.Objects.FS
{
    [Serializable]
    public class DBoxDocSettings : IBqlTable
    {
        #region DestinationDocument
        public abstract class destinationDocument : PX.Data.BQL.BqlString.Field<destinationDocument>
        {
            public abstract class Values : ListField_Billing_By { }
        }

        [PXString(2, IsFixed = true)]
        [PXUIField(DisplayName = "Destination Document", Visible = false)]
        [destinationDocument.Values.ListAtrribute]
        public virtual string DestinationDocument { get; set; }
        #endregion
        #region CustomerID
        public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }

        [PXInt]
        [PXUIField(DisplayName = "Customer ID")]
        [FSSelectorBusinessAccount_CU_PR_VC]
        public virtual int? CustomerID { get; set; }
        #endregion
        #region SrvOrdType
        public abstract class srvOrdType : PX.Data.BQL.BqlString.Field<srvOrdType> { }

        [PXString(4, IsFixed = true)]
        [PXDefault(typeof(Coalesce<
            Search<
            FSxUserPreferences.dfltSrvOrdType,
            Where<
                PX.SM.UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>>>,
            Search<
            FSSetup.dfltSrvOrdType>>))]
        [PXUIField(DisplayName = "Service Order Type")]
        [FSSelectorSrvOrdType]
        public virtual string SrvOrdType { get; set; }
        #endregion
        #region BranchID
        public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

        [PXInt]
        [PXDefault]
        [PXUIField(DisplayName = "Branch")]
        [PXSelector(typeof(Branch.branchID), SubstituteKey = typeof(Branch.branchCD), DescriptionField = typeof(Branch.acctName))]
        public virtual int? BranchID { get; set; }
        #endregion
        #region BranchLocationID
        public abstract class branchLocationID : PX.Data.BQL.BqlInt.Field<branchLocationID> { }

        [PXInt]
        [PXDefault(typeof(
            Search<
                FSxUserPreferences.dfltBranchLocationID,
                Where<
                PX.SM.UserPreferences.userID, Equal<CurrentValue<AccessInfo.userID>>,
                    And<PX.SM.UserPreferences.defBranchID, Equal<Current<DBoxDocSettings.branchID>>>>>))]
        [PXFormula(typeof(Default<branchID>))]
        [PXUIField(DisplayName = "Branch Location")]
        [PXSelector(typeof(
            Search<
                FSBranchLocation.branchLocationID,
                Where<
                FSBranchLocation.branchID, Equal<Current<branchID>>>>),
            SubstituteKey = typeof(FSBranchLocation.branchLocationCD),
            DescriptionField = typeof(FSBranchLocation.descr))]
        public virtual int? BranchLocationID { get; set; }
        #endregion
        #region Description
        public abstract class description : PX.Data.BQL.BqlString.Field<description> { }

        [PXString]
        [PXUIField(DisplayName = "Description")]
        public virtual string Description { get; set; }
		#endregion
		#region Details
		public abstract class details : PX.Data.BQL.BqlString.Field<details> { }

		[PXDBText(IsUnicode = true)]
		[PXUIField(DisplayName = "Details")]
		public virtual String LongDescr { get; set; }
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

        [ProjectDefault]
        [ProjectBase(typeof(DBoxDocSettings.customerID))]
        public virtual int? ProjectID { get; set; }
        #endregion
        #region ProjectTaskID
        public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }

        [PXInt]
        [PXFormula(typeof(Default<projectID>))]
        [PXUIField(DisplayName = "Default Project Task", Visibility = PXUIVisibility.Visible, FieldClass = ProjectAttribute.DimensionName)]
        [PXDefault(typeof(Search<PMTask.taskID,
                            Where<PMTask.projectID, Equal<Current<projectID>>,
                            And<PMTask.isDefault, Equal<True>,
                            And<PMTask.isCompleted, Equal<False>,
                            And<PMTask.isCancelled, Equal<False>>>>>>),
                            PersistingCheck = PXPersistingCheck.Nothing)]
        [FSSelectorActive_AR_SO_ProjectTask(typeof(Where<PMTask.projectID, Equal<Current<projectID>>>), typeof(On<FSSrvOrdType.srvOrdType, Equal<Current<srvOrdType>>>))]
        public virtual int? ProjectTaskID { get; set; }
        #endregion
        #region OrderDate
        public abstract class orderDate : PX.Data.BQL.BqlDateTime.Field<orderDate> { }

        [PXDate]
        [PXDefault(typeof(AccessInfo.businessDate))]
        [PXUIField(DisplayName = "Order Date")]
        public virtual DateTime? OrderDate { get; set; }
        #endregion
        #region SLAETA
        public abstract class sLAETA : PX.Data.BQL.BqlDateTime.Field<sLAETA> { }

        [PXDBDateAndTime(UseTimeZone = true, PreserveTime = true, DisplayNameDate = "SLA")]
        [PXUIField(DisplayName = "SLA")]
        public virtual DateTime? SLAETA { get; set; }
        #endregion
        #region AssignedEmpID
        public abstract class assignedEmpID : PX.Data.BQL.BqlInt.Field<assignedEmpID> { }

        [PXInt]
        [FSSelector_StaffMember_All]
        [PXUIField(DisplayName = "Supervisor")]
        public virtual int? AssignedEmpID { get; set; }
        #endregion
        #region ProblemID
        public abstract class problemID : PX.Data.BQL.BqlInt.Field<problemID> { }

        [PXInt]
        [PXUIField(DisplayName = "Problem")]
        [PXSelector(typeof(Search2<
            FSProblem.problemID,
            InnerJoin<FSSrvOrdTypeProblem,
                On<FSProblem.problemID, Equal<FSSrvOrdTypeProblem.problemID>>,
            InnerJoin<FSSrvOrdType,
                On<FSSrvOrdType.srvOrdType, Equal<FSSrvOrdTypeProblem.srvOrdType>>>>,
            Where<FSSrvOrdType.srvOrdType, Equal<Current<DBoxDocSettings.srvOrdType>>>>),
                            SubstituteKey = typeof(FSProblem.problemCD), DescriptionField = typeof(FSProblem.descr))]
        public virtual int? ProblemID { get; set; }
        #endregion

        #region HandleManuallyScheduleTime
        public abstract class handleManuallyScheduleTime : PX.Data.BQL.BqlBool.Field<handleManuallyScheduleTime> { }

        [PXBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIVisible(typeof(DBoxDocSettings.destinationDocument.FromCurrent.IsEqual<destinationDocument.Values.Appointment>))]
        [PXUIField(DisplayName = "Override")]
        public virtual bool? HandleManuallyScheduleTime { get; set; }
        #endregion
        #region ScheduledDateTimeBegin
        public abstract class scheduledDateTimeBegin : PX.Data.BQL.BqlDateTime.Field<scheduledDateTimeBegin> { }

        [PXDBDateAndTime(UseTimeZone = true, PreserveTime = true, DisplayNameDate = "Scheduled Start Date", DisplayNameTime = "Scheduled Start Time")]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIVisible(typeof(DBoxDocSettings.destinationDocument.FromCurrent.IsEqual<destinationDocument.Values.Appointment>))]
        [PXUIField(DisplayName = "Scheduled Start Date", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual DateTime? ScheduledDateTimeBegin { get; set; }
        #endregion
        #region ScheduledDateTimeEnd
        public abstract class scheduledDateTimeEnd : PX.Data.BQL.BqlDateTime.Field<scheduledDateTimeEnd> { }

        [PXDBDateAndTime(UseTimeZone = true, PreserveTime = true, DisplayNameDate = "Scheduled End Date", DisplayNameTime = "Scheduled End Time")]
        [PXDefault(typeof(Switch<
                            Case<
                                Where<handleManuallyScheduleTime, Equal<True>>,
                                    scheduledDateTimeBegin>, 
                            Null>),PersistingCheck = PXPersistingCheck.Nothing)]
        [PXFormula(typeof(Default<handleManuallyScheduleTime, scheduledDateTimeBegin>))]
        [PXUIVisible(typeof(DBoxDocSettings.destinationDocument.FromCurrent.IsEqual<destinationDocument.Values.Appointment>))]
        [PXUIEnabled(typeof(handleManuallyScheduleTime))]
        [PXUIField(DisplayName = "Scheduled End Date")]
        public virtual DateTime? ScheduledDateTimeEnd { get; set; }
        #endregion
    }
}

