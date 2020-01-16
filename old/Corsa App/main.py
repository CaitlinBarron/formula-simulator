import sys
import ac
import acsys


def acMain(ac_version):
    global xLbl, yLbl, zLbl
    appWindow = ac.newApp("SimTelem")
    ac.setSize(appWindow, 200, 200)

    ac.console("sim telemetry test")
    xLbl = ac.addLabel(appWindow, "x = 0")
    ac.setPosition(xLbl,3,30)
    yLbl = ac.addLabel(appWindow, "y = 0")
    ac.setPosition(yLbl,3,60)
    zLbl = ac.addLabel(appWindow, "z = 0")
    ac.setPosition(zLbl,3,90)
    return "SimTelem"


def acUpdate(deltaT):
    global xLbl, yLbl, zLbl

    xStr = "x = {:9.5f}".format(ac.getCarState(0, acsys.CS.LocalAngularVelocity)[0])
    ac.setText(xLbl, xStr)
    yStr = "y = {:9.5f}".format(ac.getCarState(0, acsys.CS.LocalAngularVelocity)[1])
    ac.setText(yLbl, yStr)
    zStr = "z = {:9.5f}".format(ac.getCarState(0, acsys.CS.LocalAngularVelocity)[2])
    ac.setText(zLbl, zStr)
    ac.console("SimTelem: " + "updated!")