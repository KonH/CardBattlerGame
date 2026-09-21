using Meta.Service;
using VContainer;
using VContainer.Unity;

namespace Meta.DI
{
	public class MetaLifetimeScope : LifetimeScope
	{
		protected override void Configure(IContainerBuilder builder)
		{
			builder.Register<MetaFlowService>(Lifetime.Singleton);
		}
	}
}