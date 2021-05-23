FROM microsoft/dotnet:2.1-sdk AS build
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

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=build /app/OK.Bitter.Api/out ./
ENTRYPOINT ["dotnet", "OK.Bitter.Api.dll"]