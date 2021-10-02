FROM mcr.microsoft.com/dotnet/sdk:5.0.401 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Ranker/*.csproj ./
RUN dotnet restore

# Copy source code and build
COPY Ranker ./
RUN dotnet build -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0.10-alpine3.13
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Ranker.dll"]