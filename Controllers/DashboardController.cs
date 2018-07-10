using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Mvc;
using LiveKart.Business;
using Repository.Pattern.UnitOfWork;


namespace LiveKart.Web.Controllers
{
	public class DashboardController : BaseController
	{
		private readonly IUnitOfWork _unitOfWorkAsync;

		public DashboardController(IUnitOfWork unitOfWorkAsync)
			: base()
		{
			_unitOfWorkAsync = unitOfWorkAsync;
		}

		string apiAccessCode = "CGPMGNXD3ZP2F7YYN9VJ";
		string apiKey = "QQKZN5QCJD2C25HMSJMF";
		long max01 = 0;
		string max1 = "";
		string fromDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
		string NotiyearMw1 = DateTime.Now.AddDays(-31).ToString("yyyy-MM-dd");
		string NotiyearMw2 = DateTime.Now.AddDays(-23).ToString("yyyy-MM-dd");
		string NotiyearMw3 = DateTime.Now.AddDays(-15).ToString("yyyy-MM-dd");
		string NotiyearMw4 = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
		string NotiyearME1 = DateTime.Now.AddDays(-24).ToString("yyyy-MM-dd");
		string NotiyearME2 = DateTime.Now.AddDays(-16).ToString("yyyy-MM-dd");
		string NotiyearME3 = DateTime.Now.AddDays(-8).ToString("yyyy-MM-dd");
		string NotiyearME4 = DateTime.Now.ToString("yyyy-MM-dd");

		string endDate = DateTime.Now.ToString("yyyy-MM-dd");
		int iid;

		public ActionResult Dashboard()
		{
			iid = Convert.ToInt32(Session["IID"]);
			ViewData["pagename"] = "dashboard";
			Tellmefromdate(iid);
			GetFlurryCount("NewUsers");
			GetFlurryCount("Sessions");
			GetFlurryCount("ActiveUsers");
			var abc = ViewBag.Session;
			var cd = ViewBag.NewUser;
			var ef = ViewBag.UniqueUser;
			GetFlurryConversionNotifCount("Conversion");
			GetFlurryConversionNotifCount("CloseNotificationReceived");
			GetFlurryConversionNotifCount("NearNotificationReceived");
			ViewBag.totalNotification = ViewBag.NearNotificationR + ViewBag.CloseNotificationR;
			GetFlurryOfferViewRated("OfferSaved");
			GetFlurryOfferViewRated("OfferViewed");
			GetFlurryOfferViewRated("OfferRated");
			ViewBag.fromDate = fromDate;
			ViewBag.endDate = endDate;
			return View();
		}

		public ActionResult StartUp()
		{
			return View();
		}

		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);

		private static long ConvertToTimestamp(DateTime value)
		{
			TimeSpan elapsedTime = value - Epoch;
			return (long)elapsedTime.TotalMilliseconds;
		}

		public JsonResult GetFlurryCount(string metric)
		{
			FlurryResponse response = new FlurryResponse();
			LineGraph graph = new LineGraph();
			try
			{
				DateTime dt = DateTime.Now.AddSeconds(10);
				while (DateTime.Now > dt) { }

				string url = "http://api.flurry.com/appMetrics/" + metric + "?apiAccessCode=" + apiAccessCode + "&apiKey=" + apiKey + "&startDate=" + fromDate + "&endDate=" + endDate;

				HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(url);
				var httpResponse = (HttpWebResponse)httpWReq.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var result = streamReader.ReadToEnd();
					result = result.Replace("@generatedDate", "generatedDate").Replace("@day", "day").Replace("@date", "date").Replace("@value", "value").Replace("@totalCount", "totalCount")
							.Replace("@name", "name").Replace("@type", "type").Replace("@metric", "metric").Replace("@eventName", "eventName").Replace("@version", "version");
					response = JsonHelper.JsonDeserializer<FlurryResponse>(result);
				}
				long count = 0;
				long max = 0;
				DateTime maxFootfallDate = DateTime.Now;
				foreach (Day day in response.day)
				{
					count += day.value;
					if (max < day.value)
					{
						max = day.value;
						maxFootfallDate = Convert.ToDateTime(day.date);
					}
				}
				if (metric == "NewUsers")
				{
					ViewBag.NewUser = count;
					ViewBag.MaxFootfallDate = maxFootfallDate;
				}
				else if (metric == "ActiveUsers")
				{
					ViewBag.UniqueUser = count;
				}
				else
				{
					ViewBag.Session = count;
				}

				List<decimal[,]> graphData = new List<decimal[,]>();
				response.day = response.day.Count > 10 ? response.day.OrderByDescending(d => d.date).Take(10).ToList() : response.day;

				if (metric == "NewUsers")
				{
					graphData.Add(new decimal[1, 2] { { count, 2 } });
					graph.data = graphData;
					graph.label = "Annonymous Users";
					ViewBag.LineNewUser = graph;
				}
				else if (metric == "ActiveUsers")
				{
					graphData.Add(new decimal[1, 2] { { count, 1 } });
					graph.data = graphData;
					graph.label = "Registered User";
					ViewBag.LineUniqueUser = graph;
				}
				else
				{
					graph.label = "Session";
				}
				Thread.Sleep(2000);

			}
			catch
			{
				//////// GetActiveUsers("week");
			}

