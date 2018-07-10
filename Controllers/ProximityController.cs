using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LiveKart.Repository;
using LiveKart.Service;
using Repository.Pattern.UnitOfWork;

namespace LiveKart.Web.Controllers
{
	public class ProximityController : Controller
	{

		private readonly INotificationService _notificationService;
		private readonly IUnitOfWork _unitOfWorkAsync;

		public ProximityController(
				INotificationService notificationMessage,
				IUnitOfWork unitOfWorkAsync)
			: base()
		{
			_notificationService = notificationMessage;
			_unitOfWorkAsync = unitOfWorkAsync;
		}

		[HttpPost]
		public bool Post(FormCollection fc)
		{
			int p0, p1, p2, p3;
			long notificationId;
			List<long> disabledNotifications = new List<long>();
			if (fc["DisabledNotifications"] != null)
			{
				disabledNotifications = Newtonsoft.Json.JsonConvert.DeserializeObject<List<long>>(fc["DisabledNotifications"]);
			}

			Int32.TryParse(fc["Proximity0"], out p0);
			Int32.TryParse(fc["Proximity1"], out p1);
			Int32.TryParse(fc["Proximity2"], out p2);
			Int32.TryParse(fc["Proximity3"], out p3);
			int[] proximities = { p0, p1, p2, p3 };
			Int64.TryParse(fc["NotificationId"], out notificationId);
			var messages = _unitOfWorkAsync.Repository<Entities.Notification>()
			.Query(n => n.NotificationID == notificationId)
			.Include(n => n.NotificationMessages)
			.Select().FirstOrDefault().NotificationMessages;
			foreach (var message in messages)
			{
				message.Disabled = disabledNotifications.Contains(message.NotificationMessageId);

				if (p0 == message.NotificationMessageId)
				{
					message.ProximityRange = 0;
				}
				else if (p1 == message.NotificationMessageId)
				{
					message.ProximityRange = 1;
				}
				else if (p2 == message.NotificationMessageId)
				{
					message.ProximityRange = 2;
				}
				else if (p3 == message.NotificationMessageId)
				{
					message.ProximityRange = 3;
				}
				else
				{
					message.ProximityRange = null;
				}
				//TODO : NOT sure do we need it it here if it is not working move it in notification update service implementations
				_unitOfWorkAsync.Repository<Entities.NotificationMessage>().Update(message);
			}


			var notification = _notificationService.Find(notificationId);
			notification.NotificationType = (byte)(fc["NotificationType"] == "1" ? 1 : 0);
			_notificationService.Update(notification);
			_unitOfWorkAsync.SaveChanges();

			return true;
		}
	}
}