using Core.Service;
using VContainer;
using VContainer.Unity;

namespace Core.DI
{
	public class CoreLifetimeScope : LifetimeScope
	{
		protected override void Configure(IContainerBuilder builder)
		{
			builder.Register<CoreFlowService>(Lifetime.Singleton);
		}
	}
}