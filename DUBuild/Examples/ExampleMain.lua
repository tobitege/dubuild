--@requires ExampleClass

--@class ExampleMain
--@outFilename Example
--@timer ExampleTimer

_G.BuildUnit = {}
local Unit = _G.BuildUnit
_G.BuildSystem = {}
local System = _G.BuildSystem
_G.BuildReceiver = {}
local Receiver = _G.BuildReceiver
_G.BuildScreen = {}
local Screen = _G.BuildScreen

function Unit.onStart()
	for index=1, 10, 1 do
		if index <= 5 then boo() else blaa() end
	end
end

function Unit.onStop()
end

function System.onActionStart(action)
end

function System.onActionStop(action)
end

function System.onActionLoop(action)
end

function Unit.onTimer(timer)
end

function Receiver.onReceived(channel, message, slot)
end

function Screen.onMouseDown(x, y, slot)
end

function Screen.onMouseUp(x, y, slot)
end

function System.onUpdate()
end

function System.onFlush()
end