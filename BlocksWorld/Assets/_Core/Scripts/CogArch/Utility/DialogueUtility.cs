using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning.QSR;

public static class DialogueUtility
{
    public static bool SurfaceClear(GameObject block, out GameObject blocker) {
        Debug.Log(block);
        bool surfaceClear = true;
        blocker = null;
        List<GameObject> excludeChildren = block.GetComponentsInChildren<Renderer>().Where(
            o => (GlobalHelper.GetMostImmediateParentVoxeme(o.gameObject) != block)).Select(o => o.gameObject).ToList();
        foreach (GameObject go in excludeChildren) {
            Debug.Log(go);
        }

        Bounds blockBounds = GlobalHelper.GetObjectWorldSize(block, excludeChildren);
        Debug.Log(blockBounds);
        Debug.Log(GlobalHelper.GetObjectWorldSize(block).max.y);
        Debug.Log(GlobalHelper.GetObjectWorldSize(block, excludeChildren).max.y);
        Debug.Log(blockBounds.max.y);
	    foreach (Transform otherBlock in block.transform) {
            excludeChildren = otherBlock.GetComponentsInChildren<Renderer>().Where(
	            o => (GlobalHelper.GetMostImmediateParentVoxeme(o.gameObject) != otherBlock.gameObject)).Select(o => o.gameObject).ToList();
            foreach (GameObject go in excludeChildren) {
                Debug.Log(go);
            }

		    Bounds otherBounds = GlobalHelper.GetObjectWorldSize(otherBlock.gameObject, excludeChildren);
            Debug.Log(otherBlock);
            Debug.Log(otherBounds);
		    Debug.Log(GlobalHelper.GetObjectWorldSize(otherBlock.gameObject).min.y);
		    Debug.Log(GlobalHelper.GetObjectWorldSize(otherBlock.gameObject, excludeChildren).min.y);
            Debug.Log(otherBounds.min.y);
            Region blockMax = new Region(new Vector3(blockBounds.min.x, blockBounds.max.y, blockBounds.min.z),
                new Vector3(blockBounds.max.x, blockBounds.max.y, blockBounds.max.z));
            Region otherMin = new Region(new Vector3(otherBounds.min.x, blockBounds.max.y, otherBounds.min.z),
                new Vector3(otherBounds.max.x, blockBounds.max.y, otherBounds.max.z));
//          if ((QSR.Above (otherBounds, blockBounds)) && (!QSR.Left (otherBounds, blockBounds)) &&
//              (!QSR.Right (otherBounds, blockBounds)) && (RCC8.EC (otherBounds, blockBounds))) {
            Debug.Log(GlobalHelper.RegionToString(blockMax));
            Debug.Log(GlobalHelper.RegionToString(otherMin));
            Debug.Log(GlobalHelper.RegionToString(GlobalHelper.RegionOfIntersection(blockMax, otherMin, Constants.MajorAxis.Y)));
            Debug.Log(QSR.Above(otherBounds, blockBounds));
            Debug.Log(
                ((GlobalHelper.RegionOfIntersection(blockMax, otherMin, Constants.MajorAxis.Y).Area() / blockMax.Area())));
            Debug.Log(RCC8.EC(otherBounds, blockBounds));
            if ((QSR.Above(otherBounds, blockBounds)) &&
                ((GlobalHelper.RegionOfIntersection(blockMax, otherMin, Constants.MajorAxis.Y).Area() / blockMax.Area()) >
                 0.25f) &&
                (RCC8.EC(otherBounds, blockBounds))) {
                surfaceClear = false;
	            blocker = otherBlock.gameObject;
                break;
            }
        }

        Debug.Log(surfaceClear);
        return surfaceClear;
    }

    public static bool FitsTouching(GameObject theme, Transform grabbableBlocks, GameObject obj, string dir) {
        bool fits = true;

        Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
        Bounds objBounds = GlobalHelper.GetObjectWorldSize(obj);

        foreach (GameObject test in grabbableBlocks) {
            if ((test != theme) && (test != obj)) {
                if (dir == "left") {
                    Bounds projectedBounds = new Bounds(
                        new Vector3(objBounds.min.x - themeBounds.extents.x, objBounds.center.y, objBounds.center.z),
                        themeBounds.size);
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test))) {
                        fits = false;
                    }
                }
                else if (dir == "right") {
                    Bounds projectedBounds = new Bounds(
                        new Vector3(objBounds.max.x + themeBounds.extents.x, objBounds.center.y, objBounds.center.z),
                        themeBounds.size);
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test))) {
                        fits = false;
                    }
                }
                else if (dir == "in_front") {
                    Bounds projectedBounds = new Bounds(
                        new Vector3(objBounds.center.x, objBounds.center.y, objBounds.min.z - themeBounds.extents.z),
                        themeBounds.size);
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test))) {
                        fits = false;
                    }
                }
                else if (dir == "behind") {
                    Bounds projectedBounds = new Bounds(
                        new Vector3(objBounds.center.x, objBounds.center.y, objBounds.max.z + themeBounds.extents.z),
                        themeBounds.size);
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test))) {
                        fits = false;
                    }
                }
            }
        }

        return fits;
    }
}
