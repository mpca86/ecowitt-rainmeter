-- Creates local user files that are intentionally not tracked by Git.
-- Templates are ASCII-only so they can be safely written as UTF-16 LE.

local function resource_root()
    local root = SKIN:GetVariable('ROOTCONFIGPATH')
    if root and root ~= '' then
        return root .. '@Resources\\'
    end
    return SKIN:GetVariable('@') or ''
end

local function utf16le_ascii(text)
    local out = { string.char(255, 254) }
    for i = 1, #text do
        out[#out + 1] = text:sub(i, i)
        out[#out + 1] = string.char(0)
    end
    return table.concat(out)
end

local function exists(path)
    local f = io.open(path, 'rb')
    if f then f:close(); return true end
    return false
end

local function create_file(path, content)
    if exists(path) then return false end
    local f = io.open(path, 'wb')
    if not f then return false end
    f:write(utf16le_ascii(content))
    f:close()
    return true
end

function Initialize()
    local root = resource_root()
    local includes = root .. 'Includes\\'

    create_file(
        includes .. 'UserVariables.inc',
        '[Variables]\r\n' ..
        'Channel1Label=\r\n' ..
        'Channel2Label=\r\n' ..
        'Channel3Label=\r\n' ..
        'Channel4Label=\r\n' ..
        'Channel5Label=\r\n' ..
        'Channel6Label=\r\n' ..
        'Channel7Label=\r\n' ..
        'Channel8Label=\r\n' ..
        'GatewayLabel=Gateway\r\n' ..
        'CloudRefreshSeconds=60\r\n' ..
        'UpdateChannel=beta\r\n'
    )

    create_file(
        includes .. 'CloudSecrets.inc',
        '[Variables]\r\n' ..
        'EcowittApplicationKey=\r\n' ..
        'EcowittApiKey=\r\n' ..
        'EcowittMac=\r\n'
    )
end

function Update()
    return 0
end
