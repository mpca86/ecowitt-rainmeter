-- Rainmeter adapter for Ecowitt Web API v3 using the shared UI / history model.

local json
local EcowittParser
local History

local history = nil
local lastRuntime = nil
local lastRuntimeChange = 0
local restartUntil = 0
local lastDebugSignature = nil

local function script_root()
    local root = SKIN:GetVariable('ROOTCONFIGPATH')
    if root and root ~= '' then
        return root .. '@Resources\\Scripts\\'
    end

    local current = SKIN:GetVariable('CURRENTPATH') or ''
    local parent = current:gsub('[^\\]+\\$', '')
    return parent .. '@Resources\\Scripts\\'
end

local function diagnostics_path()
    local root = SKIN:GetVariable('ROOTCONFIGPATH')
    if root and root ~= '' then
        return root .. '@Resources\\Diagnostics\\ecowitt_cloud_debug.txt'
    end
    local current = SKIN:GetVariable('CURRENTPATH') or ''
    local parent = current:gsub('[^\\]+\\$', '')
    return parent .. '@Resources\\Diagnostics\\ecowitt_cloud_debug.txt'
end

local function load_modules()
    local dir = script_root()
    json = dofile(dir .. 'json.lua')
    EcowittParser = dofile(dir .. 'cloud_parser.lua')
    History = dofile(dir .. 'history.lua')
end

local function num(v)
    if v == nil then return nil end
    if type(v) == 'number' then return v end
    local s = tostring(v):gsub(',', '.')
    local n = s:match('[-+]?%d+%.?%d*')
    return tonumber(n)
end

local function skinNum(name, fallback)
    local v = SKIN:GetVariable(name)
    local n = tonumber(v)
    if n == nil then return fallback end
    return n
end

local function skinVar(name, fallback)
    local v = SKIN:GetVariable(name)
    if v == nil or v == '' then return fallback end
    return v
end

local function setVar(name, value)
    SKIN:Bang('!SetVariable', name, tostring(value or ''))
end

local function cleanPercent(v)
    if not v then return '--' end
    return tostring(v):gsub('%%', '')
end

local function cleanPressure(v)
    if not v then return '--' end
    return tostring(v):gsub('%s*hPa', ''):gsub('%s*kPa', '')
end

local function trendState(current, past, threshold)
    current = num(current)
    past = num(past)
    if current == nil or past == nil then return 'STABLE' end
    local diff = current - past
    if diff >= threshold then return 'UP' end
    if diff <= -threshold then return 'DOWN' end
    return 'STABLE'
end

local function signedDelta(current, past)
    local c, p = num(current), num(past)
    if c == nil or p == nil then return '' end
    local d = c - p
    if math.abs(d) < 0.05 then return '0.0' end
    return string.format('%+.1f', d)
end

local function windDirectionText(value)
    local deg = num(value)
    if not deg then return '--' end
    deg = deg % 360
    local dirs = {'S','SV','V','JV','J','JZ','Z','SZ'}
    local i = math.floor((deg + 22.5) / 45) % 8 + 1
    return dirs[i]
end

local function uvTextState(value)
    local n = num(value)
    if not n then return 'LOW' end
    if n < 3 then return 'LOW' end
    if n < 6 then return 'MODERATE' end
    if n < 8 then return 'HIGH' end
    if n < 11 then return 'VERY_HIGH' end
    return 'EXTREME'
end

local function formatRuntime(seconds)
    local s = math.floor(num(seconds) or 0)
    local days = math.floor(s / 86400)
    s = s % 86400
    local hours = math.floor(s / 3600)
    s = s % 3600
    local minutes = math.floor(s / 60)
    local secs = s % 60
    return string.format('%d d %02d h %02d min %02d s', days, hours, minutes, secs)
end

