using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Shared.Config;
using Shared.Model;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Shared.Service
{
	public interface ISceneTransitionService
	{
		public UniTask<bool> GoTo(SceneType sceneType);
	}

	public interface ITargetSceneLoader
	{
		public float Progress { get; }

		public UniTask<bool> LoadPendingTarget();
		public void ActivatePendingTarget();
	}

	class SceneTransitionService : ISceneTransitionService, ITargetSceneLoader, IDisposable
	{
		public SceneType PendingTarget { get; private set; }

		public float Progress => _progressSource?.Invoke() ?? 0.0f;

		readonly SceneTransitionConfig _config;
		readonly string _loadingSceneName;

		readonly CancellationTokenSource _lifetimeSource = new();
		readonly bool _isConfigValid;

		Func<float>? _progressSource;
		bool _isLoadingSceneTransitionRunning;
		bool _isTargetSceneLoading;
		AsyncOperationHandle<SceneInstance> _targetSceneLoadingHandle;

		public SceneTransitionService(SceneTransitionConfig config)
		{
			_config = config;
			_isConfigValid = TryGetSceneName(SceneType.Loading, out _loadingSceneName);
			if (!_isConfigValid)
			{
				Debug.LogError(
					$"{nameof(SceneTransitionService)}: scene name format '{config.SceneNameByTypeFormat}' is invalid");
			}
		}

		public async UniTask<bool> GoTo(SceneType sceneType)
		{
			if (!_isConfigValid)
			{
				Debug.LogError($"{nameof(SceneTransitionService)}.{nameof(GoTo)}: configuration is invalid");
				return false;
			}
			if (_isLoadingSceneTransitionRunning)
			{
				Debug.LogError(
					$"{nameof(SceneTransitionService)}.{nameof(GoTo)}: transition to {PendingTarget} is already running");
				return false;
			}
			_isLoadingSceneTransitionRunning = true;
			PendingTarget = sceneType;
			_progressSource = null;
			try
			{
				var loadingSceneOperation = SceneManager.LoadSceneAsync(_loadingSceneName);
				if (loadingSceneOperation == null)
				{
					Debug.LogError(
						$"{nameof(SceneTransitionService)}.{nameof(GoTo)}: scene {_loadingSceneName} is not available");
					return false;
				}
				await loadingSceneOperation.ToUniTask(cancellationToken: _lifetimeSource.Token);
				return true;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return false;
			}
			finally
			{
				_isLoadingSceneTransitionRunning = false;
			}
		}

		public async UniTask<bool> LoadPendingTarget()
		{
			if (_isTargetSceneLoading)
			{
				Debug.LogError(
					$"{nameof(SceneTransitionService)}.{nameof(LoadPendingTarget)}: scene {PendingTarget} is already loading");
				return false;
			}
			if (!TryGetSceneName(PendingTarget, out var targetSceneName))
			{
				Debug.LogError(
					$"{nameof(SceneTransitionService)}.{nameof(LoadPendingTarget)}: scene name for {PendingTarget} cannot be resolved");
				return false;
			}
			_isTargetSceneLoading = true;
			try
			{
				ReleaseTargetSceneHandle();
				if (Application.isEditor)
				{
					await SimulateLoading();
					if (IsSimulatedFailure(PendingTarget))
					{
						Debug.LogWarning(
							$"{nameof(SceneTransitionService)}.{nameof(LoadPendingTarget)}: scene {targetSceneName}: simulated failure");
						return false;
					}
				}

				Debug.Log($"{nameof(SceneTransitionService)}.{nameof(LoadPendingTarget)}: scene {targetSceneName}: loading");
				_targetSceneLoadingHandle =
					Addressables.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive, activateOnLoad: false);
				_progressSource = () => _targetSceneLoadingHandle.PercentComplete;

				var result = await _targetSceneLoadingHandle.ToUniTask(cancellationToken: _lifetimeSource.Token);
				if (!result.Scene.IsValid())
				{
					Debug.LogError(
						$"{nameof(SceneTransitionService)}.{nameof(LoadPendingTarget)}: scene {targetSceneName}: loaded scene is invalid");
					return false;
				}
				Debug.Log($"{nameof(SceneTransitionService)}.{nameof(LoadPendingTarget)}: scene {targetSceneName}: ready");
				return true;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return false;
			}
			finally
			{
				_isTargetSceneLoading = false;
			}
		}

		public void ActivatePendingTarget() =>
			ActivatePendingTargetAsync().Forget();

		public void Dispose()
		{
			_lifetimeSource.Cancel();
			_lifetimeSource.Dispose();
		}

		async UniTaskVoid ActivatePendingTargetAsync()
		{
			try
			{
				if (!_targetSceneLoadingHandle.IsValid() ||
					(_targetSceneLoadingHandle.Status != AsyncOperationStatus.Succeeded))
				{
					Debug.LogError(
						$"{nameof(SceneTransitionService)}.{nameof(ActivatePendingTarget)}: scene {PendingTarget} is not loaded");
					return;
				}
				var loadingScene = SceneManager.GetActiveScene();

				var activateOperation = _targetSceneLoadingHandle.Result.ActivateAsync();
				if (activateOperation == null)
				{
					Debug.LogError(
						$"{nameof(SceneTransitionService)}.{nameof(ActivatePendingTarget)}: scene {PendingTarget} cannot be activated");
					return;
				}
				await activateOperation.ToUniTask(cancellationToken: _lifetimeSource.Token);

				SceneManager.SetActiveScene(_targetSceneLoadingHandle.Result.Scene);
				_progressSource = null;

				var unloadOperation = SceneManager.UnloadSceneAsync(loadingScene);
				if (unloadOperation == null)
				{
					Debug.LogError(
						$"{nameof(SceneTransitionService)}.{nameof(ActivatePendingTarget)}: scene {loadingScene.name} cannot be unloaded");
					return;
				}
				await unloadOperation.ToUniTask(cancellationToken: _lifetimeSource.Token);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		async UniTask SimulateLoading()
		{
			var simulatedDelay = _config.SimulatedDelay;
			if (simulatedDelay <= 0.0f)
			{
				return;
			}
			var startTime = Time.realtimeSinceStartup;
			_progressSource = () => (Time.realtimeSinceStartup - startTime) / simulatedDelay;
			await UniTask.WaitForSeconds(simulatedDelay, cancellationToken: _lifetimeSource.Token);
		}

		bool IsSimulatedFailure(SceneType sceneType)
		{
			foreach (var simulatedLoadingFailScene in _config.SimulatedLoadingFails)
			{
				if (simulatedLoadingFailScene == sceneType)
				{
					return true;
				}
			}
			return false;
		}

		void ReleaseTargetSceneHandle()
		{
			if (!_targetSceneLoadingHandle.IsValid())
			{
				return;
			}
			Addressables.Release(_targetSceneLoadingHandle);
			_targetSceneLoadingHandle = default;
		}

		bool TryGetSceneName(SceneType sceneType, out string sceneName)
		{
			var format = _config.SceneNameByTypeFormat;
			if (string.IsNullOrEmpty(format))
			{
				sceneName = string.Empty;
				return false;
			}
			try
			{
				sceneName = string.Format(format, sceneType.ToString());
				return true;
			}
			catch (FormatException)
			{
				sceneName = string.Empty;
				return false;
			}
		}
	}
}
