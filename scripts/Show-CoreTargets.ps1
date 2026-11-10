$ErrorActionPreference = 'Stop'
$coreProject = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\src\Core\Core.csproj'))
$demoDirectory = Join-Path (Split-Path $coreProject) 'bin\lab02-tfm-demo'
$null = New-Item -ItemType Directory -Path $demoDirectory -Force
$projectPath = Join-Path $demoDirectory 'CoreTargets.csproj'
$reference = [Security.SecurityElement]::Escape($coreProject)
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="$reference" /></ItemGroup>
</Project>
"@
$program = @'
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine(Core.EnvironmentInfo.Collect().BuildNote);
'@
[IO.File]::WriteAllText($projectPath, $project, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $demoDirectory 'Program.cs'), $program, [Text.UTF8Encoding]::new($false))

dotnet build $projectPath --nologo
if ($LASTEXITCODE -ne 0) { throw 'Core demo build failed.' }
foreach ($tfm in @('net8.0', 'net10.0')) {
    Write-Output "Core / $tfm"
    dotnet run --project $projectPath --framework $tfm --no-build
    if ($LASTEXITCODE -ne 0) { throw "Core demo failed for $tfm" }
}
