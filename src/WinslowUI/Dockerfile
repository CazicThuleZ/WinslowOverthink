FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

# copy all .csproj files and restore as distinct layers.   Use of the same COPY command
# for every dockerfile in the project to take advantage of docker caching
COPY winslowoverthink.sln winslowoverthink.sln
COPY src/MediaService/MediaService.csproj src/MediaService/MediaService.csproj
COPY src/SearchService/SearchService.csproj src/SearchService/SearchService.csproj
COPY src/GatewayService/GatewayService.csproj src/GatewayService/GatewayService.csproj
COPY src/Contracts/Contracts.csproj src/Contracts/Contracts.csproj
COPY src/IdentityService/IdentityService.csproj src/IdentityService/IdentityService.csproj
COPY src/DaemonDo/DaemonDo.csproj src/DaemonDo/DaemonDo.csproj
COPY src/WinslowUI/WinslowUI.csproj src/WinslowUI/WinslowUI.csproj
COPY tests/mediaService.UnitTests/mediaService.UnitTests.csproj tests/mediaService.UnitTests/mediaService.UnitTests.csproj
COPY tests/VideoFileService.IntegrationTests/VideoFileService.IntegrationTests.csproj tests/VideoFileService.IntegrationTests/VideoFileService.IntegrationTests.csproj

# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/WinslowUI/WinslowUI.csproj"
RUN dotnet publish "src/WinslowUI/WinslowUI.csproj" -c Release -o /app/out

# Stage 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "WinslowUI.dll"]
