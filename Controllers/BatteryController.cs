using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using LiveKart.Shared.Entities;
using Repository.Pattern.UnitOfWork;


namespace LiveKart.Web.Controllers
{
	public class BatteryController : BaseController
	{
		private readonly IUnitOfWork _unitOfWorkAsync;

		public BatteryController(IUnitOfWork unitOfWorkAsync)
			: base()
		{
			_unitOfWorkAsync = unitOfWorkAsync;
		}

		public ActionResult Alert()
		{
			var beaconsSchedule = _unitOfWorkAsync.Repository<Entities.BeaconSchedule>()
				.Query(b => b.CompanyID == CurrentCompany.CompanyId)
				.Include(b => b.Beacon)
				.Select().ToList();

			var alerts = new List<Alert>();

			foreach (var beaconSchedule in beaconsSchedule)
			{
				var beacon = beaconSchedule.Beacon;
				var alert = new Alert();
				if (beacon.BatteryLevel == "0" || beacon.BatteryLevel == null)
				{
					alert.Messages = "Please check the Battery of Beacon. Battery is empty.";
					alert.BeaconID = beaconSchedule.Beacon.BeaconID;
					alert.BatteryLevel = "0";
				}
				else if (beacon.BatteryLevel == "1")
				{
					alert.Messages = "Please check the Battery of Beacon. Battery remaining 25%.";
					alert.BeaconID = beaconSchedule.Beacon.BeaconID;
					alert.BatteryLevel = "1";
				}
				alerts.Add(alert);
			}

			return View(alerts);
		}
	}
}
