echo 'logging into blue for fusion'
gnome-terminal -x bash -c "ssh -t blue 'cd $PWD/fusion/; python fusion_server.py;bash;'" &
sleep 2

echo 'logging into corvette for Depth LH'
gnome-terminal -x bash -c "ssh -t corvette 'cd $PWD/handRecognition/; python depth_client_1.py LH;bash;'" &
sleep 1

echo 'logging into maserati for Depth RH'
gnome-terminal -x bash -c "ssh -t maserati 'cd $PWD/handRecognition/; python depth_client_1.py RH;bash;'" &
sleep 1

echo 'logging into cyan for skeleton recogntion'
gnome-terminal -x bash -c "ssh -t cyan 'cd $PWD/skeletonRecognition/; python engage-disengage.py;bash;'" &
