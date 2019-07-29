from demo_canet_v2 import CANet_50
import tensorflow as tf
import os
import numpy as np
import demo_canet_utils


slim = tf.contrib.slim
os.environ["CUDA_VISIBLE_DEVICES"] = "0"

class CANet_try:
    def __init__(self, log_root, batch_size=1, num_classes_intent=21, num_classes_hand=32, window_length =10):
        self.log_root = log_root

        self.batch_size = batch_size
        self.num_classes_intent = num_classes_intent
        self.num_classes_hand = num_classes_hand
        self.window_length = window_length
        IMAGE_HEIGHT, IMAGE_WIDTH, NUM_CHANNELS = 424, 512, 1

        tf.logging.set_verbosity(tf.logging.INFO)  # Set the verbosity to INFO level


        try:
            checkpoint_file = tf.train.latest_checkpoint(log_root)
            print('Latest checkpoint file is: ', checkpoint_file)
            self.global_step = tf.convert_to_tensor(int(checkpoint_file.split("-")[-1]))
            print ('gbstep is: ', self.global_step)
        except Exception as e:
            print (e)
            self.global_step = tf.convert_to_tensor(0)


        self.images = tf.placeholder(tf.float32, [None, IMAGE_HEIGHT, IMAGE_WIDTH, self.window_length*NUM_CHANNELS])
        self.mask1 = tf.placeholder(tf.float32, [None, IMAGE_HEIGHT, IMAGE_WIDTH, 1])
        self.mask2 = tf.placeholder(tf.float32, [None, IMAGE_HEIGHT, IMAGE_WIDTH, 1])

        with slim.arg_scope(demo_canet_utils.canet_arg_scope()):
            logits, end_points, logits1, logits2 = CANet_50(self.images, self.mask1, self.mask2, self.num_classes_intent, self.num_classes_hand)

        self.intent_logits = logits
        self.lh_logits = logits1
        self.rh_logits = logits2

        print ('Logits is: ', logits)
        print ('Hand mask logits1 is: ', logits1)
        print ('Hand mask logits2 is: ', logits2)

        variables_to_restore = slim.get_variables_to_restore()


        # State the metrics that you want to predict. We get a predictions that is not one_hot_encoded.
        self.predictions = tf.argmax(end_points['predictions'], 1)
        self.hand_predictions1 = tf.argmax(end_points['hand_predictions1'], 1)
        self.hand_predictions2 = tf.argmax(end_points['hand_predictions2'], 1)

        self.saver = tf.train.Saver(variables_to_restore, max_to_keep=0)

        config = tf.ConfigProto(allow_soft_placement=True)
        config.gpu_options.allow_growth = True
        self.sess = tf.Session(config=config)

        self.sess.run(tf.global_variables_initializer())
        try:
            self.saver.restore(self.sess, tf.train.latest_checkpoint(log_root))
        except Exception as e:
            print ('Exception seen: ', e)
        print ('Code finished....model initialized')

        '''
        var = [v for v in tf.model_variables() if "50/conv1" in v.name or "logits" in v.name]
        var_list = self.sess.run(var)
        for v1, v in zip(var, var_list):
            print(v1.name, v.shape, np.min(v), np.max(v), np.mean(v), np.std(v))
        '''



