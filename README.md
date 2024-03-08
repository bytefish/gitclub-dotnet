# GitClub with .NET and OpenFGA #

This is an example application based on GitHub, that's meant to model GitHub's permissions system using OpenFGA.

It's modeled after the GitClub example provided by the Oso team at:

* [https://github.com/osohq/gitclub](https://github.com/osohq/gitclub)

### Creating the OpenFGA Store ###
To get started, we need to create a Store. This is done using the FGA CLI, which has been added to the repository 
at `tools/fga_0.2.1_windows_amd64.tar.gz`. You can create the Store by running the `createFgaStore.ps1` Powershell 
script, which will output the Store ID, Authorization Model ID and the full JSON response:

```powershell
PS > .\createFgaStore.ps1
OpenFGA StoreId:                  01HJ8S5C3R7TKXPSP9N5HTDPTP
OpenFGA AuthorizationModelId:     01HJ8S5C46YMQRCC7Z1MHHJMFR
JSON Response:
{
  "store": {
    "created_at": "2023-12-22T12:49:18.968564Z",
    "id": "01HJ8S5C3R7TKXPSP9N5HTDPTP",
    "name": "Task Management Application",
    "updated_at": "2023-12-22T12:49:18.968564Z"
  },
  "model": {
    "authorization_model_id": "01HJ8S5C46YMQRCC7Z1MHHJMFR"
  }
}
```

We can see the StoreID being written to the Environment variable:

```powershell
PS C:\Users\philipp\source\repos\bytefish\OpenFgaExperiments> $env:OpenFGA__StoreId
01HJ8S5C3R7TKXPSP9N5HTDPTP
PS C:\Users\philipp\source\repos\bytefish\OpenFgaExperiments> $env:OpenFGA__AuthorizationModelId
01HJ8S5C46YMQRCC7Z1MHHJMFR
```

If you want to create the Store without Powershell run:

```powershell
./fga store create --name "Task Management Application" --model "src\Server\RebacExperiments.Server.Api\Resources\task-management-model.fga"
```

Set the `OpenFGA:StoreId` and `OpenFga:AuthorizationModelId` in the `appsettings.json` of the Backend, or set the 
Environment Variables `OpenFGA__StoreId` and `OpenFGA__AuthorizationModelId`.