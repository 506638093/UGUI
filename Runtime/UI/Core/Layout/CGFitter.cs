using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("Layout/CG Fitter", 142)]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    /// <summary>
    /// Resizes a RectTransform to fit a specified aspect ratio.
    /// </summary>
    public class CGFitter : UIBehaviour, ILayoutSelfController
    {
        /// <summary>
        /// ����ģʽ
        /// </summary>
        public enum LayoutMode
        {
            /// <summary>
            /// ��������С���е��κ�����
            /// </summary>
            Fit,
            /// <summary>
            /// ����������
            /// </summary>
            Cut,
        }


        [SerializeField] private LayoutMode m_LayoutMode = LayoutMode.Cut;
        [SerializeField] private bool m_Flip = false;

        /// <summary>
        /// CGͼ���� ���or����
        /// </summary>
        public LayoutMode CGLayoutMode { get { return m_LayoutMode; } set { if (SetPropertyUtility.SetStruct(ref m_LayoutMode, value)) SetDirty(); } }
        public bool Flip { get { return m_Flip; } set { if (SetPropertyUtility.SetStruct(ref m_Flip, value)) SetDirty(); } }
        private Vector2 _parentSizeInLastCheck = Vector2.zero;
        private int _texturePtrInLastCheck;
        [System.NonSerialized]
        private RectTransform m_Rect;
        // This "delayed" mechanism is required for case 1014834.
        private bool m_DelayedSetDirty = false;

        private MaskableGraphic m_Graphic;
        private Vector2 CutOffset = Vector2.zero;
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

        protected CGFitter() { }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Graphic = GetComponent<MaskableGraphic>();
            _parentSizeInLastCheck = GetParentSize();
            _texturePtrInLastCheck = m_Graphic.mainTexture.GetNativeTexturePtr().ToInt32();
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
                var _texturePtr = m_Graphic.mainTexture.GetNativeTexturePtr().ToInt32();
                if (_parentSize != _parentSizeInLastCheck || _texturePtr != _texturePtrInLastCheck)
                {
                    SetDirty();
                }
                _parentSizeInLastCheck = _parentSize;
                _texturePtrInLastCheck = m_Graphic.mainTexture.GetNativeTexturePtr().ToInt32();
            }
        }

        /// <summary>
        /// Function called when this RectTransform or parent RectTransform has changed dimensions.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            //UpdateRect();
        }

        private void UpdateRect()
        {
            if (!IsActive() || !m_Graphic)
                return;

            m_Tracker.Clear();
            Vector2 nativeSize = m_Graphic.GetNativeSize();
            Vector2 parentSize = GetParentSize();
            Vector2 offset = CutOffset;
            if (nativeSize.y == 0)
                return;
            float aspect = Mathf.Clamp(nativeSize.x / nativeSize.y, 0.001f, 1000f);
            if (Flip)
            {
                nativeSize = new Vector2(nativeSize.y, nativeSize.x);
                offset = new Vector2(CutOffset.y, -CutOffset.x);
                transform.localEulerAngles = Vector3.forward * -90;
            }
            else
            {

                transform.localEulerAngles = Vector3.zero;
            }



            rectTransform.anchorMin = Vector2.one * 0.5f;
            rectTransform.anchorMax = Vector2.one * 0.5f;
            Vector2 fitSize = Vector2.zero;
            switch (m_LayoutMode)
            {
                case LayoutMode.Cut:
                    {
                        fitSize.y = Mathf.Max(parentSize.y, nativeSize.y);
                        fitSize.x = fitSize.y * aspect;
                        fitSize.x = Mathf.Max(parentSize.x, fitSize.x);
                        fitSize.y = fitSize.x / aspect;
                        rectTransform.sizeDelta = fitSize;
                        float hOffset = offset.x * parentSize.y / nativeSize.y, vOffset = offset.y * parentSize.y / nativeSize.y;
                        hOffset = Mathf.Abs(hOffset) > fitSize.x / 2 ? hOffset > 0 ? fitSize.x / 2 : -fitSize.x / 2 : hOffset;
                        vOffset = Mathf.Abs(vOffset) > fitSize.y / 2 ? vOffset > 0 ? fitSize.y / 2 : -fitSize.y / 2 : vOffset;
                        rectTransform.anchoredPosition = new Vector2(hOffset, vOffset);
                        break;
                    }

                case LayoutMode.Fit:
                    {


                        if (Flip)
                        {
                            parentSize = new Vector2(parentSize.y, parentSize.x);
                        }
                        if (parentSize.y > parentSize.x)
                        {

                            fitSize.x = parentSize.x;
                            fitSize.y = fitSize.x / aspect;
                            fitSize.y = Mathf.Min(parentSize.y, fitSize.y);
                            fitSize.x = fitSize.y * aspect;
                        }
                        else
                        {
                            fitSize.y = parentSize.y;
                            fitSize.x = fitSize.y * aspect;
                            fitSize.x = Mathf.Min(parentSize.x, fitSize.x);
                            fitSize.y = fitSize.x / aspect;
                        }
                        rectTransform.sizeDelta = fitSize;
                        rectTransform.anchoredPosition = Vector2.zero;
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

        public void Refit(LayoutMode mode = LayoutMode.Cut, float offsetX = 0, float offsetY = 0, bool flip = false)
        {

            CutOffset = new Vector2(offsetX, offsetY);
            m_LayoutMode = mode;
            m_Flip = flip;
            SetDirty();
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
        /// Mark the CGFitter as dirty.
        /// </summary>
        protected void SetDirty()
        {
            UpdateRect();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            m_DelayedSetDirty = true;
        }
#endif
    }
}
