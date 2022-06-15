using PX.Data;
using System;
using System.Collections.Generic;

namespace PX.Objects.FS
{
    public class FSLogTypeAction
    {
        public class ListAttribute : PXStringListAttribute
        {
            public ListAttribute() : base(
                new[]
                {
                    Pair(FSLogActionFilter.type.Values.Service, TX.Type_Log.SERVICE),
                    Pair(FSLogActionFilter.type.Values.Travel, TX.Type_Log.TRAVEL),
                    Pair(FSLogActionFilter.type.Values.Staff, TX.Type_Log.STAFF),
                    Pair(FSLogActionFilter.type.Values.ServBasedAssignment, TX.Type_Log.SERV_BASED_ASSIGMENT),
                })
            { }
        }

        public class STListAttribute : PXStringListAttribute
        {
            public STListAttribute() : base(
                new[]
                {
                    Pair(FSLogActionFilter.type.Values.Service, TX.Type_Log.SERVICE),
                    Pair(FSLogActionFilter.type.Values.Travel, TX.Type_Log.TRAVEL),
                })
            { }
        }

        public class Service : Data.BQL.BqlString.Constant<Service>
        {
            public Service() : base(FSLogActionFilter.type.Values.Service) {; }
        }

        public class Travel : Data.BQL.BqlString.Constant<Travel>
        {
            public Travel() : base(FSLogActionFilter.type.Values.Travel) {; }
        }

        public class StaffAssignment : Data.BQL.BqlString.Constant<StaffAssignment>
        {
            public StaffAssignment() : base(FSLogActionFilter.type.Values.Staff) {; }
        }

        public class SrvBasedOnAssignment : Data.BQL.BqlString.Constant<SrvBasedOnAssignment>
        {
            public SrvBasedOnAssignment() : base(FSLogActionFilter.type.Values.ServBasedAssignment) {; }
        }
    }
}
