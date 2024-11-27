FROM gca-mcr-remote.artifactory.astralinux.ru/dotnet/sdk:6.0 AS build-env

ARG NUGET_USERNAME \
    NUGET_PASSWORD \
    PROJ_ENV
ENV NUGET_USERNAME=$NUGET_USERNAME \
    NUGET_PASSWORD=$NUGET_PASSWORD \
    PROJ_ENV=$PROJ_ENV

WORKDIR /app

COPY . .
# Add source

RUN dotnet nuget add source https://artifactory.astralinux.ru/artifactory/api/nuget/v3/dit-nuget/index.json -n Artifactory -u $NUGET_USERNAME --valid-authentication-types basic --store-password-in-clear-text -p $NUGET_PASSWORD
RUN dotnet nuget list source

# Restore as distinct layers
# -v d
RUN dotnet restore 

# Build and publish a release
RUN dotnet dev-certs https
# Set up an Environment according to variable
RUN dotnet build --configuration $PROJ_ENV --output out

RUN ls -al /app/out

# Build runtime image
FROM gca-mcr-remote.artifactory.astralinux.ru/dotnet/aspnet:6.0

ENV USER_NAME=dotnet-executer

WORKDIR /app

RUN export ASPNETCORE_ENVIRONMENT=$PROJ_ENV

COPY --from=build-env /app/out .
COPY --from=build-env /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/

RUN ls -al /app

ENTRYPOINT ["dotnet", "SearchPRBot.dll"]
