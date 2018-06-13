import sys, time

if sys.platform == 'win32':
    # On Windows, the best timer is time.clock
    safetime = time.clock
else:
    # On most other platforms the best timer is time.time
    safetime = time.time