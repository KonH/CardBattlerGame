using System;
using System.Collections.Generic;
using Shared.Model;
using UnityEngine;

namespace Shared.Config
{
	[CreateAssetMenu]
	public class SceneTransitionConfig : ScriptableObject
	{
		public string SceneNameByTypeFormat => _sceneNameByTypeFormat;
		public float SimulatedDelay => _simulatedDelay;
		public IReadOnlyList<SceneType> SimulatedLoadingFails => _simulatedLoadingFails;

		[SerializeField] string _sceneNameByTypeFormat = "Assets/Scenes/{0}.unity";
		[SerializeField] float _simulatedDelay = 2.0f;
		[SerializeField] SceneType[] _simulatedLoadingFails = Array.Empty<SceneType>();
	}
}
