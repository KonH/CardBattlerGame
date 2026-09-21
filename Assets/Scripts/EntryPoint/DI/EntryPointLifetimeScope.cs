using EntryPoint.Service;
using VContainer;
using VContainer.Unity;

namespace EntryPoint.DI
{
	public class EntryPointLifetimeScope : LifetimeScope
	{
		protected override void Configure(IContainerBuilder builder)
		{
			builder.RegisterEntryPoint<EntryPointLoaderService>();
		}
	}
}