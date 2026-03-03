using UnityEngine;

public class BulletSlotsController : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] RevolverSlots revolver;
    [SerializeField] Forge workmanship;
    BulletSlotsRayController rayController = new();
    void Awake()
    {
        revolver.Initialize(rayController, workmanship);
        inventory.Initialize(rayController, workmanship);
    }
}
