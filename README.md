# SIContentService
A service for uploading and retrieving SIGame content

The service provides API for uploading packages and avatars. These objects are refernced by Uri and could be shared with other users.

All uploaded to service objects are deleted if not used for some time.

The project provides two service clients: one for .NET (available as NuGet package) and one for web (available as NPM package).