using Meta.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Meta.ViewModel
{
	class MetaViewModel : MonoBehaviour
	{
		[SerializeField] Button _playButton = null!;

		MetaFlowService _metaFlowService = null!;

		[Inject]
		public void Init(MetaFlowService metaFlowService)
		{
			_metaFlowService = metaFlowService;
			_playButton.onClick.AddListener(() => _metaFlowService.Play());
		}
	}
}