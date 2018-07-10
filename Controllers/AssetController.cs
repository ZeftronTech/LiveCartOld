using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LiveKart.Shared;
using LiveKart.LogService;
using Repository.Pattern.UnitOfWork;
using LiveKart.Service;
using LiveKart.Entities;
using Repository.Pattern.Infrastructure;

namespace LiveKart.Web.Controllers
{
	/// <summary>
	/// Class AssetController
	/// </summary>
	public class AssetController : BaseController
	{
		private readonly IAssetService _assetService;
		private readonly IUnitOfWork _unitOfWorkAsync;

		public AssetController(IUnitOfWork unitOfWorkAsync, IAssetService assetService)
			
		{
			_unitOfWorkAsync = unitOfWorkAsync;
			_assetService = assetService;
		}

		/// <summary>
		/// The logging
		/// </summary>
		ILogService logging = new FileLogService(typeof(AssetController));

		#region Asset

		/// <summary>
		/// List of all the assets
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Index()
		{
			ViewData["pagename"] = "managebeacon";
			var assets = _unitOfWorkAsync.Repository<Asset>()
				.Query(x => x.CompanyID == CurrentCompany.CompanyId)
				.Include(x => x.AssetCategory)
				.Select();

			return View(assets);
		}

		/// <summary>
		/// Creates new asset.
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Create()
		{
			ViewData["pagename"] = "managebeacon";
			ViewBag.Categories = _unitOfWorkAsync.Repository<AssetCategory>()
				.Query(x => x.CompanyID == CurrentCompany.CompanyId && x.Active == true)
				.Select().ToList();
			return View();
		}

		/// <summary>
		/// Creates the asset.
		/// </summary>
		/// <param name="model">Asset object</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Create(Asset model)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					model.Active = true;
					model.CreatedBy = CurrentUser;
					model.CreatedDate = DateTime.UtcNow;
					model.CompanyID = CurrentCompany.CompanyId;
					model.ObjectState = ObjectState.Added;
					_unitOfWorkAsync.Repository<Asset>().Insert(model);
					
					if (_unitOfWorkAsync.SaveChanges() > 0)
					{
						response.Message = "Asset created successfully.";
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
		/// Edits the specified asset.
		/// </summary>
		/// <param name="id">Asset id.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult Edit(long id)
		{
			ViewData["pagename"] = "managebeacon";
			Asset asset = _unitOfWorkAsync.Repository<Asset>().Find(id);
			ViewBag.Categories = _unitOfWorkAsync.Repository<AssetCategory>()
										.Query(x => x.CompanyID == CurrentCompany.CompanyId && (x.Active == true || x.AssetCategoryID == asset.AssetCategoryID))
										.Select()
										.ToList();
			return View(asset);
		}

		/// <summary>
		/// Edits the specified asset.
		/// </summary>
		/// <param name="id">Asset id.</param>
		/// <param name="model">Asset object</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Edit(long id, Asset model)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					var asset = _unitOfWorkAsync.Repository<Asset>().Find(id);
					asset.AssetCategoryID = model.AssetCategoryID;
					asset.ModifiedBy = CurrentUser;
					asset.ModifiedDate = DateTime.UtcNow;
					asset.CompanyID = CurrentCompany.CompanyId;
					asset.Description = model.Description;
					asset.Active = model.Active;
					asset.Address = model.Address;
					asset.AssetName = model.AssetName;
					asset.City = model.City;
					asset.Location = model.Location;
					asset.ZipCode = model.ZipCode;
					asset.ObjectState = ObjectState.Modified;


					_unitOfWorkAsync.Repository<Asset>().Update(asset);

					if (_unitOfWorkAsync.SaveChanges() > 0)
					{
						response.Message = "Asset updated successfully.";
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
		/// Deletes the specified asset.
		/// </summary>
		/// <param name="id">Asset id.</param>
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
					_unitOfWorkAsync.Repository<Asset>().Delete(id);
					if (_unitOfWorkAsync.SaveChanges() > 0)
					{
						response.Message = "Asset deleted successfully.";
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

		#region Asset

		/// <summary>
		/// list of all asset categories.
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Categories()
		{
			ViewData["pagename"] = "managebeacon";

			var assetCategories = _unitOfWorkAsync.Repository<AssetCategory>()
				.Query(x => x.CompanyID == CurrentCompany.CompanyId)
				.Select();
			return View(assetCategories);
		}

		/// <summary>
		/// Create/Edit asset category
		/// </summary>
		/// <param name="id">asset category id.</param>
		/// <returns>ActionResult.</returns>
		public ActionResult Category(long? id = null)
		{
			ViewData["pagename"] = "managebeacon";
			AssetCategory ac = new AssetCategory();
			if (id.HasValue)
			{
				ac = _unitOfWorkAsync.Repository<AssetCategory>().Find(id.GetValueOrDefault());
			}
			return View(ac);
		}

		/// <summary>
		/// Create/Edit asset category.
		/// </summary>
		/// <param name="model">Asset category object</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult Category(AssetCategory model)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					model.CompanyID = CurrentCompany.CompanyId;
					if (model.AssetCategoryID == 0)
					{
						model.Active = true;
						model.CreatedBy = CurrentUser;
						model.CreatedDate = DateTime.UtcNow;
						model.ModifiedBy = CurrentUser;
						model.ModifiedDate = DateTime.UtcNow;
						model.ObjectState = ObjectState.Modified;

						_unitOfWorkAsync.Repository<AssetCategory>().Insert(model);

						if (_unitOfWorkAsync.SaveChanges() > 0)
						{
							response.Message = "Asset category created successfully.";
							response.Status = ConstantUtil.StatusOk;
							response.ReturnUrl = Url.Action("Categories");
						}
					}
					else
					{

						var assetCategory = _unitOfWorkAsync.Repository<AssetCategory>().Find(model.AssetCategoryID);
						assetCategory.Active = model.Active;
						assetCategory.CategoryName = model.CategoryName;
						assetCategory.Description = model.Description;
						assetCategory.ModifiedBy = CurrentUser;
						assetCategory.ModifiedDate = DateTime.UtcNow;
						assetCategory.CompanyID = CurrentCompany.CompanyId;
						assetCategory.ObjectState = ObjectState.Modified;

						_unitOfWorkAsync.Repository<AssetCategory>().Update(assetCategory);
						if (_unitOfWorkAsync.SaveChanges() > 0)
						{
							response.Message = "Asset category updated successfully.";
							response.Status = ConstantUtil.StatusOk;
							response.ReturnUrl = Url.Action("Categories");
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
		/// Deletes the asset category.
		/// </summary>
		/// <param name="id">category id.</param>
		/// <returns>ActionResult.</returns>
		[HttpPost]
		public ActionResult DeleteCategory(long id)
		{
			JsonResponse response = new JsonResponse();
			response.Message = ConstantUtil.MessageError;
			response.Status = ConstantUtil.StatusFail;
			if (ModelState.IsValid)
			{
				try
				{
					_unitOfWorkAsync.Repository<AssetCategory>().Delete(id);
					if (_unitOfWorkAsync.SaveChanges() > 0)
					{
						response.Message = "Asset category deleted successfully.";
						response.Status = ConstantUtil.StatusOk;
						response.ReturnUrl = Url.Action("Categories");
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
	}
}
