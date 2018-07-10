using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LiveKart.Business.CSV;
using LiveKart.LogService;
using LiveKart.Repository;
using LiveKart.Shared;
using LiveKart.Shared.Entities;
using Repository.Pattern.UnitOfWork;


namespace LiveKart.Web.Controllers
{
	/// <summary>
	/// Class BeaconController
	/// </summary>
	public class BeaconController : BaseController
	{
		private readonly IUnitOfWork _unitOfWorkAsync;

		public BeaconController(IUnitOfWork unitOfWorkAsync)
			: base()
		{
			_unitOfWorkAsync = unitOfWorkAsync;
		}

		private const string TempPath = "~/Content/tempdata/";

		/// <summary>
		/// The loggingE:\LiveKart\Main\Livekart\LiveKart\LiveKart.Web\Controllers\BeaconController.cs
		/// </summary>
		ILogService logging = new FileLogService(typeof(BeaconController));

		#region Beacon

		/// <summary>
		/// Returns list of all beacons
		/// </summary>
		/// <returns>beacons list.</returns>
		public ActionResult Index()
		{
			ViewData["pagename"] = "managebeacon";
			var beacons = _unitOfWorkAsync.Repository<Entities.Beacon>()
				.Query(b => b.CompanyID == CurrentCompany.CompanyId)
				.Select().ToList();
			return View(beacons);
		}

		public ActionResult Upload()
		{
			ViewData["pagename"] = "managebeacon";
			return View();
		}

		[HttpPost]
		public ActionResult Upload(HttpPostedFileBase file)
		{
			var beacon = new Entities.Beacon();
			JsonResponse response = new JsonResponse();

			if (file == null)
			{
				ViewBag.AlertMessage = "Please select a .csv File";
			}
			else
			{
				//TO:DO
				var fileName = Path.GetFileName(file.FileName);
				var path = Path.Combine(Server.MapPath("~/Content/Upload"), fileName);
				file.SaveAs(path);
				ModelState.Clear();
				ViewBag.Message = "File uploaded successfully";

				using (LiveKart.Business.CSV.ReadWriteCSV.CsvFileReader reader = new ReadWriteCSV.CsvFileReader(path))
				{
					LiveKart.Business.CSV.ReadWriteCSV.CsvRow row = new LiveKart.Business.CSV.ReadWriteCSV.CsvRow();
					long count = 0;
					long dcount = 0;
					long ccount = 0;
					while (reader.ReadRow(row))
					{
						beacon.BeaconID = row[0].ToString();
						beacon.BeaconName = row[1].ToString();
						beacon.Description = row[2].ToString();

						beacon.Active = true;
						beacon.CreatedBy = CurrentUser;
						beacon.CreatedDate = DateTime.UtcNow;
						beacon.CompanyID = CurrentCompany.CompanyId;

						var existingBeacon = _unitOfWorkAsync.Repository<Entities.Beacon>().GetByBeaconNum(beacon.BeaconID);
						if (existingBeacon == null)
						{
							_unitOfWorkAsync.Repository<Entities.Beacon>().Insert(beacon);
							var success = _unitOfWorkAsync.SaveChanges() > 0;
							if (success)
							{
								count = count + 1;
							}
							else
							{
								dcount = dcount + 1;
							}
						}
						else
						{
							ccount = ccount + 1;
						}
					}
					if (dcount == 0 && count > 0 && ccount == 0)
					{
						ViewBag.AlertMessage = " Beacons created sucessfully.";
					}
					else if (count > 0 && dcount > 0 && ccount == 0)
					{
						ViewBag.AlertMessage = count + " Beacons created sucessfully and " + dcount + " Beacons not created.";
					}
					else if (count > 0 && dcount > 0 && ccount > 0)
					{
						ViewBag.AlertMessage = count + " Beacons created sucessfully ,  " + dcount + " Beacons not created. and " + ccount + " beacons already exists.";
					}
					else if (count > 0 && dcount == 0 && ccount > 0)
					{
						ViewBag.AlertMessage = count + " Beacons created sucessfully and  " + ccount + " beacons already exists.";
					}
					else if (count == 0 && dcount == 0 && ccount > 0)
					{
						ViewBag.AlertMessage = "Beacons already Exists";
					}
				}
			}
			return View();
		}

		/// <summary>
		/// Create beacon
		/// </summary>
		/// <returns>beacon view</returns>
		public ActionResult Create()
		{
			ViewData["pagename"] = "managebeacon";
			return View();
		}

		/// <summary>
		/// Create beacon
		/// </summary>
		/// <param name="beacon">Beacon object</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Create(Entities.Beacon beacon)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					beacon.Active = true;
					beacon.CreatedBy = CurrentUser;
					beacon.CreatedDate = DateTime.UtcNow;
					beacon.CompanyID = CurrentCompany.CompanyId;

