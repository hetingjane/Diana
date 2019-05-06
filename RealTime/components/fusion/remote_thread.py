import threading
import socket
import selectors
import queue

from .conf.endpoints import serve


class Remote(threading.Thread):

    def __init__(self, name, target, input_queue, conn_event):
        threading.Thread.__init__(self)
        self.daemon = True
        self.name = name
        self.target = target
        self.input_queue = input_queue
        self._stop = threading.Event()
        self._connected = conn_event
        self._sel = selectors.DefaultSelector()

    def stop(self):
        self._stop.set()

    def is_stopped(self):
        return self._stop.is_set()

    def _log(self, text):
        print("[ {name:^10} ] {txt}".format(name=self.name, txt=text))

    def _accept(self, key):
        try:
            conn, addr = key.fileobj.accept()
        except socket.error:
            print("Error while accepting connection")
            return

        self._log("Accepted destination {host[0]}:{host[1]}".format(host=addr))
        self._connected.set()
        self._sel.register(conn, selectors.EVENT_WRITE)

    def _send(self, key, data):
        conn = key.fileobj
        try:
            conn.sendall(data)
        except (socket.error, EOFError):
            self._sel.unregister(conn)
            conn.close()
            self._log("Client disconnected")
            # Since listening socket is also registered in the selector
            # length will be greater than or equal to 1
            if len(self._sel.get_map()) == 1:
                self._connected.clear()

    def run(self):
        listen_sock = serve(self.target)
        listen_sock.listen(5)

        self._sel.register(listen_sock, selectors.EVENT_READ)
        listen_key = self._sel.get_key(listen_sock)

        self._log("Waiting for the destination to connect\n")

        data = None
        handled = set()
        while not self.is_stopped():

            while len(set(self._sel.get_map().values()) - handled) > 0:
                handled.add(listen_key)
                events = self._sel.select()
                if data is None:
                    try:
                        data = self.input_queue.get(block=True, timeout=0.01)
                    except queue.Empty:
                        pass

                for key, mask in events:
                    if mask & selectors.EVENT_READ and key is listen_key:
                        self._accept(key)
                    elif mask & selectors.EVENT_WRITE and key not in handled:
                        if data is not None:
                            self._send(key, data)
                            handled.add(key)
            data = None
            handled.clear()

        self._log("Stopped")

        for conn in self._sel.get_map():
            self._sel.unregister(conn)
            conn.close()

        self._connected.clear()
