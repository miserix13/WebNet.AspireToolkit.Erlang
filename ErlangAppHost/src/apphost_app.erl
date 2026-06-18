-module(apphost_app).
-behaviour(application).

-export([start/2, stop/1]).

start(_StartType, _StartArgs) ->
    io:format("[AppHost] Starting Erlang AppHost application~n"),
    apphost_sup:start_link().

stop(_State) ->
    io:format("[AppHost] Stopping Erlang AppHost application~n"),
    ok.
