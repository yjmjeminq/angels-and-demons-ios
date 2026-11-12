param(
  [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe',
  [switch]$ExportIOS
)
$ErrorActionPreference = 'Stop'
$projectPath = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$logDirectory = Join-Path $projectPath 'BuildLogs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw "Unity Editor not found: $UnityEditor" }

function Invoke-UnityBuild([string]$method, [string]$logName) {
  $logPath = Join-Path $logDirectory $logName
  $arguments = @('-batchmode','-quit','-projectPath',('"' + $projectPath + '"'),'-executeMethod',$method,'-logFile',('"' + $logPath + '"'))
  $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
  if ($process.ExitCode -ne 0) { throw "Unity build failed. See $logPath" }
}

Invoke-UnityBuild 'AngelsAndDemons.EditorTools.DefenseBuilder.VerifyAndBuildWindows' 'unity-windows.log'
if ($ExportIOS) {
  Invoke-UnityBuild 'AngelsAndDemons.EditorTools.DefenseBuilder.ExportIOS' 'unity-ios.log'
}
Write-Output (Join-Path $projectPath 'Builds\Windows\AngelsAndDemons.exe')
