using System;
using PX.Data;
using PX.Objects.AM.Attributes;

namespace PX.Objects.AM
{
    [PXCacheName("Production orders resources")]
    public class ProductionOrderResource : IBqlTable
    {
	    #region Id
	    [PXString(IsKey = true)]
	    [PXUIField(DisplayName = "ID", Visible = false, Visibility = PXUIVisibility.Invisible)]
	    public virtual string Id { get; set; }

	    public abstract class id : Data.BQL.BqlString.Field<id>
	    {
	    }

        #endregion

        #region Selected

        [PXBool]
        [PXUIField(DisplayName = "Selected")]
        public virtual bool? Selected { get; set; }

        public abstract class selected : Data.BQL.BqlBool.Field<selected>
        {
        }

        #endregion

        #region OrdType

        [PXString]
        [PXUIField(DisplayName = "Type")]
        public virtual string OrdType { get; set; }

        public abstract class ordType : Data.BQL.BqlString.Field<ordType>
        {
        }

        #endregion

        #region OrdNum
        [PXString()]
        [PXUIField(DisplayName = "Production Nbr.")]
        public virtual string OrdNum { get; set; }

        public abstract class ordNum : Data.BQL.BqlString.Field<ordNum>
        {
        }

        #endregion

        #region Descr

        [PXString]
        [PXUIField(DisplayName = "Production Order Description", Visible = false)]
        public virtual string Descr { get; set; }

        public abstract class descr : Data.BQL.BqlString.Field<descr>
        {
        }

        #endregion

        #region InvId

        [PXString]
        [PXUIField(DisplayName = "Inventory ID")]
        public virtual string InvId { get; set; }

        public abstract class invId : Data.BQL.BqlString.Field<invId>
        {
        }

        #endregion

        #region Priority

		[PXDBShort(MinValue = 1, MaxValue = 10)]
        [PXUIField(DisplayName = "Dispatch Priority")]
        public virtual short? Priority { get; set; }

        public abstract class priority : Data.BQL.BqlShort.Field<priority>
        {
        }

        #endregion

        #region Constraint

        [PXDate]
        [PXUIField(DisplayName = "Constraint")]
        public virtual DateTime? Constraint { get; set; }

        public abstract class constraint : Data.BQL.BqlDateTime.Field<constraint>
        {
        }

        #endregion

        #region FirmSchedule

        [PXBool]
        [PXUIField(DisplayName = "Firm Schedule", Enabled = false)]
        public virtual bool? FirmSchedule { get; set; }

        public abstract class firmSchedule : Data.BQL.BqlBool.Field<firmSchedule>
        {
        }

        #endregion

        #region ScheduleStatus
        public abstract class scheduleStatus : PX.Data.BQL.BqlString.Field<scheduleStatus> { }

        [PXString]
        [PXUIField(DisplayName = "Schedule Status", Visible = false)]
        [ProductionScheduleStatus.List]
        public virtual string ScheduleStatus { get; set; }
        #endregion

        #region Hold

        [PXBool]
        [PXUIField(DisplayName = "Hold", Visible = false)]
        public virtual bool? Hold { get; set; }

        public abstract class hold : Data.BQL.BqlBool.Field<hold>
        {
        }

        #endregion

        #region OrdTypeDescr

        [PXString]
        [PXUIField(DisplayName = "Order Type Description", Visible = false)]
        public virtual string OrdTypeDescr { get; set; }

        public abstract class ordTypeDescr : Data.BQL.BqlString.Field<ordTypeDescr>
        {
        }

        #endregion

        #region OrdStatus

        [PXString]
        [PXUIField(DisplayName = "Production Order Status", Visible = false)]
        [ProductionOrderStatus.List]
        public virtual string OrdStatus { get; set; }

        public abstract class ordStatus : Data.BQL.BqlString.Field<ordStatus>
        {
        }

        #endregion

        #region CustomerId

        [PXString]
        [PXUIField(DisplayName = "Customer ID", Visible = false)]
        public virtual string CustomerId { get; set; }

        public abstract class customerId : Data.BQL.BqlString.Field<customerId>
        {
        }

        #endregion

        #region CustomerName

        [PXString]
        [PXUIField(DisplayName = "Customer Name", Visible = false)]
        public virtual string CustomerName { get; set; }

