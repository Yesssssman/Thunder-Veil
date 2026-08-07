using System.Collections.Generic;
using Content.Character.Soldier;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using ThunderVeil.Core;
using UnityEngine.Serialization;

namespace Content
{
    /// <summary>
    /// 게임 로직 중앙 서버 + Input System의 소유자.
    ///
    /// Systems 컴포넌트에서 사용
    ///
    /// 게임플레이 로직들(사격 재사용 대기시간, AI 상태간 대기 시간 등)은 FixedUpdate에서 관리하며
    /// 이는 의도된 시간만큼 동작하도록 하기 위함.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        // 싱글톤, 인스턴스화는 Awake() 호출 시.
        public static GameManager Instance { get; private set; }

        private GameManager() {}

        [Header("Input")]

        [SerializeField]
        private InputActionAsset controls;

        [Header("Audio")]

        [SerializeField]
        private AudioMixer mixer;

        [FormerlySerializedAs("rainSource")] [SerializeField]
        private AudioSource raindropSource;      // 빗소리

        [SerializeField]
        private AudioSource bgmSource;           // 배경음

        [SerializeField]
        private AudioSource sfxSource;           // one-shots: siren / victory

        [SerializeField]
        private AudioClip sirenClip;             // 패배

        [SerializeField]
        private AudioClip victoryClip;           // 승리

        [Header("UI")]

        [SerializeField]
        private GameObject resultUI;             // 결과 화면 UI

        [Header("Play area")]

        [Tooltip("따로 설정하지 않을 시 신 내 오브젝트를 찾아 자동 바인딩")]
        [SerializeField]
        private SpriteRenderer backgroundRenderer;

        [Tooltip("따로 설정하지 않을 시 신 내 오브젝트를 찾아 자동 바인딩")]
        [SerializeField]
        private Camera gameCamera;

        // --- 입력 액션 ---
        public InputAction Look { get; private set; }
        public InputAction Fire { get; private set; }
        public InputAction AdjustVolume { get; private set; }
        public InputAction ToggleIndicators { get; private set; }

        public InputAction Zoom { get; private set; }

        // --- 전역 게임 상태 Properties ---
        public bool GameEnd { get; private set; }
        public bool Win { get; private set; }
        public bool ShowIndicators { get; private set; } = false;
        public int MaxAlarmStandbyTime => 150;

        // 게임내 유닛 List
        private readonly List<Soldier> _soldiers = new();

        // 게임 내 유닛들을 ReadOnlyList로 반환
        public IReadOnlyList<Soldier> Soldiers => _soldiers;

        private float _volumeDb;

        private void Awake()
        {
            // 예외처리: Instance(전역, static)가 null이 아니고 현재 인스턴스가 아니라면, (인스턴스가
            // 두 개 생성된 상황) 부모 오브젝트를 즉시 Destroy하고 스크립트 종료
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // FixedUpdate의 Interval을 0.016667..초(60FPS)로 설정
            Time.fixedDeltaTime = 1f / 60f;

            // Play Area 설정: 카메라가 움직일수 있는 경계 설정
            ConfigurePlayArea();

            var map = controls != null ? controls.FindActionMap("Gameplay", false) : null;

            if (map != null)
            {
                Look = map.FindAction("Look", false);
                Fire = map.FindAction("Fire", false);
                AdjustVolume = map.FindAction("AdjustVolume", false);
                ToggleIndicators = map.FindAction("ToggleIndicators", false);
                Zoom = map.FindAction("Zoom", false);
            }
        }

        /// <summary>
        /// 활성화된 Background 오브젝트로부터 플레이 가능 영역을 계산하고 카메라를 해당 영역에서만 움직일 수
        /// 있도로 제한함. Background 사이즈를 런타임에서 계산하므로 배경 Sprite의 크기에 변경이 생겨도
        /// 카메라 바운더리를 소스 코드에서 수정하지 않아도 됨.
        /// </summary>
        private void ConfigurePlayArea()
        {
            // backgroundRenderer가 null인 경우 (유니티 에디터에서 지정해주지 않은 경우) 이름이 Background인
            // 오브젝트를 WorldRoot의 자식에서 찾음
            if (backgroundRenderer == null)
            {
                var bg = GameObject.Find("WorldRoot/Background");
                if (bg != null) backgroundRenderer = bg.GetComponent<SpriteRenderer>();
            }

            // 카메라가 없을 경우 메인 카메라 (최초 생성된 메인 카메라 인스턴스) 에서 가져옴
            if (gameCamera == null) gameCamera = Camera.main;

            // background바운더리를 바탕으로 PixelWorld 바운드 생성
            if (backgroundRenderer != null)
                PixelWorld.ConfigureFromBounds(backgroundRenderer.bounds);

            //
            if (gameCamera != null && gameCamera.orthographic)
            {
                Vector3 c = PixelWorld.AreaCenterWorld;
                float z = gameCamera.transform.position.z;
                gameCamera.transform.position = new Vector3(c.x, c.y, z);
                gameCamera.orthographicSize = PixelWorld.FitOrthoSize(gameCamera.aspect);
            }
        }

        private void OnEnable()
        {
            controls?.Enable();
            if (ToggleIndicators != null) ToggleIndicators.performed += OnToggleIndicators;
        }

        private void OnDisable()
        {
            if (ToggleIndicators != null) ToggleIndicators.performed -= OnToggleIndicators;
            controls?.Disable();
        }

        private void Start()
        {
            if (resultUI != null) resultUI.SetActive(false);
            if (raindropSource != null) { raindropSource.loop = true; raindropSource.Play(); }
        }

        private void Update()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // Volume nudge (was Up/Down arrow -> DirectSound volume in the C++ build).
            if (AdjustVolume == null) return;
            float axis = AdjustVolume.ReadValue<float>();
            if (Mathf.Abs(axis) < 0.01f) return;

            if (mixer != null)
            {
                // Preferred: an AudioMixer exposed parameter named "MasterVolume" (dB).
                _volumeDb = Mathf.Clamp(_volumeDb + axis * 30f * Time.deltaTime, -40f, 0f);
                mixer.SetFloat("MasterVolume", _volumeDb);
            }
            else
            {
                // Fallback so volume works with no mixer wired: global listener volume.
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume + axis * Time.deltaTime);
            }
        }

        private void OnToggleIndicators(InputAction.CallbackContext _)
        {
            ShowIndicators = !ShowIndicators;
        }

        public void Register(Soldier s)
        {
            if (!_soldiers.Contains(s)) _soldiers.Add(s);
        }

        public void Unregister(Soldier s)
        {
            _soldiers.Remove(s);
        }

        /// <summary>Called by a soldier the moment it dies; wins when none remain alive.</summary>
        public void NotifySoldierDied()
        {
            foreach (var s in _soldiers)
                if (s.IsAlive) return;

            Victory();
        }

        public void Victory()
        {
            if (GameEnd) return;
            GameEnd = true; Win = true;
            if (raindropSource != null) raindropSource.Stop();
            if (bgmSource != null) bgmSource.Stop();
            if (sfxSource != null && victoryClip != null) sfxSource.PlayOneShot(victoryClip);
            if (resultUI != null) resultUI.SetActive(true);
        }

        public void Defeated()
        {
            if (GameEnd) return;
            GameEnd = true; Win = false;
            if (raindropSource != null) raindropSource.Stop();
            if (bgmSource != null) bgmSource.Stop();
            if (sfxSource != null && sirenClip != null) sfxSource.PlayOneShot(sirenClip);
            if (resultUI != null) resultUI.SetActive(true);
        }
    }
}