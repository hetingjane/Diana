using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using VoxSimPlatform.Global;
using VoxSimPlatform.SpatialReasoning.QSR;
using VoxSimPlatform.Vox;

public static class DialogueUtility
{
    public static bool SurfaceClear(GameObject block, out GameObject blocker) {
        Debug.Log(string.Format("Is the surface of {0} clear?",block.name));
        bool surfaceClear = true;
        blocker = null;
        List<GameObject> excludeChildren = block.GetComponentsInChildren<Renderer>().Where(
            o => (GlobalHelper.GetMostImmediateParentVoxeme(o.gameObject) != block)).Select(o => o.gameObject).ToList();

        Bounds blockBounds = GlobalHelper.GetObjectWorldSize(block, excludeChildren);
	    foreach (Transform otherBlock in block.transform) {
            excludeChildren = otherBlock.GetComponentsInChildren<Renderer>().Where(
	            o => (GlobalHelper.GetMostImmediateParentVoxeme(o.gameObject) != otherBlock.gameObject)).Select(o => o.gameObject).ToList();

		    Bounds otherBounds = GlobalHelper.GetObjectWorldSize(otherBlock.gameObject, excludeChildren);
            Region blockMax = new Region(new Vector3(blockBounds.min.x, blockBounds.max.y, blockBounds.min.z),
                new Vector3(blockBounds.max.x, blockBounds.max.y, blockBounds.max.z));
            Region otherMin = new Region(new Vector3(otherBounds.min.x, blockBounds.max.y, otherBounds.min.z),
                new Vector3(otherBounds.max.x, blockBounds.max.y, otherBounds.max.z));
            if ((QSR.Above(otherBounds, blockBounds)) &&
                ((GlobalHelper.RegionOfIntersection(blockMax, otherMin, Constants.MajorAxis.Y).Area() / blockMax.Area()) >
                 0.25f) &&
                (RCC8.EC(otherBounds, blockBounds))) {
                surfaceClear = false;
	            blocker = otherBlock.gameObject;
                Debug.Log(string.Format("{0} is above {1} and covers more than 25% of {1}'s surface",
                    blocker.name, block.name));
                break;
            }
        }

        Debug.Log(string.Format("SurfaceClear({0}):{1}",block.name,surfaceClear));
        return surfaceClear;
    }

    public static bool FitsTouching(GameObject theme, Transform grabbableBlocks, GameObject obj, string dir) {
        bool fits = true;

        Bounds themeBounds = GlobalHelper.GetObjectWorldSize(theme);
        Bounds objBounds = GlobalHelper.GetObjectWorldSize(obj);

        foreach (Transform test in grabbableBlocks) {
            if ((test != theme) && (test != obj)) {
                if (dir == "left") {
                    Bounds projectedBounds = new Bounds(
                        new Vector3(objBounds.min.x - themeBounds.extents.x, objBounds.center.y, objBounds.center.z),
                        themeBounds.size);
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject))) {
                        fits = false;
                    }
                }
                else if (dir == "right") {
                    Bounds projectedBounds = new Bounds(
                        new Vector3(objBounds.max.x + themeBounds.extents.x, objBounds.center.y, objBounds.center.z),
                        themeBounds.size);
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject))) {
                        fits = false;
                    }
                }
                else if (dir == "front") {
                    Bounds projectedBounds = new Bounds(
                        new Vector3(objBounds.center.x, objBounds.center.y, objBounds.max.z + themeBounds.extents.z),
                        themeBounds.size);
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject))) {
                        fits = false;
                    }
                }
                else if (dir == "back") {
                    Bounds projectedBounds = new Bounds(
                        new Vector3(objBounds.center.x, objBounds.center.y, objBounds.min.z - themeBounds.extents.z),
                        themeBounds.size);
                    if (!RCC8.DC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject)) &&
                        !RCC8.EC(projectedBounds, GlobalHelper.GetObjectWorldSize(test.gameObject))) {
                        fits = false;
                    }
                }
            }
        }

        return fits;
    }

    public static string GetPredicateType(string pred, VoxMLLibrary voxmllLibrary) {
        string type = string.Empty;

        if (voxmllLibrary.VoxMLEntityTypeDict.ContainsKey(pred)) {
            type = voxmllLibrary.VoxMLEntityTypeDict[pred];
        }

        return type;
    }
}
