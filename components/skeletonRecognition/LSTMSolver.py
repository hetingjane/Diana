import numpy as np

class RealTimeArmMotionRecognition(object):
    def __init__(self, model):
        self.n_classes = 7
        self.batch_size = 1
        self.feature_size = 30
        self.model = model
        self.classes = np.load('/s/red/a/nobackup/vision/dkpatil/demo/lstm_models/labels_list.npy')
        print 'model restored from ', self.model.logs_path
        self.model.saver.restore(self.model.sess, self.model.logs_path)


    def normalize_data(self, data, spine_scaling=False, verbose=False):
        frames, dims = data.shape
        mean_body = np.mean(data, axis=0).reshape((-1, 3))
        n_joints, dims = mean_body.shape

        if n_joints == 5:
            SPINE_BASE, SPINE_SHOULDER = 0, 4
        else:
            SPINE_BASE, SPINE_SHOULDER = 0, 12

        if verbose:
            print 'Mean of spine base before normalization'
            print mean_body[SPINE_BASE]

        spine_base = mean_body[SPINE_BASE]
        spine_base = np.tile(spine_base, (frames, n_joints, 1)).reshape((frames, -1))
        if spine_scaling:
            norm_dist = np.linalg.norm(mean_body[SPINE_SHOULDER] - mean_body[SPINE_BASE])
        data -= spine_base

        if verbose:
            print spine_base.shape, norm_dist
            mean_body = np.mean(data, axis=0).reshape((-1, 3))
            print 'Mean of spine base after normalization'
            print mean_body[SPINE_BASE]

        if spine_scaling:
            try:
                data /= norm_dist
            except:
                print 'Zero distance between spine base and spine shoulder'
        return data


    def calculate_velocity(self, data):
        data = np.vstack((data[0], data, data[-1]))
        samples, dimensions = data.shape
        velocity = np.zeros((samples - 2, dimensions))
        for k in range(1, samples - 1):
            velocity[k - 1, :] = (data[k + 1, :] - data[k - 1, :]) / 2

        return velocity


    def predict(self, data):
        data = data[:, [i for i in range(1, 21) if i % 4 != 0]]
        normalized_data = self.normalize_data(data, spine_scaling=True)

        velocity = self.calculate_velocity(normalized_data)
        xdata = np.hstack((normalized_data, velocity))

        x_data = np.array(xdata)
        n_frames = x_data.shape[0]

        n_frame_batch = [n_frames] * self.batch_size

        x_data = [x_data for _ in range(self.batch_size)]

        pred_val, probs = self.model.sess.run(
            [self.model.predicted_values, self.model.probabilities],
            feed_dict={self.model.x: x_data, self.model.n_frames: n_frame_batch,self.model.keep_prob: 1.0})

        return self.classes[pred_val[0]], probs[0]
