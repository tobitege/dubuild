--@requires ExampleClass

--@class ExampleMain
--@outFilename Example
--@timer ExampleTimer

_G.BuildUnit = {}
local Unit = _G.BuildUnit
_G.BuildSystem = {}
local System = _G.BuildSystem

function Unit.Start()
	for index=1;index<10;index=index+1 do
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