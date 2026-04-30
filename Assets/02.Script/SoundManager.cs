using UnityEngine;

/// <summary>
/// Singleton Pattern으로 전역 사운드 재생을 담당한다.
/// - BGM: 배경음 재생/정지
/// - SFX: 효과음 1회 재생(PlayOneShot)
/// </summary>
[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    /// <summary>
    /// 다른 스크립트에서 SoundManager.Instance로 접근한다.
    /// 예) SoundManager.Instance.PlaySfxOneShot(clip);
    /// </summary>
    public static SoundManager Instance { get; private set; }

    [Header("AudioSource (Optional)")]
    [Tooltip("비어 있으면 Awake에서 자동 생성한다. BGM 전용 AudioSource.")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("비어 있으면 Awake에서 자동 생성한다. SFX 전용 AudioSource.")]
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        // [Singleton 1단계] 이미 인스턴스가 있고, 그 인스턴스가 나 자신이 아니면 중복 오브젝트다.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // DontDestroyOnLoad는 루트 오브젝트에서만 동작하므로, 하위 계층이면 루트로 분리한다.
        if (transform.parent != null)
        {
            Debug.LogWarning("[SoundManager] GameObject가 루트가 아니어서 루트로 이동한 뒤 전역 유지를 적용합니다.");
            transform.SetParent(null, true);
        }

        // [Singleton 2단계] 최초 1개 인스턴스를 전역 참조로 등록한다.
        Instance = this;

        // [Singleton 3단계] 씬이 바뀌어도 파괴되지 않도록 유지한다.
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
    }

    /// <summary>
    /// BGM을 재생한다. 기존 BGM이 있으면 clip을 교체해 다시 재생한다.
    /// </summary>
    public void PlayBgm(AudioClip bgmClip, float volume = 1f, bool loop = true)
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[SoundManager] PlayBgm 실패: bgmClip이 null입니다.");
            return;
        }

        if (bgmSource == null)
        {
            Debug.LogWarning("[SoundManager] PlayBgm 실패: bgmSource가 없습니다.");
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.loop = loop;
        bgmSource.volume = Mathf.Clamp01(volume);
        bgmSource.Play();
    }

    /// <summary>
    /// 현재 재생 중인 BGM을 정지한다.
    /// </summary>
    public void StopBgm()
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[SoundManager] StopBgm 실패: bgmSource가 없습니다.");
            return;
        }

        bgmSource.Stop();
    }

    /// <summary>
    /// 효과음을 1회 재생한다. (AudioSource.PlayOneShot)
    /// </summary>
    public void PlaySfxOneShot(AudioClip sfxClip, float volumeScale = 1f)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("[SoundManager] PlaySfxOneShot 실패: sfxClip이 null입니다.");
            return;
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("[SoundManager] PlaySfxOneShot 실패: sfxSource가 없습니다.");
            return;
        }

        sfxSource.PlayOneShot(sfxClip, Mathf.Clamp01(volumeScale));
    }

    /// <summary>
    /// 인스펙터에서 AudioSource를 연결하지 않았을 때 자동 생성한다.
    /// "필수 컴포넌트 누락"으로 막히지 않게 하는 방어 코드다.
    /// </summary>
    private void EnsureAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // 역할 분리를 위해 기본값을 명시적으로 지정한다.
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }
}


