#requires -Version 5.1
<#
.SYNOPSIS
Creates and verifies the Phase 02 Core foundation without overwriting source.
.DESCRIPTION
Run from the repository root: .\phase02-setup.ps1
No packages, project references, existing source, or Git state are changed.
Conflicting files stop setup before creation. Text differing only in line endings
is considered identical, so Git's Windows line-ending conversion is harmless.
Restore/build create normal bin/obj output. Test reports use a fresh TestResults
subdirectory on every run. A failed run retains created files for inspection;
rerunning safely skips them. No destructive rollback or backups are needed.
Success verifies the foundation, not completion of the Phase 02 architecture review.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Root = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
$Utf8 = New-Object System.Text.UTF8Encoding($false, $true)
$States = [ordered]@{}
$ExpectedReferences = [ordered]@{
    Core = @()
    Application = @('Core')
    Infrastructure = @('Application', 'Core')
    Web = @('Application', 'Infrastructure')
    Tests = @('Core', 'Application', 'Infrastructure')
}

function Write-Step {
    param([string]$Status, [string]$Message)
    $color = switch ($Status) {
        'PASS' { 'Green' }
        'CREATE' { 'Cyan' }
        'WARNING' { 'Yellow' }
        'FAIL' { 'Red' }
        default { 'Gray' }
    }
    Write-Host "[$Status] $Message" -ForegroundColor $color
}

function Get-SafePath {
    param([string]$RelativePath)
    $path = [IO.Path]::GetFullPath((Join-Path $Root $RelativePath))
    if (!$path.StartsWith($Root + [IO.Path]::DirectorySeparatorChar,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes the repository: $RelativePath"
    }
    # Refuse links/junctions so an apparently local destination cannot redirect writes.
    $probe = $path
    while ($probe.Length -ge $Root.Length) {
        if (Test-Path -LiteralPath $probe) {
            $item = Get-Item -LiteralPath $probe -Force
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Linked paths are not supported by setup: $probe"
            }
        }
        if ($probe -eq $Root) { break }
        $probe = [IO.Path]::GetDirectoryName($probe)
    }
    return $path
}

function Get-WorkspaceFiles {
    param([string]$Directory = $Root)
    # Do not follow links or enumerate Git internals. Include build folders when
    # counting projects: an unexpected extra .csproj should be investigated.
    foreach ($item in Get-ChildItem -LiteralPath $Directory -Force) {
        if ($item.Name -eq '.git') { continue }
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Linked workspace entry requires manual review: $($item.FullName)"
        }
        if ($item.PSIsContainer) { Get-WorkspaceFiles $item.FullName }
        else { $item }
    }
}

function Read-Project {
    param([string]$Layer)
    $path = Get-SafePath "ElsheiekhHMS.$Layer/ElsheiekhHMS.$Layer.csproj"
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing Phase 01 project: $path"
    }
    return [xml][IO.File]::ReadAllText($path)
}

function Assert-Architecture {
    $projects = @(Get-WorkspaceFiles | Where-Object { $_.Extension -eq '.csproj' })
    if ($projects.Count -ne 5) { throw "Expected exactly five .csproj files; found $($projects.Count)." }
    foreach ($layer in $ExpectedReferences.Keys) {
        $project = Read-Project $layer
        $frameworks = @($project.SelectNodes('/Project/PropertyGroup/TargetFramework'))
        if ($frameworks.Count -ne 1 -or $frameworks[0].InnerText -ne 'net10.0' -or
            $project.SelectNodes('/Project/PropertyGroup/TargetFrameworks').Count -gt 0) {
            throw "ElsheiekhHMS.$layer must target only net10.0."
        }
        $projectDirectory = Get-SafePath "ElsheiekhHMS.$layer"
        $actual = @($project.SelectNodes('/Project/ItemGroup/ProjectReference') | ForEach-Object {
            [IO.Path]::GetFullPath((Join-Path $projectDirectory $_.GetAttribute('Include')))
        } | Sort-Object)
        $expected = @($ExpectedReferences[$layer] | ForEach-Object {
            Get-SafePath "ElsheiekhHMS.$_/ElsheiekhHMS.$_.csproj"
        } | Sort-Object)
        if (($actual -join '|') -ne ($expected -join '|')) {
            throw "Incorrect project references in ElsheiekhHMS.$layer. Repair Phase 01 manually."
        }
    }
    $core = Read-Project 'Core'
    $nullable = @($core.SelectNodes('/Project/PropertyGroup/Nullable'))
    if ($nullable.Count -ne 1 -or $nullable[0].InnerText -ne 'enable') {
        throw 'Core must already have Nullable enabled.'
    }
    if ($core.SelectNodes('//PackageReference | //FrameworkReference').Count -gt 0) {
        throw 'Core should have no package/framework references for this foundation. Review manually.'
    }
    foreach ($file in Get-WorkspaceFiles (Get-SafePath 'ElsheiekhHMS.Core')) {
        if ($file.Extension -ne '.cs' -or $file.FullName -match '[\\/](bin|obj)[\\/]') { continue }
        $text = [IO.File]::ReadAllText($file.FullName)
        if ($text -match 'Microsoft\.(EntityFrameworkCore|AspNetCore|Extensions\.(Configuration|DependencyInjection))|ElsheiekhHMS\.(Application|Infrastructure|Web)|System\.Net') {
            throw "Forbidden Core dependency found in $($file.FullName). Review manually."
        }
    }
    Write-Step 'PASS' 'Five net10.0 projects; exact dependency graph; Core remains independent.'
}

