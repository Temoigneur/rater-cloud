# Use the official .NET runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Rater.csproj", "."]
COPY ["SharedModels/SharedModels.csproj", "SharedModels/"]
RUN dotnet restore "Rater.csproj"

# Copy all source files
COPY . .
RUN dotnet build "Rater.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rater.csproj" -c Release -o /app/publish

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:$PORT
ENTRYPOINT ["dotnet", "Rater.dll"]