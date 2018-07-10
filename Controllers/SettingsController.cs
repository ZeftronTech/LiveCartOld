using System.Web.Mvc;
using LiveKart.Entities;
using LiveKart.Service;
using LiveKart.Shared;
using Repository.Pattern.UnitOfWork;

namespace LiveKart.Web.Controllers
{
	public class SettingsController : BaseController
	{
		private readonly ISettingsService _settingsService;
		private readonly IUnitOfWork _unitOfWork;
		public SettingsController(IUnitOfWork unitOfWork, ISettingsService settingsService)
			: base()
		{
			_unitOfWork = unitOfWork;
			_settingsService = settingsService;
		}

		public ActionResult Index()
		{
			ViewData["pagename"] = "settings";
			ViewBag.Status = "active";
			var settings = _settingsService.ByCompany(CurrentCompany.CompanyId);
			return View(settings);
		}

		[HttpPost]
		public ActionResult Proximity(Settings model)
		{
			if (model.SettingId == 0)
			{
				_settingsService.Insert(model);
			}
			else
			{
				_settingsService.Update(model);
			}
			_unitOfWork.SaveChanges();

			JsonResponse response = new JsonResponse();
			response.Status = ConstantUtil.StatusOk;
			return RedirectToAction("Index");
		}

		[HttpPost]
		public ActionResult DwellTime(Settings model)
		{
			JsonResponse response = new JsonResponse();
			response.Status = ConstantUtil.StatusOk;
			return Json(response, JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		public ActionResult SignalStrength(Settings model)
		{
			JsonResponse response = new JsonResponse();
			response.Status = ConstantUtil.StatusOk;
			return Json(response, JsonRequestBehavior.AllowGet);
		}
	}
}
