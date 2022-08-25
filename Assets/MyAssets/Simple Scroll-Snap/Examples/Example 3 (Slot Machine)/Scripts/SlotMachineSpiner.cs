// Simple Scroll-Snap - https://assetstore.unity.com/packages/tools/gui/simple-scroll-snap-140884
// Copyright (c) Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    public class SlotMachineSpiner : MonoBehaviour
    {
        #region Fields
        [SerializeField] private SimpleScrollSnap[] slots;
        public float minSpinSpeed = 2500f;
        public float maxSpinSpeed = 5000f;
        #endregion

        #region Methods
        public void Spin()
        {
            foreach (SimpleScrollSnap slot in slots)
            {
                slot.Velocity += Random.Range(minSpinSpeed, maxSpinSpeed) * Vector2.up;
            }
        }
        #endregion
    }
}