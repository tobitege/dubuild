--@require STUMP
--@class Main

_G.BuildUnit = {}
local Unit = _G.BuildUnit
_G.BuildSystem = {}
local System = _G.BuildSystem

function Unit.Start()
	STUMP.dump(_G);
end

function Unit.Stop()
end

function System.ActionStart(action)
end

function System.ActionStop(action)
end

function System.ActionLoop(action)
end

function System.Update()
end

function System.Flush()
end