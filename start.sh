#!/usr/bin/env bash

START_DIR=$(dirname "$0")
START_DIR=$(realpath "$START_DIR")

if [ -d "$1" ]
then
    VENV_DIR=$(realpath "$1")
    echo -e "Using virtualenv directory: ${VENV_DIR}\n"
else
    echo -e "No valid virtualenv directory specified. Using system environement.\nTo specify a virtualenv directory: $0 <venv_dir>\n"
    VENV_DIR="x"
fi

target="machines"

if [ ! -e "$target" ]; then
    target="machines.bak"
fi

params=""
for i in $(grep -e '^[^#].*' $target)
do
    process=${i%@*}
    machine=${i#*@}
    if [ "$machine" = "local" ]; then
        machine="$HOSTNAME"
    fi
    case "$process" in
    "fusion")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python ./fusion/fusion_server.py; bash;'\" --title ${i}"
    ;;
    "lh")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python ./handRecognition/depth_client.py LH; bash;'\" --title ${i}"
    ;;
    "rh")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python ./handRecognition/depth_client.py RH; bash;'\" --title ${i}"
    ;;
    "head")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python ./headRecognition/head_client.py RH; bash;'\" --title ${i}"
    ;;
    "speech")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; sleep 3; python ./speech/speech_client.py; bash;'\" --title ${i}"
    ;;
    "body")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; sleep 3; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python ./skeletonRecognition/apart-together.py; bash;'\" --title ${i}"
    ;;
    *)
    echo "Invalid process specified: ${process}"
    exit
    ;;
    esac
done

cmd="xfce4-terminal ${params}"
cmd=${cmd/--tab/}
#echo "$cmd"
eval $cmd
