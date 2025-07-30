#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BuildBazaarCore/BuildBazaarCore.csproj", "BuildBazaarCore/"]
RUN dotnet restore "./BuildBazaarCore/BuildBazaarCore.csproj"
COPY . .
WORKDIR "/src/BuildBazaarCore"
RUN dotnet build "./BuildBazaarCore.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BuildBazaarCore.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

################ THIS IS FOR NVIM DEBUGGING ################
# ARG INCLUDE_DEBUGGER=false
# RUN if ["$INCLUDE_DEBUGGER" = "true"];
# 	COPY ["BuildBazaarCore/netcoredbg", "/usr/bin/netcoredbg"]
# 	RUN ln -s /usr/bin/netcoredbg/netcoredbg /user/bin/netcoredbg
# fi
################ THIS IS FOR NVIM DEBUGGING ################

ENTRYPOINT ["dotnet", "BuildBazaarCore.dll"]

COPY ["BuildBazaarCore/wwwroot/media/thumbnails", "/app/wwwroot/media/thumbnails"]
COPY ["BuildBazaarCore/ExampleBuild", "/app/ExampleBuild"]
COPY ["BuildBazaarCore/Certificates", "/app/Certificates"]

