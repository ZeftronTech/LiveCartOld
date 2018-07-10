using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LiveKart.Entities;
using LiveKart.Shared;
using LiveKart.LogService;
using LiveKart.Business.ImageUtility;
using System.Drawing;
using LiveKart.Business;
using System.IO;
using System.Net;
using LiveKart.Repository;
using Repository.Pattern.UnitOfWork;
using Repository.Pattern.Infrastructure;

namespace LiveKart.Web.Controllers
{
	/// <summary>
	/// Class NotificationController
	/// </summary>
	public class NotificationController : BaseController
	{
		//private readonly INotificationMessageService _notificationMessageService;
		private readonly IUnitOfWork _unitOfWorkAsync;

		public NotificationController(
			//INotificationMessageService notificationMessage,
				IUnitOfWork unitOfWorkAsync)
			: base()
		{
			//_notificationMessageService = notificationMessage;
			_unitOfWorkAsync = unitOfWorkAsync;
		}

		/// <summary>
		/// The logging
		/// </summary>
		ILogService logging = new FileLogService(typeof(NotificationController));

		/// <summary>
		/// The notification image path
		/// </summary>
		private const string NotificationImagePath = "~/Content/notificationimages/";

		/// <summary>
		/// The offer image path
		/// </summary>
		private const string OfferImagePath = "~/Content/offerimages/";
		/// <summary>
		/// The temp path
		/// </summary>
		private const string TempPath = "~/Content/tempdata/";

		/// <summary>
		/// List all the campaigns
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Index(string status = "active")
		{
			ViewData["pagename"] = "notification";

			ViewBag.Status = status;
			return View(new List<Entities.Notification>());
		}

		/// <summary>
		/// Gets the campaigns by defined page size.
		/// </summary>
		/// <param name="startIndex">The start index.</param>
		/// <param name="count">The count.</param>
		/// <param name="status">The status.</param>
		/// <returns>PartialViewResult.</returns>
		public PartialViewResult GetNotificationsByPage(int startIndex = 0, int count = 2, string status = "active", string searchtxt = null)
		{
			ViewData["pagename"] = "notification";
			var notifications = _unitOfWorkAsync.Repository<Entities.Notification>()
				.Query(n => n.CompanyID == CurrentCompany.CompanyId
					&& (status == "active"
						? (n.NotificationSchedules.Count == 0 || n.NotificationSchedules.Any(ns => ns.EndDate >= DateTime.UtcNow))
						: (n.NotificationSchedules.Count > 0 && n.NotificationSchedules.Any(ns => ns.EndDate < DateTime.UtcNow))))
				.Include(n => n.BeaconSchedules)
				.Include(n => n.Company)
				.Include(n => n.NotificationAlerts)
				.Include(n => n.NotificationMessages)
				.Include(n => n.NotificationProducts)
				.Include(n => n.NotificationSchedules)
				.Include(n => n.Offers)
				.Select().OrderByDescending(n => n.CreatedDate).Skip(count * startIndex).Take(count).ToList();
			var notificationCount = _unitOfWorkAsync.Repository<Entities.Notification>()
				.Query(n => n.CompanyID == CurrentCompany.CompanyId
					&& (status == "active"
						? (n.NotificationSchedules.Count == 0 || n.NotificationSchedules.Any(ns => ns.EndDate >= DateTime.UtcNow))
						: (n.NotificationSchedules.Count > 0 && n.NotificationSchedules.Any(ns => ns.EndDate < DateTime.UtcNow))))
				.Select().Count();

			foreach (var n in notifications)
			{
				n.NotificationCount = notificationCount;
				if (n.Schedule != null)
				{
					n.Schedule.StartDate = Convert.ToDateTime(n.Schedule.StartDate);
					n.Schedule.EndDate = Convert.ToDateTime(n.Schedule.EndDate);
				}
			}
			return PartialView("_Notifications", notifications);
		}

		/// <summary>
		/// Create alert view
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Create()
		{
			ViewData["pagename"] = "notification";

			ViewBag.NBeacons = new List<Entities.NotificationBeaconMap>();
			var deployed = _unitOfWorkAsync.Repository<Entities.BeaconDeployment>()
				.Query(b => b.CompanyID == CurrentCompany.CompanyId && b.Active)
				.Include(b => b.Beacon)
				.Include(b => b.Asset)
				.Include(b => b.Asset.AssetCategory)
				.Select().ToList();
			ViewBag.Beacons = deployed;

			var n = new Entities.Notification();
			n.NotificationMessages = new List<NotificationMessage>();
			//n.Schedule = new NotificationSchedule();
			return View(n);
		}

