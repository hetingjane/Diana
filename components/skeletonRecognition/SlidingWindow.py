import numpy as np

#window_size_array = [(i*4+5) for i in range(10)]

def slice(a, scale):
    temp=[]
    count=0
    while count<len(a):
        temp.append(a[count, :])
        count+=scale+1

    return np.vstack(temp)


def sliding_window(array, window_size, slice_flag=True):
    rows, cols = array.shape[0], array.shape[1]
    scale = (window_size-5)/4
    #scale = [i for (i, j) in enumerate(window_size_array) if (j == window_size)][0]
    temp = []
    if(rows<window_size):
        #print 'Window size greater than number of samples!'
        return 0
    else:
        for i in range((rows-window_size)+1):
            window = array[i:i + window_size, :]
            #print 'length of window is: ', len(window)
            if slice_flag:
                window = slice(window,scale)
            #print 'shape of window is: ', window.shape
            temp.append(window)

    return temp



def sliding_window_dataset(data_list, window_size, slice_flag=True):
    return [sliding_window(data, window_size, slice_flag) for data in data_list]



#a = np.arange(45).reshape(15,3)
#sliding_window(a,5,0)