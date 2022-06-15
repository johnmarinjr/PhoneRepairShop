using System;
using PX.Data;
using PX.TM;

namespace PX.Objects.CR.Inquiry
{
	[PXCacheName(Messages.EmailActivity)]
	[PXProjection(typeof(Select<PX.Objects.CR.CRSMEmail,
		Where<PX.Objects.CR.CRSMEmail.classID, Equal<PX.Objects.CR.CRActivityClass.email>,
			And<Where<PX.Objects.CR.CRSMEmail.createdByID, Equal<CurrentValue<AccessInfo.userID>>,
				Or<PX.Objects.CR.CRSMEmail.ownerID, Equal<CurrentValue<AccessInfo.contactID>>,
				Or<PX.Objects.CR.CRSMEmail.ownerID, IsSubordinateOfContact<CurrentValue<AccessInfo.contactID>>,
				Or<PX.Objects.CR.CRSMEmail.workgroupID, IsWorkgroupOfContact<CurrentValue<AccessInfo.contactID>>>>>>>
	>>))]
	public class CRSMEmail : PX.Objects.CR.CRSMEmail
	{
	}
}
