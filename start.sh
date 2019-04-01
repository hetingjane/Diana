#!/usr/bin/env bash

function print_usage
{
    echo -e "\nUsage: start.sh [-h|--help] [-e|--env <virtual_env>] [-c|--conf <machine_specification> default:machines.bak] [-s|--single-machine default:multi-machine] [-p|--pointing-mode <desk|screen> default:screen] [-w|--wait-for-fusion <seconds> default:0]\n"
}
    
# full path to directory where start.sh resides
start_dir=$(dirname "$0")
start_dir=$(realpath "$start_dir")

# parse arguments with getopt
my_args=$(getopt -o he:c:sw:p: -l help,env:,conf:,single-machine,wait-for-fusion:,pointing-mode: -n 'start.sh' -- "$@")
eval set -- "$my_args"

# default values
env_dir=""
single_machine=yes
machine_spec="$start_dir/machines.bak"
pointing_mode=""
wait_time=0

while true
do
    case "$1" in
    
        -h|--help)
            print_usage
            exit 0
            ;;
        
        -e|--env)
            if [ -d "$2" ]
            then
                env_dir="$start_dir/$2"
                shift 2
            else
                echo "Error: virtual environment $start_dir/$2 does not exist"
                exit 1
            fi
            ;;
            
        -c|--conf)
            if [ -e "$2" ]
            then
                machine_spec="$start_dir/$2"
                shift 2
            else
                echo "Error: machine specification $start_dir/$2 does not exist"
                exit 1
            fi
            ;;
            
        -s|--single-machine)
            single_machine=yes
            shift
            ;;

        -p|--pointing-mode)
            pointing_mode="$2"
            shift 2
            ;;

        -w|--wait-for-fusion)
            wait_time="$2"
            shift 2
            ;;

        --) shift ; break ;;
        
        *) print_usage ; exit 1 ;;
    esac
done


if [ -z "$env_dir" ]
then
    echo "Virtual environment: none (user/system packages)"
else
    echo "Virtual environment: $env_dir"
fi

if [ -z "$pointing_mode" ]
then
    pointing_mode=screen
fi

echo "Single machine: $single_machine"
echo "Machine spec: $machine_spec"
echo "Pointing mode: $pointing_mode"

echo ""

tmux new -s diana -d

device=0
params=""
for i in $(grep -e '^[^#].*' $machine_spec)
do
    process=${i%@*}
    machine=${i#*@}
    if [ "$machine" = "local" ]; then
        machine="$HOSTNAME"
    fi
    
    case "$process" in
        "kinect")
            kinect_param="--kinect-host $machine"
            ;;
        "fusion")
            fusion_param="--fusion-host $machine"
            eval "tmux send-keys -t diana \"ssh -t ${machine} 'cd ${start_dir}; if [ ! -z ${env_dir} ]; then source ${env_dir}/bin/activate; fi; python3 -m components.fusion.fusion_server; bash;'\" Enter"
			eval "tmux split-window -t diana"
			eval "tmux select-layout -t diana tiled"
            ;;
        "hands")
            eval "tmux send-keys -t diana \"ssh -t ${machine} 'sleep $wait_time; cd ${start_dir}; export CUDA_VISIBLE_DEVICES=${device}; if [ ! -z ${env_dir} ]; then source ${env_dir}/bin/activate; fi; python3 -m components.handRecognition.depth_client $kinect_param $fusion_param; bash;'\" Enter"
			eval "tmux split-window -t diana"
			eval "tmux select-layout -t diana tiled"
            if [ "$single_machine" = yes ]
            then
                ((device++))
            fi
            ;;
        "RH")
            eval "tmux send-keys -t diana \"ssh -t ${machine} 'sleep $wait_time; cd ${start_dir}; export CUDA_VISIBLE_DEVICES=${device}; if [ ! -z ${env_dir} ]; then source ${env_dir}/bin/activate; fi; python3 -m components.handRecognition.depth_client --hand RH $kinect_param $fusion_param; bash;'\" Enter"
			eval "tmux split-window -t diana"
			eval "tmux select-layout -t diana tiled"
            if [ "$single_machine" = yes ]
            then
                ((device++))
            fi
            ;;
        "LH")
            eval "tmux send-keys -t diana \"ssh -t ${machine} 'sleep $wait_time; cd ${start_dir}; export CUDA_VISIBLE_DEVICES=${device}; if [ ! -z ${env_dir} ]; then source ${env_dir}/bin/activate; fi; python3 -m components.handRecognition.depth_client --hand LH $kinect_param $fusion_param; bash;'\" Enter"
			eval "tmux split-window -t diana"
			eval "tmux select-layout -t diana tiled"
            if [ "$single_machine" = yes ]
            then
                ((device++))
            fi
            ;;
        "speech")
            eval "tmux send-keys -t diana \"ssh -t ${machine} 'sleep $wait_time; cd ${start_dir}; python3 -m components.speech.speech_client $kinect_param $fusion_param; bash;'\" Enter"
			eval "tmux select-layout -t diana tiled"
			eval "tmux split-window -t diana"
            ;;
	"Emotion")
            eval "tmux send-keys -t diana \"ssh -t ${machine} 'sleep $wait_time; cd ${start_dir}; python3 -m components.emotion.emotion_client $kinect_param $fusion_param; bash;'\" Enter"
			eval "tmux select-layout -t diana tiled"
			eval "tmux split-window -t diana"
            ;;
        "body")
            eval "tmux send-keys -t diana \"ssh -t ${machine} 'sleep $wait_time; cd ${start_dir}; export CUDA_VISIBLE_DEVICES=${device}; if [ ! -z ${env_dir} ]; then source ${env_dir}/bin/activate; fi; python3 -m components.skeletonRecognition.skeleton_client $kinect_param $fusion_param; bash;'\" Enter"
			eval "tmux select-layout -t diana tiled"
			eval "tmux split-window -t diana"
            if [ "$single_machine" = yes ]
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

# now close the last split and attach the session
tmux send-keys -t diana "exit" Enter
tmux select-layout -t diana tiled
tmux set-option -t diana mouse on
tmux attach -t diana
