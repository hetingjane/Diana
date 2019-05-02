# BlocksWorld
BlocksWorld (new name in progress) is an interactive gesture- and speech-recognizing intelligent agent. See further information [here](https://www.cs.colostate.edu/~draper/CwC.php).

## Requirements
- [Unity Standard Assets](https://assetstore.unity.com/packages/essentials/asset-packs/standard-assets-32351)
- [Wooden PBR Table](https://assetstore.unity.com/packages/3d/props/wooden-pbr-table-112005)
- [MCS Female](https://assetstore.unity.com/packages/3d/characters/humanoids/mcs-female-45807)

## Asset package link
- [Latest asset export](https://drive.google.com/drive/folders/1TwdqCKDnCHP8_7xpAcJHa8Sve7za8JQu?usp=sharing)

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
