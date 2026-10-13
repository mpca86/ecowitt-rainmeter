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

local function locale_for_lcid(value)
    local lcid = tonumber(value)
    if not lcid or lcid % 1 ~= 0 or lcid < 1 or lcid > 65535 then return nil end
    local code = lcid % 1024
    if code == 27 then return 'sk' end
    if code == 9 then return 'en' end
    if code == 5 then return 'cs' end
    if code == 21 then return 'pl' end
    return nil
end

local function registry_measure_name()
    -- Rainmeter's process bitness determines the registry view it uses:
    -- 32-bit builds use the redirected WOW6432Node key on 64-bit Windows;
    -- 64-bit builds use the native SOFTWARE\Rainmeter key.
    local arch = (os.getenv('PROCESSOR_ARCHITECTURE') or ''):lower()
    local wowArch = (os.getenv('PROCESSOR_ARCHITEW6432') or ''):lower()
    if (arch == 'x86' or arch == 'i386') and wowArch ~= '' then
        return 'MeasureRainmeterLanguageWow6432'
    end
    return 'MeasureRainmeterLanguageNative'
end

local function resolve_language(preference, iniLanguage, registryLanguage)
    local mode = tostring(preference or 'auto'):lower():match('^%s*(.-)%s*$')
    if mode == 'sk' or mode == 'en' or mode == 'cs' or mode == 'pl' then
        return mode
    end
    -- A present INI value is authoritative even when malformed or unsupported.
    -- Only a genuinely missing key permits the installer registry fallback.
    if iniLanguage ~= nil then return locale_for_lcid(iniLanguage) or 'en' end
    return locale_for_lcid(registryLanguage) or 'en'
end

local function write_language(lang)
    local root = SKIN:GetVariable('ROOTCONFIGPATH') or ''
    local target = root .. '@Resources\\Includes\\LanguageState.inc'
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

function Initialize()
    -- Registry measures may still be waiting for their first update here.
    -- The regular Update callback retries after Rainmeter has sampled them.
    local settingsPath = SKIN:GetVariable('SETTINGSPATH') or ''
    local iniLanguage = get_ini(settingsPath .. 'Rainmeter.ini', 'Rainmeter', 'Language')
    local mode = tostring(SKIN:GetVariable('UiLanguage') or 'auto'):lower():match('^%s*(.-)%s*$')
    if mode == 'sk' or mode == 'en' or mode == 'cs' or mode == 'pl' or iniLanguage ~= nil then
        write_language(resolve_language(mode, iniLanguage, nil))
    end
end

function Update()
    local settingsPath = SKIN:GetVariable('SETTINGSPATH') or ''
    local iniLanguage = get_ini(settingsPath .. 'Rainmeter.ini', 'Rainmeter', 'Language')
    local mode = tostring(SKIN:GetVariable('UiLanguage') or 'auto'):lower():match('^%s*(.-)%s*$')
    local registryLanguage = nil
    if iniLanguage == nil and mode ~= 'sk' and mode ~= 'en' and mode ~= 'cs' and mode ~= 'pl' then
        local measure = SKIN:GetMeasure(registry_measure_name())
        if measure then
            registryLanguage = measure:GetValue()
            if registryLanguage == nil then return 0 end
        end
    end
    write_language(resolve_language(mode, iniLanguage, registryLanguage))
    return 0
end
