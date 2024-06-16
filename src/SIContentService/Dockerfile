FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/SIContentService/SIContentService.csproj", "src/SIContentService/"]
COPY ["src/SIContentService.Contract/SIContentService.Contract.csproj", "src/SIContentService.Contract/"]
RUN dotnet restore "src/SIContentService/SIContentService.csproj" -r linux-musl-x64
COPY ["src/", "src/"]
WORKDIR "/src/src/SIContentService"
RUN dotnet publish "SIContentService.csproj" -p:PublishSingleFile=true -r linux-musl-x64 --self-contained true -p:PublishTrimmed=true -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["./SIContentService"]