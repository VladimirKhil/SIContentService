# SIContentService
A service for uploading and retrieving SIGame content.

The service provides API for uploading packages and avatars. These objects are refernced by Uri and could be shared with other users.

All uploaded to service objects are deleted if not used for some time.

The project provides two service clients: one for .NET (available as NuGet package) and one for web (available as NPM package).

There are reasons for using this service instead of regular CDN/S3 service:

- it supports uploading and automatic extracting ZIP archives (SIGame packages) while using safe file names
- content.xml package file required for Game server only (it contains questions and answers) is provided only for authorized users
- it supports automatic clearing of unused content

# Build

    dotnet build

# Run

## Docker


    docker run -p 5000:5000 vladimirkhil/sicontentservice:1.0.15


## Helm


    dependencies:
    - name: sicontent
      version: "1.0.10"
      repository: "https://vladimirkhil.github.io/SIContentService/helm/repo"

### Parameters

| Name | Description | Value |
| ----------- | ----------- | ----------- |
| `image.repository` | Image repository | `vladimirkhil/sicontentservice` |
| `image.pullPolicy` | Image pull policy | `IfNotPresent` |
| `image.tag` | Image tag | `1.0.15` |
| `image.nginxTag` | Image Nginx tag | `alpine` |
| `volumePath` | Path for storing data | ` ` |
| `logPath` | Path for storing logs | ` ` |
| `nginxLogPath` | Path for storing Nginx logs | ` ` |
| `maxUploadSize` | Maximum upload file size | `101m` |
| `options.options.maxPackageSizeMb` | Maximum package size in megabytes | `100` |
| `options.options.maxAvatarizeMb` | Maximum avatar size in megabytes | `1` |
| `options.options.minDriveFreeSpaceMb` | Minimum drive free space for working normally (otherwise maximum allowed file size is cut by half) | `7000` |
| `options.options.minDriveCriticalSpaceMb` | Minimum drive free space for working | `2000` |
| `options.options.maxPackageLifetime` | Maximum package lifetime | `03:00:00` |
| `options.options.maxAvatarLifetime` | Maximum avatar lifetime | `04:00:00` |
| `options.options.cleaningInterval` | Garbage collector cleaning interval | `00:30:00` |
| `options.options.serveStaticFiles` | Serve static files from service (otherwise - from Nginx) | `false` |
| `options.options.logLevel` | Logging level | `Warning` |