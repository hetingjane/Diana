/*
This simple module injects a few pieces of knowledge about the avatar
onto the Blackboard, for use by other modules.  This module should be
place on the avatar GameObject, so that when you enable a particular
avatar, you get the correct self-knowledge.

It also provides a static reference to itself, so modules that need to
know something about the avatar can use this to get the needed info.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfKnowledge : ModuleBase
{
	public string avatarName = "Diana";
	public string voices = "Samantha;Microsoft Zira Desktop";

	public static SelfKnowledge instance { get; private set; }

	[Tooltip("Effector bone used for manipulating objects.")]
	public Transform primaryHand;

	[Tooltip("Secondary (non-dominant) hand; may be null.")]
	public Transform secondaryHand;

	protected void Awake() {
		instance = this;
	}

	protected override void Start() {
		base.Start();
		SetValue("me:name", avatarName, "initial state");
		SetValue("me:voice", voices, "initial state");
    }
}
