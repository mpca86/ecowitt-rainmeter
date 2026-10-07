-- Generic CSV history engine.
-- Reads old v1.x files too, because it uses the CSV header dynamically.

local History = {}
History.__index = History

local function split_csv(line)
    local out = {}
    for value in (line .. ','):gmatch('(.-),') do
        out[#out + 1] = value
    end
    return out
end

local function to_number(v)
    if v == nil or v == '' then return nil end
    v = tostring(v):gsub(',', '.')
    return tonumber(v)
end

function History.new(path, fields)
    local self = {
        path = path,
        fields = fields or {},
        rows = {},
        lastStored = 0
    }
    setmetatable(self, History)
    self:load()
    return self
end

function History:load()
    self.rows = {}
    local f = io.open(self.path, 'r')
    if not f then return end

    local header = f:read('*l')
    if not header then f:close(); return end
    local cols = split_csv(header)

    for line in f:lines() do
        local vals = split_csv(line)
        local row = {}
        for i, col in ipairs(cols) do
            if col == 'timestamp' then
                row.ts = tonumber(vals[i])
            else
                row[col] = to_number(vals[i])
            end
        end
        if row.ts then self.rows[#self.rows + 1] = row end
    end
    f:close()

    if #self.rows > 0 then
        self.lastStored = self.rows[#self.rows].ts or 0
    end
end

function History:save()
    local f = io.open(self.path, 'w')
    if not f then return false end

    f:write('timestamp')
    for _, field in ipairs(self.fields) do
        f:write(',' .. field)
    end
    f:write('\n')

    for _, row in ipairs(self.rows) do
        f:write(tostring(row.ts or ''))
        for _, field in ipairs(self.fields) do
            local v = row[field]
            if v == nil then v = '' end
            f:write(',' .. tostring(v))
        end
        f:write('\n')
    end
    f:close()
    return true
end

function History:add(now, values, sample_seconds, keep_seconds)
    sample_seconds = tonumber(sample_seconds) or 60
    if now - self.lastStored < sample_seconds then return false end

    local row = { ts = now }
    for _, field in ipairs(self.fields) do
        row[field] = to_number(values[field])
    end
    self.rows[#self.rows + 1] = row
    self.lastStored = now

    local cutoff = now - (tonumber(keep_seconds) or 14400)
    while #self.rows > 0 and self.rows[1].ts < cutoff do
        table.remove(self.rows, 1)
    end

    self:save()
    return true
end

function History:value_at(field, target_ts, tolerance_seconds)
    local candidate = nil
    for i = #self.rows, 1, -1 do
        local row = self.rows[i]
        if row.ts <= target_ts then
            candidate = row
            break
        end
    end

    if not candidate or candidate[field] == nil then return nil end
    if math.abs(candidate.ts - target_ts) > tolerance_seconds then return nil end
    return candidate[field]
end

return History
