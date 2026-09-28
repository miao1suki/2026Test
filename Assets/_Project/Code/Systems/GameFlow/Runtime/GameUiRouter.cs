using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class GameUiRouter : MonoBehaviour, IGameSystemService
    {
        [Header("Screens")]
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject gameplayHud;
        [SerializeField] private GameObject endingScreen;
        [SerializeField] private GameObject loadingScreen;

        [Header("Main menu")]
        [SerializeField] private Button level01Button;
        [SerializeField] private Button level02Button;
        [SerializeField] private Button level03Button;

        [Header("Gameplay")]
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private Button reloadLevelButton;
        [SerializeField] private Button showEndingButton;

        [Header("Ending")]
        [SerializeField] private Button endingReturnToMenuButton;

        private GameFlowController flow;
        private bool buttonsBound;

        public int InitializationOrder => 100;
        public bool IsInitialized { get; private set; }

        public void Configure(
            GameObject valueMainMenuScreen,
            GameObject valueGameplayHud,
            GameObject valueEndingScreen,
            GameObject valueLoadingScreen,
            Button valueLevel01Button,
            Button valueLevel02Button,
            Button valueLevel03Button,
            Button valueReturnToMenuButton,
            Button valueReloadLevelButton,
            Button valueShowEndingButton,
            Button valueEndingReturnToMenuButton)
        {
            mainMenuScreen = valueMainMenuScreen;
            gameplayHud = valueGameplayHud;
            endingScreen = valueEndingScreen;
            loadingScreen = valueLoadingScreen;
            level01Button = valueLevel01Button;
            level02Button = valueLevel02Button;
            level03Button = valueLevel03Button;
            returnToMenuButton = valueReturnToMenuButton;
            reloadLevelButton = valueReloadLevelButton;
            showEndingButton = valueShowEndingButton;
            endingReturnToMenuButton = valueEndingReturnToMenuButton;
        }

        public IEnumerator Initialize()
        {
            BindButtons();
            BindFlow();
            SetOnly(loadingScreen);
            IsInitialized = true;
            yield break;
        }

        public void OpenMainMenu() => Flow()?.ReturnToMainMenu();
        public void OpenLevel01() => Flow()?.RequestTransition(GameFlowSceneId.Level01);
        public void OpenLevel02() => Flow()?.RequestTransition(GameFlowSceneId.Level02);
        public void OpenLevel03() => Flow()?.RequestTransition(GameFlowSceneId.Level03);
        public void ReloadLevel() => Flow()?.ReloadActiveScene();
        public void OpenEnding() => Flow()?.RequestTransition(GameFlowSceneId.Ending);

        private void Awake()
        {
            BindButtons();
        }

        private void OnEnable()
        {
            BindFlow();
        }

        private void OnDisable()
        {
            UnbindFlow();
        }

        private void OnDestroy()
        {
            UnbindFlow();
            UnbindButtons();
        }

        private GameFlowController Flow()
        {
            if (flow == null)
            {
                BindFlow();
            }

            return flow;
        }

        private void BindFlow()
        {
            GameFlowController candidate = GameFlowController.Instance;
            if (candidate == null || candidate == flow)
            {
                return;
            }

            UnbindFlow();
            flow = candidate;
            flow.TransitionStarted += OnTransitionStarted;
            flow.ActiveSceneChanged += OnActiveSceneChanged;
            if (flow.ActiveSceneId.HasValue)
            {
                OnActiveSceneChanged(flow.ActiveSceneId.Value);
            }
        }

        private void UnbindFlow()
        {
            if (flow == null)
            {
                return;
            }

            flow.TransitionStarted -= OnTransitionStarted;
            flow.ActiveSceneChanged -= OnActiveSceneChanged;
            flow = null;
        }

        private void BindButtons()
        {
            if (buttonsBound)
            {
                return;
            }

            level01Button?.onClick.AddListener(OpenLevel01);
            level02Button?.onClick.AddListener(OpenLevel02);
            level03Button?.onClick.AddListener(OpenLevel03);
            returnToMenuButton?.onClick.AddListener(OpenMainMenu);
            reloadLevelButton?.onClick.AddListener(ReloadLevel);
            showEndingButton?.onClick.AddListener(OpenEnding);
            endingReturnToMenuButton?.onClick.AddListener(OpenMainMenu);
            buttonsBound = true;
        }

        private void UnbindButtons()
        {
            if (!buttonsBound)
            {
                return;
            }

            level01Button?.onClick.RemoveListener(OpenLevel01);
            level02Button?.onClick.RemoveListener(OpenLevel02);
            level03Button?.onClick.RemoveListener(OpenLevel03);
            returnToMenuButton?.onClick.RemoveListener(OpenMainMenu);
            reloadLevelButton?.onClick.RemoveListener(ReloadLevel);
            showEndingButton?.onClick.RemoveListener(OpenEnding);
            endingReturnToMenuButton?.onClick.RemoveListener(OpenMainMenu);
            buttonsBound = false;
        }

        private void OnTransitionStarted(GameFlowSceneId _)
        {
            SetOnly(loadingScreen);
        }

        private void OnActiveSceneChanged(GameFlowSceneId sceneId)
        {
            switch (sceneId)
            {
                case GameFlowSceneId.MainMenu:
                    SetOnly(mainMenuScreen);
                    break;
                case GameFlowSceneId.Ending:
                    SetOnly(endingScreen);
                    break;
                default:
                    SetOnly(gameplayHud);
                    break;
            }
        }

        private void SetOnly(GameObject target)
        {
            SetActive(mainMenuScreen, target == mainMenuScreen);
            SetActive(gameplayHud, target == gameplayHud);
            SetActive(endingScreen, target == endingScreen);
            SetActive(loadingScreen, target == loadingScreen);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
