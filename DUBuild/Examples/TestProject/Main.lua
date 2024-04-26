--@require STUMP
--@class Main

_G.BuildUnit = {}
local Unit = _G.BuildUnit
_G.BuildSystem = {}
local System = _G.BuildSystem

function Unit.onStart()
	STUMP.dump(_G);
end

function Unit.onStop()
end

function System.onActionStart(action)
end

function System.onActionStop(action)
end

function System.onActionLoop(action)
end

function System.onUpdate()
end

function System.onFlush()
end