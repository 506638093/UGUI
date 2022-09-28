/* 
 * ==============================================================================
 * Filename: RectMask2DNoCull
 * Created:  2022 / 7 / 6 17:04
 * Author: HuaHua
 * Purpose: RectMask2D ÐÔÄÜ°æ±¾No Cull
 * ==============================================================================
**/


using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Rect Mask 2D No Cull")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    /// <summary>
    /// A 2D rectangular mask that allows for clipping / masking of areas outside the mask.
    /// </summary>
    /// <remarks>
    /// The RectMask2D behaves in a similar way to a standard Mask component. It differs though in some of the restrictions that it has.
    /// A RectMask2D:
    /// *Only works in the 2D plane
    /// *Requires elements on the mask to be coplanar.
    /// *Does not require stencil buffer / extra draw calls
    /// *Requires fewer draw calls
    /// *Culls elements that are outside the mask area.
    /// </remarks>
    public class RectMask2DNoCull : RectMask2D
    {
        [NonSerialized]
        protected Vector2Int m_LastSoftness;

        public override void PerformClipping()
        {
            if (ReferenceEquals(Canvas, null))
            {
                return;
            }

            //TODO See if an IsActive() test would work well here or whether it might cause unexpected side effects (re case 776771)

            // if the parents are changed
            // or something similar we
            // do a recalculate here
            if (m_ShouldRecalculateClipRects)
            {
                MaskUtilities.GetRectMasksForClip(this, m_Clippers);
                m_ShouldRecalculateClipRects = false;
            }

            // get the compound rects from
            // the clippers that are valid
            bool validRect = true;
            Rect clipRect = Clipping.FindCullAndClipWorldRect(m_Clippers, out validRect);

            // If the mask is in ScreenSpaceOverlay/Camera render mode, its content is only rendered when its rect
            // overlaps that of the root canvas.
            RenderMode renderMode = Canvas.rootCanvas.renderMode;
            bool maskIsCulled =
                (renderMode == RenderMode.ScreenSpaceCamera || renderMode == RenderMode.ScreenSpaceOverlay) &&
                !clipRect.Overlaps(rootCanvasRect, true);

            if (maskIsCulled)
            {
                // Children are only displayed when inside the mask. If the mask is culled, then the children
                // inside the mask are also culled. In that situation, we pass an invalid rect to allow callees
                // to avoid some processing.
                clipRect = Rect.zero;
                validRect = false;
            }

            if (clipRect != m_LastClipRectCanvasSpace)
            {
                foreach (IClippable clipTarget in m_ClipTargets)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                }

                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    maskableTarget.SetClipRect(clipRect, validRect);
                    //maskableTarget.Cull(clipRect, validRect);
                }
            }
            else if (m_ForceClip)
            {
                foreach (IClippable clipTarget in m_ClipTargets)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                }

                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    maskableTarget.SetClipRect(clipRect, validRect);

                    //if (maskableTarget.canvasRenderer.hasMoved)
                    //    maskableTarget.Cull(clipRect, validRect);
                }
            }
            //else
            //{
            //    foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
            //    {
            //        if (maskableTarget.canvasRenderer.hasMoved)
            //            maskableTarget.Cull(clipRect, validRect);
            //    }
            //}

            m_LastClipRectCanvasSpace = clipRect;

            UpdateClipSoftness();

            m_ForceClip = false;
        }

        public override void UpdateClipSoftness()
        {
            if (ReferenceEquals(Canvas, null))
            {
                return;
            }

            if (m_LastSoftness != m_Softness || m_ForceClip)
            {
                foreach (IClippable clipTarget in m_ClipTargets)
                {
                    clipTarget.SetClipSoftness(m_Softness);
                }

                foreach (MaskableGraphic maskableTarget in m_MaskableTargets)
                {
                    maskableTarget.SetClipSoftness(m_Softness);
                }

                m_LastSoftness = m_Softness;
            }
        }

    }
}
