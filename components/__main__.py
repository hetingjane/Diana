import subprocess, time, os

print("starting KSIM")
ksim = subprocess.Popen("C:\\Users\\david\\Desktop\\portable\\KSIM\\KSIM\\bin\\Release\\KSIM.exe --listen m")
print("starting FUSION")
fusion = os.system(r"start cmd /c C:\Users\david\Anaconda3\python -m components.fusion.fusion_server --mode brandeis")
print("starting BODY")
body = os.system(r"start cmd /c C:\Users\david\Anaconda3\python -m components.skeletonRecognition.skeleton_client --fusion-host localhost localhost")
print("starting SPEECH")
speech = os.system(r"start cmd /c C:\Users\david\Anaconda3\python -m components.speech.speech_client --fusion-host localhost localhost")
print("starting Hands")
RH = os.system(r"start cmd /c C:\Users\david\Anaconda3\python -m components.handRecognition.depth_client -f localhost -k localhost")