local function tempColor(value)
    local n = num(value)
    if not n then return skinVar('colorText', '255,255,255,205') end
    local comfortMin = skinNum('TempComfortMin', 20)
    local comfortMax = skinNum('TempComfortMax', 24)
    local warnMin = skinNum('TempWarnMin', 18)
    local warnMax = skinNum('TempWarnMax', 26)

    if n >= comfortMin and n <= comfortMax then
        return skinVar('ColorComfort', '120,255,120,255')
    elseif n >= warnMin and n <= warnMax then
        return skinVar('ColorWarning', '255,210,80,255')
    elseif n < warnMin then
        return skinVar('ColorCold', '100,180,255,255')
    else
        return skinVar('ColorAlert', '255,100,100,255')
    end
end

local function humiColor(value)
    local n = num(value)
    if not n then return skinVar('colorText', '255,255,255,205') end
    local comfortMin = skinNum('HumidityComfortMin', 40)
    local comfortMax = skinNum('HumidityComfortMax', 60)
    local warnMin = skinNum('HumidityWarnMin', 35)
    local warnMax = skinNum('HumidityWarnMax', 65)

    if n >= comfortMin and n <= comfortMax then
        return skinVar('ColorComfort', '120,255,120,255')
    elseif n >= warnMin and n <= warnMax then
        return skinVar('ColorWarning', '255,210,80,255')
    else
        return skinVar('ColorAlert', '255,100,100,255')
    end
end

local function outdoorTempColor(value)
    local n = num(value)
    if not n then return skinVar('colorText', '255,255,255,205') end
    local extremeColdMax = skinNum('OutdoorTempExtremeColdMax', -20)
    local coldMax = skinNum('OutdoorTempColdMax', -10)
    local frostMax = skinNum('OutdoorTempFrostMax', 0)
    local warmMin = skinNum('OutdoorTempWarmMin', 25)
    local hotMin = skinNum('OutdoorTempHotMin', 30)
    local veryHotMin = skinNum('OutdoorTempVeryHotMin', 35)

    if n <= extremeColdMax then
        return skinVar('ColorOutdoorExtremeCold', '190,130,255,255')
    elseif n <= coldMax then
        return skinVar('ColorOutdoorCold', '100,180,255,255')
    elseif n < frostMax then
        return skinVar('ColorOutdoorFrost', '100,220,255,255')
    elseif n >= veryHotMin then
        return skinVar('ColorOutdoorVeryHot', '255,100,100,255')
    elseif n >= hotMin then
        return skinVar('ColorOutdoorHot', '255,150,50,255')
    elseif n >= warmMin then
        return skinVar('ColorOutdoorWarm', '255,210,80,255')
    else
        return skinVar('ColorOutdoorNormal', '255,255,255,205')
    end
end

local function uvColor(value)
    local n = num(value)
    if not n then return skinVar('colorText', '255,255,255,205') end
    if n < 3 then
        return skinVar('ColorUVLow', '40,149,0,255')
    elseif n < 6 then
        return skinVar('ColorUVModerate', '247,228,0,255')
    elseif n < 8 then
        return skinVar('ColorUVHigh', '248,89,0,255')
    elseif n < 11 then
        return skinVar('ColorUVVeryHigh', '216,0,29,255')
    else
        return skinVar('ColorUVExtreme', '107,73,200,255')
    end
end

local function updateCloudStatus(p, now)
    if not p:is_ok() then
        setVar('V_TitleText', 'Meteo Cloud')
        setVar('V_TitleColor', skinVar('ColorAlert', '255,100,100,255'))
        setVar('V_CloudStatus', p:error_message() or 'API error')
        setVar('V_RuntimeHuman', p:error_message() or 'API error')
        return
    end

    local latest = p:latest_time()
    if latest and latest > 20000000000 then
        latest = math.floor(latest / 1000)
    end

    local status = 'API OK'
    local color = skinVar('colorText', '255,255,255,205')

    if latest then
        local age = math.max(0, now - latest)
        local stale = skinNum('CloudStaleSeconds', 900)

        if age < 60 then
            status = 'dáta pred ' .. tostring(age) .. ' s'
        else
            status = 'dáta pred ' .. tostring(math.floor(age / 60)) .. ' min'
        end

        if age > stale * 2 then
            color = skinVar('ColorAlert', '255,100,100,255')
        elseif age > stale then
            color = skinVar('ColorWarning', '255,210,80,255')
        end
    end

    setVar('V_TitleText', 'Meteo Cloud')
    setVar('V_TitleColor', color)
    setVar('V_CloudStatus', status)
    setVar('V_RuntimeHuman', status)
