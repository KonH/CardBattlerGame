using System.Threading;
using Cysharp.Threading.Tasks;
using Loading.Service;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Loading.ViewModel
{
	class LoadingViewModel : MonoBehaviour
	{
		[SerializeField] Slider _loadingSlider = null!;
		[SerializeField] Button _retryButton = null!;

		LoadingFlowService _loadingFlowService = null!;

		[Inject]
		public void Init(LoadingFlowService loadingFlowService)
		{
			_loadingFlowService = loadingFlowService;
		}

		void Start()
		{
			_retryButton.onClick.AddListener(OnRetryClick);
			LoadTarget(this.GetCancellationTokenOnDestroy()).Forget();
		}

		void Update()
		{
			_loadingSlider.value = _loadingFlowService.Progress;
		}

		void OnRetryClick() =>
			LoadTarget(this.GetCancellationTokenOnDestroy()).Forget();

		async UniTaskVoid LoadTarget(CancellationToken token)
		{
			UpdateState(isRetryAvailable: false);
			var isLoaded = await _loadingFlowService.LoadTarget();
			if (token.IsCancellationRequested || isLoaded)
			{
				return;
			}
			UpdateState(isRetryAvailable: true);
		}

		void UpdateState(bool isRetryAvailable)
		{
			_loadingSlider.gameObject.SetActive(!isRetryAvailable);
			_retryButton.gameObject.SetActive(isRetryAvailable);
		}
	}
}