		/// <summary>
		/// Edits alert view
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult Edit(long id, string activeTab)
		{
			ViewData["pagename"] = "activenotifications";

			var notificationBeaconMaps = _unitOfWorkAsync.Repository<Entities.NotificationBeaconMap>()
				.Query(n => n.CompanyID == CurrentCompany.CompanyId && n.NotificationID == id)
				.Select().ToList();
			ViewBag.NBeacons = notificationBeaconMaps;

			var deployed = _unitOfWorkAsync.Repository<Entities.BeaconDeployment>()
				.Query(b => b.CompanyID == CurrentCompany.CompanyId && b.Active)
				.Include(b => b.Beacon)
				.Include(b => b.Asset)
				.Include(b => b.Asset.AssetCategory)
				.Select().ToList();
			ViewBag.Beacons = deployed;

			var notification = _unitOfWorkAsync.Repository<Entities.Notification>()
				.Query(x => x.NotificationID == id)
				.Include(x => x.NotificationMessages)
				.Include(x => x.NotificationMessages.Select(n => n.StandardMessage))
				.Include(x => x.NotificationMessages.Select(n => n.SurveyMessage))
				.Include(x => x.BeaconSchedules)
				.Include(x => x.NotificationSchedules)
				.Select().SingleOrDefault();
			if (notification.Schedule != null)
			{
				notification.Schedule.StartDate = Convert.ToDateTime(notification.Schedule.StartDate);
				notification.Schedule.EndDate = Convert.ToDateTime(notification.Schedule.EndDate);
			}
			else
			{
				notification.Schedule = new NotificationSchedule
					{
						NotificationID = notification.NotificationID,
						CompanyID = notification.CompanyID
					};
			}
			ViewBag.activeTab = activeTab;
			return View(notification);
		}

		/// <summary>
		/// Deletes the specified company.
		/// </summary>
		/// <param name="id">company id.</param>
		/// <returns>ActionResult.</returns>
		[HttpGet]
		public ActionResult Delete(long id)
		{
			if (ModelState.IsValid)
			{
				try
				{
					var notificationMessages = _unitOfWorkAsync.Repository<Entities.NotificationMessage>()
						.Query(n => n.NotificationId == id)
						.Select().ToList();
					foreach (var notificationMessage in notificationMessages)
					{
						if (notificationMessage.StandardMessageId != null && notificationMessage.StandardMessageId != 0)
						{
							_unitOfWorkAsync.Repository<Entities.StandardMessage>().Delete(notificationMessage.StandardMessageId);
						}
						if (notificationMessage.OfferId != null && notificationMessage.OfferId != 0)
						{
							_unitOfWorkAsync.Repository<Entities.OfferMessage>().Delete(notificationMessage.OfferId);
						}
						if (notificationMessage.SurveyId != null && notificationMessage.SurveyId != 0)
						{
							var surveyQuestions = _unitOfWorkAsync.Repository<Entities.SurveyQuestion>()
								.Query(s => s.SurveyId == notificationMessage.SurveyId)
								.Select().ToList();
							foreach (var surveyQuestion in surveyQuestions)
							{
								var surveyQuestionAnswers = _unitOfWorkAsync.Repository<Entities.SurveyQuestionAnswer>()
									.Query(q => q.QuestionId == surveyQuestion.QuestionId)
									.Select().ToList();
								foreach (var surveyQuestionAnswer in surveyQuestionAnswers)
								{
									_unitOfWorkAsync.Repository<Entities.SurveyQuestionAnswer>().Delete(surveyQuestionAnswer.AnswerId);
								}

								var surveyUserAnswers = _unitOfWorkAsync.Repository<Entities.SurveyUserAnswer>()
									.Query(a => a.QuestionId == surveyQuestion.QuestionId)
									.Select().ToList();
								foreach (var surveyUserAnswer in surveyUserAnswers)
								{
									_unitOfWorkAsync.Repository<Entities.SurveyUserAnswer>().Delete(surveyUserAnswer.AnswerId);
								}

								_unitOfWorkAsync.Repository<Entities.SurveyQuestion>().Delete(surveyQuestion.QuestionId);
							}

							_unitOfWorkAsync.Repository<Entities.SurveyMessage>().Delete(notificationMessage.SurveyId);
						}
						if (notificationMessage.ProductReviewId != null && notificationMessage.ProductReviewId != 0)
						{
							var userReviews = _unitOfWorkAsync.Repository<Entities.UserReview>()
								.Query(r => r.ReviewMessageId == notificationMessage.ProductReviewId)
								.Select().ToList();
							foreach (var userReview in userReviews)
							{
								_unitOfWorkAsync.Repository<Entities.UserReview>().Delete(userReview.UserReviewId);
							}
							_unitOfWorkAsync.Repository<Entities.ReviewMessage>().Delete(notificationMessage.ProductReviewId);
						}
						if (notificationMessage.ProductRatingId != null && notificationMessage.ProductRatingId != 0)
						{
							var ratingItems = _unitOfWorkAsync.Repository<Entities.RatingItem>()
								.Query(r => r.RatingMessageId == notificationMessage.ProductRatingId)
								.Select().ToList();
							foreach (var ratingItem in ratingItems)
							{
								var userRatingItems = _unitOfWorkAsync.Repository<Entities.UserRatingItem>()
									.Query(r => r.RatingItemId == ratingItem.RatingItemId)
									.Select().ToList();
								foreach (var userRatingItem in userRatingItems)
								{
									_unitOfWorkAsync.Repository<Entities.UserRatingItem>().Delete(userRatingItem.UserRatingItemId);
								}
								_unitOfWorkAsync.Repository<Entities.RatingItem>().Delete(ratingItem.RatingItemId);
							}
							
							_unitOfWorkAsync.Repository<Entities.RatingMessage>().Delete(notificationMessage.ProductRatingId);
						}
						_unitOfWorkAsync.Repository<Entities.NotificationMessage>().Delete(notificationMessage.NotificationMessageId);
					}

					var notificationBeaconMaps = _unitOfWorkAsync.Repository<Entities.NotificationBeaconMap>()
						.Query(n => n.NotificationID == id)
						.Select().ToList();
					foreach (var notificationBeaconMap in notificationBeaconMaps)
					{
						_unitOfWorkAsync.Repository<Entities.NotificationBeaconMap>().Delete(notificationBeaconMap.NotificationBeaconMappingID);
					}

					var notificationSchedules = _unitOfWorkAsync.Repository<Entities.NotificationSchedule>()
						.Query(n => n.NotificationID == id)
						.Select().ToList();
					foreach (var notificationSchedule in notificationSchedules)
					{
						_unitOfWorkAsync.Repository<Entities.NotificationSchedule>().Delete(notificationSchedule.NotificationScheduleID);
					}

					var beaconSchedules = _unitOfWorkAsync.Repository<Entities.BeaconSchedule>()
						.Query(n => n.NotificationID == id)
						.Select().ToList();
					foreach (var beaconSchedule in beaconSchedules)
					{
						_unitOfWorkAsync.Repository<Entities.BeaconSchedule>().Delete(beaconSchedule.BeaconScheduleID);
					}

					_unitOfWorkAsync.Repository<Entities.Notification>().Delete(id);
					_unitOfWorkAsync.SaveChanges();
				}
				catch (Exception ex)
				{
					logging.Error(ex.ToString());
				}
			}
			return RedirectToAction("Index", "Notification");
		}

