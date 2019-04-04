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
        self._sel.register(conn, selectors.EVENT_WRITE, self._send)

    def _send(self, key):
        conn = key.fileobj
        try:
            data = self.input_queue.get_nowait()
            try:
                conn.sendall(data)
            except (socket.error, EOFError):
                self._sel.unregister(conn)
                conn.close()
                self._log("Client disconnected")
                # Since server socket is also registered in the selctor
                # length will be greater than or equal to 1
                if len(self._sel.get_map()) == 1:
                    self._connected.clear()
        except queue.Empty:
            pass

    def run(self):
        remote_sock = serve(self.target)
        remote_sock.listen(5)

        self._sel.register(remote_sock, selectors.EVENT_READ, data=self._accept)

        self._log("Waiting for the destination to connect\n")

        while not self.is_stopped():
            events = self._sel.select()
            for key, mask in events:
                handler = key.data
                handler(key)

        self._log("Stopped")

        for conn in self._sel.get_map():
            self._sel.unregister(conn)
            conn.close()

        self._connected.clear()
