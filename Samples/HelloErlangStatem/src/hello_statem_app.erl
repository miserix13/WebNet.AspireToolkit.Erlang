-module(hello_statem_app).
-behaviour(application).

-export([start/2, stop/1]).

start(_StartType, _StartArgs) ->
    hello_statem_sup:start_link().

stop(_State) ->
    ok.