			return Json(graph, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetFlurryOfferViewRated(string metric)
		{
			FlurryResponse response = new FlurryResponse();
			LineGraph graph = new LineGraph();

			LineGraph Chart = new LineGraph();
			try
			{
				DateTime dt = DateTime.Now.AddSeconds(10);
				while (DateTime.Now > dt) { }

				string url = "http://api.flurry.com/eventMetrics/Event?apiAccessCode=" + apiAccessCode + "&apiKey=" + apiKey + "&startDate=" + fromDate + "&endDate=" + endDate + "&eventName=" + metric;

				HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(url);
				var httpResponse = (HttpWebResponse)httpWReq.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var result = streamReader.ReadToEnd();
					result = result.Replace("@generatedDate", "generatedDate").Replace("@day", "day").Replace("@date", "date").Replace("@value", "value").Replace("@totalCount", "totalCount")
							.Replace("@name", "name").Replace("@type", "type").Replace("@metric", "metric").Replace("@eventName", "eventName").Replace("@version", "version");
					response = JsonHelper.JsonDeserializer<FlurryResponse>(result);
				}
				long count = 0;
				foreach (Day day in response.day)
				{
					count += day.totalCount;
				}

				if (metric == "OfferViewed")
				{
					ViewBag.OfferViewed = count;
				}
				else if (metric == "OfferSaved")
				{
					ViewBag.OfferSaved = count;
				}
				else
				{
					ViewBag.OfferRated = count;
				}

				List<decimal[,]> graphData = new List<decimal[,]>();
				response.day = response.day.Count > 10 ? response.day.OrderByDescending(d => d.date).Take(10).ToList() : response.day;
				foreach (Day day in response.day.OrderByDescending(d => d.date).Take(10))
				{
					graphData.Add(new decimal[1, 2] { { day.totalCount, ConvertToTimestamp(DateTime.ParseExact(day.date, "yyyy-M-d", CultureInfo.InvariantCulture)) } });
				}
				graph.data = graphData;

				if (metric == "OfferViewed")
				{
					Key actions = (from result in response.parameters.key where result.name == "campaignId" select result).FirstOrDefault();
					foreach (Value k in actions.value)
					{
						if (max01 < Convert.ToDecimal(k.totalCount))
						{
							max01 = k.totalCount;
							max1 = k.name;
						}
					}

					var notification = _unitOfWorkAsync.Repository<Entities.Notification>().Find(Convert.ToInt32(max1));
					ViewBag.topOffer = notification.NotificationTitle;
					string NotiImg = notification.NotificationImage;
					ViewBag.NotificationImg = NotiImg;

					graph.label = "Viewed";
					ViewBag.LineOfferViewed = graph;

				}
				else if (metric == "OfferSaved")
				{
					graph.label = "Saved";
					ViewBag.LineOfferSaved = graph;

				}
				else
				{
					graph.label = "Rated";
					ViewBag.LineOfferRated = graph;
				}

				Thread.Sleep(2000);
			}
			catch
			{
				// GetActiveUsers("week");
			}
			return Json(graph, JsonRequestBehavior.AllowGet);
		}

