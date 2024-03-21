<# Licensed under the MIT license. See LICENSE file in the project root for full license information.#>

# The Path to the OpenFGA CLI, so we don't have to put it into the 
# global PATH. This makes it easier to just download a new version 
# and point the script at it.
$fgaCliExecutable = "${PSScriptRoot}\tools\fga.exe"

# The OpenFGA Model to create the Store for.
$fgaModelFilename = "${PSScriptRoot}\model\github.fga"

# The Transform Command to transform from FGA to JSON
$fgaCreateStoreCmd = "${fgaCliExecutable} store create --name ""GitClub Application"" --model ${fgaModelFilename}"

# Run the Transform Command, Pretty Print Results, Write to Output File
$fgaCreateStoreResponse = Invoke-Expression $fgaCreateStoreCmd

# Transform Response to a PSObject ...
$fgaStore = $fgaCreateStoreResponse | ConvertFrom-Json

# ... extract the required Store IDs
$fgaStoreId = $fgaStore.store.id
$fgaAuthorizationModelId = $fgaStore.model.authorization_model_id

# ... write them to the "OpenFGA__..." environment variables
$env:OpenFGA__StoreId=$fgaStoreId
$env:OpenFGA__AuthorizationModelId=$fgaAuthorizationModelId

# ... and output the StoreID for copy and pasting it somewhere else
Write-Output "OpenFGA StoreId:                  ${fgaStoreId}"
Write-Output "OpenFGA AuthorizationModelId:     ${fgaAuthorizationModelId}"

# ... and output the raw JSON response for additional information
Write-Output "JSON Response:"
Write-Output $fgaCreateStoreResponse