		/// <summary>
		/// Create/Edit overview
		/// </summary>
		/// <param name="model">The model.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Overview(Entities.Notification model)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				if (model.NotificationID == 0)
				{

					var notification = new Entities.Notification
					{
						ModifiedBy = CurrentUser,
						CreatedDate = DateTime.UtcNow,
						CompanyID = CurrentCompany.CompanyId,
						Active = false,
						Description = model.Description,
						NotificationTitle = model.NotificationTitle,
						NotificationType = model.NotificationType,
						ObjectState = ObjectState.Added,

					};
					if (!string.IsNullOrEmpty(model.NotificationImage)
						&& !System.IO.File.Exists(Server.MapPath(NotificationImagePath) + model.NotificationImage.Split('/').Last())
						&& System.IO.File.Exists(Server.MapPath(TempPath) + model.NotificationImage.Split('/').Last()))
					{
						System.IO.File.Copy(Server.MapPath(TempPath) + model.NotificationImage.Split('/').Last(), Server.MapPath(NotificationImagePath) + model.NotificationImage.Split('/').Last());
						notification.NotificationImage = UrlUtility.GetAbsoluteUrl(NotificationImagePath + model.NotificationImage.Split('/').Last());
					}

					_unitOfWorkAsync.Repository<Entities.Notification>().Insert(notification);
					var success = _unitOfWorkAsync.SaveChanges() > 0;
					if (success)
					{
						response.Status = ConstantUtil.StatusOk;
						response.UniqueId = notification.NotificationID.ToString();
						response.Message = "Campaign overview created successfully.";
					}
				}
				else
				{
					var notification = _unitOfWorkAsync.Repository<Entities.Notification>().Find(model.NotificationID);
					notification.ModifiedBy = CurrentUser;
					notification.ModifiedDate = DateTime.UtcNow;
					notification.CompanyID = CurrentCompany.CompanyId;
					notification.Active = model.Active;
					notification.Description = model.Description;
					notification.NotificationTitle = model.NotificationTitle;
					notification.NotificationType = model.NotificationType;
					if (!string.IsNullOrEmpty(model.NotificationImage)
						&& !System.IO.File.Exists(Server.MapPath(NotificationImagePath) + model.NotificationImage.Split('/').Last())
						&& System.IO.File.Exists(Server.MapPath(TempPath) + model.NotificationImage.Split('/').Last()))
					{
						System.IO.File.Copy(Server.MapPath(TempPath) + model.NotificationImage.Split('/').Last(), Server.MapPath(NotificationImagePath) + model.NotificationImage.Split('/').Last());
						notification.NotificationImage = UrlUtility.GetAbsoluteUrl(NotificationImagePath + model.NotificationImage.Split('/').Last());
					}

					_unitOfWorkAsync.Repository<Entities.Notification>().Update(notification);
					var success = _unitOfWorkAsync.SaveChanges() > 0;
					if (success)
					{
						response.Status = ConstantUtil.StatusOk;
						response.UniqueId = model.NotificationID.ToString();
						response.Message = "Campaign overview updated successfully.";
					}
				}
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public bool CreateUPC(string upc, long notificationId)
		{
			try
			{
				var notificationProduct = _unitOfWorkAsync.Repository<Entities.NotificationProduct>().
					Query(n => n.NotificationID == notificationId).
					Select().SingleOrDefault();

				if (notificationProduct == null)
				{
					var newNotificationProduct = new Entities.NotificationProduct
					{
						BarCode = upc,
						NotificationID = notificationId,
						CompanyID = CurrentCompany.CompanyId,
						CreatedBy = CurrentUser,
						CreatedDate = DateTime.UtcNow,
						Prices = "",
						Sizes = "",
						ProductName = "",
					};

					_unitOfWorkAsync.Repository<Entities.NotificationProduct>().Insert(newNotificationProduct);
					_unitOfWorkAsync.SaveChanges();
				}
				else
				{
					notificationProduct.BarCode = upc;
					notificationProduct.NotificationID = notificationId;
					notificationProduct.CompanyID = CurrentCompany.CompanyId;
					notificationProduct.CreatedBy = CurrentUser;
					notificationProduct.CreatedDate = DateTime.UtcNow;
					notificationProduct.Prices = "";
					notificationProduct.Sizes = "";
					notificationProduct.ProductName = "";

					_unitOfWorkAsync.Repository<Entities.NotificationProduct>().Update(notificationProduct);
					_unitOfWorkAsync.SaveChanges();
				}
			}
			catch { return false; }
			return true;
		}

