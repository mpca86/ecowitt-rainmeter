-- Execute language_bootstrap.lua outside Rainmeter using a small SKIN mock.
local resource = "src/EcowittWeather/@Resources/Scripts/language_bootstrap.lua"
local dir = os.tmpname() .. "_ecowitt"
assert(os.execute('mkdir -p "' .. dir .. '"') == 0)
local path = dir .. "/"
local function utf16ascii(text)
    local chars = {string.char(255,254)}
    for i=1,#text do chars[#chars+1]=string.char(text:byte(i),0) end
    return table.concat(chars)
end
local function write(pathname,text)
    local f=assert(io.open(pathname,"wb"))
    f:write(utf16ascii(text))
    f:close()
end
local state = path .. "@Resources\\Includes\\LanguageState.inc"
local rain = path .. "Rainmeter.ini"
local vars, bangs, values = {}, {}, {}
local architecture = {}
local original_getenv = os.getenv
os.getenv = function(key) return architecture[key] end
local function measure(value)
    if value == "not-ready" then return {GetValue=function() return nil end} end
    if value == nil then value = 0 end -- Registry measure returned an empty/missing key.
    return {GetValue=function() return value end}
end
SKIN = {
    GetVariable=function(self,key) return vars[key] end,
    GetMeasure=function(self,name) return measure(values[name]) end,
    Bang=function(self,command,target)
        assert(command=="!Refresh")
        bangs[#bangs+1] = target
    end,
}
dofile(resource)

local function expected_bytes(locale)
    return utf16ascii("[Variables]\r\nResolvedUiLanguage="..locale.."\r\n")
end
local function current_bytes()
    local f=io.open(state,"rb")
    if not f then return nil end
    local bytes=f:read("*a")
    f:close()
    return bytes
end
local function setup(mode, ini, registry, arch, wowArch)
    os.remove(rain)
    os.remove(state)
    if ini ~= nil then write(rain,"[Rainmeter]\r\nLanguage="..tostring(ini).."\r\n") end
    vars={SETTINGSPATH=path,ROOTCONFIGPATH=path,ROOTCONFIG="EcowittWeather",UiLanguage=mode}
    values={MeasureRainmeterLanguageWow6432=registry,MeasureRainmeterLanguageNative=registry}
    architecture={PROCESSOR_ARCHITECTURE=arch,PROCESSOR_ARCHITEW6432=wowArch}
    bangs={}
end
local function run(mode, ini, registry, expected, arch, wowArch)
    setup(mode,ini,registry,arch,wowArch)
    Initialize()
    Update()
    assert(current_bytes()==expected_bytes(expected), "wrong locale for "..tostring(mode).."/"..tostring(ini).."/"..tostring(registry))
    assert(#bangs==5, "expected one refresh of each panel after initial resolution")
    bangs={}
    Update()
    assert(#bangs==0, "stable locale caused an unnecessary refresh")
    assert(current_bytes()==expected_bytes(expected), "stable locale state changed")
    print("PASS "..tostring(mode).."/"..tostring(ini).."/"..tostring(registry).." -> "..expected)
end

-- Explicit preference wins over both Rainmeter.ini and the registry.
run("sk",1033,1029,"sk","AMD64",nil)
run("en",1051,1029,"en","AMD64",nil)
run("cs",1033,1051,"cs","AMD64",nil)
run("pl",1029,1033,"pl","AMD64",nil)

-- AUTO maps supported INI LCIDs. A stale registry never overrides a present INI value.
run("auto",1051,1033,"sk","AMD64",nil)
run("auto",1033,1051,"en","AMD64",nil)
run("auto",1029,1045,"cs","AMD64",nil)
run("auto",1045,1051,"pl","AMD64",nil)
run("auto",1031,1051,"en","AMD64",nil) -- present but unsupported
run("auto","bad",1051,"en","AMD64",nil) -- present but malformed

-- Registry fallback is used only when the INI key is absent.
run("auto",nil,1051,"sk","x86","AMD64")
run("auto",nil,1033,"en","AMD64",nil)
run("auto",nil,1029,"cs","x86",nil) -- 32-bit Windows uses the native key
run("auto",nil,1045,"pl","AMD64",nil)
run("auto",nil,nil,"en","AMD64",nil)
run("auto",nil,"bad","en","AMD64",nil)

-- The script waits if the registry measure has not sampled its value yet.
setup("auto",nil,"not-ready","x86","AMD64")
Initialize()
Update()
assert(current_bytes()==nil, "bootstrap resolved before Registry measure was ready")
assert(#bangs==0, "bootstrap refreshed panels before Registry measure was ready")
values.MeasureRainmeterLanguageWow6432=1051
values.MeasureRainmeterLanguageNative=0
Update()
assert(current_bytes()==expected_bytes("sk"), "bootstrap did not retry after Registry measure became ready")
assert(#bangs==5, "expected panel refresh after deferred resolution")
print("PASS: Registry measure initialization ordering")

-- Restart with a resolved state preserves it without unnecessary refreshes.
setup("auto",nil,1051,"x86","AMD64")
Initialize()
Update()
assert(current_bytes()==expected_bytes("sk"), "restart did not resolve missing INI from registry")
assert(#bangs==5, "initial missing-INI resolution did not refresh panels")
bangs={}
dofile(resource) -- Rainmeter reloads the script when the skin process restarts.
Initialize()
Update()
assert(current_bytes()==expected_bytes("sk"), "restart changed resolved language")
assert(#bangs==0, "restart rewrote unchanged language state")
print("PASS: restart with missing Rainmeter.ini language and persisted state")

-- Process bitness selects the matching Registry view.
setup("auto",nil,1033,"AMD64",nil)
values.MeasureRainmeterLanguageNative=1029
values.MeasureRainmeterLanguageWow6432=1051
Update()
assert(current_bytes()==expected_bytes("cs"), "64-bit process did not select native Registry view")
setup("auto",nil,1051,"x86","AMD64")
values.MeasureRainmeterLanguageNative=1029
values.MeasureRainmeterLanguageWow6432=1051
Update()
assert(current_bytes()==expected_bytes("sk"), "32-bit process did not select WOW6432Node Registry view")
setup("auto",nil,1029,"x86",nil)
values.MeasureRainmeterLanguageNative=1045
values.MeasureRainmeterLanguageWow6432=1051
Update()
assert(current_bytes()==expected_bytes("pl"), "32-bit Windows did not select native Registry view")

-- If the preferred view has no value, use the alternate registry view.
setup("auto",nil,nil,"AMD64",nil)
values.MeasureRainmeterLanguageNative=0
values.MeasureRainmeterLanguageWow6432=1051
Update()
assert(current_bytes()==expected_bytes("sk"), "missing native value did not fall back to WOW6432Node")
setup("auto",nil,nil,"x86","AMD64")
values.MeasureRainmeterLanguageWow6432=0
values.MeasureRainmeterLanguageNative=1029
Update()
assert(current_bytes()==expected_bytes("cs"), "missing WOW6432Node value did not fall back to native")

-- A nonzero unsupported value in the preferred view remains authoritative.
setup("auto",nil,nil,"AMD64",nil)
values.MeasureRainmeterLanguageNative=1031
values.MeasureRainmeterLanguageWow6432=1051
Update()
assert(current_bytes()==expected_bytes("en"), "unsupported preferred registry value fell through to stale alternate")
print("PASS: native, 32-bit-on-64-bit, 32-bit Windows, and empty-view fallback")

os.getenv = original_getenv
os.remove(rain)
os.remove(state)
assert(os.execute('rmdir "'..dir..'"')==0)
print("PASS: locale precedence, missing and unsupported values, overrides, restart, and idempotence")
