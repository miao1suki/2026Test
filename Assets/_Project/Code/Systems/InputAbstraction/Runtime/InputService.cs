using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.InputAbstraction
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class InputService : MonoBehaviour
    {
        private static InputService instance;

        [SerializeField]
        private InputActionAsset actionAsset;

        [SerializeField]
        private InputPlatformMode platformMode = InputPlatformMode.Automatic;

        private UnityInputSource unitySource;
        private IInputSource builtInSource;
        private IInputSource externalSource;

        public static InputService Instance => instance;
        public IInputSource ActiveSource => externalSource ?? builtInSource;
        public InputActionAsset ConfiguredActionAsset => actionAsset;
        public InputPlatformMode ConfiguredPlatformMode => platformMode;
        public InputPlatformMode ResolvedPlatformMode => InputPlatformResolver.Resolve(platformMode);

        public static InputService EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject gameObject = new GameObject("InputService");
            InputService created = gameObject.AddComponent<InputService>();
            if (instance == null)
            {
                // EditMode does not invoke MonoBehaviour.Awake for this component.
                instance = created;
                created.CreateUnitySource();
            }

            return instance;
        }

        public void SetExternalSource(IInputSource source)
        {
            externalSource = source;
        }

        public void ClearExternalSource(IInputSource source = null)
        {
            if (source == null || ReferenceEquals(source, externalSource))
            {
                externalSource = null;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            CreateUnitySource();
        }

        private void OnEnable()
        {
            if (instance == this && unitySource == null)
            {
                CreateUnitySource();
            }
        }

        private void OnDisable()
        {
            if (instance == this)
            {
                unitySource?.Dispose();
                unitySource = null;
                builtInSource = null;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                externalSource = null;
                instance = null;
            }
        }

        private void CreateUnitySource()
        {
            if (unitySource == null)
            {
                unitySource = new UnityInputSource(actionAsset);
                builtInSource = ResolvedPlatformMode == InputPlatformMode.Mobile
                    ? new CompositeInputSource(unitySource, new VirtualInputSource())
                    : unitySource;
            }
        }
    }
}
