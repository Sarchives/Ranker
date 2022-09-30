FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy source code and build
COPY ./ ./
RUN dotnet restore
RUN dotnet build -c Release -o out ./Ranker/Ranker.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY Ranker/Fonts .
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Ranker.dll"]
ENV PORT 8080