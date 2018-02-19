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

device=0
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
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python -m components.fusion.fusion_server; bash;'\" --title ${i}"
    ;;
    "lh")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; export CUDA_VISIBLE_DEVICES=${device}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python -m components.handRecognition.depth_client LH; bash;'\" --title ${i}"
    if [ "$2" = "--single-machine" ]
    then
        ((device++))
    fi
    ;;
    "rh")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; export CUDA_VISIBLE_DEVICES=${device}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python -m components.handRecognition.depth_client RH; bash;'\" --title ${i}"
    if [ "$2" = "--single-machine" ]
    then
        ((device++))
    fi
    ;;
    "head")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; export CUDA_VISIBLE_DEVICES=${device}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python -m components.headRecognition.head_client; bash;'\" --title ${i}"
    if [ "$2" = "--single-machine" ]
    then
        ((device++))
    fi
    ;;
    "speech")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; sleep 3; python -m components.speech.speech_client; bash;'\" --title ${i}"
    ;;
    "body")
    params="$params --tab -e \"ssh -t ${machine} 'cd ${START_DIR}; sleep 3; export CUDA_VISIBLE_DEVICES=${device}; if [ ${VENV_DIR} != 'x' ]; then source ${VENV_DIR}/bin/activate; fi; python -m components.skeletonRecognition.body_client; bash;'\" --title ${i}"
    if [ "$2" = "--single-machine" ]
    then
        ((device++))
    fi
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
