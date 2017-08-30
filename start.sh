#!/usr/bin/env bash
echo 'logging into blue for fusion'
gnome-terminal -x bash -c "ssh -t blue 'cd $PWD/fusion/; python fusion_server.py;bash;'" &
sleep 2

echo 'logging into maserati for Depth RH'
gnome-terminal -x bash -c "ssh -t maserati 'cd $PWD/handRecognition/; python depth_client.py RH;bash;'" &
sleep 1

echo 'logging into corvette for Depth LH'
gnome-terminal -x bash -c "ssh -t corvette 'cd $PWD/handRecognition/; python depth_client.py LH;bash;'" &
sleep 1

echo 'logging into lotus for Depth Head'
gnome-terminal -x bash -c "ssh -t lotus 'cd $PWD/headRecognition/; python head_client.py;bash;'" &
sleep 1

echo 'logging into cyan for skeleton recogntion'
gnome-terminal -x bash -c "ssh -t cyan 'cd $PWD/skeletonRecognition/; python apart-together.py;bash;'" &

echo 'Running speech client locally'
gnome-terminal --window --working-directory="$START_DIR" --command "python ./speech/speech_client.py"
