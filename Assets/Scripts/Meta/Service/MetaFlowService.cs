using Cysharp.Threading.Tasks;
using Shared.Model;
using Shared.Service;

namespace Meta.Service
{
	public class MetaFlowService
	{
		readonly ISceneTransitionService _sceneTransitionService;
		
		public MetaFlowService(ISceneTransitionService sceneTransitionService)
		{
			_sceneTransitionService = sceneTransitionService;
		}
		
		public void Play()
		{
			_sceneTransitionService.GoTo(SceneType.Core).Forget();
		}
	}
}