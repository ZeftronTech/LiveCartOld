using LiveKart.Service;
using Ninject.Modules;
using Ninject.Web.Common;

namespace LiveKart.Web
{
	public class ServiceModule : NinjectModule
	{
		public override void Load()
		{
			Bind<IBeaconService>().To<BeaconService>().InRequestScope();
			Bind<IBeaconScheduleService>().To<BeaconScheduleService>().InRequestScope();
			Bind<INotificationService>().To<NotificationService>().InRequestScope();
			Bind<INotificationMessageService>().To<NotificationMessageService>().InRequestScope();
			Bind<IUserService>().To<UserService>().InRequestScope();
			Bind<IStandardMessageService>().To<StandardMessageService>().InRequestScope();
			Bind<ICompanySevice>().To<CompanySevice>().InRequestScope();
			Bind<ISurveyService>().To<SurveyService>().InRequestScope();
			Bind<IAssetService>().To<AssetService>().InRequestScope();
			Bind<ISettingsService>().To<SettingsService>().InRequestScope();
		}
	}
}