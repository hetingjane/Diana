/*
This component is a way of defining "expressions" as a collection of blend shape weights,
and then applying (or even mixing) these expressions at runtime.  To use:

1. Attach this to an object with a SkinnedMeshRenderer that has blend shapes.
2. Manually adjust the blend shape weights to form an expression.
3. Right-click the gear icon on this component, and pick "Save current as new expression".
4. Enter a name for the new expression in the Expressions list.
5. Save your work.
6. At runtime, adjust the weight of the expressions in the Expressions list,
then call the Apply method.

*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendShapeMixer : MonoBehaviour
{
    [System.Serializable]
    public struct TargetWeight
    {
        public string blendShape;
        [Range(0, 100)] public float weight;
    }

    [System.Serializable]
    public class Expression
    {
        public string name;
        [Range(0, 100)] public float weight;
        public List<TargetWeight> targets;
    }

    public List<Expression> expressions;
    private readonly float recoveryRate = 50;
    [ContextMenu("Save current as new expression")]
    void SaveCurrent()
    {
        foreach (var e in expressions) e.weight = 0;
        var exp = new Expression
        {
            targets = new List<TargetWeight>(),
            weight = 100
        };
        var smr = GetComponent<SkinnedMeshRenderer>();
        var mesh = smr.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
            if (smr.GetBlendShapeWeight(i) == 0) continue;
            var tw = new TargetWeight
            {
                blendShape = mesh.GetBlendShapeName(i),
                weight = smr.GetBlendShapeWeight(i)
            };
            exp.targets.Add(tw);
        }
        if (expressions == null) expressions = new List<Expression>();
        expressions.Add(exp);
    }


    [ContextMenu("Reset to neutral")]
    public void ResetToNeutral()
    {
        Dictionary<string, float> totalWeight = new Dictionary<string, float>();

        foreach (var exp in expressions)
        {

            foreach (var tv in exp.targets)
            {
                if (tv.blendShape.Contains("Smile")) totalWeight[tv.blendShape] = 30;

                else totalWeight[tv.blendShape] = 0;
            }
        }

        var smr = GetComponent<SkinnedMeshRenderer>();
        var mesh = smr.sharedMesh;

        foreach (var kv in totalWeight)
        {

            float currStrength = smr.GetBlendShapeWeight(mesh.GetBlendShapeIndex(kv.Key));
            currStrength = Mathf.MoveTowards(currStrength, kv.Value, recoveryRate * Time.deltaTime);
            smr.SetBlendShapeWeight(mesh.GetBlendShapeIndex(kv.Key), currStrength);
        }
    }
    [ContextMenu("Apply Expressions")]
    public void Apply(string emo)
    { // This index works as a parameter to control if we want to set the blendshape weights to some value or reset to 0
      // There are opportunities for improvement here; creating and throwing out
      // a dictionary every time is pretty expensive, as is GetBlendShapeIndex.
      // But this works well enough for now.  (--JJS)
        Dictionary<string, float> totalWeight = new Dictionary<string, float>();
        foreach (var exp in expressions)
        {
            if (emo.Contains(exp.name))
            {
                foreach (var tv in exp.targets)
                {
                    float newWeight = tv.weight * exp.weight * 0.01f;
                    if (totalWeight.ContainsKey(tv.blendShape)) totalWeight[tv.blendShape] += newWeight;
                    else totalWeight[tv.blendShape] = newWeight;
                }
            }
            else
            {
                foreach (var tv in exp.targets)
                {
                    if (tv.blendShape.Contains("Smile")) totalWeight[tv.blendShape] = 30;

                    else totalWeight[tv.blendShape] = 0;
                }
            }

        }
        var smr = GetComponent<SkinnedMeshRenderer>();
        var mesh = smr.sharedMesh;

        foreach (var kv in totalWeight)
        {

            float currStrength = smr.GetBlendShapeWeight(mesh.GetBlendShapeIndex(kv.Key));
            currStrength = Mathf.MoveTowards(currStrength, kv.Value, recoveryRate * Time.deltaTime);
            smr.SetBlendShapeWeight(mesh.GetBlendShapeIndex(kv.Key), currStrength);


        }
    }

}
