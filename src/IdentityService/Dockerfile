FROM mcr.microsoft.com/dotnet/sdk:7.0 as build
WORKDIR /app
EXPOSE 80

# copy all .csproj files and restore as distinct layers.   Use of the same COPY command
# for every dockerfile in the project to take advantage of docker caching
COPY winslowoverthink.sln winslowoverthink.sln
COPY src/MediaService/MediaService.csproj src/MediaService/MediaService.csproj
COPY src/DashboardService/DashboardService.csproj src/DashboardService/DashboardService.csproj
COPY src/SearchService/SearchService.csproj src/SearchService/SearchService.csproj
COPY src/GatewayService/GatewayService.csproj src/GatewayService/GatewayService.csproj
COPY src/Contracts/Contracts.csproj src/Contracts/Contracts.csproj
COPY src/IdentityService/IdentityService.csproj src/IdentityService/IdentityService.csproj
COPY src/DaemonAtorService/DaemonAtorService.csproj src/DaemonAtorService/DaemonAtorService.csproj
COPY src/WinslowUI/WinslowUI.csproj src/WinslowUI/WinslowUI.csproj
COPY tests/mediaService.UnitTests/mediaService.UnitTests.csproj tests/mediaService.UnitTests/mediaService.UnitTests.csproj
COPY tests/VideoFileService.IntegrationTests/VideoFileService.IntegrationTests.csproj tests/VideoFileService.IntegrationTests/VideoFileService.IntegrationTests.csproj

# Restore package deps
RUN dotnet restore winslowoverthink.sln

# Copy the app folders over
COPY src/IdentityService src/IdentityService
WORKDIR /app/src/IdentityService
RUN dotnet publish -c Release -o /app/src/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/src/out .
ENTRYPOINT [ "dotnet", "IdentityService.dll" ]