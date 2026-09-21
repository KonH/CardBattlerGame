using Cysharp.Threading.Tasks;
using Shared.Service;

namespace Loading.Service
{
	public class LoadingFlowService
	{
		public float Progress => _targetSceneLoader.Progress;

		readonly ITargetSceneLoader _targetSceneLoader;

		public LoadingFlowService(ITargetSceneLoader targetSceneLoader)
		{
			_targetSceneLoader = targetSceneLoader;
		}

		public async UniTask<bool> LoadTarget()
		{
			var isLoaded = await _targetSceneLoader.LoadPendingTarget();
			if (isLoaded)
			{
				_targetSceneLoader.ActivatePendingTarget();
			}
			return isLoaded;
		}
	}
}