end

local function writeDiagnostics(p)
    if skinNum('DebugParser', 0) ~= 1 then return end

    local signature = table.concat({
        tostring(p.raw and p.raw.code or ''),
        tostring(p.raw and p.raw.time or ''),
        tostring(p:latest_time() or '')
    }, '|')

    -- The Lua measure runs every second, but cloud data normally changes much
    -- less frequently. Do not rewrite the diagnostics file for identical data.
    if signature == lastDebugSignature then return end

    local f = io.open(diagnostics_path(), 'wb')
    if not f then return end

    -- UTF-8 BOM makes Rainmeter WebParser decode local debug text correctly.
    f:write(string.char(239, 187, 191))
    f:write('Ecowitt Cloud diagnostics\n')
    f:write('=========================\n')
    f:write('Generated: ' .. os.date('%Y-%m-%d %H:%M:%S') .. '\n')
    f:write('Source: /api/v3/device/real_time\n')
    f:write('Safe output: credentials are not written to this file\n')
    f:write('\n')
    f:write('[configured sensor aliases]\n')

    local maxChannels = skinNum('MaxChannels', 8)
    for i = 1, maxChannels do
        local alias = SKIN:GetVariable('Channel' .. i .. 'Label') or ''
        if alias == '' then alias = '(default CH' .. i .. ')' end
        f:write('  CH' .. i .. '=' .. alias .. '\n')
    end
    f:write('  Gateway=' .. tostring(SKIN:GetVariable('GatewayLabel') or 'Gateway') .. '\n')
    f:write('\n')

    local lines = p:discover_lines(maxChannels)
    for _, line in ipairs(lines) do
        f:write(line .. '\n')
    end

    f:close()
    lastDebugSignature = signature
end

function Initialize()
    load_modules()

    local currentPath = SKIN:GetVariable('CURRENTPATH') or ''
    local fields = {'outTemp','whTemp','r1Temp','r2Temp','r3Temp','r4Temp','r5Temp','r6Temp','r7Temp','r8Temp','baroRel'}
    history = History.new(currentPath .. 'meteo_history.csv', fields)
end

