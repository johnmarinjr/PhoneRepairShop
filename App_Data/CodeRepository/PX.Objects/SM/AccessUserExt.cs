using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.EP;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.EP;
using PX.Objects.CR;
using PX.SM;

namespace PX.Objects.SM
{
	public class AccessExt : PXGraphExtension<Access>
	{
		[InjectDependency]
		private IMailSendProvider MailSendProvider { get; set; }

		[PXOverride]
		public void SendUserNotification(int? accountId, Notification notification, Action<int?, Notification> del)
		{
			var gen = TemplateNotificationGenerator.Create(Base, Base.UserList.Current, notification);
			gen.MailAccountId = accountId;            
			gen.To = Base.UserList.Current.Email;
			gen.LinkToEntity = true;
            gen.Body = gen.Body.Replace("((UserList.Password))", Base.UserList.Current.Password);
			var activities = gen.Send();
			foreach (SMEmail email in gen.CastToSMEmail(activities))
			{
				MailSendProvider.SendMessage(email);
			}
		}
	}
}
