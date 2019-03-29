import subprocess, time, os

print("starting KSIM")
ksim = subprocess.Popen(r"..\KSIM\KSIM\bin\x86\Release\KSIM.exe -k")
time.sleep(1)
print("starting FUSION")
fusion = os.system(r"start cmd /c python -m components.fusion.fusion_server --mode brandeis")
time.sleep(1)
print("starting BODY")
body = os.system(r"start cmd /c python -m components.skeletonRecognition.skeleton_client --model primal")
print("starting SPEECH")
speech = os.system(r"start cmd /c python -m components.speech.speech_client")
print("starting Hands")
RH = os.system(r"start cmd /c python -m components.handRecognition.depth_client")