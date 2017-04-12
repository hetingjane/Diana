from thread_sync import output_data, remote_connected
import threading
import socket
import select
import Queue
import sys


class Remote(threading.Thread):

    def __init__(self, own_address):
        self.address = own_address
        self._stop = threading.Event()
        threading.Thread.__init__(self)

    def stop(self):
        self._stop.set()

    def is_stopped(self):
        return self._stop.is_set()

    def run(self):

        remote_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

        remote_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

        remote_sock.bind(self.address)

        remote_sock.listen(5)

        inputs = [remote_sock]
        outputs = []
        excepts = []

        print "Waiting for the destination to connect\n"

        while not self.is_stopped():

            assert len(outputs) <= 1

            try:
                read_socks, write_socks, except_socks = select.select(inputs, outputs, excepts, 0.02)
            except socket.error:
                # Only input is remote_sock
                print "Problem in the server socket" + str(remote_sock.getsockname()) + ". Stopping the output thread..."
                sys.exit(0)

            for s in read_socks:
                if s is remote_sock:
                    client_sock, client_addr = s.accept()
                    if len(outputs) == 0:
                        print "Accepted destination {}:{}".format(client_addr[0], client_addr[1])
                        client_sock.shutdown(socket.SHUT_RD)
                        outputs += [client_sock]
                        remote_connected.set()
                    else:
                        print "Rejected connection attempt by {}:{}".format(client_addr[0], client_addr[1])
                        client_sock.close()

            for s in write_socks:
                while True:
                    try:
                        data = output_data.get_nowait()
                        try:
                            s.sendall(data)
                        except (socket.error, EOFError):
                            print str(s.getpeername()) + " disconnected."
                            outputs.remove(s)
                            remote_connected.clear()
                            break

                    except Queue.Empty:
                        break

        print "Stopped output thread"

        for s in outputs:
            s.close()

        remote_sock.close()
        remote_connected.clear()