		/// <summary>
		/// Schedule the campaign
		/// </summary>
		/// <param name="model">The model.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Schedule(Entities.NotificationSchedule model)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				List<Entities.NotificationBeaconMap> beacons = JsonHelper.JsonDeserializer<List<Entities.NotificationBeaconMap>>(Request.Form["TargetBeacons"]);
				List<Entities.BeaconSchedule> bSchedules = JsonHelper.JsonDeserializer<List<Entities.BeaconSchedule>>(Request.Form["BeaconSchedule"]);
				DateTime StartDate = Convert.ToDateTime(Request.Form["StartDate"]).ToUniversalTime();
				DateTime EndDate = Convert.ToDateTime(Request.Form["EndDate"]).ToUniversalTime();
				long NotificationScheduleID = 0;
				Int64.TryParse(Request.Form["NotificationScheduleID"], out NotificationScheduleID);
				long NotificationID = 0;
				Int64.TryParse(Request.Form["NotificationID"], out NotificationID);

				if (beacons.Count == 0 || beacons == null)
				{
					response.Message = "No beacon selected.";
					return Json(response, JsonRequestBehavior.AllowGet);
				}
				if (bSchedules.Count == 0 || bSchedules == null)
				{
					response.Message = "No beacon being scheduled.";
					return Json(response, JsonRequestBehavior.AllowGet);
				}

				if (NotificationScheduleID == 0)
				{
					var schedule = new Entities.NotificationSchedule();
					schedule.NotificationID = NotificationID; ;
					schedule.CompanyID = CurrentCompany.CompanyId;
					schedule.StartDate = StartDate;
					schedule.EndDate = EndDate;
					schedule.CreatedBy = CurrentUser;
					schedule.CreatedDate = DateTime.UtcNow;
					_unitOfWorkAsync.Repository<Entities.NotificationSchedule>().Insert(schedule);
					_unitOfWorkAsync.SaveChanges();
					if (schedule.NotificationScheduleID > 0)
					{
						response.Status = ConstantUtil.StatusOk;
						response.UniqueId = schedule.NotificationScheduleID.ToString();
						response.Message = "Notification scheduled successfully.";
					}
				}
				else
				{
					var schedule = _unitOfWorkAsync.Repository<Entities.NotificationSchedule>().Find(NotificationScheduleID);

					schedule.StartDate = StartDate;
					schedule.EndDate = EndDate;
					schedule.ModifiedBy = CurrentUser;
					schedule.ModifiedDate = DateTime.UtcNow;

					_unitOfWorkAsync.Repository<Entities.NotificationSchedule>().Update(schedule);
					if (_unitOfWorkAsync.SaveChanges() > 0)
					{
						response.Status = ConstantUtil.StatusOk;
						response.UniqueId = schedule.NotificationScheduleID.ToString();
						response.Message = "Notification rescheduled successfully.";
					}
				}


