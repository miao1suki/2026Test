using UnityEngine;

namespace Project.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class LevelSceneContext : MonoBehaviour
    {
        [SerializeField] private GameFlowSceneId sceneId = GameFlowSceneId.Level01;
        [SerializeField] private Transform playerSpawn;

        public GameFlowSceneId SceneId => sceneId;
        public Transform PlayerSpawn => playerSpawn;

        public void Configure(
            GameFlowSceneId valueSceneId,
            Transform valuePlayerSpawn)
        {
            sceneId = valueSceneId;
            playerSpawn = valuePlayerSpawn;
        }
    }
}
