@echo off
setlocal

if defined REBAR3_ESCRIPT goto run_escript

where rebar3 >nul 2>nul
if not errorlevel 1 (
    rebar3 %*
    exit /b %ERRORLEVEL%
)

echo rebar3 was not found on PATH. Set REBAR3_ESCRIPT to a local rebar3 escript file or install rebar3. 1>&2
exit /b 1

:run_escript
if not defined ERTS_HOME if defined ERLANG_HOME set "ERTS_HOME=%ERLANG_HOME%"
if not defined ERTS_HOME (
    echo ERTS_HOME or ERLANG_HOME must point to your Erlang installation root. 1>&2
    exit /b 1
)

set "ESCRIPT_EXE=%ERTS_HOME%\bin\escript.exe"
if not exist "%ESCRIPT_EXE%" (
    echo Could not find escript.exe at "%ESCRIPT_EXE%". 1>&2
    exit /b 1
)

"%ESCRIPT_EXE%" "%REBAR3_ESCRIPT%" %*
