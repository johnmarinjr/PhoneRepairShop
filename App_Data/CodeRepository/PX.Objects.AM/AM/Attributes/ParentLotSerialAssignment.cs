using PX.Data;

namespace PX.Objects.AM.Attributes
{
    /// <summary>
    /// Parent Lot/Serial Assign Settings
    /// </summary>
    public class ParentLotSerialAssignment
    {
        /// <summary>
        /// Never
        /// </summary>
        public const string Never = "N";
        /// <summary>
        /// On Issue
        /// </summary>
        public const string OnIssue = "I";
        /// <summary>
        /// On Completion
        /// </summary>
        public const string OnCompletion = "C";

        /// <summary>
        /// Descriptions/labels for identifiers
        /// </summary>
        public class Desc
        {
            public static string Never => Messages.GetLocal(Messages.Never);
            public static string OnIssue => Messages.GetLocal(Messages.OnIssue);
            public static string OnCompletion => Messages.GetLocal(Messages.OnCompletion);
        }

        public static string GetDescription(string id)
        {
            if (id == null)
            {
                return string.Empty;
            }

            try
            {
                var x = new ListAttribute();
                return x.ValueLabelDic[id];
            }
            catch
            {
                return string.Empty;
            }
        }

        public class never : PX.Data.BQL.BqlString.Constant<never>
        {
            public never() : base(Never) { }
        }

        public class onIssue : PX.Data.BQL.BqlString.Constant<onIssue>
        {
            public onIssue() : base(OnIssue) { }
        }

        public class onCompletion : PX.Data.BQL.BqlString.Constant<onCompletion>
        {
            public onCompletion() : base(OnCompletion) { }
        }

        public class ListAttribute : PXStringListAttribute
        {
            public ListAttribute()
                : base(
                new string[] { Never, OnIssue, OnCompletion },
                new string[] { Messages.Never, Messages.OnIssue, Messages.OnCompletion })
            { }
        }
    }
}