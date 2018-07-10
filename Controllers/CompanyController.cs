// ***********************************************************************
// Assembly         : LiveKart.Web
// Author           : Ajit
// Created          : 12-20-2013
//
// Last Modified By : Ajit
// Last Modified On : 12-23-2013
// ***********************************************************************
// <copyright file="CompanyController.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LiveKart.Business.Email;
using LiveKart.Shared;
using LiveKart.Shared.Entities;
using LiveKart.Business.Cryptography;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using LiveKart.Business.Randoms;
using LiveKart.LogService;
using LiveKart.Business.ImageUtility;
using LiveKart.Business;
using LiveKart.Service;
using Repository.Pattern.UnitOfWork;
using LiveKart.Entities;
using Repository.Pattern.Infrastructure;

namespace LiveKart.Web.Controllers
{
	/// <summary>
	/// Class CompanyController
	/// </summary>
	public class CompanyController : BaseController
	{

	
		private readonly IUserService _userService;
		private readonly ICompanySevice _companyService;
		private readonly IUnitOfWork _unitOfWorkAsync;

		public CompanyController(IUserService userService, ICompanySevice companySevice, IUnitOfWork unitOfWorkAsync)
			: base( )
		{
			_companyService = companySevice;
			_userService = userService;
			_unitOfWorkAsync = unitOfWorkAsync;
		}

		/// <summary>
		/// The logging
		/// </summary>
		ILogService logging = new FileLogService(typeof(CompanyController));
		/// <summary>
		/// The company images path
		/// </summary>
		private const string CompanyImagesPath = "~/Content/companyimages/";

		/// <summary>
		/// The temp path
		/// </summary>
		private const string TempPath = "~/Content/tempdata/";

		//
		// GET: /Company/


		/// <summary>
		/// List of all the companies registered
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Index()
		{
			try
			{

				var lstComp = new List<Company>() { };
				//var lstComp = _company.GetCompanies().OrderByDescending(s => s.CompanyId).ToList();

				var test = _companyService.ODataQueryable();

				ViewData["pagename"] = "company";
				return View("Index", test.ToList());
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());

			}
			return View();
		}
		//[HttpPost]
		/// <summary>
		/// Creates company.
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Create()
		{
			try
			{
				ViewData["pagename"] = "company";
				return View();
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
			}
			return View();

		}

