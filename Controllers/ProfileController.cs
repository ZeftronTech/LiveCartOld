// ***********************************************************************
// Assembly         : LiveKart.Web
// Author           : Ajit
// Created          : 12-20-2013
//
// Last Modified By : Ajit
// Last Modified On : 12-23-2013
// ***********************************************************************
// <copyright file="ProfileController.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LiveKart.Shared;
using LiveKart.Web.Filters;
using LiveKart.Shared.Entities;
using System.IO;
using System.Globalization;
using System.Threading;
using LiveKart.LogService;
using System.Drawing;
using LiveKart.Business.ImageUtility;
using LiveKart.Business;
using LiveKart.Service;
using Repository.Pattern.UnitOfWork;
using LiveKart.Web.Models;
using LiveKart.Entities;

namespace LiveKart.Web.Controllers
{
    /// <summary>
    /// Class ProfileController
    /// </summary>
    public class ProfileController : BaseController
    {
        private readonly ICompanySevice _companyService;
        private readonly IUnitOfWork _unitOfWorkAsync;

        public ProfileController(ICompanySevice companySevice, IUnitOfWork unitOfWorkAsync)
            : base()
        {

            _companyService = companySevice;
            _unitOfWorkAsync = unitOfWorkAsync;
        }




        /// <summary>
        /// The logging
        /// </summary>
        ILogService logging = new FileLogService(typeof(ProfileController));
        /// <summary>
        /// The company images path
        /// </summary>
        private const string CompanyImagesPath = "~/Content/companyimages/";

        /// <summary>
        /// The temp path
        /// </summary>
        private const string TempPath = "~/Content/tempdata/";

        /// <summary>
        /// Edits the company.
        /// </summary>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>ActionResult.</returns>
        public ActionResult EditCompany(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View("Company", CurrentCompany);
        }


        /// <summary>
        /// Edits the company.
        /// </summary>
        /// <param name="model">company object</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>ActionResult.</returns>
        [HttpPost]
        public ActionResult EditCompany(Company model, string returnUrl)
        {
            JsonResponse response = new JsonResponse();
            response.Message = ConstantUtil.MessageError;
            response.Status = ConstantUtil.StatusFail;
            try
            {
                if (!string.IsNullOrEmpty(model.Image) 
                    && !System.IO.File.Exists(Server.MapPath(CompanyImagesPath) + model.Image.Split('/').Last())                     
					&& System.IO.File.Exists(Server.MapPath(TempPath) + model.Image.Split('/').Last()))
                {
                    System.IO.File.Copy(Server.MapPath(TempPath) + model.Image.Split('/').Last(), Server.MapPath(CompanyImagesPath) + model.Image.Split('/').Last());
                    model.Image = UrlUtility.GetAbsoluteUrl(CompanyImagesPath + model.Image.Split('/').Last());
                }

                var companyEntity = _companyService
                    .Query(x => x.CompanyId ==  model.CompanyId)
                    .Include(x => x.User)
                    .Select().SingleOrDefault();

                companyEntity.CompanyId = model.CompanyId;
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
                companyEntity.UserName = model.UserName;
                companyEntity.Active = model.Active;
                companyEntity.ModifiedBy = CurrentUser;
                companyEntity.ModifiedDate = DateTime.UtcNow;
                companyEntity.Image = model.Image;
                companyEntity.UserName = companyEntity.User.UserName;
                companyEntity.VuforiaServerAccessKey = model.VuforiaServerAccessKey;
                companyEntity.VuforiaServerSecretKey = model.VuforiaServerSecretKey;
                companyEntity.VuforiaClientAccessKey = model.VuforiaClientAccessKey;
                companyEntity.VuforiaClientSecretKey = model.VuforiaClientSecretKey;


                _companyService.Update(companyEntity);

                bool success = _unitOfWorkAsync.SaveChanges() > 0;

                if (success)
                {
                    //Set the current culture if country changes
                    string countryCode = _companyService.FindCountry(model.CountryID).CountryCode2;
                    countryCode = string.IsNullOrEmpty(countryCode) ? "US" : countryCode;
                    var cultureInfo = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(c => c.Name.EndsWith(countryCode)).FirstOrDefault();
                    cultureInfo = cultureInfo == null ? new CultureInfo("en-US") : cultureInfo;
                    Thread.CurrentThread.CurrentCulture = cultureInfo;
                    ((User)Session["ActiveUser"]).CultureCode = cultureInfo.CompareInfo.Name;
                    ((User)Session["ActiveUser"]).Image = model.Image;
                    Session["Photo"] = model.Image;
                    logging.Info("Profile updated successfully.");
                    response.Message = "Profile updated successfully.";
                    response.Status = ConstantUtil.StatusOk;
                    response.ReturnUrl = returnUrl;

                    var obj = _companyService.Find(model.CompanyId);

                    Session["CurrentCompany"] = obj;
                }
            }
            catch (Exception ex)
            {
                logging.Error(ex.ToString());
            }
            return Json(response, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Uploads the image.
        /// </summary>
        /// <param name="userType">Type of the user.</param>
        /// <returns>JsonResult.</returns>
        [HttpPost]
        public JsonResult UploadImage(string userType)
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
                    ImageUtility.ResizeandSaveImage(img, 150, 130, 100, path);
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
    }
}
