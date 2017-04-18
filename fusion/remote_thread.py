import threading
import socket
import select
import Queue
import sys

class Remote(threading.Thread):

    def __init__(self, name, serv_address, input_queue, conn_event):
        threading.Thread.__init__(self)
        self.daemon = True
        self.name = str(name)
        self.id = "[ " + name + " ]\t"
        self.address = serv_address
        self.input_queue = input_queue
        self._stop = threading.Event()
        self._conn = conn_event
        self.client = ""


    def stop(self):
        self._stop.set()

    def is_stopped(self):
        return self._stop.is_set()

    def _log(self, msg):
        print self.id + msg

    def run(self):

        remote_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        remote_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        remote_sock.bind(self.address)

        remote_sock.listen(5)

        inputs = [remote_sock]
        outputs = []
        excepts = []

        self._log("Waiting for the destination to connect\n")

        while not self.is_stopped():

            assert len(outputs) <= 1

            try:
                read_socks, write_socks, except_socks = select.select(inputs, outputs, excepts, 0.02)
            except socket.error:
                # Only input is remote_sock
                self._log("Problem in the server socket" + str(remote_sock.getsockname()) + ". Stopping ...")
                sys.exit(0)

            for s in read_socks:
                if s is remote_sock:
                    client_sock, client_addr = s.accept()
                    if len(outputs) == 0:
                        self._log("Accepted destination {}:{}".format(client_addr[0], client_addr[1]))
                        client_sock.shutdown(socket.SHUT_RD)
                        outputs += [client_sock]
                        addr, port = client_sock.getpeername()
                        self.client = addr + ":" + str(port)
                        self._conn.set()
                    else:
                        self._log("Rejected connection attempt by {}:{}".format(client_addr[0], client_addr[1]))
                        client_sock.close()

            for s in write_socks:
                while True:
                    try:
                        data = self.input_queue.get_nowait()
                        try:
                            s.sendall(data)
                        except (socket.error, EOFError):
                            self._log(self.client + " disconnected.")
                            self.client = ""
                            outputs.remove(s)
                            self._conn.clear()
                            break

                    except Queue.Empty:
                        break

        self._log("Stopped")

        for s in outputs:
            s.close()

        remote_sock.close()
        self._conn.clear()