		public JsonResult GetFlurryConversionNotifCount(string metric)
		{
			FlurryResponse response = new FlurryResponse();
			LineGraph graph = new LineGraph();

			LineGraph Chart = new LineGraph();
			//LineGraph chart = new LineGraph();
			try
			{
				DateTime dt = DateTime.Now.AddSeconds(10);
				while (DateTime.Now > dt) { }
				string url = "http://api.flurry.com/eventMetrics/Event?apiAccessCode=" + apiAccessCode + "&apiKey=" + apiKey + "&startDate=" + fromDate + "&endDate=" + endDate + "&eventName=" + metric;
				HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(url);
				var httpResponse = (HttpWebResponse)httpWReq.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var result = streamReader.ReadToEnd();
					result = result.Replace("@generatedDate", "generatedDate").Replace("@day", "day").Replace("@date", "date").Replace("@value", "value").Replace("@totalCount", "totalCount")
							.Replace("@name", "name").Replace("@type", "type").Replace("@metric", "metric").Replace("@eventName", "eventName").Replace("@version", "version");
					response = JsonHelper.JsonDeserializer<FlurryResponse>(result);
				}
				long count = 0;
				foreach (Day day in response.day)
				{
					count += day.totalCount;
				}

				if (metric == "NearNotificationReceived")
				{
					ViewBag.NearNotificationR = count;
				}
				else if (metric == "CloseNotificationReceived")
				{
					ViewBag.closeNotificationR = count;
				}
				else if (metric == "FarNotificationReceived")
				{
					ViewBag.FarNotificationR = count;
				}
				else
				{
					ViewBag.totalConversion = count;
				}

				if (metric == "Conversion")
				{

				}
				else
				{
					List<decimal[,]> graphData = new List<decimal[,]>();
					response.day = response.day.Count > 10 ? response.day.OrderByDescending(d => d.date).Take(10).ToList() : response.day;
					foreach (Day day in response.day.OrderByDescending(d => d.date).Take(10))
					{
						graphData.Add(new decimal[1, 2] { { ConvertToTimestamp(DateTime.ParseExact(day.date, "yyyy-M-d", CultureInfo.InvariantCulture)), day.totalCount } });
					}
					graph.data = graphData;

					List<decimal[,]> ChartData = new List<decimal[,]>();
					response.day = response.day.Count > 10 ? response.day.OrderByDescending(d => d.date).Take(10).ToList() : response.day;
					foreach (Day day in response.day.OrderByDescending(d => d.date).Take(10))
					{
						ChartData.Add(new decimal[1, 2] { { ConvertToTimestamp(DateTime.ParseExact(day.date, "yyyy-M-d", CultureInfo.InvariantCulture)), day.totalCount } });
					}
					Chart.data = ChartData;


					if (metric == "CloseNotificationReceived")
					{
						graph.label = "Notified";
						ViewBag.LineCloseNotification = graph;

						Chart.label = "Close";
						ViewBag.LineCloseNotificationS = Chart;
					}
					else if (metric == "NearNotificationReceived")
					{
						graph.label = "Engaged";
						ViewBag.LineNearNotification = graph;

						Chart.label = "Near";
						ViewBag.LineNearNotificationS = Chart;
					}
					else // if (metric == "FarNotificationRecieved")
					{
						graph.label = "Engaged";
						ViewBag.LineFarNotification = graph;

						Chart.label = "Near";
						ViewBag.LineFarNotificationS = Chart;
					}
				}

				Thread.Sleep(2000);
			}
			catch
			{
				// GetActiveUsers("week");
			}
			return Json(graph, JsonRequestBehavior.AllowGet);
		}

		public ActionResult Tellmefromdate(int id)
		{
			if (id == 7)
			{
				fromDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
				Session["IID"] = 7;
			}
			else if (id == 30)
			{
				fromDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
				Session["IID"] = 30;
			}
			else if (id == 60)
			{
				fromDate = DateTime.Now.AddDays(-60).ToString("yyyy-MM-dd");
				Session["IID"] = 60;
			}
			else if (id == 90)
			{
				fromDate = DateTime.Now.AddDays(-90).ToString("yyyy-MM-dd");
				Session["IID"] = 90;
			}
			else
			{
				fromDate = DateTime.Now.AddDays(-4).ToString("yyyy-MM-dd");
				Session["IID"] = 4;
			}
			return RedirectToAction("Dashboard", "Dashboard");
		}
	}

	public class PieChart
	{
		public string label { get; set; }

		public decimal data { get; set; }
	}

	public class LineGraph
	{
		public string label { get; set; }

		public List<decimal[,]> data { get; set; }
	}

	public class Day
	{
		public string @date { get; set; }
		public long @value { get; set; }

		public long @totalCount { get; set; }

		public long @totalSessions { get; set; }

		public long @uniqueUsers { get; set; }
	}

	public class Value
	{
		public long @totalCount { get; set; }

		public string @name { get; set; }
	}

	public class Key
	{
		public string @name { get; set; }

		public List<Value> @value { get; set; }
	}

	public class Parameters
	{
		public List<Key> @key { get; set; }
	}

	public class FlurryResponse
	{
		public string @generatedDate { get; set; }

		public string @type { get; set; }

		public string @metric { get; set; }

		public string @eventName { get; set; }

		public string @version { get; set; }

		public List<Day> @day { get; set; }

		public Parameters @parameters { get; set; }
	}
}
