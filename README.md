# BlocksWorld
BlocksWorld (new name in progress) is an interactive gesture- and speech-recognizing intelligent agent. See further information [here](https://www.cs.colostate.edu/~draper/CwC.php).

## Requirements
- You shouldn't need any Unity assets since we've added all assets to the repo.
- For the included Python scripts, the following must be installed:
   - Latest Python 3 x64
   - `pip install numpy tensorflow-gpu==1.14.0 opencv-python scipy scikit-image pykinect2`
   - In the location where `pykinect2` is installed, replace existing files with these:
   `wget https://raw.githubusercontent.com/Kinect/PyKinect2/master/pykinect2/PyKinectV2.py https://raw.githubusercontent.com/Kinect/PyKinect2/master/pykinect2/PyKinectRuntime.py`
   - CUDA v10.0
   - Add `%CUDA_PATH%\bin` to `PATH`
   - cuDNN v7.6.3 (just extract the archive over CUDA install directory)
   
## VoxSim Submodule Setup
1. Setup your Git to allow Symbolic link support. 
```
git config --global core.symlinks true
```

2. Open your shell (needs to be "Run as Administrator" on Windows, so that symlinks can be created). Initialize and update the submodules:
```
# git pull if you haven't already, then
git pull --recurse-submodules
git submodule init
git submodule update
```

2a. If in File Explorer you don't see an icon like [this](https://cs.colostate.edu/~dwhite54/symlink.png), then something wend wrong. Proceed to the manual symlink creation in the troubleshooting section below.

3. Then open the BlocksWorld project in Unity, and choose Assets->Import Package->Custom Package, and select "VoxSimPaidAssets.unitypackage" from the BlocksWorld repo root.

Finally, you should be able to open BlocksWorld in Unity and click play after importing.

### VoxSim Submodule troubleshooting
- **Missing Newtonsoft and other assembly references** The symlink wasn't created for some reason. 

First remove the file BlocksWorld/BlocksWorld/Assets/Plugins/VoxSimPlatform. 
Then, from an admin cmd prompt (not tested in git bash/wsl/powershell/mingw):
```
cd BlocksWorld\BlocksWorld\Assets\Plugins
mklink /D VoxSimPlatform ..\..\..\VoxSim\Assets\VoxSimPlatform
```
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

# Contributing
It is highly recommended to read [CONTRIBUTING.md](./CONTRIBUTING.md) before you contribute work to the repository.
