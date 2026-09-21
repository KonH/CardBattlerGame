using Shared.Config;
using Shared.Service;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Shared.DI
{
	public class ProjectLifetimeScope : LifetimeScope
	{
		[SerializeField] SceneTransitionConfig _sceneTransitionConfig = null!;
		
		protected override void Configure(IContainerBuilder builder)
		{
			builder.RegisterInstance(_sceneTransitionConfig);
			builder.Register<SceneTransitionService>(Lifetime.Singleton)
				.As<ISceneTransitionService>()
				.As<ITargetSceneLoader>();
		}
	}
}