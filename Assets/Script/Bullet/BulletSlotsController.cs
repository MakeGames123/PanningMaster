using UnityEngine;

public class BulletSlotsController : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] RevolverSlots revolver;
    BulletSlotsRayController rayController = new();
    void Awake()
    {
        revolver.Initialize(rayController);
        inventory.Initialize(rayController);
    }
}
