using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LiveKart.Business;
using LiveKart.Business.ImageUtility;
using LiveKart.Entities;
using LiveKart.LogService;
using LiveKart.Shared;
using System.IO;
using LiveKart.Service;
using Repository.Pattern.UnitOfWork;
using Repository.Pattern.Infrastructure;

namespace LiveKart.Web.Controllers
{
	public class NotificationMessageController : BaseController

	{
		/// <summary>
		/// The notification image path
		/// </summary>
		private const string NotificationImagePath = "~/Content/notificationimages/";

		private const string TempPath = "~/Content/tempdata/";

		ILogService logging = new FileLogService(typeof(NotificationController));

		private readonly INotificationMessageService _notificationMessageService;
		private readonly IUnitOfWork _unitOfWorkAsync;

		public NotificationMessageController(
				INotificationMessageService notificationMessage,
				IStandardMessageService standardMessageService, IUnitOfWork unitOfWorkAsync)
			: base()
		{
			_notificationMessageService = notificationMessage;
			_unitOfWorkAsync = unitOfWorkAsync;
		}
		/// <summary>
		/// Create/Edit notification message
		/// </summary>
		/// <param name="model"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult NotificationMessage(NotificationMessage notificationMessage)
		{
			if (!notificationMessage.NotificationType.HasValue)
			{
				notificationMessage.NotificationType
					= _notificationMessageService
					.Find(notificationMessage.NotificationMessageId).NotificationType;
			}

			switch ((Enums.MessageType) notificationMessage.NotificationType)
			{
				case Enums.MessageType.StandardMessage:
					UpdateStandardMessageDetails(notificationMessage);
					notificationMessage.StandardMessageId = notificationMessage.StandardMessage.StandardMessageId;
					break;
				case Enums.MessageType.SurveyMessage:
					UpdateSurveyMessageDetails(notificationMessage);
					break;
				case Enums.MessageType.ReviewMessage:
					UpdateReviewMessageDetails(notificationMessage);
					break;
				case Enums.MessageType.RatingMessage:
					notificationMessage.SurveyMessage = null;
					notificationMessage.StandardMessage = null;
					UpdateRatingMessageDetails(notificationMessage);
					break;
				case Enums.MessageType.Offer:
					notificationMessage.SurveyMessage = null;
					notificationMessage.StandardMessage = null;
					UpdateOfferMessageDetails(notificationMessage);
					break;
			}
			notificationMessage.SurveyMessage = null;
			notificationMessage.StandardMessage = null;
			notificationMessage.ReviewMessage = null;
			notificationMessage.RatingMessage = null;
            
			if (notificationMessage.NotificationMessageId == 0)
			{
				_notificationMessageService.Insert(notificationMessage);
				_unitOfWorkAsync.SaveChanges();
			}
			else
			{
				var m = _notificationMessageService.Find(notificationMessage.NotificationMessageId);
				m.NotificationTitle = notificationMessage.NotificationTitle;
				m.NotificationType = notificationMessage.NotificationType;
				m.NotificationDescription = notificationMessage.NotificationDescription;
				m.MessageThumbImage = notificationMessage.MessageThumbImage;
				m.ModifiedDate = DateTime.UtcNow;
				_notificationMessageService.Update(m);
				_unitOfWorkAsync.SaveChanges();
			}
			JsonResponse response = new JsonResponse();
			return RedirectToAction("Edit", "Notification", new { id = notificationMessage.NotificationId });
		}

		private void UpdateOfferMessageDetails(NotificationMessage message)
		{
			if (message.OfferMessage.OfferMessageId == 0)
			{
				_notificationMessageService.InsertOfferMessage(message.OfferMessage);
				_unitOfWorkAsync.SaveChanges();
				message.OfferId = message.OfferMessage.OfferMessageId;
				message.OfferMessage = null;
			}
			else
			{
				_notificationMessageService.UpdateOfferMessage(message.OfferMessage);
				_unitOfWorkAsync.SaveChanges();
			}
		}