function Invoke-DotNet {
    param([string[]]$Arguments)
    Write-Step 'CHECK' ('dotnet ' + ($Arguments -join ' '))
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet $($Arguments[0]) failed with exit code $LASTEXITCODE." }
}

function Normalize-Text {
    param([string]$Text)
    return $Text.Replace("`r`n", "`n")
}

function Get-ContentHash {
    param([string]$Path)
    $algorithm = [Security.Cryptography.SHA256]::Create()
    try {
        $stream = [IO.File]::OpenRead($Path)
        try { return [BitConverter]::ToString($algorithm.ComputeHash($stream)) }
        finally { $stream.Dispose() }
    } finally { $algorithm.Dispose() }
}

function Get-FileState {
    param([string]$RelativePath, [string]$Content)
    $path = Get-SafePath $RelativePath
    if (!(Test-Path -LiteralPath $path)) { return 'CREATE' }
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "A directory occupies a planned file path: $RelativePath"
    }
    # Invalid UTF-8 is a conflict, never an excuse to rewrite developer work.
    try { $existing = [IO.File]::ReadAllText($path, $Utf8) }
    catch { return 'WARNING' }
    if ((Normalize-Text $existing) -ceq (Normalize-Text $Content)) { return 'SKIP' }
    return 'WARNING'
}

function Ensure-Directory {
    param([string]$RelativePath)
    $path = Get-SafePath $RelativePath
    if (Test-Path -LiteralPath $path -PathType Container) { return }
    if (Test-Path -LiteralPath $path) { throw "A file occupies a planned directory: $RelativePath" }
    [IO.Directory]::CreateDirectory($path) | Out-Null
    Write-Step 'CREATE' $RelativePath
}

function Ensure-File {
    param([string]$RelativePath, [string]$Content)
    $state = Get-FileState $RelativePath $Content
    if ($state -eq 'WARNING') {
        Write-Step 'WARNING' "$RelativePath differs; left unchanged."
        throw 'A file changed after preflight. Inspect it and rerun.'
    }
    if ($state -eq 'CREATE') {
        $path = Get-SafePath $RelativePath
        # CreateNew fails if a file appears concurrently; it never truncates it.
        $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write)
        try {
            $bytes = $Utf8.GetBytes($Content)
            $stream.Write($bytes, 0, $bytes.Length)
        } finally { $stream.Dispose() }
    }
    $States[$RelativePath] = $state
    Write-Step $state $RelativePath
}

# Protected setters let derived domain types control changes without exposing
# unrestricted mutation. No clocks or user services are hidden in Core.
# Callers/derived types supply UTC timestamps; these properties do not enforce UTC.
# Audit and deletion interfaces are omitted: mirroring these base types adds no
# current value. Concurrency is opt-in because not every entity needs a token.
$Files = [ordered]@{
    'ElsheiekhHMS.Core/Common/BaseEntity.cs' = @'
namespace ElsheiekhHMS.Core.Common;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
}
'@
    'ElsheiekhHMS.Core/Common/AuditableEntity.cs' = @'
namespace ElsheiekhHMS.Core.Common;

