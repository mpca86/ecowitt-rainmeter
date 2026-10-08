-- Ecowitt Web API v3 normalizer.
-- Normalizes cloud real-time data into the same semantic interface as the Local API parser.

local parser = {}

local function lower(v)
    return string.lower(tostring(v or ''))
end

local function number(v)
    if type(v) == 'number' then return v end
    if v == nil then return nil end
    local s = tostring(v):gsub(',', '.')
    return tonumber(s:match('[-+]?%d+%.?%d*'))
end

local function item_value(item)
    if type(item) == 'table' then
        return item.value, item.unit, item.time
    end
    return item, nil, nil
end

local function format_number(n, decimals)
    if n == nil then return nil end
    return string.format('%.' .. tostring(decimals or 1) .. 'f', n)
end

local function wind_to_ms(value, unit)
    local n = number(value)
    if n == nil then return nil end
    local u = lower(unit)

    if u:find('km/h', 1, true) or u:find('kmh', 1, true) then
        n = n / 3.6
    elseif u:find('mph', 1, true) then
        n = n * 0.44704
    elseif u:find('knot', 1, true) or u == 'kt' or u == 'kts' then
        n = n * 0.514444
    end

    return format_number(n, 1) .. ' m/s'
end

local function rain_to_mm(value, unit, rate)
    local n = number(value)
    if n == nil then return nil end
    local u = lower(unit)

    if u:find('in', 1, true) and not u:find('min', 1, true) then
        n = n * 25.4
    end

    if rate then
        return format_number(n, 1) .. ' mm/Hr'
    end
    return format_number(n, 1) .. ' mm'
end

local function pressure_to_hpa(value, unit)
    local n = number(value)
    if n == nil then return nil end
    local u = lower(unit)

    if u:find('inhg', 1, true) or u:find('in/hg', 1, true) then
        n = n * 33.8638866667
    elseif u:find('mmhg', 1, true) then
        n = n * 1.3332239
    elseif u == 'kpa' then
        n = n * 10
    end

    return format_number(n, 1) .. ' hPa'
end

function parser.new(decoded)
    local self = {
        raw = decoded or {},
        data = type(decoded) == 'table' and decoded.data or {}
    }
    if type(self.data) ~= 'table' then self.data = {} end
    setmetatable(self, { __index = parser })
    return self
end

function parser:is_ok()
    local code = self.raw and self.raw.code
    return (code == nil or tonumber(code) == 0) and type(self.data) == 'table'
end

function parser:error_message()
    if self:is_ok() then return nil end
    return tostring((self.raw and (self.raw.msg or self.raw.message)) or 'Ecowitt Cloud API error')
end

function parser:get_item(section, key)
    local s = self.data[section]
    if type(s) ~= 'table' then return nil end
    return s[key]
end

function parser:get_value(section, key)
    local value = item_value(self:get_item(section, key))
    return value
end

function parser:resolve(name)
    local section, key, transform

    if name == 'outdoor_temp' then section, key = 'outdoor', 'temperature'
    elseif name == 'outdoor_humidity' then section, key = 'outdoor', 'humidity'
    elseif name == 'dew_point' then section, key = 'outdoor', 'dew_point'
    elseif name == 'wind_speed' then section, key, transform = 'wind', 'wind_speed', 'wind'
    elseif name == 'wind_gust' then section, key, transform = 'wind', 'wind_gust', 'wind'
    elseif name == 'wind_direction' then section, key = 'wind', 'wind_direction'
    elseif name == 'solar_radiation' then section, key, transform = 'solar_and_uvi', 'solar', 'solar'
    elseif name == 'uv_index' then section, key = 'solar_and_uvi', 'uvi'
    elseif name == 'rain_rate' then section, key, transform = 'rainfall', 'rain_rate', 'rain_rate'
    elseif name == 'rain_daily' then section, key, transform = 'rainfall', 'daily', 'rain'
    elseif name == 'indoor_temp' then section, key = 'indoor', 'temperature'
    elseif name == 'indoor_humidity' then section, key = 'indoor', 'humidity'
    elseif name == 'pressure_abs' then section, key, transform = 'pressure', 'absolute', 'pressure'
    elseif name == 'pressure_rel' then section, key, transform = 'pressure', 'relative', 'pressure'
    elseif name == 'runtime' then return nil
    else return nil
    end

    local item = self:get_item(section, key)
    local value, unit = item_value(item)
    if value == nil then return nil end

    if transform == 'wind' then
        return wind_to_ms(value, unit)
    elseif transform == 'rain_rate' then
        return rain_to_mm(value, unit, true)
    elseif transform == 'rain' then
        return rain_to_mm(value, unit, false)
    elseif transform == 'pressure' then
        return pressure_to_hpa(value, unit)
    elseif transform == 'solar' then
        local n = number(value)
        if n == nil then return tostring(value) end
        return format_number(n, 2) .. ' W/m2'
    end

    return value
