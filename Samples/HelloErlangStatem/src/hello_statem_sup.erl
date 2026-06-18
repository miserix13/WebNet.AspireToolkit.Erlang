-module(hello_statem_sup).
-behaviour(supervisor).

-export([start_link/0]).
-export([init/1]).

start_link() ->
    supervisor:start_link({local, ?MODULE}, ?MODULE, []).

init([]) ->
    SupFlags = #{
        strategy => one_for_all,
        intensity => 5,
        period => 30
    },
    ChildSpecs = [
        #{
            id => hello_statem_registry,
            start => {hello_statem_registry, start_link, []},
            restart => permanent,
            type => worker
        }
    ],
    {ok, {SupFlags, ChildSpecs}}.