		private void UpdateRatingMessageDetails(NotificationMessage message)
		{
			if (message.RatingMessage.RatingMessageId == 0)
			{
				var items = new List<RatingItem>(message.RatingMessage.RatingItems);
				message.RatingMessage.RatingItems = null;
				_notificationMessageService.InsertRatingMessage(message.RatingMessage);
				_unitOfWorkAsync.SaveChanges();
				items.ForEach(i => i.RatingMessageId = message.RatingMessage.RatingMessageId);
				_notificationMessageService.InsertRatingItems(items);
				message.ProductRatingId = message.RatingMessage.RatingMessageId;
				message.RatingMessage = null;
			}
			else
			{
				_notificationMessageService.UpdateRatingMessage(message.RatingMessage);
				_unitOfWorkAsync.SaveChanges();
			}
		}

		private void UpdateReviewMessageDetails(NotificationMessage message)
		{
			if (message.ReviewMessage.ReviewMessageId == 0)
			{
				_notificationMessageService.InsertReviewMessage(message.ReviewMessage);
				_unitOfWorkAsync.SaveChanges();
				message.ProductReviewId = message.ReviewMessage.ReviewMessageId;
			}
			else
			{
				var oldReview = _notificationMessageService.FindReviewMessage(message.ProductReviewId.GetValueOrDefault(0));
				var review = message.ReviewMessage;
				oldReview.MessageHeader = review.MessageHeader;
				oldReview.MessageDescription = review.MessageDescription;
				oldReview.MessageShortDescription = review.MessageShortDescription;
				oldReview.MessageImage = review.MessageImage;
				oldReview.ScreenName = review.ScreenName;
				oldReview.State = review.State;
				oldReview.City = review.City;
				_notificationMessageService.UpdateReviewMessage(oldReview);
			}
		}

		private void UpdateStandardMessageDetails(Entities.NotificationMessage message)
		{
			if (message.StandardMessage == null) return;

			if (message.StandardMessage.StandardMessageId == 0)
			{
				_notificationMessageService.InsertStandartMessage(message.StandardMessage);
				_unitOfWorkAsync.SaveChanges();
				message.StandardMessageId = message.StandardMessageId;
			}
			else
			{
				var sm = _notificationMessageService.FindStandartMessage(message.StandardMessageId.GetValueOrDefault(0));
				sm.MessageDescription = message.StandardMessage.MessageDescription;
				sm.MessageHeader = message.StandardMessage.MessageHeader;
				sm.MessageImage = message.StandardMessage.MessageImage;
				sm.MessageShortDescription = message.StandardMessage.MessageShortDescription;


				_notificationMessageService.UpdateStandartMessage(sm);

				_unitOfWorkAsync.SaveChanges();
			}
			return;
		}