        public abstract class customerName : Data.BQL.BqlString.Field<customerName>
        {
        }

        #endregion

        #region WorkgroupDsc

        [PXString]
        [PXUIField(DisplayName = "Product Workgroup", Visible = false)]
        public virtual string WorkgroupDsc { get; set; }

        public abstract class workgroupDsc : Data.BQL.BqlString.Field<workgroupDsc>
        {
        }

        #endregion

        #region ProductManager

        [PXString]
        [PXUIField(DisplayName = "Product Manager", Visible = false)]
        public virtual string ProductManager { get; set; }

        public abstract class productManager : Data.BQL.BqlString.Field<productManager>
        {
        }

        #endregion

        #region StartDate

        [PXDate]
        [PXUIField(DisplayName = "Start Date", Visible = false)]
        public virtual DateTime? StartDate { get; set; }

        public abstract class startDate : Data.BQL.BqlDateTime.Field<startDate>
        {
        }

        #endregion

        #region EndDate

        [PXDate]
        [PXUIField(DisplayName = "End Date", Visible = false)]
        public virtual DateTime? EndDate { get; set; }

        public abstract class endDate : Data.BQL.BqlDateTime.Field<endDate>
        {
        }

        #endregion

        #region QtyP

        [PXDecimal]
        [PXUIField(DisplayName = "Qty to Produce", Visible = false)]
        public virtual decimal? QtyP { get; set; }

        public abstract class qtyP : Data.BQL.BqlDecimal.Field<qtyP>
        {
        }

        #endregion

        #region QtyR

        [PXDecimal]
        [PXUIField(DisplayName = "Qty Remaining", Visible = false)]
        public virtual decimal? QtyR { get; set; }

        public abstract class qtyR : Data.BQL.BqlDecimal.Field<qtyR>
        {
        }

        #endregion

        #region Uom

        [PXString]
        [PXUIField(DisplayName = "UOM", Visible = false)]
        public virtual string Uom { get; set; }

        public abstract class uom : Data.BQL.BqlString.Field<uom>
        {
        }

        #endregion

        #region OrdDate

        [PXDate]
        [PXUIField(DisplayName = "Order Date", Visible = false)]
        public virtual DateTime? OrdDate { get; set; }

        public abstract class ordDate : Data.BQL.BqlDateTime.Field<ordDate>
        {
        }

        #endregion

        #region Warehouse

        [PXString]
        [PXUIField(DisplayName = "Warehouse", Visible = false)]
        public virtual string Warehouse { get; set; }

        public abstract class warehouse : Data.BQL.BqlString.Field<warehouse>
        {
        }

        #endregion

        #region SoOrderType

        [PXString]
        [PXUIField(DisplayName = "SO Order Type", Visible = false)]
        public virtual string SoOrderType { get; set; }

        public abstract class soOrderType : Data.BQL.BqlString.Field<soOrderType>
        {
        }

        #endregion

        #region SoOrderNumber

        [PXString]
        [PXUIField(DisplayName = "SO Order Nbr.", Visible = false)]
        public virtual string SoOrderNumber { get; set; }

        public abstract class soOrderNumber : Data.BQL.BqlString.Field<soOrderNumber>
        {
        }

        #endregion

        #region RequestedOn

        [PXDate]
        [PXUIField(DisplayName = "Requested On", Visible = false)]
        public virtual DateTime? RequestedOn { get; set; }

        public abstract class requestedOn : Data.BQL.BqlDateTime.Field<requestedOn>
        {
        }

        #endregion

        #region TotalCost

        [PXDecimal]
        [PXUIField(DisplayName = "WIP Total", Visible = false)]
        public virtual decimal? TotalCost { get; set; }

        public abstract class totalCost : Data.BQL.BqlDecimal.Field<totalCost>
        {
        }

		#endregion

		#region SchedulingMethod

		[PXString]
		[PXUIField(DisplayName = "Scheduling Method", Visible = true)]
		public virtual string SchedulingMethod { get; set; }

		public abstract class schedulingMethod : Data.BQL.BqlString.Field<schedulingMethod>
		{
		}

		#endregion

		#region ReadOnly
		public abstract class readOnly : Data.BQL.BqlBool.Field<readOnly> { }

