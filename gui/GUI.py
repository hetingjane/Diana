from PyQt5.QtWidgets import *#QMainWindow, QApplication, QPushButton, QWidget, QAction, QTabWidget, QVBoxLayout, QLabel, QMessageBox, QBoxLayout, QTextEdit
from PyQt5.QtGui import *#QIcon, QFont, QTextCursor, QColor
from PyQt5.QtCore import QTimer

import pyqtgraph as pg
from pyqtgraph.Qt import QtGui

import numpy as np
import sys

import Queue

from collections import deque



class App(QMainWindow):
    def __init__(self, queue,endApplication):
        super(App, self).__init__()
        self.title = 'GUI DEMO'
        self.left = 0
        self.top = 0
        self.width = 800
        self.height = 600
        self.setWindowTitle(self.title)
        self.setGeometry(self.left, self.top, self.width, self.height)

        self.tab_widget = MyTabWidget(self, queue)
        self.setCentralWidget(self.tab_widget)


        self.show()


class MyTabWidget(QWidget):
    def __init__(self, parent, queue):
        self.queue = queue
        super(QWidget, self).__init__(parent)
        self.layout = QVBoxLayout(self)
        self.event_list = deque([],maxlen=10)

        # Initialize tab screen
        self.tabs = QTabWidget()
        self.tab1 = QWidget()
        self.tab2 = QWidget()
        self.tabs.resize(800, 600)

        # Add tabs
        self.tabs.addTab(self.tab1, "Probabilities")
        self.tabs.addTab(self.tab2, "Labels")

        # Create first tab
        self.create_Bar_Grid()
        self.tab1.setLayout(self.barLayout)

        self.create_Labels()
        self.tab2.setLayout(self.labelLayout)

        # Add tabs to widget
        self.layout.addWidget(self.tabs)
        self.setLayout(self.layout)

        self.timer = QTimer()
        self.timer.timeout.connect(self.update)
        self.timer.start(10)


    def update(self):
        try:
            msg = self.queue.get(0)
            decoded_probs = msg[:76]
            events_list = msg[76:]
        except Queue.Empty:
            return

        y1 = decoded_probs[44:]#RH
        y2 = decoded_probs[12:44]#LH
        y3 = decoded_probs[:6] #LA
        y4 = decoded_probs[6:12]#RA
        y5 = np.random.rand(3)

        y3 = [1-sum(y3)]+y3
        y4 = [1 - sum(y4)] + y4

        curr_tab = self.tabs.currentIndex()

        #events_list = [self.ra_gestures[np.argmax(y4)]]

        if curr_tab == 1 and len(events_list)>0:
            font = QFont()
            font.setPointSize(10)

            self.event_log.setCurrentFont(font)
            self.event_log.setTextColor(QColor( "black" ))
            self.event_log.setText("\n".join(self.event_list))
            self.event_log.moveCursor(QTextCursor.End)


            curr_event = "\n".join(events_list)

            font = QFont()
            font.setFamily("Courier")
            font.setPointSize(30)
            self.event_log.setCurrentFont(font)
            self.event_log.setTextColor(QColor("red"))
            self.event_log.append(curr_event)

            self.event_list.append(curr_event+"\n\n")

            sb = self.event_log.verticalScrollBar()
            sb.setValue(sb.maximum())

        else:

            for bar, data in zip([self.rh, self.lh, self.la, self.ra, self.head],[y1, y2, y3, y4, y5]):
                bar.setOpts(height=data)

                brushes = ['b'] * len(data)
                brushes[np.argmax(data)] = 'r'

                bar.setOpts(brushes=brushes)

            #self.tabs.setCurrentIndex((curr_tab + 1) % 2)

        #raw_input()



    def create_Bar_Grid(self):
        self.barLayout = QVBoxLayout()
        self.l = pg.GraphicsLayoutWidget(border=(100, 100, 100))

        text = """Gesture Probabilities"""

        x1 = np.arange(32)
        y1 = np.random.rand(32)
        y2 = np.random.rand(32)

        x2 = np.arange(7)
        y3 = np.random.rand(7)
        y4 = np.random.rand(7)

        x3 = np.arange(3)
        y5 = np.random.rand(3)

        self.l.addLabel(text, col=0, colspan=3)
        self.l.nextRow()

        self.p1 = self.l.addPlot(title="Right Hand", )
        self.p2 = self.l.addPlot(title="Left Hand")

        self.l1 = self.l.addLayout()

        self.p3 = self.l1.addPlot(title="Right Arm")
        self.p3.setXRange(0,1)
        self.l1.nextRow()

        self.p4 = self.l1.addPlot(title="Left Arm")
        self.p4.setXRange(0, 1)
        self.l1.nextRow()

        self.p5 = self.l1.addPlot(title="Head")

        self.rh = pg.BarGraphItem(x=x1, height=y1, width=0.8, brushes=['b']*32)
        self.lh = pg.BarGraphItem(x=x1, height=y2, width=0.8, brush='r')

        self.ra = pg.BarGraphItem(x=x2, height=y3, width=0.8, brush='r')

        self.la = pg.BarGraphItem(x=x2, height=y4, width=0.8, brush='r')

        self.head = pg.BarGraphItem(x=x3, height=y5, width=0.8, brush='r')

        self.p1.addItem(self.rh)
        self.p2.addItem(self.lh)

        self.p3.addItem(self.ra)
        self.p4.addItem(self.la)
        self.p5.addItem(self.head)

        self.rh.rotate(-90)
        self.lh.rotate(-90)
        self.ra.rotate(-90)
        self.la.rotate(-90)
        self.head.rotate(-90)

        self.lh_gestures = ['blank', 'hands together', 'other', 'lh beckon', 'lh claw down',
                       'lh claw front', 'lh claw right', 'lh claw up', 'lh closed back',
                       'lh closed down', 'lh closed front', 'lh closed right', 'lh fist',
                       'lh five front', 'lh four front', 'lh inch', 'lh l', 'lh one front',
                       'lh open back', 'lh open down', 'lh open right', 'lh point down',
                       'lh point front', 'lh point right', 'lh stop', 'lh three back',
                       'lh three front', 'lh thumbs down', 'lh thumbs up', 'lh to face',
                       'lh two back', 'lh two front']

        self.rh_gestures = ['blank', 'hands together', 'other', 'rh beckon', 'rh claw down',
                       'rh claw front', 'rh claw left', 'rh claw up', 'rh closed back',
                       'rh closed down', 'rh closed front', 'rh closed left', 'rh fist',
                       'rh five front', 'rh four front', 'rh inch', 'rh l', 'rh one front',
                       'rh open back', 'rh open down', 'rh open left', 'rh point down',
                       'rh point front', 'rh point left', 'rh stop', 'rh three back',
                       'rh three front', 'rh thumbs down', 'rh thumbs up', 'rh to face',
                       'rh two back', 'rh two front']

        self.ra_gestures = ['still','ra move right','ra move left','ra move up','ra move down','ra move back','ra move front']
        self.la_gestures = ['still', 'la move right', 'la move left', 'la move up', 'la move down',  'la move back', 'la move front']

        self.head_gestures = ['still', 'nod', 'shake']

        lh_gestures = [l.replace("lh ", "") for l in self.lh_gestures]
        rh_gestures = [r.replace("rh ", "") for r in self.rh_gestures]

        la_gestures = [l.replace("la ", "") for l in self.la_gestures]
        ra_gestures = [r.replace("ra ", "") for r in self.ra_gestures]

        self.p1.getAxis('left').setTicks([list(zip(range(0, -len(self.rh_gestures), -1), rh_gestures))])
        self.p2.getAxis('left').setTicks([list(zip(range(0, -len(self.lh_gestures), -1), lh_gestures))])

        self.p3.getAxis('left').setTicks([list(zip(range(0, -len(self.ra_gestures), -1), ra_gestures))])
        self.p4.getAxis('left').setTicks([list(zip(range(0, -len(self.la_gestures), -1), la_gestures))])

        self.p5.getAxis('left').setTicks([list(zip(range(0, -len(self.head_gestures), -1), self.head_gestures))])

        self.barLayout.addWidget(self.l)


    def create_Labels(self):
        self.labelLayout = QVBoxLayout()
        self.event_log = QTextEdit()
        self.event_log.setReadOnly(True)
        self.event_log.setLineWrapMode(QTextEdit.NoWrap)

        '''self.event_log.setTextColor(QtGui.QColor("blue"))
        self.event_log.append("I'm blue !")
        self.event_log.setTextColor(QtGui.QColor("red"))
        self.event_log.append("I'm red !")
        self.event_log.setTextColor(QtGui.QColor("yellow"))
        self.event_log.append("I'm ywllo !")'''

        self.labelLayout.addWidget(self.event_log)

    def tabChangedSlot(self,argTabIndex):
        QMessageBox.information(self, "Tab Index Changed!", "Current Tab Index: "+ str(argTabIndex));


if __name__ == '__main__':
    app = QApplication(sys.argv)
    ex = App()
    sys.exit(app.exec_())