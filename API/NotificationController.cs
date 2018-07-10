using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using LiveKart.Entities;
using LiveKart.Service;
using LiveKart.Web.Models;
using Repository.Pattern.UnitOfWork;
using LiveKart.LogService;


namespace LiveKart.Web.API
{
	public class NotificationController : ApiController
	{
		private INotificationMessageService _notificationMessageService;
		private readonly ISettingsService _settingsService;
		private readonly IBeaconService _beaconService;
		private readonly IUnitOfWork _unitOfWorkAsync;

		public NotificationController(INotificationMessageService notificationMessageService, IUnitOfWork unitOfWorkAsync, 
			ISettingsService settingsService, IBeaconService beaconService)
		{
			_notificationMessageService = notificationMessageService;
			_settingsService = settingsService;
			_beaconService = beaconService;
			_unitOfWorkAsync = unitOfWorkAsync;
		}

		ILogService logging = new FileLogService(typeof(NotificationController));


		/// <summary>
		/// returns list of Notifications
		/// </summary>
		/// <param name="iCompanyId">try with 3 or 4 </param>
		/// <returns></returns>
		[HttpGet, ActionName("All")]
		[ResponseType(typeof(IEnumerable<Notification>))]
		public IHttpActionResult All(long? iCompanyId = null)
		{
			var result = _unitOfWorkAsync.Repository<Entities.Notification>()
				.Query(n => n.CompanyID == iCompanyId)
				.Include(n => n.BeaconSchedules)
				.Include(n => n.Company)
				.Include(n => n.NotificationAlerts)
				.Include(n => n.NotificationMessages)
				.Include(n => n.NotificationProducts)
				.Include(n => n.NotificationSchedules)
				.Include(n => n.Offers)
				.Select().AsQueryable();
			return Ok<IQueryable<Entities.Notification>>(result);
		}

		/// <summary>
		/// Returns list of NotificationMessage including message details
		/// </summary>
		/// <param name="beaconId"></param>
		/// <returns></returns>
		[HttpGet, ActionName("ByBeaconId")]
		[ResponseType(typeof(IEnumerable<NotificationMessage>))]
		public IHttpActionResult ByBeaconId(string beaconId, int rssi)
		{
			Enums.ProximityRange proximity = 0;
			rssi = Math.Abs(rssi);
			var beacon = _beaconService.GetByBeaconNum(beaconId);
			if (beacon.CompanyID == null)
			{
				//if beacon not assigned to company no messages can be scheduled
				return Ok(new List<NotificationMessage>());
			}

			var settings = _settingsService.ByCompany((long)beacon.CompanyID);
			if (rssi > settings.FarMinRSSI && rssi < settings.FarMaxRSSI)
			{
				proximity = Enums.ProximityRange.Far;
			}
			if (rssi > settings.NearMinRSSI && rssi < settings.NearMaxRSSI)
			{
				proximity = Enums.ProximityRange.Near;
			}
			if (rssi > settings.CloseMinRSSI && rssi < settings.CloseMaxRSSI)
			{
				proximity = Enums.ProximityRange.Close;
			}
			
			logging.Info("ByBeaconId: tring to get notifications for beacon ID - " + beaconId);

			var messages = _notificationMessageService.GetByBeaconNum(beaconId).ToList();

			logging.Info(String.Format("ByBeaconId: {0} notifications found", messages.Count()));
			
			var filteredMessages = messages.ToList();
			filteredMessages.RemoveAll(m => m.Disabled || (m.ProximityRange != 0 && m.ProximityRange != (int)proximity)); 

			foreach (var message in filteredMessages)
			{
				if (message.OfferId != null)
				{
					message.OfferMessage = _notificationMessageService.FindOfferMessage((long)message.OfferId);
				}
				if (message.ProductRatingId != null)
				{
					message.RatingMessage = _notificationMessageService.FindRatingMessage((long)message.ProductRatingId);
				}
				if (message.ProductReviewId != null)
				{
					message.ReviewMessage = _notificationMessageService.FindReviewMessage((long) message.ProductReviewId);
				}
				if (message.VideoId != null)
				{
					message.VideoMessage = new VideoMessage
						{
							VideoMessageId = 1,
							MessageHeader = "Video message title",
							MessageDescription = "This is video message",
							MessageShortDescription = "This is video message",
							MessageImage = "http://livekart.altumsoft.com/Content/notificationimages/220420141023166565.jpg",
							VideoUrl = "https://www.youtube.com/watch?v=aiVByOBGBjA"
						};

				}
				if (message.GameId != null)
				{
					message.GameMessage = new GameMessage
						{
							GameMessageId = 1,
							MessageHeader = "Game message title",
							MessageDescription = "This is game message",
							MessageShortDescription = "This is game message",
							MessageImage = "http://livekart.altumsoft.com/Content/notificationimages/220420141023166565.jpg",
							GameUrl = "http://google.com"
						};
				}
			}
			return Ok(filteredMessages);
		}

