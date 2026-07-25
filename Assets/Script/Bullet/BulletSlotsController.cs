using UnityEngine;

public class BulletSlotsController : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] RevolverSlots[] revolvers; // 사수별 전용 리볼버 3개(공유 안 함)
    BulletSlotsRayController rayController = new();
    void Awake()
    {
        foreach (RevolverSlots revolver in revolvers)
            if (revolver != null) revolver.Initialize(rayController);
        inventory.Initialize(rayController);
    }
}
