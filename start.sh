
echo 'logging into chip for synchronization'
gnome-terminal -x bash -c "ssh -t chip 'cd $PWD/synchronization/; python fusion_server.py;bash;'" &
sleep 10

echo 'logging into corvette for Depth LH'
gnome-terminal -x bash -c "ssh -t corvette 'cd $PWD/handRecognition/; python depth_client_1.py LH;bash;'" &
sleep 1

echo 'logging into maserati for Depth RH'
gnome-terminal -x bash -c "ssh -t maserati 'cd $PWD/handRecognition/; python depth_client_1.py RH;bash;'" &
sleep 1

echo 'logging into cyan for skeleton recogntion'
gnome-terminal -x bash -c "ssh -t cyan 'cd $PWD/skeletonRecognition/; python prady_gui.py;bash;'" &
