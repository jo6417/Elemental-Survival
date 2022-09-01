// Simple Scroll-Snap - https://assetstore.unity.com/packages/tools/gui/simple-scroll-snap-140884
// Copyright (c) Daniel Lochner

using System;
using UnityEngine;
using UnityEngine.UI;

namespace DanielLochner.Assets.SimpleScrollSnap
{
    public class DynamicContent : MonoBehaviour
    {
        #region Fields
        [SerializeField] private GameObject panelPrefab;
        [SerializeField] private bool useTogglePagenation;
        [SerializeField] private Toggle togglePrefab;
        [SerializeField] private ToggleGroup toggleGroup;
        [SerializeField] private InputField addInputField, removeInputField;
        [SerializeField] private SimpleScrollSnap scrollSnap;

        private float toggleWidth;
        #endregion

        #region Methods
        private void Awake()
        {
            if (useTogglePagenation)
                toggleWidth = (togglePrefab.transform as RectTransform).sizeDelta.x * (Screen.width / 2048f); ;
        }

        public GameObject Add(int index)
        {
            // Pagination
            if (useTogglePagenation)
            {
                Toggle toggle = Instantiate(togglePrefab, scrollSnap.Pagination.transform.position + new Vector3(toggleWidth * (scrollSnap.NumberOfPanels + 1), 0, 0), Quaternion.identity, scrollSnap.Pagination.transform);
                toggle.group = toggleGroup;
                scrollSnap.Pagination.transform.position -= new Vector3(toggleWidth / 2f, 0, 0);
            }

            // Panel
            panelPrefab.GetComponent<Image>().color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
            GameObject panel = scrollSnap.Add(panelPrefab, index);

            // 생성한 패널 오브젝트를 반환
            return panel;
        }
        public GameObject AddAtIndex()
        {
            return Add(Convert.ToInt32(addInputField.text));
        }
        public GameObject AddToFront()
        {
            return Add(0);
        }
        public GameObject AddToBack()
        {
            return Add(scrollSnap.NumberOfPanels);
        }

        public void Remove(int index)
        {
            if (scrollSnap.NumberOfPanels > 0)
            {
                // Pagination
                DestroyImmediate(scrollSnap.Pagination.transform.GetChild(scrollSnap.NumberOfPanels - 1).gameObject);
                scrollSnap.Pagination.transform.position += new Vector3(toggleWidth / 2f, 0, 0);

                // Panel
                scrollSnap.Remove(index);
            }
        }
        public void RemoveAtIndex()
        {
            Remove(Convert.ToInt32(removeInputField.text));
        }
        public void RemoveFromFront()
        {
            Remove(0);
        }
        public void RemoveFromBack()
        {
            if (scrollSnap.NumberOfPanels > 0)
            {
                Remove(scrollSnap.NumberOfPanels - 1);
            }
            else
            {
                Remove(0);
            }
        }
        #endregion
    }
}