				long notificationId = NotificationID;
				foreach (var b in beacons)
				{
					b.CompanyID = CurrentCompany.CompanyId;
					b.CreatedBy = CurrentUser;
					b.CreatedDate = DateTime.UtcNow;
				}

				foreach (var b in beacons)
				{
					if (b.NotificationBeaconMappingID != 0)
					{
						_unitOfWorkAsync.Repository<Entities.NotificationBeaconMap>().Delete(b.NotificationBeaconMappingID);
					}
				}
				_unitOfWorkAsync.SaveChanges();
				var success = true;
				if (success)
				{
					foreach (var b in beacons)
					{
						_unitOfWorkAsync.Repository<Entities.NotificationBeaconMap>().Insert(b);
					}
					success = _unitOfWorkAsync.SaveChanges() > 0;
					if (success)
					{
						foreach (var b in bSchedules)
						{
							b.FromDate = StartDate;
							b.ToDate = EndDate;
							b.CompanyID = CurrentCompany.CompanyId;
							b.CreatedBy = CurrentUser;
							b.CreatedDate = DateTime.UtcNow;
						}
						var finalSchedule = new List<Entities.BeaconSchedule>();
						foreach (var bs in bSchedules)
						{
							if (finalSchedule.Where(b => b.BeaconID == bs.BeaconID && b.FromDate == bs.FromDate && b.ToDate == bs.ToDate).Count() > 0)
							{
								string days = finalSchedule.Where(b => b.BeaconID == bs.BeaconID && b.FromDate == bs.FromDate && b.ToDate == bs.ToDate).FirstOrDefault().Days;
								if (days != bs.Days)
								{
									foreach (string str in bs.Days.Split(','))
									{
										if (!days.Contains(str))
											days = days + "," + str;
									}
									finalSchedule.Where(b => b.BeaconID == bs.BeaconID && b.FromDate == bs.FromDate && b.ToDate == bs.ToDate).FirstOrDefault().Days = days;
								}
							}
							else
								finalSchedule.Add(bs);
						}

						var beaconsSchedule = _unitOfWorkAsync.Repository<Entities.BeaconSchedule>()
							.Query(b => b.NotificationID == notificationId)
							.Select().ToList();
						foreach (var beaconScheduleID in beaconsSchedule.Select(b => b.BeaconScheduleID))
						{
							_unitOfWorkAsync.Repository<Entities.BeaconSchedule>().Delete(beaconScheduleID);
						}
						_unitOfWorkAsync.SaveChanges();
						if (success)
						{
							foreach (var bsch in finalSchedule)
							{
								var beacon = _unitOfWorkAsync.Repository<Entities.Beacon>().Find(bsch.BeaconID);
								var newBeaconSchedule = new Entities.BeaconSchedule
								{
									BeaconID = beacon.Id,
									CompanyID = bsch.CompanyID,
									CreatedBy = CurrentUser,
									CreatedDate = DateTime.UtcNow,
									Days = bsch.Days,
									FromDate = bsch.FromDate,
									NotificationID = bsch.NotificationID,
									ToDate = bsch.ToDate
								};
								_unitOfWorkAsync.Repository<Entities.BeaconSchedule>().Insert(newBeaconSchedule);
							}
							success = _unitOfWorkAsync.SaveChanges() > 0;
						}
					}
				}
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}


		/// <summary>
		/// Targets the beacons for campaign.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult TargetBeacons()
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				var beacons = JsonHelper.JsonDeserializer<List<Entities.NotificationBeaconMap>>(Request.Form["TargetBeacons"]);
				var bSchedules = JsonHelper.JsonDeserializer<List<Entities.BeaconSchedule>>(Request.Form["BeaconSchedule"]);
				if (beacons.Count == 0 || beacons == null)
				{
					response.Message = "No beacon selected.";
					return Json(response, JsonRequestBehavior.AllowGet);
				}
				if (bSchedules.Count == 0 || bSchedules == null)
				{
					response.Message = "No beacon being scheduled.";
					return Json(response, JsonRequestBehavior.AllowGet);
				}
				long notificationId = beacons[0].NotificationID;
				foreach (var b in beacons)
				{
					b.CompanyID = CurrentCompany.CompanyId;
					b.CreatedBy = CurrentUser;
					b.CreatedDate = DateTime.UtcNow;
				}

