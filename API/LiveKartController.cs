using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using LiveKart.Business.Email;
using LiveKart.Repository;
using Newtonsoft.Json.Linq;
using Repository.Pattern.UnitOfWork;
using LiveKart.Entities;
using System.Collections.Generic;


namespace LiveKart.Web.API
{
	/// <summary>
	/// Class LiveKartController
	/// </summary>
	public class LiveKartController : ApiController
	{

		private readonly IUnitOfWork _unitOfWorkAsync;

		public LiveKartController(IUnitOfWork unitOfWorkAsync)
		{
			_unitOfWorkAsync = unitOfWorkAsync;
		}


		//Request={"beaconId":"", "lastRequestTime":""}
		//if want to use seperate parameters, content-type=x-www-form-urlencoded will work. No other type will work
		/// <summary>
		/// Returns latest campaigns against provided beacon id
		/// </summary>
		/// <param name="request">beacon id and last requested time</param>
		/// <returns>List of campaigns.</returns>
		[HttpPost]
		[ResponseType(typeof(RestNotification))]
		public IHttpActionResult Notifications(JObject request)
		{
			dynamic d = request;


			RestNotification responce = new RestNotification();
			string beaconId = Convert.ToString(d.beaconId);

			var notificationIDs = _unitOfWorkAsync.Repository<NotificationBeaconMap>()
				.Query(x => x.Beacon.BeaconID == beaconId)
				.Select(x => x.NotificationID);

			responce.Notifications  = _unitOfWorkAsync.Repository<Notification>()
				.Query(x => notificationIDs.Contains(x.NotificationID))
				.Select().ToList();
			
			responce.message = "success";
			responce.status = responce.Notifications.Count.ToString() + " new campaign found.";
			return Ok();
		}

		[HttpPost]
		public RestResponse CreateNotificationRating(NotificationRating nr)
		{
			RestResponse response = new RestResponse();
			response.status = "fail";
			response.message = "Something went wrong. Please try again.";
			try
			{
				var notificationRating = new Entities.NotificationRating
				{
					NotificationId = nr.NotificationId,
					Rate = nr.Rate,
					CreatedDate = DateTime.UtcNow,
					CreatedOn = DateTime.UtcNow
				};

				_unitOfWorkAsync.Repository<Entities.NotificationRating>().Insert(notificationRating);
				var success = _unitOfWorkAsync.SaveChanges() > 0;
				if (success)
				{
					response.status = "success";
					response.message = "Campaign rated successfully.";
				}
			}
			catch
			{ }
			return response;
		}

		[HttpGet]
		public decimal GetNotificationRating(long id)
		{
			var notificationRatings = _unitOfWorkAsync.Repository<Entities.NotificationRating>().
				Query(n => n.NotificationId == id).
				Select().ToList();
			decimal rate = 0;
			var count = notificationRatings.Count;
			foreach (var notificationRating in notificationRatings)
			{
				rate += notificationRating.Rate.GetValueOrDefault(0);
			}

			return rate / count;
		}

		[HttpPost]
		public RestResponse UpdateBeaconBattery(JObject request)
		{
			RestResponse response = new RestResponse();
			response.status = "fail";
			response.message = "Something went wrong.";
			dynamic d = request;
			string bL = Convert.ToString(d.batteryLevel);
			string BI = Convert.ToString(d.beaconId);
			var beacon = _unitOfWorkAsync.Repository<Entities.Beacon>().GetByBeaconNum(BI);
			beacon.BatteryLevel = bL;
			_unitOfWorkAsync.Repository<Entities.Beacon>().Update(beacon);
			var success = _unitOfWorkAsync.SaveChanges() > 0;
			if (bL == "0")
			{
				EmailHelper.SendEmail("Notification about Beacons Battery", "pankaj.thakur@droisys.com", "<html xmlns='http://www.w3.org/1999/xhtml'><head><title></title></head><body><h3><i>Beacons Battery Status!</i></h3><p>Please check the battery of beacons No - ' " + BI + " '.</p><p>Battery is empty </p>		<p>Thanks !</p></body></html>");
			}
			else if (bL == "1")
			{
				EmailHelper.SendEmail("Notification about Beacons Battery", "pankaj.thakur@droisys.com", "<html xmlns='http://www.w3.org/1999/xhtml'><head><title></title></head><body><h3><i>Beacons Battery Status!</i></h3><p>Please check the battery of beacons No - ' " + BI + " '.</p><p>Battery remaining 25 % </p>		<p>Thanks !</p></body></html>");
			}
			else if (bL == "2")
			{
				EmailHelper.SendEmail("Notification about Beacons Battery", "pankaj.thakur@droisys.com", "<html xmlns='http://www.w3.org/1999/xhtml'><head><title></title></head><body><h3><i>Beacons Battery Status!</i></h3><p>Please check the battery of beacons No -'  " + BI + " '.</p><p>Battery remaining 75 % </p>		<p>Thanks !</p></body></html>");
			}
			else if (bL == null)
			{
				EmailHelper.SendEmail("Notification about Beacons Battery", "pankaj.thakur@droisys.com", "<html xmlns='http://www.w3.org/1999/xhtml'><head><title></title></head><body><h3><i>Beacons Battery Status!</i></h3><p>Please check the battery of beacons No - ' " + BI + " '.</p><p>Battery is empty </p>		<p>Thanks !</p></body></html>");
			}
			if (success)
			{
				response.status = "success";
				response.message = "Battery updated successfully.";
			}
			return response;
		}

		/// <summary>
		/// Gets the product details against barcode.
		/// </summary>
		/// <param name="barcode">barcode.</param>
		/// <returns>Product details</returns>
		[HttpGet]
		public IHttpActionResult GetProduct(string barcode)
		{
			var notificationProduct = _unitOfWorkAsync.Repository<Entities.NotificationProduct>().
				Query(n => n.BarCode == barcode).
				Select().SingleOrDefault();

			return Ok(notificationProduct);
		}
	}


	public class RestResponse
	{
		public string status { get; set; }

		public string message { get; set; }
	}


	public class RestNotification : RestResponse
	{
		public List<Notification> Notifications { get; set; }
	}

}
