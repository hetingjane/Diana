# BlocksWorld
BlocksWorld (new name in progress) is an interactive gesture- and speech-recognizing intelligent agent. See further information [here](https://www.cs.colostate.edu/~draper/CwC.php).

# KSIM
## KSIM requires 
* [MS Kinect SDK (v2)](https://www.microsoft.com/en-us/download/details.aspx?id=44561)
* [MS Speech Platform runtime (v11)](https://www.microsoft.com/en-us/download/details.aspx?id=27225)
* MSP acoustic model(s) for English 
    * ["kinect"](https://www.microsoft.com/en-us/download/details.aspx?id=34809) or ["TELE"(telephony)](https://www.microsoft.com/en-us/download/details.aspx?id=27224) 

## Command line options: 
```
Options:
  -l, --listen=VALUE         the microphone to use in speech module("k" to use
                               kinect, edfault: default microphone array).
  -p, --port=VALUE           port number to use to send kinect streams. (
                               default: 8000)
  -g, --grammar=VALUE        grammar file name to use for speech (cfg or grxml,
                               default: out.grxml)
  -h, --help                 show this message
```

# LFS limitation(s)
We are using GitHub for LFS exclusively. This means we cannot host files over 2G. This excludes the hand models ( /RealTime/models/{LH,RH}/forest.pickle). To acquire these now, use [LH](http://www.cs.colostate.edu/~vision/hand_models/LH/forest.pickle) and [RH](http://www.cs.colostate.edu/~vision/hand_models/RH/forest.pickle)

# Git Flow
We are using git flow now! To get started, first install it ([instructions](https://github.com/nvie/gitflow/wiki/Installation)). Then make sure you're up to date (`git pull --all`) and don't have any uncommitted changes (`git status` says "nothing to commit"). If this failed, commit and push your changes, and then merge master into develop (or let @dwhite54 know). Once you're up to date and have no uncommitted changes, run:
```
git flow init -d
```
If you have any errors, let @dwhite54 know. From here on out, follow the git flow wikis and tutorials as normal (i.e. work from the 'develop' branch, make feature branches). There is basic usage [here](https://github.com/nvie/gitflow).
