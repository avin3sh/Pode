if ((Get-Module -Name Pode | Measure-Object).Count -ne 0)
{
    Remove-Module -Name Pode
}

$path = Split-Path -Parent -Path (Split-Path -Parent -Path $MyInvocation.MyCommand.Path)
Import-Module "$($path)/src/Pode.psm1" -ErrorAction Stop

# or just:
# Import-Module Pode

# web-pages-https.ps1 example notes:
# ----------------------------------
# Adding a self-signed/existing cert only supported for Windows.
# This will not clear the binding afterwards (netsh http delete sslcert 0.0.0.0:8443), nor will it remove the certificate
# from the personal store.  Cleanup should be done manually as required. Generated self-signed cert for fqdn localhost,
# this is just for dev/testing and proof of concept

$port = 8443

# create a server, flagged to generate a self-signed cert for dev/testing
Server {

    # bind to ip/port and set as https with self-signed cert
    listen *:$port https -cert self

    # set view engine for web pages
    engine pode

    # GET request for web page at "/"
    route 'get' '/' {
        param($session)
        view 'simple' -Data @{ 'numbers' = @(1, 2, 3); }
    }

    # GET request throws fake "500" server error status code
    route 'get' '/error' {
        param($session)
        status 500
    }

}