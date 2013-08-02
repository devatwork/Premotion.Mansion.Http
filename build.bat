@echo Off
set config=%1
if "%config%" == "" (
	set config=Release
)

set version=
if not "%PackageVersion%" == "" (
   set version=-Version %PackageVersion%
)

set nuget=
if "%nuget%" == "" (
    set nuget=.nuget\NuGet.exe
)

set apikey=
if "%apikey%" == "" (
    set apikey=%2
)

rem Update self %nuget%
%nuget% update -self

rem Set API key
%nuget% setapikey %apikey% -Source "https://www.myget.org/F/premotion/api/v2/package"
%nuget% setapikey %apikey% -Source "https://nuget.symbolsource.org/MyGet/premotion"

rem Build
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild Premotion.Mansion.Http.sln /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false

rem Package
%nuget% pack Premotion.Mansion.Http\Premotion.Mansion.Http.csproj -sym -Prop Configuration=Release

rem Publish
%nuget% push Premotion.Mansion.Http.0.1.0.nupkg -Source "https://www.myget.org/F/premotion/api/v2/package"
%nuget% push Premotion.Mansion.Http.0.1.0.symbols.nupkg -Source "https://nuget.symbolsource.org/MyGet/premotion"