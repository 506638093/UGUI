using System.Collections.Generic;

namespace UnityEngine.UI
{
    /// <summary>
    /// Mask related utility class. This class provides masking-specific utility functions.
    /// </summary>
    public class MaskUtilities
    {
#if OPTIMIZE_UGUI
        static List<RectMask2D> cacheMaskes = new List<RectMask2D>();
#endif

        /// <summary>
        /// Notify all IClippables under the given component that they need to recalculate clipping.
        /// </summary>
        /// <param name="mask">The object thats changed for whose children should be notified.</param>
        public static void Notify2DMaskStateChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;

                var toNotify = components[i] as IClippable;
                if (toNotify != null)
                    toNotify.RecalculateClipping();
            }
            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// Notify all IMaskable under the given component that they need to recalculate masking.
        /// </summary>
        /// <param name="mask">The object thats changed for whose children should be notified.</param>
        public static void NotifyStencilStateChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;

                var toNotify = components[i] as IMaskable;
                if (toNotify != null)
                    toNotify.RecalculateMasking();
            }
            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// Find a root Canvas.
        /// </summary>
        /// <param name="start">Transform to start the search at going up the hierarchy.</param>
        /// <returns>Finds either the most root canvas, or the first canvas that overrides sorting.</returns>
        public static Transform FindRootSortOverrideCanvas(Transform start)
        {
            var canvasList = ListPool<Canvas>.Get();
            start.GetComponentsInParent(false, canvasList);
            Canvas canvas = null;

            for (int i = 0; i < canvasList.Count; ++i)
            {
                canvas = canvasList[i];

                // We found the canvas we want to use break
                if (canvas.overrideSorting)
                    break;
            }
            ListPool<Canvas>.Release(canvasList);

            return canvas != null ? canvas.transform : null;
        }

        /// <summary>
        /// Find the stencil depth for a given element.
        /// </summary>
        /// <param name="transform">The starting transform to search.</param>
        /// <param name="stopAfter">Where the search of parents should stop</param>
        /// <returns>What the proper stencil buffer index should be.</returns>
        public static int GetStencilDepth(Transform transform, Transform stopAfter)
        {
            var depth = 0;
            if (transform == stopAfter)
                return depth;

            var t = transform.parent;
            var components = ListPool<Mask>.Get();
            while (t != null)
            {
                t.GetComponents<Mask>(components);
                for (var i = 0; i < components.Count; ++i)
                {
                    if (components[i] != null && components[i].MaskEnabled() && components[i].graphic.IsActive())
                    {
                        ++depth;
                        break;
                    }
                }

                if (t == stopAfter)
                    break;

                t = t.parent;
            }
            ListPool<Mask>.Release(components);
            return depth;
        }


        /// <summary>
        /// Helper function to determine if the child is a descendant of father or is father.
        /// </summary>
        /// <param name="father">The transform to compare against.</param>
        /// <param name="child">The starting transform to search up the hierarchy.</param>
        /// <returns>Is child equal to father or is a descendant.</returns>
        public static bool IsDescendantOrSelf(Transform father, Transform child)
        {
            // HuaHua.
#if OPTIMIZE_UGUI
            if (father == null || child == null)
                return false;

            if ((System.Object)father == (System.Object)child)
                return true;

            Transform parent = child.parent;
            while ((System.Object)parent != null)
            {
                if ((System.Object)parent == (System.Object)father)
                    return true;

                parent = parent.parent;
            }

            return false;
#else
            if (father == null || child == null)
                return false;

            if (father == child)
                return true;

            while (child.parent != null)
            {
                if (child.parent == father)
                    return true;

                child = child.parent;
            }

            return false;
#endif
        }

        /// <summary>
        /// Find the correct RectMask2D for a given IClippable.
        /// </summary>
        /// <param name="clippable">Clippable to search from.</param>
        /// <returns>The Correct RectMask2D</returns>
        public static RectMask2D GetRectMaskForClippable(IClippable clippable)
        {
            // HuaHua.
#if OPTIMIZE_UGUI
            RectMask2D componentToReturn = null;
            GameObject go = clippable.gameObject;

            go.GetComponentsInParent(false, cacheMaskes);
            for (int rmi = 0, count = cacheMaskes.Count; rmi < count; rmi++)
            {
                componentToReturn = cacheMaskes[rmi];
                if ((System.Object)componentToReturn.gameObject == (System.Object)go)
                {
                    componentToReturn = null;
                    continue;
                }
                if (!componentToReturn.isActiveAndEnabled)
                {
                    componentToReturn = null;
                    continue;
                }

                // 这里我们不计算Mask2D和Canvas中间的切断
                //List<Canvas> canvasComponents = ListPool<Canvas>.Get();
                //var components = DictionaryPool<System.Object, bool>.Get();

                //componentToReturn.transform.GetComponentsInParent(false, canvasComponents);
                //foreach (var t in canvasComponents)
                //{
                //    components.Add((System.Object)t.transform, true);
                //}
                //canvasComponents.Clear();

                //go.GetComponentsInParent(false, canvasComponents);
                //for (int i = canvasComponents.Count - 1; i >= 0; i--)
                //{
                //    if (canvasComponents[i].overrideSorting && !components.ContainsKey((System.Object)canvasComponents[i].transform))
                //    {
                //        componentToReturn = null;
                //        break;
                //    }
                //}

                //DictionaryPool<System.Object, bool>.Release(components);
                //ListPool<Canvas>.Release(canvasComponents);
                break;
            }

            cacheMaskes.Clear();

            return componentToReturn;
#else
            List<RectMask2D> rectMaskComponents = ListPool<RectMask2D>.Get();
            List<Canvas> canvasComponents = ListPool<Canvas>.Get();
            RectMask2D componentToReturn = null;

            clippable.gameObject.GetComponentsInParent(false, rectMaskComponents);

            if (rectMaskComponents.Count > 0)
            {
                for (int rmi = 0; rmi < rectMaskComponents.Count; rmi++)
                {
                    componentToReturn = rectMaskComponents[rmi];
                    if (componentToReturn.gameObject == clippable.gameObject)
                    {
                        componentToReturn = null;
                        continue;
                    }
                    if (!componentToReturn.isActiveAndEnabled)
                    {
                        componentToReturn = null;
                        continue;
                    }
                    clippable.gameObject.GetComponentsInParent(false, canvasComponents);
                    for (int i = canvasComponents.Count - 1; i >= 0; i--)
                    {
                        if (!IsDescendantOrSelf(canvasComponents[i].transform, componentToReturn.transform) && canvasComponents[i].overrideSorting)
                        {
                            componentToReturn = null;
                            break;
                        }
                    }
                    break;
                }
            }

            ListPool<RectMask2D>.Release(rectMaskComponents);
            ListPool<Canvas>.Release(canvasComponents);

            return componentToReturn;
#endif
        }

        /// <summary>
        /// Search for all RectMask2D that apply to the given RectMask2D (includes self).
        /// </summary>
        /// <param name="clipper">Starting clipping object.</param>
        /// <param name="masks">The list of Rect masks</param>
        public static void GetRectMasksForClip(RectMask2D clipper, List<RectMask2D> masks)
        {
            masks.Clear();

            List<Canvas> canvasComponents = ListPool<Canvas>.Get();
            List<RectMask2D> rectMaskComponents = ListPool<RectMask2D>.Get();
            clipper.transform.GetComponentsInParent(false, rectMaskComponents);

            if (rectMaskComponents.Count > 0)
            {
                clipper.transform.GetComponentsInParent(false, canvasComponents);
                for (int i = rectMaskComponents.Count - 1; i >= 0; i--)
                {
                    if (!rectMaskComponents[i].IsActive())
                        continue;
                    bool shouldAdd = true;
                    for (int j = canvasComponents.Count - 1; j >= 0; j--)
                    {
                        if (!IsDescendantOrSelf(canvasComponents[j].transform, rectMaskComponents[i].transform) && canvasComponents[j].overrideSorting)
                        {
                            shouldAdd = false;
                            break;
                        }
                    }
                    if (shouldAdd)
                        masks.Add(rectMaskComponents[i]);
                }
            }

            ListPool<RectMask2D>.Release(rectMaskComponents);
            ListPool<Canvas>.Release(canvasComponents);
        }
    }
}
