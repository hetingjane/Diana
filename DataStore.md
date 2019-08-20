# Key-Value Pairs

|Key                        |Type   |Value(s)|Meaning                                                                   |
|---------------------------|-------|--------|--------------------------------------------------------------------------|
|me:alertness               |Integer|0 to 10 |How alert Diana is; 0 = asleep, 7 = normal, 10 = hyperexcited             |
|me:alertness               |Integer|0 to 10 |How alert Diana is; 0 = asleep, 7 = normal, 10 = hyperexcited             |
|me:attending               |String |        |What Diana is paying attention to: "none", "user"                         |
|me:eyes:open               |Integer|0 to 100|Current position of Diana's eyelids; 0 = closed, 100 = wide open          |
|me:gesture                 |String |        |Name of gesture user is currently doing, e.g. "stop", "servo left", etc.  |
|me:intent:action           |String |        |What action Diana intends to do: "point", "grab", etc.                    |
|me:intent:eyesClosed       |Boolean|T or F  |Whether Diana intends to close her eyes (e.g. because told to do so)      |
|me:intent:lookAt           |String |        |Name of what Diana intends to look at, e.g. "userPoint"                   |
|me:intent:pointAt          |String |        |Name of what Diana intends to point at, e.g. "userPoint"                  |
|me:intent:target           |Vector3|        |target of me.intent.action, specifying location of interest               |
|me:speech:current          |String |        |Text that Diana is currently speaking                                     |
|me:speech:intent           |String |        |Text that Diana intends to speak                                          |
|user:communication         |Communication|  |Semantic representation of the user's last utterance                      |
|user:dominant emotion:Angry|Integer|1 to 100|Measure of how angry the user looks                                       |
|user:dominant emotion:Happy|Integer|1 to 100|Measure of how happy the user looks                                       |
|user:engaged               |Boolean|T or F  |True if closest body frame is engaged and false otherwise                 |
|user:isPointing            |Boolean|T or F  |Whether the user appears to be pointing (anywhere at all)                 |
|user:isSpeaking            |Boolean|T or F  |Whether the user appears to be speaking                                   |
|user:joint:AnkleLeft       |Vector3|Reals   |Location of left ankle point of closest body frame in "camera space"      |
|user:joint:AnkleRight      |Vector3|Reals   |Location of right ankle point of closest body frame in "camera space"     |
|user:joint:ElbowLeft       |Vector3|Reals   |Location of left elbow point of closest body frame in "camera space"      |
|user:joint:ElbowRight      |Vector3|Reals   |Location of right elbow point of closest body frame in "camera space"     |
|user:joint:FootLeft        |Vector3|Reals   |Location of left foot point of closest body frame in "camera space"       |
|user:joint:FootRight       |Vector3|Reals   |Location of right foot point of closest body frame in "camera space"      |
|user:joint:HandLeft        |Vector3|Reals   |Location of left Hand point of closest body frame in "camera space"       |
|user:joint:HandRight       |Vector3|Reals   |Location of right hand point of closest body frame in "camera space"      |
|user:joint:HandTipLeft     |Vector3|Reals   |Location of left hand tip point of closest body frame in "camera space"   |
|user:joint:HandTipRight    |Vector3|Reals   |Location of right hand tip point of closest body frame in "camera space"  |
|user:joint:Head            |Vector3|Reals   |Location of head point of closest body frame in "camera space"            |
|user:joint:HipLeft         |Vector3|Reals   |Location of left hip point of closest body frame in "camera space"        |
|user:joint:HipRight        |Vector3|Reals   |Location of right hip point of closest body frame in "camera space"       |
|user:joint:KneeLeft        |Vector3|Reals   |Location of left knee point of closest body frame in "camera space"       |
|user:joint:KneeRight       |Vector3|Reals   |Location of right knee point of closest body frame in "camera space"      |
|user:joint:Neck            |Vector3|Reals   |Location of neck point of closest body frame in "camera space"            |
|user:joint:ShoulderLeft    |Vector3|Reals   |Location of left shoulder point of closest body frame in "camera space"   |
|user:joint:ShoulderRight   |Vector3|Reals   |Location of right shoulder point of closest body frame in "camera space"  |
|user:joint:SpineBase       |Vector3|Reals   |Location of spine base point of closest body frame in "camera space"      |
|user:joint:SpineMid        |Vector3|Reals   |Location of spine mid point of closest body frame in "camera space"       |
|user:joint:SpineShoulder   |Vector3|Reals   |Location of spine shoulder point of closest body frame in "camera space"  |
|user:joint:ThumbLeft       |Vector3|Reals   |Location of left hand thumb point of closest body frame in "camera space" | 
|user:joint:ThumbRight      |Vector3|Reals   |Location of right hand thumb point of closest body frame in "camera space"| 
|user:joint:timestamp       |String |Reals>=0|Timestamp of arrival of body frame                                        |
|user:joint:WristLeft       |Vector3|Reals   |Location of left wrist point of closest body frame in "camera space"      |
|user:joint:WristRight      |Vector3|Reals   |Location of right wrist point of closest body frame in "camera space"     |
|user:pointPos              |Vector3|        |Position the user is pointing at in the scene                             |
|user:pointPos:left         |Vector3|Reals   |Location of left hand pointing position in pixel space. (z==0)            |
|user:pointPos:left:valid   |Boolean|T or F  |True if pointing at screen and false if not pointing at screen            |
|user:pointPos:right        |Vector3|Reals   |Location of right hand pointing position in pixel space. (z==0)           |
|user:pointPos:right:valid  |Boolean|T or F  |True if pointing at screen and false if not pointing at screen            |
|user:pointValid            |Boolean|T or F  |True if the user is pointing at a valid location (Table) false otherwise  |
|user:speech                |String |        |Text of utterance user has just spoken (or typed)                         |

