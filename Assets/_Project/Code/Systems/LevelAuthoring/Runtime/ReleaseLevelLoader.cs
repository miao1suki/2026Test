using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.LevelAuthoring
{
    [DisallowMultipleComponent]
    public sealed class ReleaseLevelLoader : MonoBehaviour
    {
        [SerializeField] private string generatedMapSceneName;

        public string GeneratedMapSceneName => generatedMapSceneName;

        public void Configure(string sceneName)
        {
            generatedMapSceneName = sceneName;
        }

        private IEnumerator Start()
        {
            if (string.IsNullOrWhiteSpace(generatedMapSceneName) ||
                SceneManager.GetSceneByName(generatedMapSceneName).isLoaded)
            {
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(
                generatedMapSceneName,
                LoadSceneMode.Additive);
            if (operation != null)
            {
                yield return operation;
            }
        }
    }
}
