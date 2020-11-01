--@require ExampleClass
--@require NonExistingClass

--@class FailingMain

_G.BuildUnit = {}
local Unit = _G.BuildUnit
_G.BuildSystem = {}
local System = _G.BuildSystem
_G.BuildReceiver = {}
local Receiver = _G.BuildReceiver
_G.BuildScreen = {}
local Screen = _G.BuildScreen


function Unit.Start()
	for index=1,10,1 do
		if test then boo() else blaa() end
	end
end

function Unit.Stop()
end

function System.ActionStart(action)
end

function System.ActionStop(action)
end

function System.ActionLoop(action)
end

function Unit.Tick(timer)
end

function Receiver.Received(channel, message, slot)
end

function Screen.MouseDown(x, y, slot)
end

function Screen.MouseUp(x, y, slot)
end