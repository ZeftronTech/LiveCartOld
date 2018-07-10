using LiveKart.Web.App_Start;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Ninject;
using Ninject.Activation;
using Ninject.Parameters;
using Ninject.Syntax;
using Ninject.Web.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dependencies;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(NinjectWebCommon), "Stop")]
namespace LiveKart.Web.App_Start
{

	#region Ninject Scope

	// Provides a Ninject implementation of IDependencyScope
	// which resolves services using the Ninject container.
	public class NinjectScope : IDependencyScope
	{
		IResolutionRoot resolutionRoot;

		public NinjectScope(IResolutionRoot kernel)
		{
			resolutionRoot = kernel;
		}

		public object GetService(Type serviceType)
		{
			IRequest request = resolutionRoot.CreateRequest(serviceType, null, new Parameter[0], true, true);
			return resolutionRoot.Resolve(request).SingleOrDefault();
		}

		public IEnumerable<object> GetServices(Type serviceType)
		{
			IRequest request = resolutionRoot.CreateRequest(serviceType, null, new Parameter[0], true, true);
			return resolutionRoot.Resolve(request).ToList();
		}

		public void Dispose()
		{
			IDisposable disposable = resolutionRoot as IDisposable;
			if (disposable != null)
				disposable.Dispose();

			resolutionRoot = null;
		}
	}
	#endregion

	#region Ninject Resolver
	// This class is the resolver, but it is also the global scope
	// so we derive from NinjectScope.
	public class NinjectDependencyResolver : NinjectScope, IDependencyResolver
	{
		IKernel kernel;

		public NinjectDependencyResolver(IKernel kernel)
			: base(kernel)
		{
			this.kernel = kernel;
		}

		public IDependencyScope BeginScope()
		{
			return new NinjectScope(kernel.BeginBlock());
		}
	}
	#endregion

	#region Ninject Common
	public static class NinjectWebCommon
	{
		private static readonly Bootstrapper bootstrapper = new Bootstrapper();

		/// <summary>
		/// Starts the application
		/// </summary>
		public static void Start()
		{
			DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
			DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
			bootstrapper.Initialize(CreateKernel);
		}

		/// <summary>
		/// Stops the application.
		/// </summary>
		public static void Stop()
		{
			bootstrapper.ShutDown();
		}

		/// <summary>
		/// Creates the kernel that will manage your application.
		/// </summary>
		/// <returns>The created kernel.</returns>
		private static IKernel CreateKernel()
		{
			var kernel = new StandardKernel();
			kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
			kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

			RegisterServices(kernel);

			// Install our Ninject-based IDependencyResolver into the Web API config
			GlobalConfiguration.Configuration.DependencyResolver = new NinjectDependencyResolver(kernel);

			return kernel;
		}

		/// <summary>
		/// Load your modules or register your services here!
		/// </summary>
		/// <param name="kernel">The kernel.</param>
		private static void RegisterServices(IKernel kernel)
		{
			var modules = new Ninject.Modules.INinjectModule[]
								  {
									 new APIModule(),
									 new RepoModule(),
									 new ServiceModule(),
								  };

			kernel.Load(modules);
		}

	}
	#endregion
}
