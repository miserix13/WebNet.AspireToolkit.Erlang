@echo off
REM Rebar3 wrapper for HelloErlangPool sample
REM Uses REBAR3_ESCRIPT environment variable or rebar3 from PATH

if not defined REBAR3_ESCRIPT (
    call rebar3 %*
) else (
    if not defined ERTS_HOME (
        set ERTS_HOME=%ERLANG_HOME%
    )
    "%ERTS_HOME%\bin\escript.exe" "%REBAR3_ESCRIPT%" %*
)
