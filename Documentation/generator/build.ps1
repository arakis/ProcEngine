$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

cd $dir

docfx build docfx.json