#Requires -Version 7.2
<#
.SYNOPSIS
    Verifies candidate recognizer+dictionary pairs.
.DESCRIPTION
    Checks existence, SHA-256, size, dictionary token count, and expected dictionary class count.
    Does not execute OCR or decode text.
#>
param(
    [string]$ManifestPath = "$PSScriptRoot\paddleocr-recognizer-pair-candidates-manifest.json",
    [string]$PairId = "rapidocr-modelscope-ppocrv5-en-mobile-onnx"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Manifest not found: $ManifestPath"
    exit 1
}

function Resolve-RepoPath([string]$relativePath) {
    $repoRoot = Resolve-Path -Path (Join-Path $PSScriptRoot "..\..\..\..")
    return Join-Path -Path $repoRoot -ChildPath $relativePath
}

function Verify-File([string]$id, [string]$relativePath, [string]$expectedHash, [long]$expectedSize) {
    $path = Resolve-RepoPath $relativePath
    if (-not (Test-Path $path)) {
        Write-Host "$id : MISSING ($path)" -ForegroundColor Red
        return $false
    }

    $actualSize = (Get-Item $path).Length
    if ($expectedSize -gt 0 -and $actualSize -ne $expectedSize) {
        Write-Host "$id : SIZE MISMATCH (expected $expectedSize, got $actualSize)" -ForegroundColor Red
        return $false
    }

    $actualHash = (Get-FileHash -Path $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash.ToLowerInvariant()) {
        Write-Host "$id : HASH MISMATCH (expected $expectedHash, got $actualHash)" -ForegroundColor Red
        return $false
    }

    Write-Host "$id : VERIFIED ($actualSize bytes, SHA-256 $actualHash)" -ForegroundColor Green
    return $true
}

function Get-DictionaryTokenCount([string]$relativePath) {
    $path = Resolve-RepoPath $relativePath
    $text = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($path))
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $lines = $normalized -split "`n", 0
    if ($lines.Length -gt 0 -and $lines[-1].Length -eq 0 -and $normalized.EndsWith("`n")) {
        $lines = $lines[0..($lines.Length - 2)]
    }
    return $lines.Count
}

$manifest = Get-Content -Raw -Path $ManifestPath | ConvertFrom-Json
$pair = @($manifest.pairs | Where-Object { $_.pairId -eq $PairId })[0]
if ($null -eq $pair) {
    Write-Error "Pair not found in manifest: $PairId"
    exit 1
}

$ok = $true
$ok = (Verify-File $pair.recognizerModel.modelId $pair.recognizerModel.localRelativePath $pair.recognizerModel.sha256 ([long]$pair.recognizerModel.fileSizeBytes)) -and $ok
$ok = (Verify-File $pair.dictionary.dictionaryId $pair.dictionary.localRelativePath $pair.dictionary.sha256 ([long]$pair.dictionary.fileSizeBytes)) -and $ok

if ($ok) {
    $tokenCount = Get-DictionaryTokenCount $pair.dictionary.localRelativePath
    if ($tokenCount -ne [int]$pair.dictionary.tokenCount) {
        Write-Host "$($pair.dictionary.dictionaryId) : TOKEN COUNT MISMATCH (expected $($pair.dictionary.tokenCount), got $tokenCount)" -ForegroundColor Red
        $ok = $false
    } else {
        Write-Host "$($pair.dictionary.dictionaryId) : TOKEN COUNT VERIFIED ($tokenCount)" -ForegroundColor Green
    }

    $expectedClassCount = [int]$pair.dictionary.tokenCount + 1
    if ($expectedClassCount -ne [int]$pair.dictionary.expectedClassCount -or [int]$pair.recognizerModel.expectedOutputClassCount -ne $expectedClassCount) {
        Write-Host "$PairId : CLASS COUNT MISMATCH (dictionary+blank=$expectedClassCount, dictionary expected=$($pair.dictionary.expectedClassCount), model expected=$($pair.recognizerModel.expectedOutputClassCount))" -ForegroundColor Red
        $ok = $false
    } else {
        Write-Host "$PairId : CLASS COUNT VERIFIED ($expectedClassCount = $tokenCount + CTC blank index $($pair.dictionary.ctcBlankIndex))" -ForegroundColor Green
    }
}

if (-not $ok) {
    Write-Error "Recognizer pair verification failed."
    exit 1
}

Write-Host "Recognizer pair verified: $PairId"
