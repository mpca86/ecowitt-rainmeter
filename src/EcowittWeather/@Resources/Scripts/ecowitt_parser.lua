-- Generic Ecowitt Local API normalizer.
-- The parser knows structure, not a specific gateway model.
-- Semantic mappings live in REGISTRY and can have fallback sources.

local parser = {}

local REGISTRY = {
    outdoor_temp = {
        { kind='id', section='common_list', id='0x02', field='val' }
    },
    outdoor_humidity = {
        { kind='id', section='common_list', id='0x07', field='val' }
    },
    dew_point = {
        { kind='id', section='common_list', id='0x03', field='val' }
    },
    wind_speed = {
        { kind='id', section='common_list', id='0x0B', field='val' }
    },
    wind_gust = {
        { kind='id', section='common_list', id='0x19', field='val' }
    },
    wind_direction = {
        { kind='id', section='common_list', id='0x0A', field='val' }
    },
    solar_radiation = {
        { kind='id', section='common_list', id='0x15', field='val' }
    },
    uv_index = {
        { kind='id', section='common_list', id='0x17', field='val' }
    },
    rain_rate = {
        { kind='id', section='rain', id='0x0E', field='val' }
    },
    rain_daily = {
        { kind='id', section='rain', id='0x10', field='val' }
    },
    indoor_temp = {
        { kind='first', section='wh25', field='intemp' }
    },
    indoor_humidity = {
        { kind='first', section='wh25', field='inhumi' }
    },
    pressure_abs = {
        { kind='first', section='wh25', field='abs' }
    },
    pressure_rel = {
        { kind='first', section='wh25', field='rel' }
    },
    runtime = {
        { kind='first', section='debug', field='runtime' }
    }
}

local function is_array(t)
    if type(t) ~= 'table' then return false end
    local n = 0
    for k, _ in pairs(t) do
        if type(k) ~= 'number' then return false end
        if k > n then n = k end
    end
    return n > 0
end

local function index_by_field(list, field)
    local idx = {}
    if type(list) ~= 'table' then return idx end
    for _, item in ipairs(list) do
        if type(item) == 'table' and item[field] ~= nil then
            idx[tostring(item[field])] = item
        end
    end
    return idx
end

function parser.new(decoded)
    local self = {
        raw = decoded or {},
        indexes = {}
    }

    for section, value in pairs(self.raw) do
        if is_array(value) then
            self.indexes[section] = {
                id = index_by_field(value, 'id'),
                channel = index_by_field(value, 'channel')
            }
        end
    end

    setmetatable(self, { __index = parser })
    return self
end

function parser:get_by_id(section, id, field)
    local section_index = self.indexes[section]
    if not section_index or not section_index.id then return nil end
    local item = section_index.id[tostring(id)]
    if not item then return nil end
    if field == nil then return item end
    return item[field]
end

function parser:get_channel(channel, field)
    local section_index = self.indexes.ch_aisle
    if not section_index or not section_index.channel then return nil end
    local item = section_index.channel[tostring(channel)]
    if not item then return nil end
    if field == nil then return item end
    return item[field]
end

function parser:get_first(section, field)
    local value = self.raw[section]
    if type(value) ~= 'table' then return nil end
    local item = value[1]
    if type(item) ~= 'table' then return nil end
    if field == nil then return item end
    return item[field]
end

function parser:resolve(name)
    local sources = REGISTRY[name]
    if not sources then return nil end

    for _, src in ipairs(sources) do
        local value = nil
        if src.kind == 'id' then
            value = self:get_by_id(src.section, src.id, src.field)
        elseif src.kind == 'first' then
            value = self:get_first(src.section, src.field)
        elseif src.kind == 'channel' then
            value = self:get_channel(src.channel, src.field)
        end
        if value ~= nil and value ~= '' then
            return value
        end
    end

    return nil
end

function parser:capabilities(max_channels)
    local caps = {
        outdoor = self:resolve('outdoor_temp') ~= nil,
        humidity = self:resolve('outdoor_humidity') ~= nil,
        wind = self:resolve('wind_speed') ~= nil,
        wind_direction = self:resolve('wind_direction') ~= nil,
        solar = self:resolve('solar_radiation') ~= nil,
        uv = self:resolve('uv_index') ~= nil,
        rain = self.raw.rain ~= nil,
        indoor = self:resolve('indoor_temp') ~= nil,
        pressure = self:resolve('pressure_rel') ~= nil,
        runtime = self:resolve('runtime') ~= nil,
        channels = 0
    }

    max_channels = tonumber(max_channels) or 8
    for i = 1, max_channels do
        if self:get_channel(i) then
            caps.channels = caps.channels + 1
        end
    end

    return caps
end

function parser:discover_lines(max_channels)
    local lines = {}
    table.insert(lines, 'Ecowitt parser discovery')
    table.insert(lines, '========================')
    table.insert(lines, '')

    local sections = {}
    for section, _ in pairs(self.raw) do
        sections[#sections + 1] = section
    end
    table.sort(sections)

    table.insert(lines, 'Sections:')
    for _, section in ipairs(sections) do
        table.insert(lines, '  - ' .. tostring(section))
    end

    for _, section in ipairs(sections) do
        local value = self.raw[section]
        if type(value) == 'table' and is_array(value) then
            table.insert(lines, '')
            table.insert(lines, '[' .. section .. ']')
            for _, item in ipairs(value) do
                if type(item) == 'table' then
                    local id = item.id and ('id=' .. tostring(item.id)) or nil
                    local ch = item.channel and ('channel=' .. tostring(item.channel)) or nil
                    local prefix = id or ch or 'item'
                    local fields = {}
                    for k, v in pairs(item) do
                        if k ~= 'id' and k ~= 'channel' then
                            fields[#fields + 1] = tostring(k) .. '=' .. tostring(v)
                        end
                    end
                    table.sort(fields)
                    table.insert(lines, '  ' .. prefix .. ' | ' .. table.concat(fields, ' | '))
                end
            end
        end
    end

    local caps = self:capabilities(max_channels)
    table.insert(lines, '')
    table.insert(lines, '[capabilities]')
    local keys = {}
    for k, _ in pairs(caps) do keys[#keys+1] = k end
    table.sort(keys)
    for _, k in ipairs(keys) do
        table.insert(lines, '  ' .. k .. '=' .. tostring(caps[k]))
    end

    return lines
end

parser.REGISTRY = REGISTRY
return parser
