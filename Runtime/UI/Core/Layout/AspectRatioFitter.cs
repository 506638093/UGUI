using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/Aspect Ratio Fitter", 142)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    /// <summary>
    /// Resizes a RectTransform to fit a specified aspect ratio.
    /// </summary>
    public class AspectRatioFitter : UIBehaviour, ILayoutSelfController
    {
        /// <summary>
        /// Specifies a mode to use to enforce an aspect ratio.
        /// </summary>
        public enum AspectMode
        {
            /// <summary>
            /// The aspect ratio is not enforced
            /// </summary>
            RecordData,
            /// <summary>
            /// Changes the height of the rectangle to match the aspect ratio.
            /// </summary>
            WidthControlsHeight,
            /// <summary>
            /// Changes the width of the rectangle to match the aspect ratio.
            /// </summary>
            HeightControlsWidth,
            /// <summary>
            /// Sizes the rectangle such that it's fully contained within the parent rectangle.
            /// </summary>
            FitInParent,
            /// <summary>
            /// Sizes the rectangle such that the parent rectangle is fully contained within.
            /// </summary>
            EnvelopeParent,
            CenterFit,
            ScaleFit
        }

        [SerializeField] private AspectMode m_AspectMode = AspectMode.RecordData;

        /// <summary>
        /// The mode to use to enforce the aspect ratio.
        /// </summary>
        public AspectMode aspectMode { get { return m_AspectMode; } set { if (SetPropertyUtility.SetStruct(ref m_AspectMode, value)) SetDirty(); } }

        [SerializeField] private float m_AspectRatio = 1;
        [SerializeField] private Vector2 nativeSize = Vector2.zero, nativeParentSize = Vector2.zero;
        [SerializeField] private Vector3 nativeScale = Vector3.one;
        [SerializeField] private List<RectTransform> childrenList = new List<RectTransform>();
        [SerializeField] private List<Vector2> childrenPosList = new List<Vector2>();
        [SerializeField] private bool fitChildren = false;
        [SerializeField] private RectTransform linkToRt = null;
        private Vector2 _parentSizeInLastCheck = Vector2.zero;
        /// <summary>
        /// The aspect ratio to enforce. This means width divided by height.
        /// </summary>
        public float aspectRatio { get { return m_AspectRatio; } set { if (SetPropertyUtility.SetStruct(ref m_AspectRatio, value)) SetDirty(); } }

        [System.NonSerialized]
        private RectTransform m_Rect;

        // This "delayed" mechanism is required for case 1014834.
        private bool m_DelayedSetDirty = false;

        private RectTransform rectTransform
        {
            get
            {
                if (m_Rect == null)
                    m_Rect = GetComponent<RectTransform>();
                return m_Rect;
            }
        }

        private DrivenRectTransformTracker m_Tracker;

        protected AspectRatioFitter() { }

        protected override void OnEnable()
        {
            base.OnEnable();
            _parentSizeInLastCheck = GetParentSize();
            SetDirty();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            base.OnDisable();
        }

        /// <summary>
        /// Update the rect based on the delayed dirty.
        /// Got around issue of calling onValidate from OnEnable function.
        /// </summary>
        protected virtual void Update()
        {
            if (m_DelayedSetDirty)
            {
                m_DelayedSetDirty = false;
                SetDirty();
            }
            else
            {
                var _parentSize = GetParentSize();
                if(m_AspectMode == AspectMode.CenterFit && linkToRt)
                {
                    _parentSize = linkToRt.rect.size;
                }
                if (_parentSize != _parentSizeInLastCheck)
                {
                    SetDirty();
                }
                _parentSizeInLastCheck = _parentSize;
            }
        }

        /// <summary>
        /// Function called when this RectTransform or parent RectTransform has changed dimensions.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            UpdateRect();
        }

        private void UpdateRect()
        {
            if (!IsActive())
                return;

            m_Tracker.Clear();

            switch (m_AspectMode)
            {
#if UNITY_EDITOR
                //只用来获取保存初始值
                case AspectMode.RecordData:
                    {
                        if (!Application.isPlaying)
                            m_AspectRatio = Mathf.Clamp(rectTransform.rect.width / rectTransform.rect.height, 0.001f, 1000f);
                        nativeSize = new Vector2(rectTransform.rect.width, rectTransform.rect.height);
                        nativeParentSize = GetParentSize();
                        nativeScale = transform.localScale;
                        childrenList.Clear();
                        childrenPosList.Clear();
                        foreach (Transform tran in transform)
                        {
                            var child = tran.GetComponent<RectTransform>();
                            if (child)
                            {
                                childrenList.Add(child);
                                childrenPosList.Add(child.anchoredPosition);
                            }
                        }
                        break;
                    }
#endif
                case AspectMode.HeightControlsWidth:
                    {
                        m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
                        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.height * m_AspectRatio);
                        break;
                    }
                case AspectMode.WidthControlsHeight:
                    {
                        m_Tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
                        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.width / m_AspectRatio);
                        break;
                    }
                case AspectMode.FitInParent:
                    {
                        m_Tracker.Add(this, rectTransform,
                           DrivenTransformProperties.Anchors |
                           DrivenTransformProperties.AnchoredPosition |
                           DrivenTransformProperties.SizeDeltaX |
                           DrivenTransformProperties.SizeDeltaY);

                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.anchoredPosition = Vector2.zero;
                        Vector2 sizeDelta = GetParentSize();
                        rectTransform.sizeDelta = sizeDelta;
                        break;
                    }
                case AspectMode.EnvelopeParent:
                    {
                        m_Tracker.Add(this, rectTransform,
                            DrivenTransformProperties.Anchors |
                            DrivenTransformProperties.AnchoredPosition |
                            DrivenTransformProperties.SizeDeltaX |
                            DrivenTransformProperties.SizeDeltaY);

                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.anchoredPosition = Vector2.zero;
                        

                        Vector2 sizeDelta = Vector2.zero;
                        Vector2 parentSize = GetParentSize();
                        if ((parentSize.y * aspectRatio < parentSize.x) ^ (m_AspectMode == AspectMode.FitInParent))
                        {
                            sizeDelta.y = GetSizeDeltaToProduceSize(parentSize.x / aspectRatio, 1);
                        }
                        else
                        {
                            sizeDelta.x = GetSizeDeltaToProduceSize(parentSize.y * aspectRatio, 0);
                        }
                        rectTransform.sizeDelta = sizeDelta;
                        break;
                    }
                case AspectMode.CenterFit:
                    {
                        m_Tracker.Add(this, rectTransform,
                            DrivenTransformProperties.Anchors |
                            DrivenTransformProperties.AnchoredPosition |
                            DrivenTransformProperties.SizeDelta
                            );
                        rectTransform.anchorMin = Vector2.one * 0.5f;
                        rectTransform.anchorMax = Vector2.one * 0.5f;
                        Vector2 fitSize = Vector2.zero;
                        Vector2 parentSize = GetParentSize();
                        if (linkToRt)
                        {
                            parentSize = linkToRt.rect.size;
                            rectTransform.position = linkToRt.position;
                        }
                        else
                        {
                            if (!Application.isPlaying)
                                rectTransform.anchoredPosition = Vector2.zero;
                        }
                        

                        fitSize.y = Mathf.Max(parentSize.y, nativeSize.y);
                        fitSize.x = fitSize.y * m_AspectRatio;
                        fitSize.x = Mathf.Max(parentSize.x, fitSize.x);
                        fitSize.y = fitSize.x / m_AspectRatio;
                        rectTransform.sizeDelta = fitSize;
                        if (fitChildren)
                        {
                            int childCount = childrenList.Count;
                            float zoomRate = fitSize.x / nativeSize.x;
                            for (int i = 0; i < childCount; i++)
                            {
                                RectTransform _r = childrenList[i];
                                if (_r)
                                {
                                    _r.anchoredPosition = childrenPosList[i] * zoomRate;
                                }
                            }
                        }
                        break;
                    }
                case AspectMode.ScaleFit:
                    {
                        if (nativeParentSize.x == 0 || nativeParentSize.y == 0)
                        {
                            return;
                        }
                        Vector2 parentSize = GetParentSize();
                        if (parentSize.x == 0 || parentSize.y == 0)
                        {
                            return;
                        }
                        float ratio = Mathf.Max(parentSize.x / nativeParentSize.x, parentSize.y / nativeParentSize.y);
                        transform.localScale = nativeScale * ratio;
                        break;
                    }
            }
        }

        private float GetSizeDeltaToProduceSize(float size, int axis)
        {
            return size - GetParentSize()[axis] * (rectTransform.anchorMax[axis] - rectTransform.anchorMin[axis]);
        }

        private Vector2 GetParentSize()
        {
            RectTransform parent = rectTransform.parent as RectTransform;
            if (!parent)
                return Vector2.zero;
            return parent.rect.size;
        }

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public virtual void SetLayoutHorizontal() { }

        /// <summary>
        /// Method called by the layout system. Has no effect
        /// </summary>
        public virtual void SetLayoutVertical() { }

        /// <summary>
        /// Mark the AspectRatioFitter as dirty.
        /// </summary>
        protected void SetDirty()
        {
            UpdateRect();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_AspectRatio = Mathf.Clamp(m_AspectRatio, 0.001f, 1000f);
            m_DelayedSetDirty = true;
        }

#endif
    }
}
