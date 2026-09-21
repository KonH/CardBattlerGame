using Loading.Service;
using VContainer;
using VContainer.Unity;

namespace Loading.DI
{
	public class LoadingLifetimeScope : LifetimeScope
	{
		protected override void Configure(IContainerBuilder builder)
		{
			builder.Register<LoadingFlowService>(Lifetime.Singleton);
		}
	}
}