		[HttpPost, ActionName("AddSurveyAnswers")]
		public IHttpActionResult AddSurveyAnswers(List<SurveyUserAnswer> answers)
		{
			
			var guid = Guid.NewGuid();
			var multiSelectAnswers = new List<SurveyUserAnswer>();
			answers.ForEach(a =>
				{
					a.RecordId = guid;
					if (a.SelectedAnswerIds != null)
					{
						if (a.SelectedAnswerIds.Count() > 1)
						{
							multiSelectAnswers.AddRange(a.SelectedAnswerIds.Select(id => new SurveyUserAnswer
								{
									QuestionId = a.QuestionId,
									SelectedAnswerId = id,
									UserId = a.UserId,
									RecordId = guid
								}));
						}
						if (a.SelectedAnswerIds.Count() == 1)
						{
							a.SelectedAnswerId = a.SelectedAnswerIds[0];
						}
					}
				});
			answers.AddRange(multiSelectAnswers);
			_notificationMessageService.AddUserAnswers(answers);
			_unitOfWorkAsync.SaveChanges();
			return Ok();
		}

		[HttpPost, ActionName("PostProductReview")]
		public IHttpActionResult PostProductReview(UserReview review)
		{
			_unitOfWorkAsync.Repository<UserReview>().Insert(review);
			_unitOfWorkAsync.SaveChanges();
			return Ok();
		}

		[HttpPost, ActionName("PostProductRating")]
		public IHttpActionResult PostProductRating(List<UserRatingItem> ratingItemsitems)
		{
			var guid = Guid.NewGuid();
			ratingItemsitems.ForEach(x=>x.UserId = guid);
			_notificationMessageService.InsertUserRatingItems(ratingItemsitems);
			_unitOfWorkAsync.SaveChanges();
			return Ok();
		}

		/// <summary>
		/// return Notification by it Id
		/// </summary>
		/// <param name="Id">from 10 to 20</param>
		/// <returns></returns>
		[HttpGet, ActionName("Get")]
		[ResponseType(typeof(Notification))]
		public IHttpActionResult Get(long Id)
		{
			var notification = _unitOfWorkAsync.Repository<Entities.Notification>().Find(Id);
			return Ok(notification);
		}

		[HttpGet, ActionName("ByPage")]
		[ResponseType(typeof(IQueryable<Notification>))]
		public IHttpActionResult ByPage(long? iCompanyId, int startIndex, int count, string status, string searchtxt)
		{

			var notifications = _unitOfWorkAsync.Repository<Entities.Notification>()
									.Query(n => n.CompanyID == iCompanyId && (status == "active" ? (n.NotificationSchedules.Count == 0 || n.NotificationSchedules.Any(ns => ns.EndDate > DateTime.UtcNow)) : true))
									.Include(n => n.BeaconSchedules)
									.Include(n => n.Company)
									.Include(n => n.NotificationAlerts)
									.Include(n => n.NotificationMessages)
									.Include(n => n.NotificationProducts)
									.Include(n => n.NotificationSchedules)
									.Include(n => n.Offers)
									.Select().OrderByDescending(n => n.CreatedDate).Skip(count * startIndex).Take(count).AsQueryable();
			return Ok<IQueryable<Notification>>(notifications);

		}

		[Queryable]
		[HttpGet, ActionName("Messages")]
		[ResponseType(typeof(IQueryable<MessageBrief>))]
		public IHttpActionResult Messages(long companyId)
		{
			var notifications = _unitOfWorkAsync.Repository<Entities.Notification>()
				.Query(n => n.CompanyID == companyId)
				.Include(n => n.NotificationMessages)
				.Select().ToList();
			var messages = new List<Entities.NotificationMessage>();
			notifications.ForEach(n => messages.AddRange(n.NotificationMessages));
			var result = messages.Select(message => new MessageBrief
							{
								MessageId = message.NotificationMessageId,
								NotificationHeader = message.NotificationTitle,
								NotificationShortDescription = message.NotificationDescription,
								NotificationImage = message.MessageThumbImage
							}).AsQueryable();

			return Ok<IQueryable<MessageBrief>>(result);
		}

		[HttpDelete]
		public bool Delete(long? id = null)
		{
			_unitOfWorkAsync.Repository<Entities.Notification>().Delete(id);
			return _unitOfWorkAsync.SaveChanges() > 0;
		}

		[HttpPost]
		public long Post(Entities.Notification n)
		{
			_unitOfWorkAsync.Repository<Entities.Notification>().Insert(n);
			_unitOfWorkAsync.SaveChanges();
			return n.NotificationID;
		}

		[HttpPut]
		public bool Put(Entities.Notification n)
		{
			_unitOfWorkAsync.Repository<Entities.Notification>().Update(n);
			return _unitOfWorkAsync.SaveChanges() > 0;
		}

		[HttpGet, ActionName("Publish")]
		public bool Publish(long notificationId, long companyId)
		{
			var notification = _unitOfWorkAsync.Repository<Entities.Notification>().Find(notificationId);
			_unitOfWorkAsync.Repository<Entities.Notification>().Update(notification);
			return _unitOfWorkAsync.SaveChanges() > 0;
		}

		[HttpGet, ActionName("GetStates")]
		public IEnumerable<dynamic> GetStates()
		{
			var states = new List<object>();
			foreach (var state in (Enums.State[])Enum.GetValues(typeof(Enums.State)))
			{
				states.Add(new
					{
						stateName = state.ToString(),
						stateId = (int) state
					});
			}
			return states;
		}
	}
}