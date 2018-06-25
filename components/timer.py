import sys, time

if sys.platform == 'win32':
    # On Windows, the best timer is time.clock
    safetime = time.clock
else:
    # On most other platforms the best timer is time.time
    safetime = time.time

global t_0
t_0 = safetime()

# this call waits so that it occurs a maximum of "fps" times per second
def wait(FPS):
    global t_0
    max_d_t = 1 / FPS
    t_1 = safetime()
    d_t = t_1 - t_0
    if d_t < max_d_t:
        time.sleep(max_d_t - d_t)
    t_0 = safetime()