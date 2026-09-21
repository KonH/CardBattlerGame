using System.Threading;
using Cysharp.Threading.Tasks;
using Shared.Model;
using Shared.Service;
using VContainer.Unity;

namespace EntryPoint.Service
{
	public class EntryPointLoaderService : IAsyncStartable
	{
		readonly ISceneTransitionService _transition;

		public EntryPointLoaderService(ISceneTransitionService transition)
		{
			_transition = transition;
		}

		public async UniTask StartAsync(CancellationToken cancellation)
		{
			await _transition.GoTo(SceneType.Meta);
		}
	}
}