public abstract class AuditableEntity : BaseEntity
{
    // Supply UTC timestamps and opaque user identifiers at the application boundary.
    public DateTimeOffset CreatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public string? UpdatedBy { get; protected set; }
}
'@
    'ElsheiekhHMS.Core/Common/SoftDeletableEntity.cs' = @'
namespace ElsheiekhHMS.Core.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    // Derived domain behavior must update this metadata together, using UTC.
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }
}
'@
    'ElsheiekhHMS.Core/Interfaces/IHasConcurrencyToken.cs' = @'
namespace ElsheiekhHMS.Core.Interfaces;

/// <summary>
/// Opt-in contract for an opaque concurrency token. Infrastructure will configure
/// persistence and conflict detection later; Core does not interpret token bytes.
/// </summary>
public interface IHasConcurrencyToken
{
    byte[] RowVersion { get; set; }
}
'@
    'ElsheiekhHMS.Core/Exceptions/DomainException.cs' = @'
namespace ElsheiekhHMS.Core.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
'@
    'ElsheiekhHMS.Core/Exceptions/BusinessRuleException.cs' = @'
namespace ElsheiekhHMS.Core.Exceptions;

/// <summary>A domain operation violates a business rule.</summary>
public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException) { }
}
'@
    'ElsheiekhHMS.Core/Exceptions/DomainValidationException.cs' = @'
namespace ElsheiekhHMS.Core.Exceptions;

/// <summary>A value is invalid for the domain, independently of transport validation.</summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }

    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
'@
    'ElsheiekhHMS.Tests/Unit/Domain/Common/BaseEntityTests.cs' = @'
using ElsheiekhHMS.Core.Common;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Common;

public class BaseEntityTests
{
    [Fact]
    public void Derived_entity_can_assign_integer_identity()
    {
        BaseEntity entity = new TestEntity(42);
        Assert.Equal(42, entity.Id);
    }

    private sealed class TestEntity : BaseEntity
    {
        public TestEntity(int id) => Id = id;
    }
}
'@
    'ElsheiekhHMS.Tests/Unit/Domain/Common/AuditableEntityTests.cs' = @'
using ElsheiekhHMS.Core.Common;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Common;

public class AuditableEntityTests
{
    [Fact]
    public void New_entity_has_no_update_or_user_metadata()
    {
        var entity = new TestEntity();
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedAt);
        Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public void Creation_metadata_preserves_supplied_utc_time_and_identifier()
    {
        var createdAt = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var entity = new TestEntity(createdAt, "user-42");
        Assert.Equal(createdAt, entity.CreatedAt);
        Assert.Equal(TimeSpan.Zero, entity.CreatedAt.Offset);
        Assert.Equal("user-42", entity.CreatedBy);
        Assert.Null(entity.UpdatedAt);
    }

    private sealed class TestEntity : AuditableEntity
    {
        public TestEntity() { }

        public TestEntity(DateTimeOffset createdAt, string createdBy)
        {
            CreatedAt = createdAt;
            CreatedBy = createdBy;
        }
    }
}
'@
    'ElsheiekhHMS.Tests/Unit/Domain/Common/SoftDeletableEntityTests.cs' = @'
using ElsheiekhHMS.Core.Common;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Common;

public class SoftDeletableEntityTests
{
    [Fact]
    public void New_entity_is_active_without_deletion_metadata()
    {
        var entity = new TestEntity();
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
        Assert.Null(entity.UpdatedAt);
    }

    private sealed class TestEntity : SoftDeletableEntity { }
}
'@
    'ElsheiekhHMS.Tests/Unit/Domain/Exceptions/DomainExceptionTests.cs' = @'
using ElsheiekhHMS.Core.Exceptions;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Exceptions;

public class DomainExceptionTests
{
    [Theory]
    [InlineData("domain")]
    [InlineData("business")]
    [InlineData("validation")]
    public void Exceptions_preserve_message(string kind)
    {
        const string message = "The domain operation is invalid.";
        DomainException exception = kind switch
        {
            "business" => new BusinessRuleException(message),
            "validation" => new DomainValidationException(message),
            _ => new DomainException(message)
        };
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData("domain")]
    [InlineData("business")]
    [InlineData("validation")]
    public void Exceptions_preserve_original_cause(string kind)
    {
        const string message = "The domain operation is invalid.";
        var cause = new InvalidOperationException("Original cause");
        DomainException exception = kind switch
        {
            "business" => new BusinessRuleException(message, cause),
            "validation" => new DomainValidationException(message, cause),
            _ => new DomainException(message, cause)
        };
        Assert.Equal(message, exception.Message);
        Assert.Same(cause, exception.InnerException);
    }
}
'@
}

