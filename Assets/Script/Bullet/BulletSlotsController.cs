using UnityEngine;

public class BulletSlotsController : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] RevolverSlots[] revolvers; // 사수별 전용 리볼버 3개(공유 안 함)
    BulletSlotsRayController rayController = new();

    // Awake가 아니라 Start: 슬롯 초기화가 AllBulletList.Instance(Awake에서 세팅)를 참조하므로
    // 모든 Awake가 끝난 뒤에 돌아야 실행 순서가 보장된다
    void Start()
    {
        foreach (RevolverSlots revolver in revolvers)
            if (revolver != null) revolver.Initialize(rayController);
        inventory.Initialize(rayController);
    }
}