		private void UpdateSurveyMessageDetails(NotificationMessage message)
		{
			if (message.SurveyMessage == null) return;

			SurveyMessage survey = null;

			if (message.SurveyId.GetValueOrDefault(0) == 0) // create NEW
			{
				survey = new SurveyMessage();
				_notificationMessageService.InsertSurveyMessage(survey);
				_unitOfWorkAsync.SaveChanges();
				message.SurveyId = survey.SurveyId;
				message.SurveyMessage.SurveyId = survey.SurveyId;
				message.SurveyMessage.Questions.ToList().ForEach(q =>
				{
					q.SurveyId = survey.SurveyId;
					var answers = new List<SurveyQuestionAnswer>();
					if (q.Answers != null)
					{
						answers = new List<SurveyQuestionAnswer>(q.Answers);
						q.Answers = null;
					}
					_notificationMessageService.InsertSurveyQuestion(q);
					answers.ForEach(a => a.QuestionId = q.QuestionId);
					_notificationMessageService.InsertSurveyAnswers(answers.Where(a => a.Answer != null));
					q.Answers = answers;
					survey.Questions.Add(q);
				});


				survey.MessageHeader = message.SurveyMessage.MessageHeader;
				survey.MessageShortDescription = message.SurveyMessage.MessageShortDescription;
				survey.MessageDescription = message.SurveyMessage.MessageDescription;
				message.SurveyMessage = survey;

			}
			else // edit existing 
			{
				survey = _notificationMessageService.FindSurveyMessage(message.SurveyId.GetValueOrDefault(0));
				survey.MessageHeader = message.SurveyMessage.MessageHeader;
				survey.MessageShortDescription = message.SurveyMessage.MessageShortDescription;
				survey.MessageDescription = message.SurveyMessage.MessageDescription;

				foreach (var question in message.SurveyMessage.Questions)
				{
					var originalQuestion = survey.Questions
						.SingleOrDefault(c => c.QuestionId == question.QuestionId && c.QuestionId != 0);
					// Is original child item with same ID in DB?
					if (originalQuestion != null)
					{
						// Yes -> Update scalar properties of child item
						// lets first update answers
						if (question.Answers != null)
						{
							foreach (var answer in question.Answers)
							{
								var originalAnswer = originalQuestion.Answers
								                                     .SingleOrDefault(c => c.AnswerId == answer.AnswerId && c.AnswerId != 0);
								// Is original child item with same ID in DB?
								if (originalAnswer != null)
								{
									// Yes -> Update scalar properties of child item
									originalAnswer.ModifiedDate = DateTime.UtcNow;
									originalAnswer.Answer = answer.Answer;
									originalAnswer.ObjectState = ObjectState.Modified;
									_notificationMessageService.UpdateSurveyAnswer(originalAnswer);
								}
								else
								{
									// No -> It's a new child item -> Insert
									answer.QuestionId = question.QuestionId;
									answer.CreatedDate = DateTime.UtcNow;
									answer.ObjectState = ObjectState.Added;
									_notificationMessageService.InsertSurveyAnswer(answer);
								}
							}

							//removing answers wich is not present in request (was deleter by user)
							var originalAnswersIds = originalQuestion.Answers.Select(a => a.AnswerId).ToList();
							foreach (var originalAnswerId in originalAnswersIds)
							{
								if (!question.Answers.Select(a => a.AnswerId).Contains(originalAnswerId))
								{
									var answer = originalQuestion.Answers.First(a => a.AnswerId == originalAnswerId);
									answer.ObjectState = ObjectState.Deleted;
									_notificationMessageService.DeleteSurveyAnswer(answer);
								}
							}
						}

						originalQuestion.ModifiedDate = DateTime.UtcNow;
						originalQuestion.ObjectState = ObjectState.Modified;
						originalQuestion.Question = question.Question;
						originalQuestion.QuestionType = question.QuestionType; //TODO: think about it

						_notificationMessageService.UpdateSurveyQuestion(originalQuestion);
					}
					else
					{
						// No -> It's a new child item -> Insert
						question.ObjectState = ObjectState.Added;
						question.SurveyId = survey.SurveyId;
						var answers = new List<SurveyQuestionAnswer>();
						if (question.Answers != null)
						{
							answers = new List<SurveyQuestionAnswer>(question.Answers);
							question.Answers = null;
						}
						_notificationMessageService.InsertSurveyQuestion(question);
						answers.ForEach(a => a.QuestionId = question.QuestionId);
						_notificationMessageService.InsertSurveyAnswers(answers.Where(a => a.Answer != null));
						question.Answers = answers;
					}
				}

				//removing questions with is not present in request
				var questionList = new List<SurveyQuestion>(survey.Questions);
				foreach (var question in questionList)
				{
					if (!message.SurveyMessage.Questions.Select(q => q.QuestionId).Contains(question.QuestionId))
					{
						var answersList = new List<SurveyQuestionAnswer>(question.Answers);
						answersList.ForEach(a =>
							{
								a.ObjectState = ObjectState.Deleted;
								_notificationMessageService.DeleteSurveyAnswer(a);
							});

						var userAnswersList = new List<SurveyUserAnswer>(question.UserAnswers);
						userAnswersList.ForEach(ua =>
							{
								ua.ObjectState = ObjectState.Deleted;
								_notificationMessageService.DeleteSurveyUserAnswer(ua);
							});

						question.ObjectState = ObjectState.Deleted;
						_notificationMessageService.DeleteSurveyQuestion(question);
					}
				}


				message.SurveyMessage = survey;
			}

			_notificationMessageService.UpdateSurveyMessage(survey);
			_unitOfWorkAsync.SaveChanges();
		}

		[HttpGet]
		public ActionResult NotificationMessage(long id)
		{
			var message = _notificationMessageService.GetNotification(id);
			message.Notification = null; // we don't need parent entity in response
			return Json(message, JsonRequestBehavior.AllowGet);
		}

