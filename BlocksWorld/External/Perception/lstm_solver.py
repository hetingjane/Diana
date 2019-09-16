import numpy as np
import os

class RealTimeArmMotionRecognition(object):
    def __init__(self, model):
        self.n_classes = 8
        self.batch_size = 1
        self.feature_size = 15
        self.model = model
        self.classes = np.load(os.path.abspath('External/Perception/data/labels_body.npy'))
        self.classes = [x.decode('utf-8') for x in self.classes]
        print ('Classes are: ', self.classes)
        print ('model restored from ', self.model.logs_path)
        self.model.saver.restore(self.model.sess, self.model.logs_path)

        self._past_probs = None


    def normalize_data(self, data, verbose=False):
        data = data[:, 3:]
        frames, dims = data.shape
        mean_body = np.mean(data, axis=0).reshape((-1, 3))
        n_joints, dims = mean_body.shape

        assert n_joints==5
        SPINE_SHOULDER= 3
        if verbose:
            print ('Mean of spine shoulder before normalization')
            print (mean_body[SPINE_SHOULDER])

        spine_shoulder = mean_body[SPINE_SHOULDER]
        spine_shoulder = np.tile(spine_shoulder, (frames, n_joints, 1)).reshape((frames, -1))
        data -= spine_shoulder

        if verbose:
            print (spine_shoulder.shape)
            mean_body = np.mean(data, axis=0).reshape((-1, 3))
            print ('Mean of spine base after normalization')
            print (mean_body[SPINE_SHOULDER])
        return data


    def predict(self, data):
        normalized_data = self.normalize_data(data, verbose=False)
        assert normalized_data.shape==(15, 15)

        x_data = np.array(normalized_data)
        n_frames = x_data.shape[0]

        n_frame_batch = [n_frames] * self.batch_size

        x_data = [x_data for _ in range(self.batch_size)]

        pred_val, probs = self.model.sess.run(
            [self.model.predicted_values, self.model.probabilities],
            feed_dict={self.model.x: x_data, self.model.n_frames: n_frame_batch,self.model.keep_prob: 1.0})

        # print ('Predicted value before smoothing: ', pred_val)
        # print ('Probability before: ', probs[0])

        if self._past_probs is None:
            self._past_probs = probs[0]
        else:
            self._past_probs = (self._past_probs+probs[0])/2

        pred_val = np.argmax(self._past_probs)
        # print ('Predicted value after smoothing; ', pred_val)
        # print ('Probability after: ', self._past_probs)
        return self.classes[pred_val], self._past_probs
