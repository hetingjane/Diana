START_DIR=$(dirname "$0")
START_DIR=$(realpath "$START_DIR")

target="machines"

if [ ! -e "$target" ]; then
    target="machines.bak"
fi

params=""
for i in $(cat $target)
do
    process=${i%@*}
    machine=${i#*@}
    if [ "$machine" = "local" ]; then
        machine="$HOSTNAME"
    fi
    case "$process" in
    "fusion")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; python ./fusion/fusion_server.py; bash;'\" -t ${i}"
    ;;
    "lh")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; source env/bin/activate; python ./handRecognition/depth_client.py LH; bash;'\" -t ${i}"
    ;;
    "rh")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; source env/bin/activate; python ./handRecognition/depth_client.py RH; bash;'\" -t ${i}"
    ;;
    "head")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; source env/bin/activate; python ./headRecognition/head_client.py RH; bash;'\" -t ${i}"
    ;;
    "speech")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; source env/bin/activate; sleep 3; python ./speech/speech_client.py; bash;'\" -t ${i}"
    ;;
    "body")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; python ./skeletonRecognition/apart-together.py; bash;'\" -t ${i}"
    ;;
    *)
    echo "Invalid process specified: ${process}"
    exit
    ;;
    esac
done

cmd="gnome-terminal ${params}"
#echo "$cmd"
eval $cmd
