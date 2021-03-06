# BlocksWorld
BlocksWorld (new name in progress) is an interactive gesture- and speech-recognizing intelligent agent. See further information [here](https://www.cs.colostate.edu/~draper/CwC.php).

=======
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
- Unity comes with [SmartMerge tool](https://docs.unity3d.com/Manual/SmartMerge.html) to properly merge YAML serialized files.
- To use SmartMerge, you should set it as a Git Merge driver as:
  ```
  git config --local merge.unityyamlmerge.driver "'<path_to_smart_merge>' merge -h -p --force %O %B %A %A"
  git config --local merge.unityyamlmerge.recursive binary
  git config --local merge.unityyamlmerge.name "Smart Merge"
  ```
- The `.gitattributes` is already configured to use `unityyamlmerge` merge driver for scenes, prefabs, etc. so the above driver will be invoked automatically whenever you do a merge.
- Additionally, you can modify the `mergespecfile.txt` to enable a fallback merge tool to be run when SmartMerge tool detects merge conflicts. For example, in case of [Meld](http://meldmerge.org) (you can create a similar rule for your own choice of merge tool):
  ```
  * use "%programs%\Meld\Meld.exe" "%l" "%b" "%r" --output "%d"
  ```
  Alternatively, you could achieve the same effect by configuring a merge tool in Git itself instead of `mergespecfile.txt`:
  ```
  git merge.tool meld
  git mergetool.meld.path <path_to_meld.exe>
  git mergetool.meld.prompt false
  git mergetool.meld.keepBackup false
  git mergetool.meld.keepTemporaries false
  ```
  However, the latter means you have to invoke `git mergetool` after `git merge` if there are conflicts to start the merge tool.

## Contributing
See [CONTRIBUTING.md](./CONTRIBUTING.md).
