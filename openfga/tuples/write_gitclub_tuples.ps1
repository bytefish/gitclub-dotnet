<# Licensed under the MIT license. See LICENSE file in the project root for full license information.#>

# The Path to the OpenFGA CLI.
$fgaCliExecutable = "${PSScriptRoot}\..\tools\fga.exe"

# CLI Command to execute.
$fgaCliCmd = "${fgaCliExecutable} tuple write --store-id=${env:OpenFGA__StoreId} --file tuples.json"

# Invoke the CLI Command.
$fgaCliCmdResponse = Invoke-Expression $fgaCliCmd

# Output the Response.
Write-Output $fgaCliCmdResponse