function Update()
    local rawMeasure = SKIN:GetMeasure('MeasureEcowittRaw')
    if not rawMeasure then return end

    local raw = rawMeasure:GetStringValue()
    if not raw or raw == '' then return end

    local ok, decoded = pcall(json.decode, raw)
    if not ok or type(decoded) ~= 'table' then
        setVar('V_TitleColor', skinVar('ColorAlert', '255,100,100,255'))
        setVar('V_CloudStatus', 'JSON parse error')

        if skinNum('DebugParser', 0) == 1 then
            local f = io.open(diagnostics_path(), 'wb')
            if f then
                f:write(string.char(239, 187, 191))
                f:write('Ecowitt Cloud diagnostics\n')
                f:write('=========================\n')
                f:write('Generated: ' .. os.date('%Y-%m-%d %H:%M:%S') .. '\n')
                f:write('ERROR: JSON response could not be decoded\n')
                f:write('Details: ' .. tostring(decoded) .. '\n')
                f:write('Raw API body intentionally omitted.\n')
                f:close()
            end
        end
        return
    end

    local p = EcowittParser.new(decoded)
    local now = os.time()

    if not p:is_ok() then
        updateCloudStatus(p, now)
        writeDiagnostics(p)
        SKIN:Bang('[!UpdateMeter *][!Redraw]')
        return
    end

    local outTemp = p:resolve('outdoor_temp') or '--'
    local outHumi = cleanPercent(p:resolve('outdoor_humidity'))
    local windSpeed = p:resolve('wind_speed') or '--'
    local windGust = p:resolve('wind_gust') or '--'
    local windDir = windDirectionText(p:resolve('wind_direction'))
    local solar = p:resolve('solar_radiation') or '--'
    local uv = p:resolve('uv_index') or '--'
    local rainRateRaw = p:resolve('rain_rate') or '0.0 mm/Hr'
    local rainTodayRaw = p:resolve('rain_daily') or '0.0 mm'
    local whTemp = p:resolve('indoor_temp') or '--'
    local whHumi = cleanPercent(p:resolve('indoor_humidity'))
    local baroAbs = cleanPressure(p:resolve('pressure_abs'))
    local baroRel = cleanPressure(p:resolve('pressure_rel'))

    local maxChannels = skinNum('MaxChannels', 8)
    local channelTemps = {}
    local channelHumis = {}

    local activeChannels = {}
    for i = 1, maxChannels do
        local item = p:get_channel(i)
        local configuredLabel = skinVar('Channel' .. i .. 'Label', '')
        local active = item ~= nil

        local name = 'CH' .. i
        local temp = '--'
        local humi = '--'
        local battery = '--'

        if active then
            name = item.name or name
            temp = item.temp or '--'
            humi = cleanPercent(item.humidity)
            battery = item.battery or '--'
            activeChannels[#activeChannels + 1] = i
        end

        local displayName = configuredLabel ~= '' and configuredLabel or name

        channelTemps[i] = temp
        channelHumis[i] = humi

        setVar('V_CH' .. i .. 'Name', name)
        setVar('V_CH' .. i .. 'DisplayName', displayName)
        setVar('V_CH' .. i .. 'Temp', temp)
        setVar('V_CH' .. i .. 'Humi', humi)
        setVar('V_CH' .. i .. 'Battery', battery)
        setVar('V_CH' .. i .. 'Hidden', active and 0 or 1)
    end

    local rainRateNum = num(rainRateRaw) or 0
    local rainTodayNum = num(rainTodayRaw) or 0
    local rainLabel, rainValue, showRain = '', '', false

    if rainRateNum > 0 then
        rainLabel = 'Zrážky'
        rainValue = string.format('%.1f mm/h / %.1f mm', rainRateNum, rainTodayNum)
        showRain = true
    elseif rainTodayNum > 0 then
        rainLabel = 'Zrážky dnes'
        rainValue = string.format('%.1f mm', rainTodayNum)
        showRain = true
    end

    local historyValues = {
        outTemp = outTemp,
        whTemp = whTemp,
        baroRel = baroRel
    }
    for i = 1, 8 do
        historyValues['r' .. i .. 'Temp'] = channelTemps[i]
    end

    history:add(
        now,
        historyValues,
        skinNum('HistorySampleSeconds', 60),
        skinNum('HistoryKeepHours', 4) * 3600
    )

    local tempMinutes = skinNum('TrendTempMinutes', 15)
    local tempThreshold = skinNum('TrendTempThreshold', 0.2)
    local tempTarget = now - tempMinutes * 60
    local tempTolerance = math.max(300, math.floor(tempMinutes * 60 * 0.5))

    local pressureMinutes = skinNum('TrendPressureMinutes', 180)
    local pressureThreshold = skinNum('TrendPressureThreshold', 0.5)
    local pressureTarget = now - pressureMinutes * 60
    local pressureTolerance = 30 * 60

    local outTrend = trendState(outTemp, history:value_at('outTemp', tempTarget, tempTolerance), tempThreshold)
    local whTrend = trendState(whTemp, history:value_at('whTemp', tempTarget, tempTolerance), tempThreshold)

    local channelTrends = {}
    for i = 1, maxChannels do
        channelTrends[i] = trendState(
            channelTemps[i],
            history:value_at('r' .. i .. 'Temp', tempTarget, tempTolerance),
            tempThreshold
        )
    end

    local baroPast = history:value_at('baroRel', pressureTarget, pressureTolerance)
    local baroTrend = trendState(baroRel, baroPast, pressureThreshold)
    local baroDelta3h = signedDelta(baroRel, baroPast)

    -- Dynamic interior layout. Only active CH sensors consume a row.
    local interiorStartY = 128
    local rowHeight = 18
    local packedIndex = 0

    for i = 1, maxChannels do
        if p:get_channel(i) then
            local y = interiorStartY + packedIndex * rowHeight
            setVar('V_CH' .. i .. 'Y', y)
            packedIndex = packedIndex + 1
        else
            setVar('V_CH' .. i .. 'Y', interiorStartY)
        end

        setVar('V_CH' .. i .. 'TrendState', channelTrends[i] or 'STABLE')
        setVar('V_CH' .. i .. 'TempColor', tempColor(channelTemps[i]))
        setVar('V_CH' .. i .. 'HumiColor', humiColor(channelHumis[i]))
    end

    local gatewayY = interiorStartY + packedIndex * rowHeight
    local baroSeparatorY = gatewayY + rowHeight
    setVar('V_GatewayY', gatewayY)
    setVar('V_BaroSeparatorY', baroSeparatorY)
    setVar('V_BaroAbsY', baroSeparatorY + 6)
    setVar('V_BaroRelY', baroSeparatorY + 22)
    setVar('V_PanelHeight', baroSeparatorY + 40)

    setVar('V_OutTemp', outTemp)
    setVar('V_OutHumi', outHumi)
    setVar('V_OutTempTrendState', outTrend)
    setVar('V_OutTempColor', outdoorTempColor(outTemp))
    setVar('V_WindDir', windDir)
    setVar('V_WindSpeed', windSpeed)
    setVar('V_WindGust', windGust)
    setVar('V_Solar', solar)
    setVar('V_UV', uv)
    setVar('V_UVTextState', uvTextState(uv))
    setVar('V_UVColor', uvColor(uv))
    setVar('V_RainLabel', rainLabel)
    setVar('V_RainValue', rainValue)

    setVar('V_WHTemp', whTemp)
    setVar('V_WHHumi', whHumi)
    setVar('V_WHTempTrendState', whTrend)
    setVar('V_WHTempColor', tempColor(whTemp))
    setVar('V_WHHumiColor', humiColor(whHumi))
    setVar('V_BaroAbs', baroAbs)
    setVar('V_BaroRel', baroRel)
    setVar('V_BaroTrendState', baroTrend)
    setVar('V_BaroDelta3h', baroDelta3h)

    -- Backward-compatible current layout: CH1..CH3 -> R1..R3.
    for i = 1, 3 do
        setVar('V_R' .. i .. 'Temp', channelTemps[i] or '--')
        setVar('V_R' .. i .. 'Humi', channelHumis[i] or '--')
        setVar('V_R' .. i .. 'TempTrendState', channelTrends[i] or 'STABLE')
        setVar('V_R' .. i .. 'TempColor', tempColor(channelTemps[i]))
        setVar('V_R' .. i .. 'HumiColor', humiColor(channelHumis[i]))
    end

    if showRain then
        SKIN:Bang('!ShowMeter', 'meterLabelRain')
        SKIN:Bang('!ShowMeter', 'meterValueRain')
    else
        SKIN:Bang('!HideMeter', 'meterLabelRain')
        SKIN:Bang('!HideMeter', 'meterValueRain')
    end

    for i = 1, maxChannels do
        if p:get_channel(i) then
            SKIN:Bang('!ShowMeter', 'meterLabelCH' .. i)
            SKIN:Bang('!ShowMeter', 'meterValueCH' .. i)
        else
            SKIN:Bang('!HideMeter', 'meterLabelCH' .. i)
            SKIN:Bang('!HideMeter', 'meterValueCH' .. i)
        end
    end

    updateCloudStatus(p, now)
    writeDiagnostics(p)

    SKIN:Bang('[!UpdateMeter *][!Redraw]')
end
