FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /app

COPY ./src/OK.Bitter.Common/. ./OK.Bitter.Common/.
COPY ./src/OK.Bitter.Core/. ./OK.Bitter.Core/.
COPY ./src/OK.Bitter.DataAccess/. ./OK.Bitter.DataAccess/.
COPY ./src/OK.Bitter.Engine/. ./OK.Bitter.Engine/.
COPY ./src/OK.Bitter.Services/. ./OK.Bitter.Services/.
COPY ./src/OK.Bitter.Api/. ./OK.Bitter.Api/.

WORKDIR /app/OK.Bitter.Api
RUN dotnet restore

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime
WORKDIR /app
COPY --from=build /app/OK.Bitter.Api/out ./
ENTRYPOINT ["dotnet", "OK.Bitter.Api.dll"]