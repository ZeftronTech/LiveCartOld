using LiveKart.Business.UTCInfo;
using LiveKart.LogService;
using LiveKart.Shared;
using LiveKart.Entities;
//using LiveKart.Shared.Entities;
using LiveKart.Shared.Localization;
using LiveKart.Web.Filters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LiveKart.Web.Controllers
{
	/// <summary>
	/// Class BaseController
	/// </summary>
	[AuthorizationFilter()]
	public abstract class BaseController : LocalizedControllerBase
	{

		/// <summary>
		/// The logging
		/// </summary>
		ILogService logging = new FileLogService(typeof(BaseController));
		/// <summary>
		/// Gets the current user.
		/// </summary>
		/// <value>The current user.</value>
		protected string CurrentUser
		{
			get
			{
				return User.Identity.Name;
			}
		}

		protected OAuth GimbleAuth
		{
			get
			{
				return (OAuth)Session["OAuth"];
			}
		}

		/// <summary>
		/// Gets the current company.
		/// </summary>
		/// <value>The current company.</value>
		protected Company CurrentCompany
		{
			get
			{
				return (Company)Session["CurrentCompany"];
			}
		}

		/// <summary>
		/// Gets the active user.
		/// </summary>
		/// <value>The active user.</value>
		protected User ActiveUser
		{
			get
			{
				return (User)Session["ActiveUser"];
			}
		}


		/// <summary>
		/// Refreshes the session.
		/// </summary>
		/// <returns>JsonResult.</returns>
		public JsonResult RefreshSession()
		{
			JsonResponse response = new JsonResponse();
			response.Message = "Your session has been expired. Please login again to continue.";
			response.Status = ConstantUtil.StatusInfo;
			try
			{
				int timeout = (Session.Timeout * 60000) - 60000;
				response.Message = string.Empty;
				response.Status = ConstantUtil.StatusOk;
				response.ReturnUrl = timeout.ToString();
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Deletes the file.
		/// </summary>
		/// <param name="relativePath">The relative path.</param>
		/// <param name="imagePath">The image path.</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
		public bool DeleteFile(string relativePath, string imagePath)
		{
			if (!string.IsNullOrEmpty(imagePath))
			{
				string dImg = Server.MapPath(relativePath + imagePath.Split('/').Last());
				if (System.IO.File.Exists(dImg))
				{
					System.IO.File.Delete(dImg);
				}
			}
			return true;
		}

		/// <summary>
		/// Gets the target path.
		/// </summary>
		/// <param name="relativePath">The relative path.</param>
		/// <param name="file">The file.</param>
		/// <returns>System.String.</returns>
		public string GetTargetPath(string relativePath, HttpPostedFileBase file)
		{
			if (!Directory.Exists(Server.MapPath(relativePath)))
			{
				Directory.CreateDirectory(Server.MapPath(relativePath));
			}
			var fileName = GetTargetFileName(file);
			return Path.Combine(Server.MapPath(relativePath), fileName);
		}

		public string GetTargetFileName(HttpPostedFileBase file)
		{
			var fileName = Path.GetExtension(file.FileName);
			string targetName = DateTime.Now.ToString("ddMMyyyyhhmmssffff");
			fileName = targetName + fileName;
			return fileName;
		}

		public DateTime GetUTCTime(DateTime localDateTime)
		{
			return localDateTime.AddMinutes(Convert.ToDouble(UtcPageExt.UtcOffset()));
		}

		public DateTime GetLocalTime(DateTime utcDateTime)
		{
			return UtcPageExt.LocalTimeFromTimeOffset(utcDateTime);
		}
	}
}