		/// <summary>
		/// Flag to control if the row should be enabled
		/// </summary>
        [PXBool]
        [PXUIField(DisplayName = "Read-Only", Visible = false, Visibility = PXUIVisibility.Invisible)]
        public virtual bool? ReadOnly { get; set; }
        #endregion
	}

	[PXCacheName("Work centers resources")]
    public class WorkCenterResource : IBqlTable
    {
        #region Id
        [PXString(IsKey = true)]
        [PXUIField(DisplayName = "ID", Visible = false, Visibility = PXUIVisibility.Invisible)]
        public virtual string Id { get; set; }

        public abstract class id : Data.BQL.BqlString.Field<id>
        {
        }

        #endregion

        #region Code

        [PXString]
        [PXUIField(DisplayName = "Work Center")]
        public virtual string Code { get; set; }

		public abstract class code : Data.BQL.BqlString.Field<code>
		{
		}

        #endregion

        #region Shift

        [PXString]
        [PXUIField(DisplayName = "Shift")]
        public virtual string Shift { get; set; }

        public abstract class shift : Data.BQL.BqlString.Field<shift>
        {
        }

        #endregion

		#region ShiftCode

        [PXString]
        [PXUIField(DisplayName = "Shift Code", Visibility = PXUIVisibility.Invisible)]
        public virtual string ShiftCode { get; set; }

        public abstract class shiftCode : Data.BQL.BqlString.Field<shiftCode>
        {
        }

        #endregion

        #region CrewSize

        [PXDecimal]
        [PXUIField(DisplayName = "Crew Size")]
        public virtual decimal? CrewSize { get; set; }

        public abstract class crewSize : Data.BQL.BqlDecimal.Field<crewSize>
        {
        }

        #endregion

        #region Machines

        [PXDecimal]
        [PXUIField(DisplayName = "Machines")]
        public virtual decimal? Machines { get; set; }

        public abstract class machines : Data.BQL.BqlDecimal.Field<machines>
        {
        }

        #endregion
    }

    [PXCacheName("Machines resources")]
    public class MachineResource : IBqlTable
    {
        #region Id
        [PXString(IsKey = true)]
        [PXUIField(DisplayName = "Machine")]
        public virtual string Id { get; set; }

        public abstract class id : Data.BQL.BqlString.Field<id>
        {
        }

        #endregion
    }

    [PXCacheName("Work center shift calendar resources")]
    public class WorkCenterShiftCalendarResource : IBqlTable
    {
        #region Id
        [PXString(IsKey = true)]
        [PXUIField(DisplayName = "Shift")]
        public virtual string Id { get; set; }

        public abstract class id : Data.BQL.BqlString.Field<id>
        {
        }

        #endregion

        #region Name
        [PXString(IsKey = true)]
        [PXUIField(DisplayName = "Name")]
        public virtual string Name { get; set; }

        public abstract class name : Data.BQL.BqlString.Field<name>
        {
        }

        #endregion

        #region UnspecifiedTimeIsWorking

        [PXBool]
        [PXUIField(DisplayName = "Unspecified Time Is Working", Visible = false)]
        public virtual bool? UnspecifiedTimeIsWorking { get; set; }

        public abstract class unspecifiedTimeIsWorking : Data.BQL.BqlBool.Field<unspecifiedTimeIsWorking>
        {
        }

        #endregion

        public virtual CalendarInterval[] Intervals { get; set; }
    }

	[PXCacheName("Machine calendar resources")]
    public class MachineCalendarResource : IBqlTable
    {
        #region Id
        [PXString(IsKey = true)]
        [PXUIField(DisplayName = "Machine")]
        public virtual string Id { get; set; }

        public abstract class id : Data.BQL.BqlString.Field<id>
        {
        }

        #endregion

        #region Name
        [PXString(IsKey = true)]
        [PXUIField(DisplayName = "Name")]
        public virtual string Name { get; set; }

        public abstract class name : Data.BQL.BqlString.Field<name>
        {
        }

        #endregion

        #region UnspecifiedTimeIsWorking

        [PXBool]
        [PXUIField(DisplayName = "Unspecified Time Is Working", Visible = false)]
        public virtual bool? UnspecifiedTimeIsWorking { get; set; }

        public abstract class unspecifiedTimeIsWorking : Data.BQL.BqlBool.Field<unspecifiedTimeIsWorking>
        {
        }

        #endregion

        public virtual CalendarInterval[] Intervals { get; set; }
    }
}
