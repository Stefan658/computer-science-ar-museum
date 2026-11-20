using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Iara.AR
{
    public sealed class ImageTrackingAssets : ScriptableObject
    {
        [SerializeField] private XRReferenceImageLibrary referenceLibrary;
        [SerializeField] private GameObject diagnosticPrefab;
        public XRReferenceImageLibrary ReferenceLibrary => referenceLibrary;
        public GameObject DiagnosticPrefab => diagnosticPrefab;
        public void Configure(XRReferenceImageLibrary library, GameObject prefab)
        { referenceLibrary = library; diagnosticPrefab = prefab; }
    }
}
