from PyQt5.QtWidgets import *#QMainWindow, QApplication, QPushButton, QWidget, QAction, QTabWidget, QVBoxLayout, QLabel, QMessageBox, QBoxLayout, QTextEdit
from PyQt5.QtGui import *#QIcon, QFont, QTextCursor, QColor
from PyQt5.QtCore import *

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
        self.curr_event = ""
        self.prev_event = ""

        # Initialize tab screen
        self.tabs = QTabWidget()
        self.tab1 = QWidget()
        self.tab2 = QWidget()
        self.tab3 = QWidget()
        self.tabs.resize(800, 600)

        # Add tabs

        self.tabs.addTab(self.tab2, "Gestural Events")
        self.tabs.addTab(self.tab1, "Atomic Probabilities")
        self.tabs.addTab(self.tab3, "Demo")

        # Create first tab
        self.create_Bar_Grid()
        self.tab1.setLayout(self.barLayout)

        self.create_Labels()
        self.tab2.setLayout(self.labelLayout)

        self.create_Demo_Labels()
        self.tab3.setLayout(self.demoLabelLayout)

        # Add tabs to widget
        self.layout.addWidget(self.tabs)
        self.setLayout(self.layout)

        self.timer = QTimer()
        self.timer.timeout.connect(self.update)
        self.timer.start(10)


    def update(self):
        try:
            msg = self.queue.get(0)
            decoded_probs = msg[:79]
            events_list = msg[79:]
        except Queue.Empty:
            return

        y1 = np.array(decoded_probs[44:76])[self.shuffle_indices]#RH
        y2 = np.array(decoded_probs[12:44])[self.shuffle_indices]#LH
        y3 = decoded_probs[:6] #LA
        y4 = decoded_probs[6:12]#RA
        y5 = decoded_probs[76:]#Head

        y3 = [1-sum(y3)]+y3
        y4 = [1 - sum(y4)] + y4

        curr_tab = self.tabs.currentIndex()

        #events_list = [self.ra_gestures[np.argmax(y4)]]

        if curr_tab == 0 and len(events_list)>0:

            font = QFont()
            font.setPointSize(10)

            self.event_log.setCurrentFont(font)
            self.event_log.setTextColor(QColor("black"))
            self.event_log.append(self.prev_event)
            self.event_log.moveCursor(QTextCursor.End)

            self.prev_event = self.curr_event
            self.curr_event = "\n".join(events_list)

            self.curr_event_label.setText(self.curr_event)
            self.prev_event_label.setText(self.prev_event)

            sb = self.event_log.verticalScrollBar()
            sb.setValue(sb.maximum())

        elif curr_tab == 2 and len(events_list)>0:
            self.prev_event = self.curr_event
            self.curr_event = "\n".join(events_list)

            self.curr_event_label.setText(self.curr_event)
            self.prev_event_label.setText(self.prev_event)

        else:

            for bar, data in zip([self.rh, self.lh, self.la, self.ra, self.head],[y1, y2, y3, y4, y5]):
                bar.setOpts(height=data)

                brushes = ['y'] * len(data)
                brushes[np.argmax(data)] = 'g'

                bar.setOpts(brushes=brushes)

            #self.tabs.setCurrentIndex((curr_tab + 1) % 2)

        #raw_input()



    def create_Bar_Grid(self):
        self.barLayout = QVBoxLayout()
        self.l = pg.GraphicsLayoutWidget(border=(100, 100, 100))

        #text = """Gesture Probabilities"""

        x1 = np.arange(32)
        y1 = np.random.rand(32)
        y2 = np.random.rand(32)

        x2 = np.arange(7)
        y3 = np.random.rand(7)
        y4 = np.random.rand(7)

        x3 = np.arange(3)
        y5 = np.random.rand(3)

        #self.l.addLabel(text, col=0, colspan=3)
        #self.l.nextRow()

        self.p1 = self.l.addPlot(title='<font size="5" color="white"><b>Right Hand</b></font>')
        self.p2 = self.l.addPlot(title='<font size="5" color="white"><b>Left Hand</b></font>')

        #font = QtGui.QFont()
        #font.setPixelSize(20)
        #self.p1.getAxis('left').tickFont = font


        self.l1 = self.l.addLayout()

        self.p3 = self.l1.addPlot(title='<font size="5" color="white"><b>Right Arm</b></font>')
        self.p3.setXRange(0,1)
        self.l1.nextRow()

        self.p4 = self.l1.addPlot(title='<font size="5" color="white"><b>Left Arm</b></font>')
        self.p4.setXRange(0, 1)
        self.l1.nextRow()

        self.p5 = self.l1.addPlot(title='<font size="5" color="white"><b>Head</b></font>')

        self.rh = pg.BarGraphItem(x=x1, height=y1, width=0.8, brushes=['b']*32)
        self.lh = pg.BarGraphItem(x=x1, height=y2, width=0.8, brush='c')

        self.ra = pg.BarGraphItem(x=x2, height=y3, width=0.8, brush='b')

        self.la = pg.BarGraphItem(x=x2, height=y4, width=0.8, brush='b')

        self.head = pg.BarGraphItem(x=x3, height=y5, width=0.8, brush='b')

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

        self.shuffle_indices = [7, 4, 5, 6, 10, 8, 9, 11, 18, 19, 20, 21, 22, 23, 28, 27, 17, 31, 30, 26, 25, 14, 13, 24,
                           12, 15, 16, 3, 29, 1, 0, 2]

        self.lh_gestures = np.array(['blank', 'hands together', 'other', 'lh beckon', 'lh claw down',
                       'lh claw front', 'lh claw right', 'lh claw up', 'lh closed back',
                       'lh closed down', 'lh closed front', 'lh closed right', 'lh fist',
                       'lh five front', 'lh four front', 'lh inch', 'lh l', 'lh one front',
                       'lh open back', 'lh open down', 'lh open right', 'lh point down',
                       'lh point front', 'lh point right', 'lh stop', 'lh three back',
                       'lh three front', 'lh thumbs down', 'lh thumbs up', 'lh to face',
                       'lh two back', 'lh two front'])[self.shuffle_indices]

        self.rh_gestures = np.array(['blank', 'hands together', 'other', 'rh beckon', 'rh claw down',
                       'rh claw front', 'rh claw left', 'rh claw up', 'rh closed back',
                       'rh closed down', 'rh closed front', 'rh closed left', 'rh fist',
                       'rh five front', 'rh four front', 'rh inch', 'rh l', 'rh one front',
                       'rh open back', 'rh open down', 'rh open left', 'rh point down',
                       'rh point front', 'rh point left', 'rh stop', 'rh three back',
                       'rh three front', 'rh thumbs down', 'rh thumbs up', 'rh to face',
                       'rh two back', 'rh two front'])[self.shuffle_indices]

        self.ra_gestures = ['still','ra move right','ra move left','ra move up','ra move down','ra move back','ra move front']
        self.la_gestures = ['still', 'la move right', 'la move left', 'la move up', 'la move down',  'la move back', 'la move front']

        self.head_gestures = ['nod', 'shake', 'other']

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

        self.curr_event_label = QLabel()
        self.prev_event_label = QLabel()
        self.event_log = QTextEdit()

        self.curr_event_label.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.curr_event_label.setAlignment(Qt.AlignCenter)

        self.prev_event_label.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.prev_event_label.setAlignment(Qt.AlignCenter)

        self.curr_event_label.setFrameStyle(QFrame.Box | QFrame.Plain);
        self.curr_event_label.setLineWidth(3);

        self.prev_event_label.setFrameStyle(QFrame.Box | QFrame.Plain);
        self.prev_event_label.setLineWidth(3);

        self.event_log.setFrameStyle(QFrame.Box | QFrame.Plain);
        self.event_log.setLineWidth(3);

        self.event_log.setReadOnly(True)
        self.event_log.setLineWrapMode(QTextEdit.NoWrap)

        font = QFont();
        font.setPointSize(32);
        font.setBold(True);

        self.curr_event_label.setFont(font)
        self.prev_event_label.setFont(font)

        self.curr_event_label.setStyleSheet('color: green')
        self.prev_event_label.setStyleSheet('color: blue')

        self.curr_event_label.setText("Current Event")
        self.prev_event_label.setText("Previous Event")

        self.labelLayout.addWidget(self.curr_event_label)
        self.labelLayout.addWidget(self.prev_event_label)
        self.labelLayout.addWidget(self.event_log)


    def create_Demo_Labels(self):
        self.demoLabelLayout = QVBoxLayout()

        self.layout1 = QHBoxLayout()



        self.curr_event_label = QLabel()
        self.prev_event_label = QLabel()

        self.curr_event_label.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.curr_event_label.setAlignment(Qt.AlignCenter)

        self.prev_event_label.setSizePolicy(QSizePolicy.Expanding, QSizePolicy.Expanding)
        self.prev_event_label.setAlignment(Qt.AlignCenter)

        self.curr_event_label.setFrameStyle(QFrame.Box | QFrame.Plain);
        self.curr_event_label.setLineWidth(3);

        self.prev_event_label.setFrameStyle(QFrame.Box | QFrame.Plain);
        self.prev_event_label.setLineWidth(3);


        font = QFont();
        font.setPointSize(32);
        font.setBold(True);

        self.curr_event_label.setFont(font)
        self.prev_event_label.setFont(font)

        self.curr_event_label.setStyleSheet('color: green')
        self.prev_event_label.setStyleSheet('color: blue')

        self.curr_event_label.setText("Current Event")
        self.prev_event_label.setText("Previous Event")

        self.layout1.addWidget(self.curr_event_label)
        self.layout1.addWidget(self.prev_event_label)
        self.demoLabelLayout.addLayout(self.layout1)
        self.demoLabelLayout.addStretch(2)


    def tabChangedSlot(self,argTabIndex):
        QMessageBox.information(self, "Tab Index Changed!", "Current Tab Index: "+ str(argTabIndex));


if __name__ == '__main__':
    app = QApplication(sys.argv)
    ex = App()
    sys.exit(app.exec_())

