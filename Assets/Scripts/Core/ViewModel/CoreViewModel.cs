using Core.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Core.ViewModel
{
	class CoreViewModel : MonoBehaviour
	{
		[SerializeField] Button _exitButton = null!;

		CoreFlowService _coreFlowService = null!;

		[Inject]
		public void Init(CoreFlowService coreFlowService)
		{
			_coreFlowService = coreFlowService;
			_exitButton.onClick.AddListener(() => _coreFlowService.Exit());
		}
	}
}