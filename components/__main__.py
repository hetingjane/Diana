import subprocess, time, os

print("starting KSIM")
ksim = subprocess.Popen(r"..\KSIM\KSIM\bin\x64\Release\KSIM.exe --listen m")
print("starting FUSION")
fusion = os.system(r"start cmd /c python -m components.fusion.fusion_server --mode brandeis")
print("starting BODY")
body = os.system(r"start cmd /c python -m components.skeletonRecognition.skeleton_client --fusion-host localhost localhost")
print("starting SPEECH")
speech = os.system(r"start cmd /c python -m components.speech.speech_client --fusion-host localhost localhost")
print("starting Hands")
RH = os.system(r"start cmd /c python -m components.handRecognition.depth_client -f localhost -k localhost")