					var existingBeacon = _unitOfWorkAsync.Repository<Entities.Beacon>().GetByBeaconNum(beacon.BeaconID);
					if (existingBeacon == null)
					{
						_unitOfWorkAsync.Repository<Entities.Beacon>().Insert(beacon);
						var success = _unitOfWorkAsync.SaveChanges() > 0;
						if (success)
						{
							response.Message = "Beacon created successfully.";
							response.Status = ConstantUtil.StatusOk;
							response.ReturnUrl = Url.Action("Index");
						}
					}
					else
					{
						response.Message = "Beacon already exist.";
					}
				}
				catch (Exception ex)
				{
					logging.Error(ex.ToString());
				}
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Edits beacon.
		/// </summary>
		/// <param name="id">Beacon id.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult Edit(long id)
		{
			ViewData["pagename"] = "managebeacon";
			var beacon = _unitOfWorkAsync.Repository<Entities.Beacon>().Find(id);
			return View(beacon);
		}

		/// <summary>
		/// Edits beacon.
		/// </summary>
		/// <param name="id">beacon id.</param>
		/// <param name="beacon">Beacon object</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Edit(long id, Entities.Beacon beacon)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					var newBeacon = _unitOfWorkAsync.Repository<Entities.Beacon>().Find(beacon.Id);
					newBeacon.ModifiedBy = CurrentUser;
					newBeacon.ModifiedDate = DateTime.UtcNow;
					newBeacon.CompanyID = CurrentCompany.CompanyId;
					newBeacon.BeaconName = beacon.BeaconName;
					newBeacon.Description = beacon.Description;
					newBeacon.Active = beacon.Active;
					_unitOfWorkAsync.Repository<Entities.Beacon>().Update(newBeacon);
					var success = _unitOfWorkAsync.SaveChanges() > 0;
					if (success)
					{
						response.Message = "Beacon updated successfully.";
						response.Status = ConstantUtil.StatusOk;
						response.ReturnUrl = Url.Action("Index");
					}
				}

				catch (Exception ex)
				{
					logging.Error(ex.ToString());
				}
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Deletes the specified beacon id.
		/// </summary>
		/// <param name="id">beacon id.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Delete(long id)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					_unitOfWorkAsync.Repository<Entities.Beacon>().Delete(id);
					var success = _unitOfWorkAsync.SaveChanges() > 0;
					if (success)
					{
						response.Message = "Beacon deleted successfully.";
						response.Status = ConstantUtil.StatusOk;
						response.ReturnUrl = Url.Action("Index");
					}
				}
				catch (Exception ex)
				{
					logging.Error(ex.ToString());
				}
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		#endregion

		#region Deployment

		/// <summary>
		/// Deletes the specified beacon Deployment id.
		/// </summary>
		/// <param name="id">beacon Deployment id.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult DeleteDeployment(long id)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					_unitOfWorkAsync.Repository<Entities.BeaconDeployment>().Delete(id);
					var success = _unitOfWorkAsync.SaveChanges() > 0;
					if (success)
					{
						response.Message = "Beacon Deployment deleted successfully.";
						response.Status = ConstantUtil.StatusOk;
						response.ReturnUrl = Url.Action("Deployed");
					}
				}
				catch (Exception ex)
				{
					logging.Error(ex.ToString());
				}
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}


		public ActionResult EditDeployment(long id)
		{
			ViewData["pagename"] = "managebeacon";
			ViewBag.Assets = _unitOfWorkAsync.Repository<LiveKart.Entities.Asset>()
				.Query(x => x.CompanyID == CurrentCompany.CompanyId && x.Active == true)
				.Select().ToList();
			var beaconDeployment = _unitOfWorkAsync.Repository<Entities.BeaconDeployment>().Find(id);
			return View(beaconDeployment);
		}

		[HttpPost]
		public ActionResult EditDeployment(long id, Entities.BeaconDeployment beaconDeployment)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				var newBeaconDeployment = _unitOfWorkAsync.Repository<Entities.BeaconDeployment>().Find(beaconDeployment.BeaconDeploymentID);
				newBeaconDeployment.ModifiedBy = CurrentUser;
				newBeaconDeployment.ModifiedDate = DateTime.UtcNow;
				newBeaconDeployment.CompanyID = CurrentCompany.CompanyId;
				newBeaconDeployment.AssetID = beaconDeployment.AssetID;
				_unitOfWorkAsync.Repository<Entities.BeaconDeployment>().Update(newBeaconDeployment);
				var success = _unitOfWorkAsync.SaveChanges() > 0;
				if (success)
				{
					response.Message = "BeaconDeployment updated successfully.";
					response.Status = ConstantUtil.StatusOk;
					response.ReturnUrl = Url.Action("Deployed");
				}
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Get list of all deployed beacons
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Deployed()
		{
			ViewData["pagename"] = "managebeacon";
			var beaconsDeployment = _unitOfWorkAsync.Repository<Entities.BeaconDeployment>()
					.Query(b => b.CompanyID == CurrentCompany.CompanyId)
					.Include(b => b.Beacon)
					.Include(b => b.Asset)
					.Include(b => b.Asset.AssetCategory)
					.Select().ToList();
			return View(beaconsDeployment);
		}

		/// <summary>
		/// Deploys the specified beacon id.
		/// </summary>
		/// <param name="beaconId">The beacon id.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult Deploy(long beaconId = 0)
		{
			ViewData["pagename"] = "managebeacon";

			List<Entities.Beacon> beacons = _unitOfWorkAsync.Repository<Entities.Beacon>()
				.Query(b => b.CompanyID == CurrentCompany.CompanyId && b.Active == true && b.BeaconDeployments.Count == 0)
				.Select().ToList();

			ViewBag.Beacons = beacons;
			ViewBag.Assets = _unitOfWorkAsync.Repository<Entities.Asset>()
				.Query(x => x.CompanyID == CurrentCompany.CompanyId && x.Active == true)
				.Select().ToList();
			return View(new Entities.BeaconDeployment { BeaconID = beaconId });
		}

		/// <summary>
		/// Deploys the specified beacon.
		/// </summary>
		/// <param name="model">Beacon object</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Deploy(BeaconDeployment model)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					var beacon = _unitOfWorkAsync.Repository<Entities.Beacon>().GetByBeaconNum(model.BeaconId);

					var beaconDeployment = new Entities.BeaconDeployment
					{
						AssetID = model.AssetId,
						CompanyID = CurrentCompany.CompanyId,
						BeaconID = beacon.Id,
						CreatedBy = CurrentUser,
						CreatedDate = DateTime.UtcNow,
						Active = false
					};
					_unitOfWorkAsync.Repository<Entities.BeaconDeployment>().Insert(beaconDeployment);
					var success = _unitOfWorkAsync.SaveChanges() > 0;
					if (success)
					{
						response.Message = "Beacon deployed successfully.";
						response.Status = ConstantUtil.StatusOk;
						response.ReturnUrl = Url.Action("Deployed");
					}
				}
				catch (Exception ex)
				{
					logging.Error(ex.ToString());
				}
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Activates the specified beacon id to get targeted by campaigns.
		/// </summary>
		/// <param name="id">Beacon id.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Activate(long id)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				var beaconDeployment = _unitOfWorkAsync.Repository<Entities.BeaconDeployment>().Find(id);
				beaconDeployment.Active = !beaconDeployment.Active;
				_unitOfWorkAsync.Repository<Entities.BeaconDeployment>().Update(beaconDeployment);
				var success = _unitOfWorkAsync.SaveChanges() > 0;
				if (success && beaconDeployment.Active)
				{
					response.Message = "Beacon have been activate successfully.";
					response.Status = ConstantUtil.StatusOk;
				}
				else if (success && !beaconDeployment.Active)
				{
					response.Message = "Beacon have been De-Activate successfully .";
					response.Status = ConstantUtil.StatusOk;
				}

			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		public bool IsBeaconExistOnGimble(string beaconId, string operation)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://api.getfyx.com/api/v1/transmitters/" + beaconId + "?access_token=" + GimbleAuth.access_token);
				request.AllowWriteStreamBuffering = true;
				request.Method = "GET";
				var httpResponse = (HttpWebResponse)request.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var result = streamReader.ReadToEnd();
					if (result.ToString().Contains("error") && result.ToString().Contains("4002"))
					{
						return false;
					}
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		public bool ActivateBeacononGimble(string beaconId, string displayName, string operation)
		{
			HttpWebRequest request;
			string post = string.Empty;
			try
			{
				if (operation == "create")
				{
					request = (HttpWebRequest)HttpWebRequest.Create("https://api.getfyx.com/api/v1/transmitters?access_token=" + GimbleAuth.access_token);
					post = "{\"factory_id\": \"" + beaconId + "\"," +
							  "\"display_name\": \"" + displayName + "\"}";
					request.Method = "POST";
				}
				else if (operation == "edit")
				{
					request = (HttpWebRequest)HttpWebRequest.Create("https://api.getfyx.com/api/v1/transmitters/" + beaconId + "?access_token=" + GimbleAuth.access_token);
					post = "{\"display_name\": \"" + displayName + "\"}";
					request.Method = "PUT";
				}
				else
				{
					request = (HttpWebRequest)HttpWebRequest.Create(" https://api.getfyx.com/api/v1/transmitters/" + beaconId + "?access_token=" + GimbleAuth.access_token);
					request.Method = "DELETE";
				}
				if (operation != "delete")
				{
					request.AllowWriteStreamBuffering = true;
					byte[] b1 = System.Text.Encoding.ASCII.GetBytes(post);
					request.ContentLength = b1.Length;
					request.ContentType = "application/json";
					Stream writer = request.GetRequestStream();
					writer.Write(b1, 0, b1.Length);
				}
				var httpResponse = (HttpWebResponse)request.GetResponse();
				using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
				{
					var result = streamReader.ReadToEnd();
					if (result.ToString() == "{}")
						return true;
				}
			}
			catch { }
			return false;
		}
		#endregion
	}
}
