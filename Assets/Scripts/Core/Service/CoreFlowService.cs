using Cysharp.Threading.Tasks;
using Shared.Model;
using Shared.Service;

namespace Core.Service
{
	public class CoreFlowService
	{
		readonly ISceneTransitionService _sceneTransitionService;
		
		public CoreFlowService(ISceneTransitionService sceneTransitionService)
		{
			_sceneTransitionService = sceneTransitionService;
		}
		
		public void Exit()
		{
			_sceneTransitionService.GoTo(SceneType.Meta).Forget();
		}
	}
}