#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine
WORKDIR /src
COPY ["DUBuild/DUBuild.csproj", "DUBuild/"]
RUN dotnet restore "DUBuild/DUBuild.csproj"
COPY . .
WORKDIR "/src/DUBuild"
RUN dotnet build "DUBuild.csproj" -c Release -o /app/build
RUN dotnet publish "DUBuild.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
WORKDIR "/app"
COPY --from=0 /app/publish .