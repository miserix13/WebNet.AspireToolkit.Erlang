-module(hello_erlang_sup).
-behaviour(supervisor).

-export([start_link/0]).
-export([init/1]).

start_link() ->
    supervisor:start_link({local, ?MODULE}, ?MODULE, []).

init([]) ->
    Server = #{
        id => hello_erlang_server,
        start => {hello_erlang_server, start_link, []},
        restart => permanent,
        shutdown => 5000,
        type => worker,
        modules => [hello_erlang_server]
    },
    {ok, {{one_for_one, 5, 10}, [Server]}}.
