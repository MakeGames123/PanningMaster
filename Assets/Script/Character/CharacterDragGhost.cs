using UnityEngine;
using UnityEngine.UI;

// 캐릭터 드래그 고스트(탄환 DragSlot 문법) — 포인터를 따라다니는 캐릭터 스프라이트 1개.
// 씬의 캔버스 최상단(다른 UI 위)에 1개 배치.
public class CharacterDragGhost : MonoBehaviour
{
    public static CharacterDragGhost Instance { get; private set; }

    [SerializeField] Image image;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 고스트가 포인터 밑에 있으므로 드롭 레이캐스트를 가리면 안 됨
        foreach (var g in GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

        Hide();
    }

    // 탄환 DragSlot 문법: GameObject는 항상 활성(비활성이면 Awake가 안 돌아 Instance 미등록) — Image 컴포넌트만 켜고 끈다
    public void Show(Sprite sprite)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    public void Move(Vector2 screenPos) => transform.position = screenPos;

    public void Hide()
    {
        if (image != null) image.enabled = false;
    }
}