		[HttpGet]
		public ActionResult Delete(long id)
		{
			var notificationMessage = _notificationMessageService.Find(id);
			var campaignId = notificationMessage.NotificationId;
			_notificationMessageService.Delete(id);
			_unitOfWorkAsync.SaveChanges();
			return RedirectToAction("Edit", "Notification", new { id = campaignId });
		}

		[HttpGet]
		[ActionName("ProductReview")]
		public PartialViewResult ProductReview(long messageId = 0)
		{
			NotificationMessage message;
			if (messageId == 0)
			{
				message = new NotificationMessage();
				message.ReviewMessage = new ReviewMessage();
				ViewBag.Answers = new List<UserReview>();
				ViewBag.ActionText = "Create";
			}
			else
			{
				message = _notificationMessageService.GetNotification(messageId);
				ViewBag.Answers = _notificationMessageService.GetUserReviews((long)message.ProductReviewId);
				ViewBag.ActionText = "Save";
			}
			return PartialView("_Review", message);
		}

		[HttpGet]
		[ActionName("ProductRating")]
		public PartialViewResult ProductRating(long messageId = 0)
		{
			NotificationMessage message = null;
			if (messageId == 0)
			{
				message = new NotificationMessage();
				message.RatingMessage = new RatingMessage {RatingItems = new List<RatingItem> {new RatingItem()}};
				ViewBag.Items = null;
				ViewBag.Users = null;
				ViewBag.Answers = null;
				ViewBag.ActionText = "Create";
			}
			else
			{
				message = _notificationMessageService.GetNotification(messageId);
				ViewBag.Items = message.RatingMessage.RatingItems;
				var answers = _notificationMessageService.GetUserRatingItemsByRatingId((long)message.ProductRatingId).ToList();
				ViewBag.Users = answers.Select(a => a.UserId).Distinct().ToList();
				ViewBag.Answers = answers;
				ViewBag.ActionText = "Save";
			}
			return PartialView("_Rating", message);
		}

		[HttpGet]
		public PartialViewResult OfferMessage(long messageId = 0)
		{
			NotificationMessage message;
			if (messageId == 0)
			{
				message = new NotificationMessage {OfferMessage = new OfferMessage()};
				ViewBag.ActionText = "Create";
			}
			else
			{
				message = _notificationMessageService.GetNotification(messageId);
				message.OfferMessage = _notificationMessageService.FindOfferMessage((long)message.OfferId);
				ViewBag.ActionText = "Save";
			}
			return PartialView("_Offer", message);
		}

		[HttpGet]
		public PartialViewResult StandardMessage(long messageId = 0)
		{
			NotificationMessage message = messageId == 0 ? 
				                              new NotificationMessage {StandardMessage = new StandardMessage()} : 
				                              _notificationMessageService.GetNotification(messageId);
			ViewBag.ActionText = messageId == 0 ? "Create" : "Save";
			return PartialView("_Standard", message);
		}

		[HttpGet]
		public PartialViewResult SurveyMessage(long messageId = 0)
		{
			NotificationMessage message;
			if (messageId == 0)
			{
				message = new NotificationMessage {SurveyMessage = new SurveyMessage()};
				message.SurveyMessage.Questions = new List<SurveyQuestion>();
				ViewBag.ActionText = "Create";
			}
			else
			{
				message = _notificationMessageService.GetNotification(messageId);
				ViewBag.Questions = message.SurveyMessage.Questions.ToList();
				var answers = _notificationMessageService.GetSurveyUserAnswerBySurvey((long)message.SurveyId).ToList();

				if (answers.Count > 0)
				{
					ViewBag.Answers = answers;
					ViewBag.Users = answers.Select(a => a.RecordId).ToList();
				}
				ViewBag.UserRecords = answers.Select(a => a.RecordId).Distinct().ToList();
				ViewBag.ActionText = "Save";
			}
			return PartialView("_Survey", message);
		}

		[HttpPost]
		public JsonResult UploadImage()
		{
			var response = new JsonResponse
				{
					Message = ConstantUtil.MessageError, 
					Status = ConstantUtil.StatusFail
				};
			try
			{
				const string relativePath = NotificationImagePath;
				HttpPostedFileBase file = Request.Files[0];
				if (file != null && file.ContentLength > 0)
				{
					var path = GetTargetPath(relativePath, file);
					file.SaveAs(path);
					//TODO: remove unneded files
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

	}
}
