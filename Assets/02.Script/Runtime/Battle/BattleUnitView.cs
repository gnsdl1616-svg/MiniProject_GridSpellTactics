using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnitView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image image;
    [SerializeField] private Animator animator;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fallback Tween")]
    [SerializeField] private float moveDuration = 0.26f;
    [SerializeField] private float punchScale = 1.12f;
    [SerializeField] private float punchDuration = 0.18f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color guardFlashColor = new Color(0.55f, 0.85f, 1f, 1f);

    [Header("Animator Trigger Names")]
    [SerializeField] private string moveTrigger = "Move";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string guardTrigger = "Guard";
    [SerializeField] private string deathTrigger = "Death";

    private Coroutine moveCoroutine;
    private Coroutine punchCoroutine;
    private Color defaultColor = Color.white;

    public RectTransform RectTransform => rectTransform != null ? rectTransform : (RectTransform)transform;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (image != null)
        {
            defaultColor = image.color;
        }
    }

    public void ApplySprite(Sprite sprite)
    {
        if (image != null && sprite != null)
        {
            image.sprite = sprite;
            image.SetNativeSize();
        }
    }

    public void SnapTo(Vector2 anchoredPosition)
    {
        RectTransform.anchoredPosition = anchoredPosition;
    }

    public void MoveTo(Vector2 anchoredPosition)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(CoMoveTo(anchoredPosition));
    }


    public void PlayMove()
    {
        if (TryTrigger(moveTrigger))
        {
            return;
        }
    }

    public void PlayAttack()
    {
        if (TryTrigger(attackTrigger))
        {
            return;
        }

        PlayPunch(defaultColor);
    }

    public void PlayHit()
    {
        if (TryTrigger(hitTrigger))
        {
            return;
        }

        PlayPunch(hitFlashColor);
    }

    public void PlayGuard()
    {
        if (TryTrigger(guardTrigger))
        {
            return;
        }

        PlayPunch(guardFlashColor);
    }

    public void PlayDeath()
    {
        if (!TryTrigger(deathTrigger))
        {
            StartCoroutine(CoFadeOut());
        }
    }

    public void ShowImmediate()
    {
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private bool TryTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return false;
        }

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
        return true;
    }

    private void PlayPunch(Color flashColor)
    {
        if (punchCoroutine != null)
        {
            StopCoroutine(punchCoroutine);
        }

        punchCoroutine = StartCoroutine(CoPunch(flashColor));
    }

    private IEnumerator CoMoveTo(Vector2 target)
    {
        Vector2 start = RectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = moveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / moveDuration);
            RectTransform.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }

        RectTransform.anchoredPosition = target;
        moveCoroutine = null;
    }

    private IEnumerator CoPunch(Color flashColor)
    {
        Vector3 originScale = RectTransform.localScale;
        float half = punchDuration * 0.5f;

        if (image != null)
        {
            image.color = flashColor;
        }

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = half <= 0f ? 1f : Mathf.Clamp01(elapsed / half);
            RectTransform.localScale = Vector3.Lerp(originScale, originScale * punchScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = half <= 0f ? 1f : Mathf.Clamp01(elapsed / half);
            RectTransform.localScale = Vector3.Lerp(originScale * punchScale, originScale, t);
            yield return null;
        }

        RectTransform.localScale = originScale;
        if (image != null)
        {
            image.color = defaultColor;
        }

        punchCoroutine = null;
    }

    private IEnumerator CoFadeOut()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float duration = 0.25f;
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
