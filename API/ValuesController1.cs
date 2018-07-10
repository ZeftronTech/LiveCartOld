using LiveKart.Service;
using LiveKart.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LiveKart.Web.API
{
	public class TestController : ApiController
	{
		private INotificationService _notificationService;
		private INotificationMessageService _notificationMessageService;

		public TestController(INotificationService notificationService, INotificationMessageService notificationMessageService)
		{
			_notificationService = notificationService;
			_notificationMessageService = notificationMessageService;

		}
		// GET api/<controller>/5
		public IEnumerable<NotificationMessage> Get(string id)
		{
			var notifications = (from n in _notificationService.GetByBeaconNum(id)
								select n.NotificationID).ToList();

			var messages = _notificationMessageService.NotificationMessages().Where(x => notifications.Contains(x.NotificationId)).AsEnumerable();
			return messages;

		}

	}
}