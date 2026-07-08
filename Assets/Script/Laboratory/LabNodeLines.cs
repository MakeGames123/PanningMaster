using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 페이지 루트에 붙여서 노드 사이 연결선을 자동 생성한다.
// LabEdge 시트의 엣지(From -> To)를 기준으로, 같은 페이지 안의 두 노드를
// 가는 Image 를 회전시켜 잇는 방식. 선은 노드 뒤(첫 번째 자식)에 그려진다.
public class LabNodeLines : MonoBehaviour
{
    [SerializeField] Image linePrefab;  // 선으로 쓸 이미지 프리팹(비우면 기본 사각 이미지 생성)
    [SerializeField] float thickness = 4f; // 선 두께(px)
    [SerializeField] Color lineColor = new(1f, 0.85f, 0.3f, 0.5f); // 프리팹이 없을 때 쓸 색

    bool built;

    void OnEnable()
    {
        if (!built) StartCoroutine(BuildWhenReady());
    }

    // 시트(LabEdge)가 로드된 뒤 한 번만 생성
    IEnumerator BuildWhenReady()
    {
        while (LabEdgeLoader.Instance == null || !LabEdgeLoader.Instance.IsLoaded)
            yield return null;

        if (built) yield break;
        built = true;
        Build();
    }

    void Build()
    {
        // 이 페이지 안의 노드 UI 수집
        Dictionary<int, RectTransform> nodeById = new();
        foreach (LabNodeUI ui in GetComponentsInChildren<LabNodeUI>(true))
            nodeById[ui.NodeId] = ui.GetComponent<RectTransform>();

        // 선 컨테이너: 노드보다 뒤에 그려지도록 첫 번째 자식으로
        RectTransform container = new GameObject("Lines", typeof(RectTransform)).GetComponent<RectTransform>();
        container.SetParent(transform, false);
        container.SetAsFirstSibling();

        foreach (var kv in nodeById)
        {
            foreach (int from in LabEdgeLoader.Instance.GetPrerequisites(kv.Key))
            {
                // 선행 노드가 같은 페이지에 없으면(다른 페이지 연결) 선을 그리지 않음
                if (!nodeById.TryGetValue(from, out RectTransform fromRect)) continue;

                CreateLine(container, fromRect, kv.Value);
            }
        }
    }

    // 가는 이미지를 두 노드 중점에 놓고, 거리만큼 늘리고, 방향으로 회전시켜 연결
    void CreateLine(RectTransform parent, RectTransform from, RectTransform to)
    {
        Image img;
        if (linePrefab != null)
        {
            img = Instantiate(linePrefab, parent);
        }
        else
        {
            img = new GameObject("Line", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(parent, false);
            img.color = lineColor;
        }
        img.raycastTarget = false; //노드 클릭 방해 금지

        RectTransform rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // 부모(컨테이너) 로컬 좌표로 변환해 계층 구조가 달라도 정확히 연결
        Vector2 a = parent.InverseTransformPoint(from.position);
        Vector2 b = parent.InverseTransformPoint(to.position);
        Vector2 dir = b - a;

        rt.localPosition = (a + b) * 0.5f;                     //중점에 배치
        rt.sizeDelta = new Vector2(dir.magnitude, thickness);  //길이 = 두 노드 거리
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg); //방향으로 회전
    }
}