				_unitOfWorkAsync.Repository<Entities.NotificationBeaconMap>().Delete(notificationId);
				var success = _unitOfWorkAsync.SaveChanges() > 0;
				if (success)
				{
					foreach (var b in beacons)
					{
						_unitOfWorkAsync.Repository<Entities.NotificationBeaconMap>().Insert(b);
					}
					success = _unitOfWorkAsync.SaveChanges() > 0;
					if (success)
					{

						foreach (var b in bSchedules)
						{
							b.FromDate = b.FromDate.GetValueOrDefault();
							b.ToDate = b.ToDate.GetValueOrDefault();
							b.CompanyID = CurrentCompany.CompanyId;
							b.CreatedBy = CurrentUser;
							b.CreatedDate = DateTime.UtcNow;
						}
						var beaconsSchedule = _unitOfWorkAsync.Repository<Entities.BeaconSchedule>()
							.Query(b => b.NotificationID == notificationId)
							.Select().ToList();
						foreach (var beaconScheduleID in beaconsSchedule.Select(b => b.BeaconScheduleID))
						{
							_unitOfWorkAsync.Repository<Entities.BeaconSchedule>().Delete(beaconScheduleID);
						}
						_unitOfWorkAsync.SaveChanges();

						success = _unitOfWorkAsync.SaveChanges() > 0;
						if (success)
						{
							foreach (var bsch in bSchedules)
							{
								var beacon = _unitOfWorkAsync.Repository<Entities.Beacon>().GetByBeaconNum(bsch.Beacon.BeaconID);
								var newBeaconSchedule = new Entities.BeaconSchedule
								{
									BeaconID = beacon.Id,
									CompanyID = bsch.CompanyID,
									CreatedBy = CurrentUser,
									CreatedDate = DateTime.UtcNow,
									Days = bsch.Days,
									FromDate = bsch.FromDate,
									NotificationID = bsch.NotificationID,
									ToDate = bsch.ToDate
								};
								_unitOfWorkAsync.Repository<Entities.BeaconSchedule>().Insert(newBeaconSchedule);
							}
							success = _unitOfWorkAsync.SaveChanges() > 0;
						}
					}
					if (success)
					{
						response.Message = "Selected beacons target successfully.";
						response.Status = ConstantUtil.StatusOk;
					}
				}
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Publishes the specified campaign to make it available for consumers.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Publish(long id)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				//var offer = _unitOfWorkAsync.Repository<Entities.Offer>()
				//	.Query(o => o.NotificationID == id)
				//	.Select().SingleOrDefault();
				//if (offer == null)
				//{
				//	response.Message = "No offer created for the notification. Please create atleast one offer.";
				//	return Json(response, JsonRequestBehavior.AllowGet);
				//}
				//NotificationProduct p = _notification.GetNotificationProduct(id);
				//if (p == null)
				//{
				//    response.Message = "No product created for the notification. Please create atleast one notification.";
				//    return Json(response, JsonRequestBehavior.AllowGet);
				//}

				var schedule = _unitOfWorkAsync.Repository<Entities.NotificationSchedule>()
					.Query(n => n.NotificationID == id)
					.Select().FirstOrDefault();
				if (schedule == null)
				{
					response.Message = "Scheduling not being done. Please specify the scheduling options.";
					return Json(response, JsonRequestBehavior.AllowGet);
				}
				var notification = _unitOfWorkAsync.Repository<Entities.Notification>().Find(id);
				notification.Active = true;
				_unitOfWorkAsync.Repository<Entities.Notification>().Update(notification);
				var success = _unitOfWorkAsync.SaveChanges() > 0;
				if (success)
				{
					response.Message = "Notification published successfully.";
					response.Status = ConstantUtil.StatusOk;
					//var pushSettings = _notification.GetPushNotificationDetails(CurrentCompany.CompanyId).ToList();
					//PushNotification.SendNotification(pushSettings);
				}
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		#region Upload Images

		/// <summary>
		/// Uploads the alert image.
		/// </summary>
		/// <returns>JsonResult.</returns>
		[HttpPost]
		public JsonResult UploadNotificationImage()
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				string relativePath = TempPath;
				string preImage = Request.Form["ImageURL"].ToString();
				HttpPostedFileBase file = Request.Files[0];
				if (file != null && file.ContentLength > 0)
				{
					var path = GetTargetPath(relativePath, file);
					System.Drawing.Image streamingImage = System.Drawing.Image.FromStream(file.InputStream);
					Bitmap img = new Bitmap(streamingImage);
					ImageUtility.ResizeandSaveImage(img, 250, 250, 100, path);
					DeleteFile(NotificationImagePath, preImage);
					response.Status = ConstantUtil.StatusOk;
					response.ReturnUrl = UrlUtility.GetAbsoluteUrl(relativePath + path.Split('\\').Last());
					return Json(response, JsonRequestBehavior.AllowGet);
				}
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Uploads the offer image.
		/// </summary>
		/// <returns>JsonResult.</returns>
		[HttpPost]
		public JsonResult UploadOfferImage()
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				string relativePath = TempPath;
				string preImage = Request.Form["ImageURL"].ToString();
				HttpPostedFileBase file = Request.Files[0];
				if (file != null && file.ContentLength > 0)
				{
					var path = GetTargetPath(relativePath, file);
					System.Drawing.Image streamingImage = System.Drawing.Image.FromStream(file.InputStream);
					Bitmap img = new Bitmap(streamingImage);
					ImageUtility.ResizeandSaveImage(img, 250, 250, 100, path);
					DeleteFile(OfferImagePath, preImage);
					response.Status = ConstantUtil.StatusOk;
					response.ReturnUrl = UrlUtility.GetAbsoluteUrl(relativePath + path.Split('\\').Last()); ;
					return Json(response, JsonRequestBehavior.AllowGet);
				}
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		#endregion

