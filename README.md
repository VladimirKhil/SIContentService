# SIContentService
A service for uploading and retrieving SIGame content.

The service provides API for uploading packages and avatars. These objects are refernced by Uri and could be shared with other users.

All uploaded to service objects are deleted if not used for some time.

The project provides two service clients: one for .NET (available as NuGet package) and one for web (available as NPM package).

There are reasons for using this service instead of regular CDN/S3 service:

- it supports uploading and automatic extracting ZIP archives (SIGame packages) while using safe file names
- context.xml package file required for Game server only (it contains questions and answers) is provided only for authorized users
- it supports automatic clearing of unused content