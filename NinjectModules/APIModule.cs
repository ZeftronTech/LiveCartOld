using LiveKart.Entities;
using Ninject.Modules;
using Ninject.Web.Common;
using Repository.Pattern.DataContext;
using Repository.Pattern.Ef6;
using Repository.Pattern.UnitOfWork;

namespace LiveKart.Web
{
	public class APIModule : NinjectModule
	{
		public override void Load()
		{
			Bind<IDataContext>().To<LiveKartEntities>().InRequestScope();
			Bind<IUnitOfWork>().To<UnitOfWork>().InRequestScope();
		}
	}
}