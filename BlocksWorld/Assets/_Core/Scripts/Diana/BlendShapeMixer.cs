﻿/*
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
	public struct TargetWeight {
		public string blendShape;
		[Range(0,100)] public float weight;
	}
	
	[System.Serializable]
	public class Expression {
		public string name;
		[Range(0,100)] public float weight;
		public List<TargetWeight> targets;
	}

	public List<Expression> expressions;
	
	[ContextMenu("Save current as new expression")]
	void SaveCurrent() {
		foreach (var e in expressions) e.weight = 0;
		var exp = new Expression();
		exp.targets = new List<TargetWeight>();
		exp.weight = 100;
		var smr = GetComponent<SkinnedMeshRenderer>();
		var mesh = smr.sharedMesh;
		for (int i=0; i<mesh.blendShapeCount; i++) {
			if (smr.GetBlendShapeWeight(i) == 0) continue;
			var tw = new TargetWeight();
			tw.blendShape = mesh.GetBlendShapeName(i);
			tw.weight = smr.GetBlendShapeWeight(i);
			exp.targets.Add(tw);
		}
		if (expressions == null) expressions = new List<Expression>();
		expressions.Add(exp);
	}

	
	[ContextMenu("Reset to neutral")]
	void ResetToNeutral() {
		foreach (var exp in expressions) {
			exp.weight = 0;
		}
		Apply();
	}
	
	[ContextMenu("Apply Expressions")]
	void Apply() {
		// There are opportunities for improvement here; creating and throwing out
		// a dictionary every time is pretty expensive, as is GetBlendShapeIndex.
		// But this works well enough for now.  (--JJS)
		Dictionary<string, float> totalWeight = new Dictionary<string, float>();
		foreach (var exp in expressions) {
			foreach (var tv in exp.targets) {
				float newWeight = tv.weight * exp.weight * 0.01f;
				if (totalWeight.ContainsKey(tv.blendShape)) totalWeight[tv.blendShape] += newWeight;
				else totalWeight[tv.blendShape] = newWeight;
			}
		}
		var smr = GetComponent<SkinnedMeshRenderer>();
		var mesh = smr.sharedMesh;
		foreach (var kv in totalWeight) {
			smr.SetBlendShapeWeight(mesh.GetBlendShapeIndex(kv.Key), kv.Value);
		}
	}
}
