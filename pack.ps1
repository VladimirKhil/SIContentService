param (
    [string]$version = "1.0.0",
    [string]$apikey = ""
)

dotnet pack src\SIContentService.Contract\SIContentService.Contract.csproj
dotnet pack src\SIContentService.Client\SIContentService.Client.csproj
dotnet nuget push bin\SIContentService.Contract\VKhil.SIContentService.Contract.$version.nupkg --api-key $apikey --source https://api.nuget.org/v3/index.json
dotnet nuget push bin\SIContentService.Client\VKhil.SIContentService.Client.$version.nupkg --api-key $apikey --source https://api.nuget.org/v3/index.json