-- Execute language_bootstrap.lua outside Rainmeter using a tiny SKIN mock.
local resource = "src/EcowittWeather/@Resources/Scripts/language_bootstrap.lua"
local dir = os.tmpname() .. "_ecowitt"
-- file names intentionally contain backslashes, as the Rainmeter Lua script does
-- on Windows; Linux treats them as ordinary characters during this unit test.
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
local vars, bangs = {}, {}
SKIN = {
    GetVariable=function(self,key) return vars[key] end,
    Bang=function(self,command,target)
        assert(command=="!Refresh")
        bangs[#bangs+1] = target
    end,
}
dofile(resource)
local cases={
    {"auto",1051,"sk"},
    {"auto",1033,"en"},
    {"auto",1029,"cs"},
    {"auto",1045,"pl"},
    {"auto",1031,"en"},
    {"auto",2057,"en"},
    {"pl",1051,"pl"},
    {"sk",1031,"sk"},
    {"en",1029,"en"},
    {"auto","bad","en"}
}
for _,case in ipairs(cases) do
    write(rain, "[Rainmeter]\r\nLanguage="..tostring(case[2]).."\r\n")
    vars={SETTINGSPATH=path,ROOTCONFIGPATH=path,ROOTCONFIG="EcowittWeather",UiLanguage=case[1]}
    bangs={}
    Initialize()
    local f=assert(io.open(state,"rb"))
    local bytes=f:read("*a")
    f:close()
    assert(bytes==utf16ascii("[Variables]\r\nResolvedUiLanguage="..case[3].."\r\n"),
        "wrong locale for "..tostring(case[1])..":"..tostring(case[2]))
    assert(#bangs==0 or #bangs==5,"expected either no refresh or five panels")
    bangs={}
    Initialize()
    assert(#bangs==0,"Unnecessary refresh for stable locale")
    print("PASS "..case[1].."/"..tostring(case[2]).." -> "..case[3])
end
os.remove(rain)
os.remove(state)
assert(os.execute('rmdir "'..dir..'"')==0)
print("PASS: Lua Rainmeter locale initializer idempotence and locale refresh")
