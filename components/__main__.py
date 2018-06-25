import subprocess, time, os

print("starting KSIM")
ksim = subprocess.Popen("F:\\portable\\KSIM\\KSIM\\bin\\Release\\KSIM.exe --listen m")
print("starting FUSION")
fusion = os.system("start cmd /c python.exe -m components.fusion.fusion_server --mode brandeis")
print("starting RH")
RH = os.system("start cmd /c python -m components.handRecognition.depth_client --fusion-host localhost RH localhost")
print("starting LH")
LH = os.system("start cmd /c python -m components.handRecognition.depth_client --fusion-host localhost LH localhost")
print("starting SPEECH")
speech = os.system("start cmd /c python -m components.speech.speech_client --fusion-host localhost localhost")
print("starting BODY")
body = os.system("start cmd /c python -m components.skeletonRecognition.body_client --fusion-host localhost localhost")
print("starting HEAD")
head = os.system("start cmd /c python -m components.headRecognition.head_client --fusion-host localhost localhost")
