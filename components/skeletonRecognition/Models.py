import tensorflow as tf
from tensorflow.contrib import rnn

import os
os.environ["CUDA_VISIBLE_DEVICES"] = "0"


class Arms_LSTM:
    def __init__(self, logs_path, cell_type='lstm', n_hidden=30, n_classes=8, batch_size=1, features=15, n_layers=2):

        self.logs_path = logs_path
        self.n_hidden = n_hidden
        self.n_classes = n_classes
        self._feature_size = features
        self.num_layers = n_layers
        self._cell_type = cell_type

        self.batch_size= batch_size
        self.keep_prob = tf.placeholder(tf.float32, [])


        self.x = tf.placeholder(tf.float32, [None, None, self._feature_size])
        self.n_frames = tf.placeholder(tf.int32, [None])

        self.size = tf.to_float(tf.shape(self.x)[0])
        self.max_length = tf.to_int32(tf.shape(self.x)[1])

        self.weights = {
            'out': tf.Variable(tf.random_normal([self.n_hidden, self.n_classes]))
        }

        self.biases = {
            'out': tf.Variable(tf.random_normal([self.n_classes]))
        }

        print ('----------Tensor shapes---------')
        print ('Name of model: ', self._cell_type)
        print ('shape of x is: ', self.x.shape)
        print ('Max length of data is: ', self.max_length)
        print ('shape of n_frames is: ', self.n_frames.shape)
        print ('feature_size is: ', self._feature_size)
        print ('n_hidden units is: ', self.n_hidden)
        print ('classes is: ', self.n_classes)
        print ('batch size is: ', self.batch_size)
        print ('number of layers of stacked MultiRNNCells: ', self.num_layers)
        print ('-------------------------------')


        #MAIN LSTM Network
        with tf.variable_scope('main_lstm'):
            cell_out = tf.contrib.rnn.MultiRNNCell([self.get_cell() for _ in range(self.num_layers)])
            init_state = cell_out.zero_state(self.batch_size, tf.float32)

            outputs, state = tf.nn.dynamic_rnn(cell_out, self.x, sequence_length=self.n_frames, dtype=tf.float32, time_major=False, initial_state=init_state)
            self.cell_output, self.cell_state = outputs, state

            output = []
            for i in range(self.batch_size):
                output.append(tf.matmul(outputs[i], self.weights['out']) + self.biases['out'])


        with tf.variable_scope('fully_connected_preprocessing'):
            output = tf.reshape(output, [-1, self.max_length, self.n_classes])
            self.outputs = output

            output_data =[]
            #Clipping to each sequences' n_frames
            for i in range(self.batch_size):
                output_data.append(output[i, :self.n_frames[i], :])

            self.output_data = output_data
            #Summing over all the scores at all timesteps
            sum_data = [tf.reduce_sum(data, axis=0) for data in output_data]
            self.sum_data = sum_data

            #Softmax of tensor of size(batch_size, n_classes)
            prediction = tf.nn.softmax(sum_data)
            self.probabilities = prediction
            self.predicted_values = tf.argmax(prediction, 1)

        try:
            config = tf.ConfigProto(allow_soft_placement=True)
            config.gpu_options.allow_growth = True
            self.sess = tf.Session(config= config)
        except:
            config = tf.ConfigProto(allow_soft_placement=True)
            config.gpu_options.allow_growth = True
            self.sess = tf.Session(config=config)

        # model saver
        self.saver = tf.train.Saver()
        self.sess.run(tf.global_variables_initializer())


    def get_cell(self):
        if self._cell_type== 'LSTM' or 'lstm':
            self.cell = tf.nn.rnn_cell.LSTMCell(self.n_hidden)
        elif self._cell_type== 'GRU' or 'gru':
            self.cell = tf.nn.rnn_cell.GRUCell(self.n_hidden)
        self.dropout_cell = tf.nn.rnn_cell.DropoutWrapper(self.cell, output_keep_prob=self.keep_prob)
        return self.cell








