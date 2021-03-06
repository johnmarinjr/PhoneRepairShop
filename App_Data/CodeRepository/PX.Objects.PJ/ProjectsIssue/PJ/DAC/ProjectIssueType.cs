using PX.Objects.PJ.ProjectManagement.Descriptor;
using PX.Data;
using PX.Data.BQL;
using PX.Objects.CN.Common.Descriptor.Attributes;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.PJ.ProjectsIssue.PJ.DAC
{
    [PXCacheName("Project Issue Type")]
    public class ProjectIssueType : IBqlTable
    {

        #region Keys

        /// <summary>
        /// Primary Key
        /// </summary>
        public class PK : PrimaryKeyOf<ProjectIssueType>.By<projectIssueTypeId>
        {
            public static ProjectIssueType Find(PXGraph graph, int? projectIssueTypeId) => FindBy(graph, projectIssueTypeId);
        }

        #endregion

        [PXDBIdentity(IsKey = true)]
        public virtual int? ProjectIssueTypeId
        {
            get;
            set;
        }

        [PXDBString(255, IsUnicode = true)]
        [PXDefault]
        [Unique(ErrorMessage = ProjectManagementMessages.ProjectIssueTypeUniqueConstraint)]
        [PXUIField(DisplayName = "Project Issue Type", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string TypeName
        {
            get;
            set;
        }

        [PXDBString(255, IsUnicode = true)]
        [PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual string Description
        {
            get;
            set;
        }

        public abstract class projectIssueTypeId : BqlInt.Field<projectIssueTypeId>
        {
        }

        public abstract class typeName : BqlString.Field<typeName>
        {
        }

        public abstract class description : BqlString.Field<description>
        {
        }
    }
}