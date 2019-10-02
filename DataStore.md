# General Principles

Data store keys form a hierarchy, with two or more parts separated by colons.  
The top-level part is always either "me" (refers to something about Diana) or
"user" (refers to the human participant).

Groupings at the second level are somewhat arbitrary, but do try to group related things together.

As a general rule, each part of a key should be named like a variable or property in C#, i.e., lowerCamelCase.
(But we haven't been entirely consistent about this, and it's no big deal.)


# Key-Value Pairs

|Key                          |Type         |Value(s)|Meaning                                                                   |
|-----------------------------|-------------|--------|--------------------------------------------------------------------------|
|`me:actual:handPosR`         |Vector3      |Position|Position of Diana's right hand in world coordinates             |
|`me:alertness`               |Integer      |0 to 10 |How alert Diana is; 0 = asleep, 7 = normal, 10 = hyperexcited             |
|`me:attending`               |String       |        |What Diana is paying attention to: "none", "user"                         |
|`me:eyes:open`               |Integer      |0 to 100|Current position of Diana's eyelids; 0 = closed, 100 = wide open          |
|`me:holding`                 |String       |block name|Name of block Diana is currently holding             |
|`me:intent:action`           |String       |        |What action Diana intends to do: "point", "grab", etc.                    |
|`me:intent:eyesClosed`       |Boolean      |T or F  |Whether Diana intends to close her eyes (e.g. because told to do so)      |
|`me:intent:handPosR`         |Vector3      |Position|Position in world coordinates where Diana wants her hand to be             |
|`me:intent:lookAt`           |String       |        |Name of what Diana intends to look at, e.g. "userPoint"                   |
|`me:intent:pointAt`          |String       |        |Name of what Diana intends to point at, e.g. "userPoint"                  |
|`me:intent:target`           |Vector3      |        |target of me.intent.action, specifying location of interest               |
|`me:name`                    |String       |        |Avatar's own name (currently "Diana", "Sam", or "Botarm"            |
|`me:speech:current`          |String       |        |Text that Diana is currently speaking                                     |
|`me:speech:intent`           |String       |        |Text that Diana intends to speak                                          |
|`me:standingBy`              |Boolean      |T or F  |Whether agent is on "stand by" and ignoring input until she hears her name |
|`me:voice`                   |String       |voice name|Name of voice (or voices) agent should use, e.g.: "Victoria;Microsoft Zira Desktop"|
|`user:armMotion:left`        |String       |        |servo behavior of left arm: "servo" or "still" |
|`user:armMotion:right`       |String       |        |servo behavior of right arm: "servo" or "still" |
|`user:communication`         |Communication|        |Semantic representation of the user's last utterance                      |
|`user:dominantEmotion:`      |String       |        |Current user dominant emotion, e.g. "Happy", "Angry"                      |
|`user:dominantEmotion:Angry` |Integer      |0 to 100|Measure of how angry the user looks                                       |
|`user:dominantEmotion:Happy` |Integer      |0 to 100|Measure of how happy the user looks                                       |
|`user:isEngaged`             |Boolean      |T or F  |True if closest body frame is engaged and false otherwise                 |
|`user:isPointing`            |Boolean      |T or F  |Whether the user appears to be pointing (anywhere at all)                 |
|`user:isPointing:left`       |Boolean      |T or F  |Whether the user appears to be pointing with left hand (anywhere at all)  |
|`user:isPointing:right`      |Boolean      |T or F  |Whether the user appears to be pointing with right hand (anywhere at all)  |
|`user:isSpeaking`            |Boolean      |T or F  |Whether the user appears to be speaking                                   |
|`user:hands:[right,left]`    |String       |        |Current hand pose for either left or right hand							|
|`user:joint:AnkleLeft`       |Vector3      |Position|Location of left ankle point of closest body frame in "camera space"      |
|`user:joint:AnkleRight`      |Vector3      |Position|Location of right ankle point of closest body frame in "camera space"     |
|`user:joint:ElbowLeft`       |Vector3      |Position|Location of left elbow point of closest body frame in "camera space"      |
|`user:joint:ElbowRight`      |Vector3      |Position|Location of right elbow point of closest body frame in "camera space"     |
|`user:joint:FootLeft`        |Vector3      |Position|Location of left foot point of closest body frame in "camera space"       |
|`user:joint:FootRight`       |Vector3      |Position|Location of right foot point of closest body frame in "camera space"      |
|`user:joint:HandLeft`        |Vector3      |Position|Location of left Hand point of closest body frame in "camera space"       |
|`user:joint:HandRight`       |Vector3      |Position|Location of right hand point of closest body frame in "camera space"      |
|`user:joint:HandTipLeft`     |Vector3      |Position|Location of left hand tip point of closest body frame in "camera space"   |
|`user:joint:HandTipRight`    |Vector3      |Position|Location of right hand tip point of closest body frame in "camera space"  |
|`user:joint:Head`            |Vector3      |Position|Location of head point of closest body frame in "camera space"            |
|`user:joint:HipLeft`         |Vector3      |Position|Location of left hip point of closest body frame in "camera space"        |
|`user:joint:HipRight`        |Vector3      |Position|Location of right hip point of closest body frame in "camera space"       |
|`user:joint:inferred:<joint>`|Boolean      |T or F  |Whether joint <joint> is inferred |
|`user:joint:KneeLeft`        |Vector3      |Position|Location of left knee point of closest body frame in "camera space"       |
|`user:joint:KneeRight`       |Vector3      |Position|Location of right knee point of closest body frame in "camera space"      |
|`user:joint:Neck`            |Vector3      |Position|Location of neck point of closest body frame in "camera space"            |
|`user:joint:ShoulderLeft`    |Vector3      |Position|Location of left shoulder point of closest body frame in "camera space"   |
|`user:joint:ShoulderRight`   |Vector3      |Position|Location of right shoulder point of closest body frame in "camera space"  |
|`user:joint:SpineBase`       |Vector3      |Position|Location of spine base point of closest body frame in "camera space"      |
|`user:joint:SpineMid`        |Vector3      |Position|Location of spine mid point of closest body frame in "camera space"       |
|`user:joint:SpineShoulder`   |Vector3      |Position|Location of spine shoulder point of closest body frame in "camera space"  |
|`user:joint:ThumbLeft`       |Vector3      |Position|Location of left hand thumb point of closest body frame in "camera space" |
|`user:joint:ThumbRight`      |Vector3      |Position|Location of right hand thumb point of closest body frame in "camera space"|
|`user:joint:timestamp`       |String       |Reals>=0|Timestamp of arrival of body frame                                        |
|`user:joint:tracked:<joint>` |Boolean      |T or F  |Whether joint <joint> is tracked |
|`user:joint:WristLeft`       |Vector3      |Position|Location of left wrist point of closest body frame in "camera space"      |
|`user:joint:WristRight`      |Vector3      |Position|Location of right wrist point of closest body frame in "camera space"     |
|`user:jointOrientation:<joint>`|Quaternion |Orientation|Joint local orientation (relative to parent joint) |
|`user:parse`                 |WordValue    |        |Shallow parse of the user's input (probably not used with VoxSim) |
|`user:pointPos`              |Vector3      |        |Position the user is pointing at in the scene                             |
|`user:pointPos:left`         |Vector3      |Position|Location of left hand pointing position in camera space, calibration only. (z==0)            |
|`user:pointPos:right`        |Vector3      |Position|Location of right hand pointing position in camera space, calibration only. (z==0)           |
|`user:pointValid`            |Boolean      |T or F  |True if the user is pointing at a valid location (Table) false otherwise  |
|`user:pointValid:left`       |Boolean      |T or F  |True if user is pointing with their left hand at a valid location (Table) false otherwise |
|`user:pointValid:right`      |Boolean      |T or F  |True if user is pointing with their right hand at a valid location (Table) false otherwise |
|`user:speech`                |String       |        |Text of utterance user has just spoken (or typed)                         |