try {
    Write-Step 'CHECK' 'Checking Phase 01 before any file creation.'
    if ((Get-Location).Provider.Name -ne 'FileSystem' -or
        [IO.Path]::GetFullPath((Get-Location).Path).TrimEnd([IO.Path]::DirectorySeparatorChar) -ne $Root) {
        throw "Run this script from its repository root: $Root"
    }
    $null = Get-Command dotnet -ErrorAction Stop
    $solutions = @(Get-WorkspaceFiles | Where-Object { $_.Extension -in '.sln', '.slnx' })
    if ($solutions.Count -ne 1 -or $solutions[0].DirectoryName -ne $Root -or
        $solutions[0].BaseName -ne 'ElsheiekhHMS') {
        throw 'Expected one root ElsheiekhHMS.sln or ElsheiekhHMS.slnx, with no nested solutions.'
    }
    $solution = $solutions[0].FullName
    Assert-Architecture
    $membersOutput = @(& dotnet sln $solution list)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to list solution projects.' }
    $members = @($membersOutput | Where-Object { $_.Trim() -match '\.csproj$' } | ForEach-Object {
        [IO.Path]::GetFullPath((Join-Path $Root $_.Trim()))
    } | Sort-Object)
    $expectedMembers = @($ExpectedReferences.Keys | ForEach-Object {
        Get-SafePath "ElsheiekhHMS.$_/ElsheiekhHMS.$_.csproj"
    } | Sort-Object)
    if (($members -join '|') -ne ($expectedMembers -join '|')) {
        throw 'Solution membership does not match the five Phase 01 projects.'
    }
    $tests = Read-Project 'Tests'
    foreach ($requiredPackage in @('xunit', 'xunit.runner.visualstudio', 'Microsoft.NET.Test.Sdk')) {
        if ($tests.SelectNodes("//PackageReference[@Include='$requiredPackage']").Count -ne 1) {
            throw "Existing test infrastructure is missing $requiredPackage. No packages will be installed."
        }
    }
    Write-Step 'PASS' 'Phase 01 prerequisites and test infrastructure.'

    $directories = @('Common', 'Constants', 'Entities', 'Enums', 'Exceptions', 'Interfaces') |
        ForEach-Object { "ElsheiekhHMS.Core/$_" }
    $directories += @('ElsheiekhHMS.Tests/Unit/Domain/Common', 'ElsheiekhHMS.Tests/Unit/Domain/Exceptions')
    # Only preserve otherwise-empty folders; never add speculative constants/enums.
    foreach ($directory in $directories) {
        $path = Get-SafePath $directory
        if ((Test-Path -LiteralPath $path) -and !(Test-Path -LiteralPath $path -PathType Container)) {
            throw "A file occupies a planned directory: $directory"
        }
    }
    foreach ($folder in @('Constants', 'Entities', 'Enums')) {
        $path = Get-SafePath "ElsheiekhHMS.Core/$folder"
        if (!(Test-Path -LiteralPath $path) -or @(Get-ChildItem -LiteralPath $path -Force).Count -eq 0 -or
            (Test-Path -LiteralPath (Join-Path $path '.gitkeep'))) {
            $Files["ElsheiekhHMS.Core/$folder/.gitkeep"] = ''
        }
    }
    # Stable UTF-8 content with a final newline, independent of script checkout EOL.
    foreach ($key in @($Files.Keys)) {
        if ($key.EndsWith('.cs')) { $Files[$key] = (Normalize-Text $Files[$key]).TrimEnd("`n") + "`n" }
    }
    $conflicts = 0
    foreach ($key in $Files.Keys) {
        if ((Get-FileState $key $Files[$key]) -eq 'WARNING') {
            Write-Step 'WARNING' "$key differs; it will not be overwritten."
            $conflicts++
        }
    }
    if ($conflicts -gt 0) {
        throw "$conflicts conflicting file(s). No files were created. Review differences manually before rerunning."
    }

    $frozenFiles = @($solution) + $expectedMembers
    $beforeHashes = @{}
    foreach ($path in $frozenFiles) { $beforeHashes[$path] = Get-ContentHash $path }
    foreach ($directory in $directories) { Ensure-Directory $directory }
    foreach ($key in $Files.Keys) { Ensure-File $key $Files[$key] }
    Assert-Architecture

    Invoke-DotNet -Arguments @('restore', $solution)
    Write-Step 'PASS' 'Restore'
    Invoke-DotNet -Arguments @('build', $solution, '--no-restore')
    Write-Step 'PASS' 'Build (review any warnings above; none are suppressed)'

    $resultsRelative = 'ElsheiekhHMS.Tests/TestResults/Phase02/' + [Guid]::NewGuid().ToString('N')
    Ensure-Directory $resultsRelative
    $resultsPath = Get-SafePath $resultsRelative
    Invoke-DotNet -Arguments @('test', $solution, '--no-build', '--logger', 'trx', '--results-directory', $resultsPath)
    $reports = @(Get-ChildItem -LiteralPath $resultsPath -Filter '*.trx' -File)
    if ($reports.Count -eq 0) { throw 'No fresh TRX report was produced; test discovery is not verified.' }
    $passedNames = @()
    $executed = 0
    foreach ($report in $reports) {
        [xml]$trx = [IO.File]::ReadAllText($report.FullName)
        $counters = $trx.SelectSingleNode('//*[local-name()="ResultSummary"]/*[local-name()="Counters"]')
        if ($null -eq $counters) { throw "Missing test counters: $($report.Name)" }
        if ([int]$counters.GetAttribute('failed') -gt 0 -or [int]$counters.GetAttribute('error') -gt 0) {
            throw "The test report contains failed tests or errors: $($report.Name)"
        }
        $executed += [int]$counters.GetAttribute('executed')
        foreach ($result in $trx.SelectNodes('//*[local-name()="UnitTestResult"]')) {
            if ($result.GetAttribute('outcome') -eq 'Passed') { $passedNames += $result.GetAttribute('testName') }
        }
    }
    if ($executed -eq 0) { throw 'Zero tests executed. Phase 02 requires discovered, executed tests.' }
    $requiredTests = @{
        'Common.BaseEntityTests.Derived_entity_can_assign_integer_identity' = 1
        'Common.AuditableEntityTests.New_entity_has_no_update_or_user_metadata' = 1
        'Common.AuditableEntityTests.Creation_metadata_preserves_supplied_utc_time_and_identifier' = 1
        'Common.SoftDeletableEntityTests.New_entity_is_active_without_deletion_metadata' = 1
        'Exceptions.DomainExceptionTests.Exceptions_preserve_message' = 3
        'Exceptions.DomainExceptionTests.Exceptions_preserve_original_cause' = 3
    }
    foreach ($name in $requiredTests.Keys) {
        $prefix = 'ElsheiekhHMS.Tests.Unit.Domain.' + $name
        if (@($passedNames | Where-Object { $_ -eq $prefix -or $_.StartsWith($prefix + '(') }).Count -ne $requiredTests[$name]) {
            throw "Expected foundation test cases did not all pass: $prefix"
        }
    }
    Write-Step 'PASS' "Unit tests: $executed executed; all 10 foundation cases discovered and passed."
    Assert-Architecture
    foreach ($path in $frozenFiles) {
        if ((Get-ContentHash $path) -ne $beforeHashes[$path]) {
            throw "Frozen solution/project file changed during setup: $path. Inspect manually."
        }
    }

    Write-Host "`n============================================================"
    Write-Host ' PHASE 02 - CORE FOUNDATION SETUP'
    Write-Host '============================================================'
    Write-Step 'PASS' 'Phase 01 prerequisites; Core folders; Core independence; restore; build; unit tests'
    foreach ($key in $States.Keys) { Write-Step $States[$key] $key }
    Write-Step 'PASS' 'Existing solution and project files unchanged; no package or Git changes.'
    Write-Host "Test reports: $resultsRelative"
    Write-Host 'NEXT: Review generated files before committing.'
    Write-Host 'A separate Phase 02 architecture review is still required.'
} catch {
    Write-Step 'FAIL' $_.Exception.Message
    Write-Host 'Setup stopped. Existing files were not overwritten. Inspect any created files before rerunning.'
    exit 1
}
