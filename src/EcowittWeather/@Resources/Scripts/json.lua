-- Minimal JSON decoder for Ecowitt Rainmeter.
-- Supports objects, arrays, strings, numbers, booleans and null.
-- Written for this package to avoid regex-based JSON parsing.

local json = {}

local function decode_error(str, pos, msg)
    error(string.format("JSON decode error at position %d: %s", pos, msg))
end

local function skip_ws(str, pos)
    while true do
        local c = str:sub(pos, pos)
        if c == " " or c == "\t" or c == "\r" or c == "\n" then
            pos = pos + 1
        else
            return pos
        end
    end
end

local escapes = {
    ['"'] = '"', ['\\'] = '\\', ['/'] = '/',
    ['b'] = '\b', ['f'] = '\f', ['n'] = '\n',
    ['r'] = '\r', ['t'] = '\t'
}

local function utf8_from_codepoint(cp)
    if cp <= 0x7F then
        return string.char(cp)
    elseif cp <= 0x7FF then
        return string.char(
            0xC0 + math.floor(cp / 0x40),
            0x80 + (cp % 0x40)
        )
    elseif cp <= 0xFFFF then
        return string.char(
            0xE0 + math.floor(cp / 0x1000),
            0x80 + (math.floor(cp / 0x40) % 0x40),
            0x80 + (cp % 0x40)
        )
    else
        return string.char(
            0xF0 + math.floor(cp / 0x40000),
            0x80 + (math.floor(cp / 0x1000) % 0x40),
            0x80 + (math.floor(cp / 0x40) % 0x40),
            0x80 + (cp % 0x40)
        )
    end
end

local parse_value

local function parse_string(str, pos)
    pos = pos + 1
    local out = {}
    while pos <= #str do
        local c = str:sub(pos, pos)
        if c == '"' then
            return table.concat(out), pos + 1
        elseif c == '\\' then
            local e = str:sub(pos + 1, pos + 1)
            if e == 'u' then
                local hex = str:sub(pos + 2, pos + 5)
                if not hex:match('^%x%x%x%x$') then
                    decode_error(str, pos, "invalid unicode escape")
                end
                local cp = tonumber(hex, 16)
                table.insert(out, utf8_from_codepoint(cp))
                pos = pos + 6
            else
                local repl = escapes[e]
                if repl == nil then
                    decode_error(str, pos, "invalid escape")
                end
                table.insert(out, repl)
                pos = pos + 2
            end
        else
            table.insert(out, c)
            pos = pos + 1
        end
    end
    decode_error(str, pos, "unterminated string")
end

local function parse_number(str, pos)
    local start = pos
    local pat = "[-+0-9.eE]"
    while pos <= #str and str:sub(pos, pos):match(pat) do
        pos = pos + 1
    end
    local raw = str:sub(start, pos - 1)
    local n = tonumber(raw)
    if n == nil then
        decode_error(str, start, "invalid number")
    end
    return n, pos
end

local function parse_array(str, pos)
    local arr = {}
    pos = skip_ws(str, pos + 1)
    if str:sub(pos, pos) == ']' then
        return arr, pos + 1
    end

    while true do
        local value
        value, pos = parse_value(str, pos)
        arr[#arr + 1] = value
        pos = skip_ws(str, pos)
        local c = str:sub(pos, pos)
        if c == ']' then
            return arr, pos + 1
        elseif c ~= ',' then
            decode_error(str, pos, "expected ',' or ']'")
        end
        pos = skip_ws(str, pos + 1)
    end
end

local function parse_object(str, pos)
    local obj = {}
    pos = skip_ws(str, pos + 1)
    if str:sub(pos, pos) == '}' then
        return obj, pos + 1
    end

    while true do
        if str:sub(pos, pos) ~= '"' then
            decode_error(str, pos, "expected object key")
        end
        local key
        key, pos = parse_string(str, pos)
        pos = skip_ws(str, pos)
        if str:sub(pos, pos) ~= ':' then
            decode_error(str, pos, "expected ':'")
        end
        pos = skip_ws(str, pos + 1)
        local value
        value, pos = parse_value(str, pos)
        obj[key] = value
        pos = skip_ws(str, pos)
        local c = str:sub(pos, pos)
        if c == '}' then
            return obj, pos + 1
        elseif c ~= ',' then
            decode_error(str, pos, "expected ',' or '}'")
        end
        pos = skip_ws(str, pos + 1)
    end
end

parse_value = function(str, pos)
    pos = skip_ws(str, pos)
    local c = str:sub(pos, pos)

    if c == '"' then
        return parse_string(str, pos)
    elseif c == '{' then
        return parse_object(str, pos)
    elseif c == '[' then
        return parse_array(str, pos)
    elseif c == '-' or c:match('%d') then
        return parse_number(str, pos)
    elseif str:sub(pos, pos + 3) == 'true' then
        return true, pos + 4
    elseif str:sub(pos, pos + 4) == 'false' then
        return false, pos + 5
    elseif str:sub(pos, pos + 3) == 'null' then
        return nil, pos + 4
    end

    decode_error(str, pos, "unexpected token")
end

function json.decode(str)
    if type(str) ~= 'string' then
        error("json.decode expects string")
    end
    local value, pos = parse_value(str, 1)
    pos = skip_ws(str, pos)
    if pos <= #str then
        decode_error(str, pos, "trailing data")
    end
    return value
end

return json
