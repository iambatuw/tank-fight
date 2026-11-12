using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public sealed class DuelOpponentMarker : MonoBehaviour
{
    private Camera view;
    private Transform opponent;
    private GameObject hud;
    private RectTransform rect;
    private TextMeshProUGUI label;
    private Image plate;

    public void Configure(Camera camera, Transform target, GameObject matchHud, TMP_FontAsset font, Color color)
    {
        view = camera;
        opponent = target;
        hud = matchHud;
        rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(168, 50);
        plate = GetComponent<Image>();
        if (plate == null) plate = gameObject.AddComponent<Image>();
        plate.color = new Color(0.045f, 0.07f, 0.06f, 0.60f);
        plate.sprite = TankDuel.RoundedSprite();
        plate.type = Image.Type.Sliced;
        plate.raycastTarget = false;
        if (label != null)
        {
            label.color = Color.Lerp(color, Color.white, 0.45f);
            return;
        }
        var text = new GameObject("Direction label", typeof(RectTransform));
        text.transform.SetParent(transform, false);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        label = text.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = 22;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.Lerp(color, Color.white, 0.45f);
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;
    }

    private void LateUpdate()
    {
        if (label == null) return;
        bool visible = view != null && opponent != null && opponent.gameObject.activeInHierarchy &&
            hud != null && hud.activeInHierarchy && Time.timeScale > 0f &&
            TankDuel.Instance != null && TankDuel.Instance.gameManager != null &&
            TankDuel.Instance.gameManager.IsRoundPlaying;
        Vector3 viewport = visible ? view.WorldToViewportPoint(opponent.position) : Vector3.zero;
        visible &= viewport.x < 0.07f || viewport.x > 0.93f || viewport.y < 0.10f || viewport.y > 0.83f;
        plate.enabled = label.enabled = visible;
        if (!visible) return;
        Vector2 location = new Vector2(Mathf.Clamp(viewport.x, 0.10f, 0.90f),
            Mathf.Clamp(viewport.y, 0.13f, 0.72f));
        rect.anchorMin = rect.anchorMax = location;
        rect.anchoredPosition = Vector2.zero;
        string name = TankDuelLocalization.IsTurkish ? "Rakip" : "Opponent";
        float x = viewport.x - 0.5f;
        float y = viewport.y - DuelCameraFraming.PlayerScreenHeight;
        label.text = Mathf.Abs(x) > Mathf.Abs(y) ?
            (x > 0f ? name + " →" : "← " + name) : (y > 0f ? "↑ " + name : "↓ " + name);
    }
}