		/// <summary>
		/// Creates company.
		/// </summary>
		/// <param name="model">Company object</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Create(Company model)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			ViewData["pagename"] = "company";
			if (ModelState.IsValid)
			{
				try
				{
					model.Active = true;
					if (_userService.IsUserExist(model.Email))
					{
						response.Message = ConstantUtil.UaernameExist;
					}
					else
					{
						if (!string.IsNullOrEmpty(model.Image) 
                            && !System.IO.File.Exists(Server.MapPath(CompanyImagesPath) + model.Image.Split('/').Last())
                            && System.IO.File.Exists(Server.MapPath(TempPath) + model.Image.Split('/').Last()))

						{
							System.IO.File.Copy(Server.MapPath(TempPath) + model.Image.Split('/').Last(), Server.MapPath(CompanyImagesPath) + model.Image.Split('/').Last());
							model.Image = UrlUtility.GetAbsoluteUrl(CompanyImagesPath + model.Image.Split('/').Last());
						}

						string password = model.Password;
						model.Password = EncryptionHelper.Encrypt(model.Password);
						model.CreatedBy = CurrentUser;
						model.CreatedDate = DateTime.UtcNow;
						model.Active = true;
						model.RoleId = 2;
						//bool success = _company.CreateCompany(model);
						var companyAdmin = new LiveKart.Entities.User()
						{
							UserName = model.UserName,
							Email = model.Email,
							Password = model.Password,
							RoleType = model.RoleId.GetValueOrDefault(2),
							CreatedDate = DateTime.UtcNow,
						};

						var companyEntity = new LiveKart.Entities.Company()
												{
													CompanyId =				 model.CompanyId,
													CompanyName =			 model.CompanyName,
													ContactPerson =			 model.ContactPerson,
													Address1 =				 model.Address1,
													Address2 =				 model.Address2,
													State =					 model.State,
													City =					 model.City,
													Zip =					 model.Zip,
													CountryID =				 model.CountryID,
													Phone =					 model.Phone,
													Email =					 model.Email,
													UserName =				 model.UserName,
													Active =				 model.Active,
													CreatedBy =				 model.CreatedBy,
													CreatedDate =			 DateTime.UtcNow,
													ModifiedBy =			 model.ModifiedBy,
													ModifiedDate =			 DateTime.UtcNow,
													Image =				 model.Image,
													VuforiaServerAccessKey = model.VuforiaServerAccessKey,
													VuforiaServerSecretKey = model.VuforiaServerSecretKey,
													VuforiaClientAccessKey = model.VuforiaClientAccessKey,
													VuforiaClientSecretKey = model.VuforiaClientSecretKey,
													User = companyAdmin,
												};
						
						_unitOfWorkAsync.Repository<LiveKart.Entities.User>().Insert(companyAdmin);
						_unitOfWorkAsync.Repository<LiveKart.Entities.Company>().Insert(companyEntity);
						int saveResult = _unitOfWorkAsync.SaveChanges();

						bool success = saveResult >= 0;
						if (success)
						{
							response.Message = "Company created successfully but Password sending failed.";
							response.Status = ConstantUtil.StatusOk;
							response.ReturnUrl = Url.Action("Index");

							logging.Info("New company created with name " + model.CompanyName);
							string appUrl = Business.UrlUtility.GetApplicationUrl();
							string emailbody = "<html xmlns='http://www.w3.org/1999/xhtml'><head><title></title></head><body><h3><i>Greetings!</i></h3>" +
												"<p>Thank you for registering your account with us.</p><p>Your AR user name is: " + model.Email + " </p>" +
												"<p>Password: " + password + "</p><p>Click <a href=" + appUrl + ">here</a> to login</p><p>Thanks again for signing up!</p>" +
												"<p>The AR Team</p></body></html>";
							
							bool sendPassword = EmailHelper.SendEmail("Welcome to LivaKart", model.Email, emailbody);
							if (sendPassword)
							{
								response.Message = "Company created successfully. Password has been sent to their email.";
							}
							else
							{
								logging.Info("Password sending failed to newly created user named " + model.Email);
								response.Message = "Company created successfully but Password sending failed.";
								response.Status = ConstantUtil.StatusFail;
							}
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
		/// Uploads the alert image.
		/// </summary>
		/// <returns>JsonResult.</returns>
		[HttpPost]
		public JsonResult UploadImage()
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				string preImage = Request.Form["Image"].ToString();

				HttpPostedFileBase file = Request.Files[0];
				if (file != null && file.ContentLength > 0)
				{
					string relativePath = TempPath;
					var path = GetTargetPath(relativePath, file);
					System.Drawing.Image streamingImage = System.Drawing.Image.FromStream(file.InputStream);
					Bitmap img = new Bitmap(streamingImage);
					ImageUtility.ResizeandSaveImage(img, 25, 25, 100, path);
					DeleteFile(CompanyImagesPath, preImage);
					var spath = UrlUtility.GetAbsoluteUrl(relativePath + path.Split('\\').Last());
					response.Status = ConstantUtil.StatusOk;
					response.ReturnUrl = spath;
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
		/// Edits the specified company.
		/// </summary>
		/// <param name="model">company object</param>
		/// <param name="id">Company id.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Edit(Company model, long? id)
		{
			ViewData["pagename"] = "company";
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					if (!string.IsNullOrEmpty(model.Image) 
						&& !System.IO.File.Exists(Server.MapPath(CompanyImagesPath) + model.Image.Split('/').Last())
						&& System.IO.File.Exists(Server.MapPath(TempPath) + model.Image.Split('/').Last()))
					{
						System.IO.File.Copy(Server.MapPath(TempPath) + model.Image.Split('/').Last(), Server.MapPath(CompanyImagesPath) + model.Image.Split('/').Last());
						model.Image = UrlUtility.GetAbsoluteUrl(CompanyImagesPath + model.Image.Split('/').Last());
					}
					model.ModifiedBy = CurrentUser;
					model.ModifiedDate = DateTime.UtcNow;

					var companyEntity = _companyService.Find(model.CompanyId);


					companyEntity.CompanyName = model.CompanyName;
					companyEntity.ContactPerson = model.ContactPerson;
					companyEntity.Address1 = model.Address1;
					companyEntity.Address2 = model.Address2;
					companyEntity.State = model.State;
					companyEntity.City = model.City;
					companyEntity.Zip = model.Zip;
					companyEntity.CountryID = model.CountryID;
					companyEntity.Phone = model.Phone;
					companyEntity.Email = model.Email;
					companyEntity.Active = model.Active;
					companyEntity.CreatedDate = DateTime.UtcNow;
					companyEntity.ModifiedBy = model.ModifiedBy;
					companyEntity.ModifiedDate = DateTime.UtcNow;
					companyEntity.Image = model.Image;
					companyEntity.VuforiaServerAccessKey = model.VuforiaServerAccessKey;
					companyEntity.VuforiaServerSecretKey = model.VuforiaServerSecretKey;
					companyEntity.VuforiaClientAccessKey = model.VuforiaClientAccessKey;
					companyEntity.VuforiaClientSecretKey = model.VuforiaClientSecretKey;

					
					companyEntity.ObjectState = ObjectState.Modified;
					_companyService.Update(companyEntity);

					bool success = _unitOfWorkAsync.SaveChanges() > 0;

					if (success)
					{
						logging.Info("Company updated with name " + model.CompanyName);
						response.Message = "Company updated successfully";
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
		/// Detailses the specified company id.
		/// </summary>
		/// <param name="id">company id.</param>
		/// <param name="CountryID">The country ID.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult Details(long? id, long? CountryID)
		{
			try
			{
				if (ActiveUser.RoleType == 2)
				{
					id = Convert.ToInt64(CurrentCompany);
				}

				ViewData["pagename"] = "company";
				
				var lstCompany = _companyService.Find(id);


				ViewBag.CountryName = _unitOfWorkAsync.Repository<LiveKart.Entities.Country>().Find(CountryID).CountryName;

				//ViewBag.CountryName = (from s in _settings.GetCountries().Where(s => s.CountryID == CountryID)
				//					   select s.CountryName).SingleOrDefault();

				return View("Details", lstCompany);
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());

			}
			return View();
		}

		/// <summary>
		/// Edits the specified company.
		/// </summary>
		/// <param name="id">company id.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult Edit(long? id)
		{
			try
			{
				ViewData["pagename"] = "company";
				var company = _companyService.Find(id);
				return View(company);
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());

			}
			return View();
		}

		/// <summary>
		/// Deletes the specified company.
		/// </summary>
		/// <param name="id">company id.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Delete(long id)
		{

			ViewData["pagename"] = "company";

			if (ActiveUser.RoleType != 1)
			{
				return RedirectToAction("Login", "Account");
			}
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			try
			{
				_companyService.Delete(id);

				if (_unitOfWorkAsync.SaveChanges() >= 1)
				{
					logging.Info("Company id " + id.ToString() + " removed.");
					response.Message = "Company deleted successfully";
					response.Status = ConstantUtil.StatusOk;
					return Json(response, JsonRequestBehavior.AllowGet);
				}
				else
				{
					logging.Info("Company id " + id.ToString() + " removal failed.");
					return Json(response, JsonRequestBehavior.AllowGet);
				}
			}
			catch (Exception ex)
			{
				logging.Error(ex.ToString());
				return Json(response, JsonRequestBehavior.AllowGet);
			}
		}
	}
}
