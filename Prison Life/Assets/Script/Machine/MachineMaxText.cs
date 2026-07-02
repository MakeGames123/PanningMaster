using UnityEngine;

public class MachineMaxText : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform rect;
    [SerializeField] private Vector3 offset;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        rect.position = mainCam.WorldToScreenPoint(target.position + offset);
    }
}