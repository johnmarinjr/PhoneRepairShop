using PX.Data;
using System;

namespace PX.Objects.CR
{
	[Obsolete]
	public class CRAnnouncementViewer : PXGraph<CRAnnouncementViewer>
	{
		#region Select
		public PXSelect<CRAnnouncement> Announcement;
		#endregion

		#region Action
		public PXCancel<CRAnnouncement> Cancel;
		#endregion
	}
}
