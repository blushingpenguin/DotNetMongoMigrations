#!/usr/bin/pwsh

Param(
    [switch]
    [Parameter(
        Mandatory = $false,
        HelpMessage = "Enable generation of a cobertura report")]
    $cobertura
)

$ErrorActionPreference="Stop"
Set-StrictMode -Version Latest

function script:exec {
    [CmdletBinding()]

	param(
		[Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
		[Parameter(Position=1,Mandatory=0)][string]$errorMessage = ("Error executing command: {0}" -f $cmd)
    )
    # write-host $cmd
	& $cmd
	if ($lastexitcode -ne 0)
	{
		throw $errorMessage
	}
}

# Install ReportGenerator
if (!(Test-Path "tools/reportgenerator") -and !(Test-Path "tools/reportgenerator.exe"))
{
    #Using alternate nuget.config due to https://github.com/dotnet/cli/issues/9586
    exec { dotnet tool install --configfile nuget.tool.config --tool-path tools dotnet-reportgenerator-globaltool }
}

Write-Host "Running dotnet test"

$filter = '\"[*TestAdapter*]*,[*]*.Migrations.*,[*.Test*]*,[nunit*]*\"'
$attributeFilter = 'Obsolete,GeneratedCode' # ,CompilerGeneratedAttribute - misses a bunch of code
$path = "src/MongoMigrations.Test/MongoMigrations.Test.csproj"

# prints each test name
# "--logger", "console;verbosity=detailed",

$testArgs = @( `
    "test", `
    "--blame-hang-timeout", "60000", `
    "--blame-hang-dump-type", "none", `
    "--configuration", "Release", `
    "--no-build", `
    "--collect:`"XPlat Code Coverage`"", `
    "--results-directory", "coverage", `
    $path, `
    "--", `
        "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude=$filter", `
        "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute=$attributeFilter", `
        "NUnit.DisplayName=FullNameSep" `
)

# makes test results file, not really worth it as we just fail if any test fails
# and it has the duplicate outputs bug
# --logger "trx;LogFileName=$asm.trx"
# write-host $testArgs
exec { & dotnet $testArgs }

Write-Host "Running ReportGenerator"
$reportTypes="-reporttypes:Html"
if ($cobertura)
{
    $reportTypes += ";Cobertura";
}
exec { tools/reportgenerator "-reports:coverage/coverage.cobertura.xml" "-targetdir:coverage" $reportTypes }
