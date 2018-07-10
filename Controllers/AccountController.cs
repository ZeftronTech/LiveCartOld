using LiveKart.Business;
using LiveKart.Business.ConfigSettings;
using LiveKart.Business.Cryptography;
using LiveKart.Business.Email;
using LiveKart.LogService;
using LiveKart.Service;
using Repository.Pattern.UnitOfWork;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using LiveKart.Entities;


using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using LiveKart.Web.Models;
using LiveKart.Shared.Entities;
using LiveKart.Shared;


namespace LiveKart.Web.Controllers
{
	/// <summary>
	/// Class AccountController
	/// </summary>
	[Authorize]
	public class AccountController : Controller
	{
		private readonly IUserService _userService;
		private readonly IUnitOfWork _unitOfWorkAsync;

		public AccountController(IUserService userService, IUnitOfWork unitOfWorkAsync)
		{
			_userService = userService;
			_unitOfWorkAsync = unitOfWorkAsync;

		}

		/// <summary>
		/// The logging
		/// </summary>
		ILogService logging = new FileLogService(typeof(AccountController));

		/// <summary>
		/// The credential cookie name
		/// </summary>
		private string CredentialCookieName = "LiveKartCredentials";

		private string RedirectURL = UrlUtility.GetApplicationUrl() + "/Account/getauthenticationtoken";
		private string GimbleClientId = ConfigSettings.ReadConfigValue("GimbleClientId", "");
		private string GimbleClientSecret = ConfigSettings.ReadConfigValue("GimbleClientSecret", "");
		/// <summary>
		/// Logins 
		/// </summary>
		/// <param name="returnUrl">The return URL.</param>
		/// <returns>Login view.</returns>
		[AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
			Session.Abandon();
			FormsAuthentication.SignOut();
			ViewBag.ReturnUrl = returnUrl;
			ViewBag.Message = "Enter username and password.";
			User myLogin = new User();
			HttpCookie LiveKartCredentials = Request.Cookies[CredentialCookieName];
			if (LiveKartCredentials != null && LiveKartCredentials.Value != "")
			{
				myLogin.UserName = LiveKartCredentials.Value;
				myLogin.RememberMe = true;
			}
			myLogin.Password = string.Empty;
			RevokeOAuthToken();
			return View(myLogin);
		}


		/// <summary>
		/// Login with users credentials
		/// </summary>
		/// <param name="model">User id and password</param>
		/// <param name="returnUrl">The return URL.</param>
		/// <param name="action">The action.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		[AllowAnonymous]
		public ActionResult Login(User model, string returnUrl, string action)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;

			try
			{
				bool isUserExist = _userService.IsUserExist(model.UserName);
				if (isUserExist)
				{
					logging.Info(model.UserName + " try to login.");
					User user = _userService.Login(model.UserName.ToLower(), EncryptionHelper.Encrypt(model.Password));
					if (user != null)
					{
						if (model.RememberMe)
						{
							Response.Cookies.Add(new HttpCookie(CredentialCookieName, model.UserName));
						}
						else
						{
							Response.Cookies.Set(new HttpCookie(CredentialCookieName, null));
						}

						((HttpCookie)Response.Cookies[CredentialCookieName]).HttpOnly = true;
						if (user.LoginID > 0)
						{
							string countryCode = string.Empty;
							if (user.RoleType == 1)   // Admin
							{
								Session["IsAdmin"] = true;
								Session["CurrentCompany"] = null;
								Session["ActiveUser"] = user;
								Session["Photo"] = "";
								FormsAuthentication.SetAuthCookie(model.UserName, false);
								logging.Info("User logged in successfully, Username=" + model.UserName);
								response.Message = "Logged in successfully. Redirecting to users list.....";
								response.Status = ConstantUtil.StatusOk;
								response.ReturnUrl = Url.Action("Index", "Company");
							}
							else if (user.RoleType == 2)  // Company
							{

								var company = _userService
												.Query(x => x.LoginID == user.LoginID)
												.Include(x => x.Company)
												.Select()
												.SingleOrDefault().Company;
								Session["IsAdmin"] = false;
								Session["CurrentCompany"] = company;
								Session["ActiveUser"] = user;
								Session["Photo"] = company.Image;
								FormsAuthentication.SetAuthCookie(model.UserName, false);
								response.Message = "Logged in successfully. Redirecting to campaigns.....";
								response.Status = ConstantUtil.StatusOk;
								response.ReturnUrl = Url.Action("Index", "Notification");
							}
							else
							{
								response.Message = "Unauthorize access.";
							}
							countryCode = string.IsNullOrEmpty(countryCode) ? "US" : countryCode;
							var cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(c => c.Name.EndsWith(countryCode)).FirstOrDefault();
							cultureInfo = cultureInfo == null ? new CultureInfo("en-US") : cultureInfo;
							Thread.CurrentThread.CurrentCulture = cultureInfo;
							((User)Session["ActiveUser"]).CultureCode = cultureInfo.CompareInfo.Name;
						}
						else
							response.Message = "User Name or Password is incorrect.";
					}
					else
						response.Message = "User Name or Password is incorrect.";
				}
				else
					response.Message = "User Name or Password is incorrect.";
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return Json(response, JsonRequestBehavior.AllowGet);

		}


