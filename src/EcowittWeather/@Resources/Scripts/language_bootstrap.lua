-- Rainmeter language resolver. This file is ASCII and does not require
-- UTF-16 conversion. It writes only an allowlisted locale state file.
local function utf16_ascii(text)
    local out = {string.char(255, 254)}
    for i = 1, #text do
        local c = text:byte(i)
        out[#out + 1] = string.char(c, 0)
    end
    return table.concat(out)
end

local function read_ascii_ini(path)
    local f = io.open(path, 'rb')
    if not f then return nil end
    local raw = f:read('*a')
    f:close()
    if not raw then return nil end
    local a, b = raw:byte(1, 2)
    if (a == 255 and b == 254) or (a == 254 and b == 255) then
        local le = a == 255
        local out = {}
        for i = 3, #raw - 1, 2 do
            local x, y = raw:byte(i, i + 1)
            local code = le and (x + y * 256) or (y + x * 256)
            out[#out + 1] = code <= 127 and string.char(code) or '?'
        end
        return table.concat(out)
    end
    return raw:gsub('^\239\187\191', '')
end

local function get_ini(path, section, key)
    local contents = read_ascii_ini(path)
    if not contents then return nil end
    local active, found = false, nil
    for line in (contents .. '\n'):gmatch('(.-)\n') do
        line = line:match('^%s*(.-)%s*$')
        if line:sub(1, 1) ~= ';' and line:sub(1, 1) ~= '#' then
            local heading = line:match('^%[([^%]]+)%]$')
            if heading then
                active = heading:lower() == section:lower()
            elseif active then
                local k, v = line:match('^([^=]+)=(.*)$')
                if k and k:match('^%s*(.-)%s*$'):lower() == key:lower() then
                    found = v:match('^%s*(.-)%s*$')
                end
            end
        end
    end
    return found
end

local function resolve_language(preference)
    local mode = tostring(preference or 'auto'):lower():match('^%s*(.-)%s*$')
    if mode == 'sk' or mode == 'en' or mode == 'cs' or mode == 'pl' then
        return mode
    end
    local settingsPath = SKIN:GetVariable('SETTINGSPATH') or ''
    local lcid = tonumber(get_ini(settingsPath .. 'Rainmeter.ini', 'Rainmeter', 'Language'))
    if not lcid or lcid % 1 ~= 0 or lcid < 1 or lcid > 65535 then return 'en' end
    local code = lcid % 1024
    if code == 27 then return 'sk' end
    if code == 9 then return 'en' end
    if code == 5 then return 'cs' end
    if code == 21 then return 'pl' end
    return 'en'
end

function Initialize()
    local root = SKIN:GetVariable('ROOTCONFIGPATH') or ''
    local target = root .. '@Resources\\Includes\\LanguageState.inc'
    local lang = resolve_language(SKIN:GetVariable('UiLanguage'))
    if get_ini(target, 'Variables', 'ResolvedUiLanguage') == lang then return end
    local f = io.open(target, 'wb')
    if not f then return end
    f:write(utf16_ascii('[Variables]\r\nResolvedUiLanguage=' .. lang .. '\r\n'))
    f:close()
    if get_ini(target, 'Variables', 'ResolvedUiLanguage') ~= lang then return end
    local prefix = SKIN:GetVariable('ROOTCONFIG') or 'EcowittWeather'
    for _, panel in ipairs({'Meteo', 'Settings', 'Sensors', 'Diagnostics', 'Update'}) do
        SKIN:Bang('!Refresh', prefix .. '\\' .. panel)
    end
end

function Update()
    return 0
end
