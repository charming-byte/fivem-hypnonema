fx_version 'cerulean'
games { 'gta5' }

author 'simpled-dev'
description 'a synchronized mediaPlayer resource'
version '2.0.0'
ui_page 'client/index.html'

client_script 'client/Hypnonema.Client.net.dll'
server_script 'server/Hypnonema.Server.net.dll'

files {
    'wwwroot/index.html',
    'client/index.html',
    'client/Hypnonema.Client.net.pdb',
    'client/Hypnonema.Shared.dll',
    'client/Newtonsoft.Json.dll',
    'client/DotNetZip.dll'
}