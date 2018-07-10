using LiveKart.Entities;
using Ninject.Modules;
using Ninject.Web.Common;
using Repository.Pattern.Ef6;
using Repository.Pattern.Repositories;

namespace LiveKart.Web
{
	public class RepoModule : NinjectModule
	{
		public override void Load()
		{
			Bind<IRepository<Beacon>>().To<Repository<Beacon>>().InRequestScope();
			Bind<IRepository<BeaconSchedule>>().To<Repository<BeaconSchedule>>().InRequestScope();
			Bind<IRepository<Notification>>().To<Repository<Notification>>().InRequestScope();
			Bind<IRepository<NotificationMessage>>().To<Repository<NotificationMessage>>().InRequestScope();
			Bind<IRepository<StandardMessage>>().To<Repository<StandardMessage>>().InRequestScope();
			Bind<IRepository<SurveyMessage>>().To<Repository<SurveyMessage>>().InRequestScope();
			Bind<IRepository<User>>().To<Repository<User>>().InRequestScope();
			Bind<IRepository<Company>>().To<Repository<Company>>().InRequestScope();
			Bind<IRepository<Settings>>().To<Repository<Settings>>().InRequestScope();
		}
	}
}