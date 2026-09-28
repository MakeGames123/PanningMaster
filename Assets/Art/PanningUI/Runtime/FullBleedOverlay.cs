using UnityEngine;

namespace PanningUI
{
    // A secondary modal keeps its controls in Column and its dim on the FullBleed layer.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class FullBleedOverlay : MonoBehaviour
    {
        [SerializeField] GameObject dim;
        public void Configure(GameObject value) {dim=value;Sync();}
        void OnEnable() {Sync();}
        void OnDisable() {if(dim!=null)dim.SetActive(false);}
        void Sync() {if(dim!=null)dim.SetActive(gameObject.activeInHierarchy);}
    }
}
