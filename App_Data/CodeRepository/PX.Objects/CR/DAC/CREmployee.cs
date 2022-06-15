using PX.Data;
using System;
using PX.Objects.AP;
using PX.Objects.EP;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.CR.Standalone
{
	/// <summary>
	/// Represents an Employee of the organization utilizing Acumatica ERP.
	/// </summary>
	/// <remarks>
	/// An employee is a person working for the organization that utilizes Acumatica ERP.
	/// The records of this type are created and edited on the <i>Employees (EP203000)</i> form,
	/// which correspond to the <see cref="EmployeeMaint"/> graph.
	/// </remarks>
	[System.SerializableAttribute()]
    [PXTable(typeof(PX.Objects.CR.BAccount.bAccountID))]
    [PXCacheName(Messages.Employee)]
    [CRCacheIndependentPrimaryGraph(
                typeof(EmployeeMaint),
                typeof(Select<EP.EPEmployee,
                    Where<EP.EPEmployee.bAccountID, Equal<Current<EPEmployee.bAccountID>>>>))]
	[PXHidden]
	public partial class EPEmployee : BAccount
    {
		#region Keys
		/// <summary>
		/// Primary Key
		/// </summary>
		public new class PK : PrimaryKeyOf<EPEmployee>.By<bAccountID>
        {
            public static EPEmployee Find(PXGraph graph, int? bAccountID) => FindBy(graph, bAccountID);
		}
		/// <summary>
		/// Unique Key
		/// </summary>
		public new class UK : PrimaryKeyOf<EPEmployee>.By<acctCD>
        {
            public static EPEmployee Find(PXGraph graph, string acctCD) => FindBy(graph, acctCD);
		}
		/// <summary>
		/// Foreign Keys
		/// </summary>
		public new static class FK
		{
			/// <summary>
			/// Customer Class
			/// </summary>
			public class EmployeeClass : CR.CRCustomerClass.PK.ForeignKeyOf<EPEmployee>.By<classID> { }
			/// <summary>
			/// Branch or location
			/// </summary>
			public class ParentBusinessAccount : CR.BAccount.PK.ForeignKeyOf<EPEmployee>.By<parentBAccountID> { }

			/// <summary>
			/// Address
			/// </summary>
			public class Address : CR.Address.PK.ForeignKeyOf<EPEmployee>.By<defAddressID> { }
			/// <summary>
			/// Contact
			/// </summary>
			public class ContactInfo : CR.Contact.PK.ForeignKeyOf<EPEmployee>.By<defContactID> { }
			/// <summary>
			/// Default Location
			/// </summary>
			public class DefaultLocation : CR.Location.PK.ForeignKeyOf<EPEmployee>.By<bAccountID, defLocationID> { }
			/// <summary>
			/// Primary Contact
			/// </summary>
			public class PrimaryContact : CR.Contact.PK.ForeignKeyOf<EPEmployee>.By<primaryContactID> { }
			/// <summary>
			/// Department
			/// </summary>
			public class Department : EP.EPDepartment.PK.ForeignKeyOf<EPEmployee>.By<departmentID> { }
			/// <summary>
			/// The employee's supervisor to whom the reports are sent
			/// </summary>
			public class ReportsTo : EP.EPEmployee.PK.ForeignKeyOf<EPEmployee>.By<supervisorID> { }

			/// <summary>
			/// Tax Zone ID (obsolete)
			/// </summary>
			public class TaxZone : TX.TaxZone.PK.ForeignKeyOf<EPEmployee>.By<taxZoneID> { }

			/// <summary>
			/// Vendor's owner
			/// </summary>
			public class Owner : EP.EPEmployee.PK.ForeignKeyOf<EPEmployee>.By<ownerID> { }
			/// <summary>
			/// Workgroup
			/// </summary>
			public class Workgroup : TM.EPCompanyTree.PK.ForeignKeyOf<EPEmployee>.By<workgroupID> { }

			/// <summary>
			/// Login information
			/// </summary>
			public class User : PX.SM.Users.PK.ForeignKeyOf<EPEmployee>.By<userID> { }
        }
        #endregion

        #region BAccountID
        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
        #endregion
        #region AcctCD
        public new abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }
		/// <summary>
		/// The human-readable identifier of the employee that is
		/// specified by the user or defined by the EMPLOYEE auto-numbering sequence during the
		/// creation of the employee. This field is a natural key, as opposed
		/// to the surrogate key <see cref="BAccountID"/>.
		/// </summary>
		[EP.EmployeeRaw]
        [PXDBString(30, IsUnicode = true, IsKey = true, InputMask = "")]
        [PXDefault()]
        [PXUIField(DisplayName = "Employee ID", Visibility = PXUIVisibility.SelectorVisible)]
        [PX.Data.EP.PXFieldDescription]
        public override String AcctCD
        {
            get
            {
                return this._AcctCD;
            }
            set
            {
                this._AcctCD = value;
            }
        }
        #endregion
        #region DefContactID
        public new abstract class defContactID : PX.Data.BQL.BqlInt.Field<defContactID> { }
		/// <summary>
		/// The identifier of the <see cref="CR.Contact"/> object linked with the current employee as Contact Info.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CR.Contact.ContactID"/> field.
		/// </value>
		[PXDBInt()]
        [PXDBChildIdentity(typeof(Contact.contactID))]
        [PXUIField(DisplayName = "Default Contact")]
        [PXSelector(typeof(Search<Contact.contactID, Where<Contact.bAccountID, Equal<Current<EPEmployee.parentBAccountID>>>>))]
        public override Int32? DefContactID
        {
            get
            {
                return this._DefContactID;
            }
            set
            {
                this._DefContactID = value;
            }
        }
        #endregion
        #region DefAddressID
        public new abstract class defAddressID : PX.Data.BQL.BqlInt.Field<defAddressID> { }
		/// <summary>
		/// The identifier of the <see cref="CR.Address"/> object linked with the current employee as Address Info.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CR.Address.AddressID"/> field.
		/// </value>
		[PXDBInt()]
        [PXDBChildIdentity(typeof(Address.addressID))]
        [PXUIField(DisplayName = "Default Address")]
        [PXSelector(typeof(Search<Address.addressID, Where<Address.bAccountID, Equal<Current<EPEmployee.parentBAccountID>>>>))]
        public override Int32? DefAddressID
        {
            get
            {
                return this._DefAddressID;
            }
            set
            {
                this._DefAddressID = value;
            }
        }

        #endregion
        #region AcctName
        public new abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }
		/// <summary>
		/// The employee name, which is usually a concatenation of the
		/// <see cref="Contact.FirstName">first</see> and <see cref="Contact.LastName">last name</see>
		/// of the appropriate contact.
		/// </summary>
		[PXDBString(60, IsUnicode = true)]
        [PXDefault()]
        [PXUIField(DisplayName = "Employee Name", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
        public override string AcctName
        {
            get
            {
                return base.AcctName;
            }
            set
            {
                base.AcctName = value;
            }
        }
		#endregion
		#region AcctReferenceNbr
		/// <summary>
		/// The external reference number of the employee.</summary>
		/// <remarks>It can be an additional number of the employee used in external integration.
		/// </remarks>
		[PXDBString(50, IsUnicode = true)]
        [PXUIField(DisplayName = "Employee Ref. No.", Visibility = PXUIVisibility.Visible)]
        public override string AcctReferenceNbr
        {
            get
            {
                return base.AcctReferenceNbr;
            }
            set
            {
                base.AcctReferenceNbr = value;
            }
        }
        #endregion

        #region DepartmentID
        public abstract class departmentID : PX.Data.BQL.BqlString.Field<departmentID> { }
        protected String _DepartmentID;
		/// <summary>
		/// Identifier of the <see cref="EPDepartment">employee department</see> 
		/// that the employee belongs to.
		/// </summary>
		[PXDBString(10, IsUnicode = true)]
        [PXDefault()]
        [PXSelector(typeof(EPDepartment.departmentID), DescriptionField = typeof(EPDepartment.description))]
        [PXUIField(DisplayName = "Department", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual String DepartmentID
        {
            get
            {
                return this._DepartmentID;
            }
            set
            {
                this._DepartmentID = value;
            }
        }
        #endregion
        #region DefLocationID
        public new abstract class defLocationID : PX.Data.BQL.BqlInt.Field<defLocationID> { }
		/// <summary>
		/// The identifier of the <see cref="Location"/> object linked with the employee and marked as default.
		/// The fields from the linked location are shown on the <b>Financial Settings</b> tab.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="Location.LocationID"/> field.
		/// </value>
		/// <remarks>
		/// Also, the <see cref="Location.BAccountID" /> value must also be equal to
		/// the <see cref="BAccount.BAccountID" /> value of the current employee.
		/// </remarks>
		[PXDefault()]
        [PXDBInt()]
        [PXUIField(DisplayName = "Default Location", Visibility = PXUIVisibility.SelectorVisible)]
        [DefLocationID(typeof(Search<Location.locationID, Where<Location.bAccountID, Equal<Current<EPEmployee.bAccountID>>>>), SubstituteKey = typeof(Location.locationCD), DescriptionField = typeof(Location.descr))]
        [PXDBChildIdentity(typeof(Location.locationID))]
        public override int? DefLocationID
        {
            get
            {
                return base.DefLocationID;
            }
            set
            {
                base.DefLocationID = value;
            }
        }
        #endregion
        #region SupervisorID
        public abstract class supervisorID : PX.Data.BQL.BqlInt.Field<supervisorID> { }
        protected Int32? _SupervisorID;
		/// <summary>
		/// The identifier of the <see cref="EPEmployee"/> that the current employee sends reports to.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="BAccount.BAccountID"/> field.
		/// </value>
		[PXDBInt()]
        [PXEPEmployeeSelector]
        [PXUIField(DisplayName = "Reports to", Visibility = PXUIVisibility.Visible)]
        public virtual Int32? SupervisorID
        {
            get
            {
                return this._SupervisorID;
            }
            set
            {
                this._SupervisorID = value;
            }
        }
        #endregion

        #region VStatus
        public new abstract class vStatus : PX.Data.BQL.BqlString.Field<vStatus> { }

		/// <summary>
		/// The status of the employee.
		/// </summary>
		/// <value>
		/// The possible values of the field are listed in
		/// the <see cref="VendorStatus"/> class. These values can be changed and extended by using the workflow engine.
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Status")]
		[PXDefault(VendorStatus.Active)]
		[VendorStatus.List]
		public override String VStatus { get; set; }
        #endregion

        #region ClassID
        public new abstract class classID : PX.Data.BQL.BqlString.Field<classID> { }
        #endregion

        #region UserID
        public abstract class userID : PX.Data.BQL.BqlGuid.Field<userID> { }
		/// <summary>
		/// The identifier of the <see cref="PX.SM.Users">Users</see> to be used for the employee to sign into the system.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="PX.SM.Users.PKID">Users.PKID</see> field.
		/// </value>
		[PXDBGuid]
        [PXUser]
        [PXUIField(DisplayName = "Employee Login", Visibility = PXUIVisibility.Visible)]
        public virtual Guid? UserID { get; set; }
        #endregion
       
        #region NoteID
        public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		/// <inheritdoc/>
        [PXSearchable(SM.SearchCategory.OS, EP.Messages.SearchableTitleEmployee, new Type[] { typeof(EPEmployee.acctCD), typeof(EPEmployee.acctName) },
           new Type[] { typeof(EPEmployee.defContactID), typeof(Contact.eMail) },
           NumberFields = new Type[] { typeof(EPEmployee.acctCD) },
             Line1Format = "{1}{2}", Line1Fields = new Type[] { typeof(EPEmployee.defContactID), typeof(Contact.eMail), typeof(Contact.phone1) },
             Line2Format = "{1}", Line2Fields = new Type[] { typeof(EPEmployee.departmentID), typeof(EPDepartment.description) }
         )]
        [PXUniqueNote(
            DescriptionField = typeof(EPEmployee.acctCD),
            Selector = typeof(EPEmployee.acctCD))]
        public override Guid? NoteID
        {
            get
            {
                return this._NoteID;
            }
            set
            {
                this._NoteID = value;
            }
        }
        #endregion

		#region ParentBAccountID
		public new abstract class parentBAccountID : PX.Data.BQL.BqlInt.Field<parentBAccountID> { }
		#endregion
	}
}

namespace PX.Objects.CR
{
	/// <inheritdoc/>
    [System.SerializableAttribute()]
	[PXBreakInheritance]
    [PXCacheName(Messages.Employee)]
    [CRCacheIndependentPrimaryGraph(
            typeof(EmployeeMaint),
            typeof(Select<EP.EPEmployee,
                Where<EP.EPEmployee.bAccountID, Equal<Current<CREmployee.bAccountID>>>>))]
    public partial class CREmployee : PX.Objects.CR.Standalone.EPEmployee
    {
		#region BAccountID
        public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
        #endregion
        #region AcctCD
        public new abstract class acctCD : PX.Data.BQL.BqlString.Field<acctCD> { }        
        #endregion
        #region DefContactID
        public new abstract class defContactID : PX.Data.BQL.BqlInt.Field<defContactID> { }        
        #endregion
        #region DefAddressID
        public new abstract class defAddressID : PX.Data.BQL.BqlInt.Field<defAddressID> { }        
        #endregion
        #region AcctName
        public new abstract class acctName : PX.Data.BQL.BqlString.Field<acctName> { }
		/// <inheritdoc/>
		[PXDBString(60, IsUnicode = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Employee Name", Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public override string AcctName
		{
			get
			{
				return base.AcctName;
			}
			set
			{
				base.AcctName = value;
			}
		}
		#endregion
		#region AcctReferenceNbr
		public new abstract class acctReferenceNbr : PX.Data.BQL.BqlString.Field<acctReferenceNbr> { }        
        #endregion                
		  #region ParentBAccountID
		  public new abstract class parentBAccountID : PX.Data.BQL.BqlInt.Field<parentBAccountID> { }
		#endregion

		#region DepartmentID
		public new abstract class departmentID : PX.Data.BQL.BqlString.Field<departmentID> { }
		#endregion
        #region DefLocationID
		public new abstract class defLocationID : PX.Data.BQL.BqlInt.Field<defLocationID> { }       
        #endregion
        #region SupervisorID
        public new abstract class supervisorID : PX.Data.BQL.BqlInt.Field<supervisorID> { }        
        #endregion     
		#region VStatus
		public new abstract class vStatus : PX.Data.BQL.BqlString.Field<vStatus> { }

		/// <inheritdoc/>
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Status")]
		[PXDefault(VendorStatus.Active)]
		[VendorStatus.List]
		public override String VStatus { get; set; }
		#endregion

        #region ClassID
        public new abstract class classID : PX.Data.BQL.BqlString.Field<classID> { }
        #endregion

        #region UserID
        public new abstract class userID : PX.Data.BQL.BqlGuid.Field<userID> { }
        #endregion      
             
        #region NoteID
        public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		/// <inheritdoc/>
		[PXUniqueNote(
		   DescriptionField = typeof(EPEmployee.acctCD),
		   Selector = typeof(EPEmployee.acctCD))]
		public override Guid? NoteID
		{
			get
			{
				return this._NoteID;
			}
			set
			{
				this._NoteID = value;
			}
		}
		#endregion
	}
}
