param (
    [string]$version = "1.0.0",
    [string]$apikey = ""
)

dotnet pack src\SIContentService.Contract\SIContentService.Contract.csproj -c Release /property:Version=$version
dotnet pack src\SIContentService.Client\SIContentService.Client.csproj -c Release /property:Version=$version
dotnet nuget push bin\.Release\SIContentService.Contract\VKhil.SIContentService.Contract.$version.nupkg --api-key $apikey --source https://api.nuget.org/v3/index.json
dotnet nuget push bin\.Release\SIContentService.Client\VKhil.SIContentService.Client.$version.nupkg --api-key $apikey --source https://api.nuget.org/v3/index.json