		[HttpPost]
		public JsonResult GetUPC(UPCRequest req)
		{
			UPCResponse response = new UPCResponse();
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://api.simpleupc.com/v1.php");
			request.AllowWriteStreamBuffering = true;
			request.Method = "POST";
			string post = JsonHelper.JsonSerializer<UPCRequest>(req).ToString();

			byte[] b1 = System.Text.Encoding.ASCII.GetBytes(post);
			request.ContentLength = b1.Length;
			request.ContentType = "application/json";

			Stream writer = request.GetRequestStream();
			writer.Write(b1, 0, b1.Length);
			var httpResponse = (HttpWebResponse)request.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				response = JsonHelper.JsonDeserializer<UPCResponse>(result);
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetBeaconScheduleForNotification(long notificationId, long beaconId, string fromDate, string toDate)
		{
			if (notificationId == 0 || beaconId == 0 || string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate)) { return null; }
			string dnames = string.Empty; ;
			//List<BeaconSchedule> unscheduled = new List<BeaconSchedule>();
			//List<BeaconSchedule> scheduled = new List<BeaconSchedule>();
			DateTime from = Convert.ToDateTime(fromDate);
			DateTime to = Convert.ToDateTime(toDate);
			//List<BeaconSchedule> bs = _notification.GetBeaconSchedule(beaconId);
			var bs = _unitOfWorkAsync.Repository<Entities.BeaconSchedule>()
				.Query(b => b.BeaconID == beaconId)
				.Select().ToList();

			//foreach (var b in bs)
			//{
			//	b.FromDate = Convert.ToDateTime(b.FromDate);
			//	b.ToDate = Convert.ToDateTime(b.ToDate);
			//}

			var scheduled = bs.Where(s => s.NotificationID == notificationId).ToList();
			var unscheduled = new List<Entities.BeaconSchedule>();
			List<DateTime> allDates = new List<DateTime>();
			DateTime stDate = from;
			while (stDate <= to)
			{
				allDates.Add(stDate);
				if (!dnames.Contains(stDate.ToString("ddd")))
					dnames = dnames + stDate.ToString("ddd") + ",";
				stDate = stDate.AddDays(1);
			}
			dnames = dnames.Substring(0, dnames.Length - 1);
			if (bs == null || bs.Count == 0)
			{
				unscheduled.Add(new Entities.BeaconSchedule
				{
					FromDate = from,
					ToDate = to,
					Days = dnames,
					BeaconID = beaconId
				});
			}
			else
			{
				List<DateTime> scheduledDates = new List<DateTime>();
				foreach (var s in bs.Where(b => b.NotificationID != notificationId))
				{
					List<string> days = s.Days.ToLower().Split(',').ToList();
					DateTime sd = s.FromDate.GetValueOrDefault();
					while (sd <= s.ToDate)
					{
						if (days.Contains(sd.ToString("ddd").ToLower()))
							scheduledDates.Add(sd);
						sd = sd.AddDays(1);
					}
				}
				scheduledDates = scheduledDates.Distinct().OrderBy(d => d.Date).ToList();


				allDates.RemoveAll(s => scheduledDates.Contains(s.Date));
				DateTime? stopdate = null;
				foreach (DateTime d in allDates)
				{
					if (stopdate == null || d > stopdate)
					{
						List<string> sdays = new List<string>();
						DateTime st = d;
						while (allDates.Contains(st))
						{
							if (!sdays.Contains(st.ToString("ddd")))
								sdays.Add(st.ToString("ddd"));
							st = st.AddDays(1);
						}
						stopdate = st;
						unscheduled.Add(new Entities.BeaconSchedule { FromDate = d, ToDate = st.AddDays(-1), BeaconID = beaconId, Days = string.Join(",", sdays) });
					}
				}
			}
			foreach (var ubs in unscheduled)
			{
				foreach (var sbs in scheduled)
				{
					if (ubs.FromDate == sbs.FromDate && ubs.ToDate == sbs.ToDate)
					{
						foreach (string str in sbs.Days.Split(','))
						{
							ubs.Days = ubs.Days.Replace("," + str, "").Replace(str + ",", "").Replace(str, "");
						}
					}
				}
			}
			unscheduled.RemoveAll(ub => string.IsNullOrEmpty(ub.Days));
			return Json(new { Scheduled = scheduled, Unscheduled = unscheduled }, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetBeaconSchedule(string beaconId)
		{
			if (string.IsNullOrEmpty(beaconId)) { return null; }

			var beacon = _unitOfWorkAsync.Repository<Entities.Beacon>().GetByBeaconNum(beaconId);
			var beaconsSchedule = _unitOfWorkAsync.Repository<Entities.BeaconSchedule>()
				.Query(b => b.BeaconID == beacon.Id)
				.Include(b => b.Beacon)
				.Select().ToList();

			var notifications = _unitOfWorkAsync.Repository<Entities.Notification>()
				.Query(n => n.CompanyID == CurrentCompany.CompanyId)
				.Select().ToList();
			List<Calendar> scheduledDates = new List<Calendar>();
			foreach (var s in beaconsSchedule)
			{
				List<string> days = s.Days.ToLower().Split(',').ToList();
				DateTime sd = s.FromDate.GetValueOrDefault();
				while (sd <= s.ToDate)
				{
					if (days.Contains(sd.ToString("ddd").ToLower())
						&& scheduledDates.Where(sch => sch.id == s.NotificationID.ToString()
						&& sch.start == sd.Year.ToString() + "-" + sd.Month.ToString() + "-" + sd.Day).Count() == 0)
					{
						scheduledDates.Add(new Calendar
						{
							id = s.NotificationID.ToString(),
							start = sd.Year.ToString() + "-" + sd.Month.ToString() + "-" + sd.Day,
							end = sd.Year.ToString() + "-" + sd.Month.ToString() + "-" + sd.Day,
							title = notifications.Where(n => n.NotificationID == s.NotificationID).FirstOrDefault().NotificationTitle
						});
					}
					sd = sd.AddDays(1);
				}
			}
			return Json(scheduledDates, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetNotificationSchedule(long id)
		{
			if (id == 0) { return null; }
			var beaconsSchedule = _unitOfWorkAsync.Repository<Entities.BeaconSchedule>()
				.Query(b => b.NotificationID == id)
				.Include(b => b.Beacon)
				.Select().ToList();

			List<Calendar> scheduledDates = new List<Calendar>();
			foreach (var beaconSchedule in beaconsSchedule)
			{
				List<string> days = beaconSchedule.Days.ToLower().Split(',').ToList();
				var sd = beaconSchedule.FromDate.GetValueOrDefault();
				string beaconName = beaconSchedule.Beacon.BeaconName + "(" + beaconSchedule.Beacon.BeaconID + ")";
				while (sd <= beaconSchedule.ToDate)
				{
					if (days.Contains(sd.ToString("ddd").ToLower())
						&& scheduledDates.Where(sch => sch.id == beaconSchedule.NotificationID.ToString()
						&& sch.start == sd.Year.ToString() + "-" + sd.Month.ToString() + "-" + sd.Day).Count() == 0)
					{
						scheduledDates.Add(new Calendar
						   {
							   id = beaconSchedule.NotificationID.ToString(),
							   start = sd.Year.ToString() + "-" + sd.Month.ToString() + "-" + sd.Day,
							   end = sd.Year.ToString() + "-" + sd.Month.ToString() + "-" + sd.Day,
							   title = beaconName
						   });
					}
					sd = sd.AddDays(1);
				}
			}
			return Json(scheduledDates, JsonRequestBehavior.AllowGet);
		}


		//protected override void Dispose(bool disposing)
		//{
		//	if (disposing)
		//	{
		//		_unitOfWorkAsync.Dispose();
		//	}
		//	base.Dispose(disposing);
		//}

	}

	public class Calendar
	{
		public string id;
		public string title;
		public string start;
		public string end;
		public string url;
	}

	public class parameters
	{
		public string upc { get; set; }
	}

	public class UPCRequest
	{
		public string auth { get; set; }
		public string method { get; set; }
		public parameters @params { get; set; }
		public string returnFormat { get; set; }
	}

	public class UPCResponse
	{
		public bool success { get; set; }

		public Result result { get; set; }
	}

	public class Result
	{
		public string brand { get; set; }

		public string category { get; set; }

		public string description { get; set; }

		public bool ProductHasImage { get; set; }

		public string imageThumbURL { get; set; }

		public string imageURL { get; set; }

		public string upc { get; set; }

		public string size { get; set; }

		public string units { get; set; }

		public string container { get; set; }
	}
}
