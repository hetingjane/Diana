# BlocksWorld
BlocksWorld (new name in progress) is an interactive gesture- and speech-recognizing intelligent agent. See further information [here](https://www.cs.colostate.edu/~draper/CwC.php).

## Requirements
- You shouldn't need anything else since we've added all assets to the repo.

# KSIM
## Requirements
* [MS Kinect SDK (v2)](https://www.microsoft.com/en-us/download/details.aspx?id=44561)
* [MS Speech Platform runtime (v11)](https://www.microsoft.com/en-us/download/details.aspx?id=27225)
* MSP acoustic model(s) for English 
    * ["kinect"](https://www.microsoft.com/en-us/download/details.aspx?id=34809) or ["TELE"(telephony)](https://www.microsoft.com/en-us/download/details.aspx?id=27224) 

# LFS limitation(s)
We are using GitHub for LFS exclusively. This means we cannot host files over 2G. This excludes the hand models ( /RealTime/models/{LH,RH}/forest.pickle). To acquire these now, use [LH](http://www.cs.colostate.edu/~vision/hand_models/LH/forest.pickle) and [RH](http://www.cs.colostate.edu/~vision/hand_models/RH/forest.pickle)

## Actually...
We are using the toucan GitLab server for LFS, which has no limits. Still, find the hand models as above.

# Git Flow
We are using git flow now! To get started, first install it ([instructions](https://github.com/nvie/gitflow/wiki/Installation)). 

## Migrating
This isn't necessary unless you've not pulled the repo recently. See this file's history if that's somehow the case.

If you have any errors, let @dwhite54 know. From here on out, follow the git flow wikis and tutorials as normal (i.e. work from the 'develop' branch, make feature branches). There is basic usage [here](https://github.com/nvie/gitflow).

# Unity merge tool
- To properly merge \*.unity files, see here: https://docs.unity3d.com/Manual/SmartMerge.html