end

function parser:get_channel(channel, field)
    local section = self.data['temp_and_humidity_ch' .. tostring(channel)]
    if type(section) ~= 'table' then return nil end

    local t, _, _ = item_value(section.temperature)
    local h, _, _ = item_value(section.humidity)

    if t == nil and h == nil then return nil end

    local battery = '--'
    local batterySection = self.data.battery
    if type(batterySection) == 'table' then
        local b, _, _ = item_value(batterySection['temp_humidity_sensor_ch' .. tostring(channel)])
        if b ~= nil then battery = b end
    end

    local item = {
        channel = tostring(channel),
        name = 'CH' .. tostring(channel),
        temp = t,
        humidity = h,
        battery = battery
    }

    if field == nil then return item end
    return item[field]
end

function parser:latest_time()
    local latest = nil

    local function consider(v)
        local n = tonumber(v)
        if n and (latest == nil or n > latest) then latest = n end
    end

    consider(self.raw.time)

    for _, section in pairs(self.data) do
        if type(section) == 'table' then
            for _, item in pairs(section) do
                if type(item) == 'table' then
                    consider(item.time)
                end
            end
        end
    end

    return latest
end

function parser:capabilities(max_channels)
    local caps = {
        outdoor = self:resolve('outdoor_temp') ~= nil,
        humidity = self:resolve('outdoor_humidity') ~= nil,
        wind = self:resolve('wind_speed') ~= nil,
        wind_direction = self:resolve('wind_direction') ~= nil,
        solar = self:resolve('solar_radiation') ~= nil,
        uv = self:resolve('uv_index') ~= nil,
        rain = self.data.rainfall ~= nil,
        indoor = self:resolve('indoor_temp') ~= nil,
        pressure = self:resolve('pressure_rel') ~= nil,
        runtime = false,
        channels = 0
    }

    max_channels = tonumber(max_channels) or 8
    for i = 1, max_channels do
        if self:get_channel(i) then caps.channels = caps.channels + 1 end
    end

    return caps
end

function parser:discover_lines(max_channels)
    local lines = {
        'Ecowitt Cloud API parser discovery',
        '===================================',
        '',
        'API code=' .. tostring(self.raw.code),
        'API msg=' .. tostring(self.raw.msg or self.raw.message or ''),
        ''
    }

    local sections = {}
    for section, _ in pairs(self.data) do sections[#sections + 1] = section end
    table.sort(sections)

    table.insert(lines, 'Sections:')
    for _, section in ipairs(sections) do
        table.insert(lines, '  - ' .. tostring(section))
        local block = self.data[section]
        if type(block) == 'table' then
            local keys = {}
            for key, _ in pairs(block) do keys[#keys + 1] = key end
            table.sort(keys)
            for _, key in ipairs(keys) do
                local value, unit, time = item_value(block[key])
                if type(value) ~= 'table' then
                    local suffix = ''
                    if unit then suffix = suffix .. ' unit=' .. tostring(unit) end
                    if time then suffix = suffix .. ' time=' .. tostring(time) end
                    table.insert(lines, '      ' .. tostring(key) .. '=' .. tostring(value) .. suffix)
                end
            end
        end
    end

    local caps = self:capabilities(max_channels)
    table.insert(lines, '')
    table.insert(lines, '[capabilities]')
    local keys = {}
    for k, _ in pairs(caps) do keys[#keys + 1] = k end
    table.sort(keys)
    for _, k in ipairs(keys) do
        table.insert(lines, '  ' .. k .. '=' .. tostring(caps[k]))
    end

    return lines
end

return parser