		/// <summary>
		/// Forget password view.
		/// </summary>
		/// <returns>ActionResult.</returns>
		[AllowAnonymous]
		public ActionResult GetPassword()
		{
			return View();
		}


		/// <summary>
		/// Email the password if user forgets it.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		[AllowAnonymous]
		public ActionResult GetPassword(User model)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				string email = model.UserName;//Request["RememberEmail"].ToString();
				string password = string.Empty; //ServeAR.GetPassword(email);
				if (string.IsNullOrEmpty(password))
				{
					response.Message = "Email does not exist.";
				}
				else
				{
					logging.Info(email + " requested password.");
					bool sendPassword = EmailHelper.SendEmail("AR Password", email, "AR password is: " + EncryptionHelper.Decrypt(password));
					if (sendPassword)
					{
						logging.Info("Password sent successfully to " + email);
						response.Message = "Your Password has been sent to your email.";
						response.Status = ConstantUtil.StatusOk;
						response.ReturnUrl = Url.Action("Login");
					}
					else
					{
						logging.Info("Password sent failed to " + email + ".Some problem occurred during sending email.");
						response.Message = "Email sending fail.";
						response.Status = ConstantUtil.StatusFail;
						response.ReturnUrl = Url.Action("Login");
					}
				}
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
				return Json(response, JsonRequestBehavior.AllowGet);
			}
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Changes the password.
		/// </summary>
		/// <param name="returnUrl">The return URL.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult ChangePassword(string returnUrl)
		{
			ViewData["pagename"] = "profile";
			ViewBag.ReturnUrl = returnUrl;
			return View();
		}

		/// <summary>
		/// Changes the password.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <param name="returnUrl">The return URL.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult ChangePassword(ChangePassword model, string returnUrl)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					response.Message = "Current Password is Incorrect. Please try again.";
					response.Status = ConstantUtil.StatusFail;

					if (_userService.ChangePassword(User.Identity.Name.ToLower(), EncryptionHelper.Encrypt(model.CurrentPassword), EncryptionHelper.Encrypt(model.NewPassword)) != null)
					{
						_unitOfWorkAsync.SaveChanges();
						if (_userService.Login(User.Identity.Name.ToLower(), EncryptionHelper.Encrypt(model.NewPassword)) != null)
						{
							logging.Info(User.Identity.Name + " changed their password successfully.");
							response.Status = ConstantUtil.StatusOk;
							response.ReturnUrl = returnUrl;
							response.Message = "Password changed successfully";
						}
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
		/// Getauthenticationtokens the specified code.
		/// </summary>
		/// <param name="code">The code.</param>
		/// <param name="clientId">The client id.</param>
		/// <param name="clientSecret">The client secret.</param>
		/// <returns>OAuth.</returns>
		[HttpGet]
		public ActionResult getauthenticationtoken(string code)
		{
			OAuth response = new OAuth();
			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://getfyx.com/oauth/token");
			request.AllowWriteStreamBuffering = true;
			request.Method = "POST";
			string post = "code=" + code +
						  "&client_id=" + GimbleClientId +
						  "&grant_type=authorization_code" +
						  "&client_secret=" + GimbleClientSecret +
						  "&redirect_uri=" + RedirectURL;

			byte[] b1 = System.Text.Encoding.ASCII.GetBytes(post);
			request.ContentLength = b1.Length;
			request.ContentType = "application/x-www-form-urlencoded";

			Stream writer = request.GetRequestStream();
			writer.Write(b1, 0, b1.Length);
			var httpResponse = (HttpWebResponse)request.GetResponse();
			using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
			{
				var result = streamReader.ReadToEnd();
				response = JsonHelper.JsonDeserializer<OAuth>(result);
			}
			Session["OAuth"] = response;
			return RedirectToAction("Index", "Notification");
		}

		/// <summary>
		/// Revoke authentication token
		public void RevokeOAuthToken()
		{
			try
			{
				if (Session["OAuth"] != null)
				{
					HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://getfyx.com/oauth/token?access_token=" + ((OAuth)Session["OAuth"]).access_token);
					request.AllowWriteStreamBuffering = true;
					request.Method = "DELETE";
					var httpResponse = (HttpWebResponse)request.GetResponse();
				}
			}
			catch { }
		}
	}
}
