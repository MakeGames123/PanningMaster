using UnityEngine;

public class BulletSlotsController : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] RevolverSlots revolver;
    [SerializeField] Workmanship workmanship;
    BulletSlotsRayController rayController = new();
    void Awake()
    {
        inventory.Initialize(rayController, workmanship);
        revolver.Initialize(rayController, workmanship);
    }
}
