using System;
using PX.Data;

namespace PX.Objects.AM
{
    [PXCacheName("Default events")]
    public abstract class DefaultEvent : IBqlTable
    {
        #region Id
        [PXString(IsKey = true)]
        public virtual string Id { get; set; }

        public abstract class id : Data.BQL.BqlString.Field<id>
        {
        }

        #endregion

        #region ResourceId

        public virtual string ResourceId { get; set; }

        public abstract class resourceId : Data.BQL.BqlString.Field<resourceId>
        {
        }

        #endregion

        #region Name

        public virtual string Name { get; set; }

        public abstract class name : Data.BQL.BqlString.Field<name>
        {
        }

        #endregion

        #region StartDate

        public virtual DateTime? StartDate { get; set; }

        public abstract class startDate : Data.BQL.BqlDateTime.Field<startDate>
        {
        }

        #endregion

        #region EndDate

        public virtual DateTime? EndDate { get; set; }

        public abstract class endDate : Data.BQL.BqlDateTime.Field<endDate>
        {
        }

        #endregion
    }

    [PXCacheName("Production orders events")]
    public class ProductionOrderEvent : DefaultEvent
    {
        #region Outside

        public virtual bool? Outside { get; set; }

        public abstract class outside : Data.BQL.BqlBool.Field<outside>
        {
        }

        #endregion

        #region Descr

        public virtual string Descr { get; set; }

        public abstract class descr : Data.BQL.BqlString.Field<descr>
        {
        }

        #endregion

        #region LackOfMaterials

        public virtual bool? LackOfMaterials { get; set; }

        public abstract class lackOfMaterials : Data.BQL.BqlBool.Field<lackOfMaterials>
        {
        }

        #endregion
    }

    [PXCacheName("Base scheduled events")]
    public abstract class BaseScheduledEvent : DefaultEvent
    {
	    #region OrdRef

	    public virtual string OrdRef { get; set; }

	    public abstract class ordRef : Data.BQL.BqlString.Field<ordRef>
	    {
	    }

	    #endregion

        #region OrdNum

        public virtual string OrdNum { get; set; }

        public abstract class ordNum : Data.BQL.BqlString.Field<ordNum>
        {
        }

        #endregion
    }

    [PXCacheName("Work centers events")]
    public class WorkCenterEvent : BaseScheduledEvent
    {
    }

    [PXCacheName("Machines events")]
    public class MachineEvent : BaseScheduledEvent
    {
    }

    [PXCacheName("Calendar Intervals")]
    public class CalendarInterval : IBqlTable
    {
        #region Id
        [PXString(IsKey = true)]
        public virtual string Id { get; set; }

        public abstract class id : Data.BQL.BqlString.Field<id>
        {
        }

        #endregion

        #region CalendarId

        public virtual string CalendarId { get; set; }

        public abstract class calendarId : Data.BQL.BqlString.Field<calendarId>
        {
        }

        #endregion

        #region RecurrentStartDate (string)

        public virtual string RecurrentStartDate { get; set; }

        public abstract class recurrentStartDate : Data.BQL.BqlString.Field<recurrentStartDate>
        {
        }

        #endregion

        #region RecurrentEndDate (string)

        public virtual string RecurrentEndDate { get; set; }

        public abstract class recurrentEndDate : Data.BQL.BqlString.Field<recurrentEndDate>
        {
        }

        #endregion

        #region IsWorking

        [PXBool]
        [PXUIField(DisplayName = "Is Working", Visible = false)]
        public virtual bool? IsWorking { get; set; }

        public abstract class isWorking : Data.BQL.BqlBool.Field<isWorking>
        {
        }

        #endregion
    }
}
