-module(apphost_sup).
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
    
    % Get configuration from environment
    AsrireSocketPath = application:get_env(apphost, aspire_socket_path, undefined),
    AspireToken = application:get_env(apphost, aspire_token, undefined),
    
    ChildSpecs = [
        % JSON-RPC message handler
        #{
            id => apphost_json_rpc,
            start => {apphost_json_rpc, start_link, []},
            restart => permanent,
            type => worker
        },
        % Socket/connection manager
        #{
            id => apphost_socket,
            start => {apphost_socket, start_link, [AsrireSocketPath, AspireToken]},
            restart => permanent,
            type => worker
        },
        % Resource manager
        #{
            id => apphost_resource_mgr,
            start => {apphost_resource_mgr, start_link, []},
            restart => permanent,
            type => worker
        },
        % Main coordinator
        #{
            id => apphost_coordinator,
            start => {apphost_coordinator, start_link, []},
            restart => permanent,
            type => worker
        }
    ],
    
    {ok, {SupFlags, ChildSpecs}}.
