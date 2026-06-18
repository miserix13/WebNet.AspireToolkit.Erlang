-module(apphost_coordinator).
-behaviour(gen_server).

-export([start_link/0, orchestrate/1, status/0]).
-export([init/1, handle_call/3, handle_cast/2, handle_info/2, terminate/2, code_change/3]).

-record(state, {
    orchestration_state = idle,  % idle, running, stopped, error
    resource_graph = [],         % Topologically sorted resource list
    active_resources = #{}       % Map of resource_name -> pid
}).

start_link() ->
    gen_server:start_link({local, ?MODULE}, ?MODULE, [], []).

%% @doc Start orchestration with a resource configuration
orchestrate(ResourceConfig) ->
    gen_server:call(?MODULE, {orchestrate, ResourceConfig}).

%% @doc Get current orchestration status
status() ->
    gen_server:call(?MODULE, status).

init([]) ->
    io:format("[Coordinator] Starting AppHost coordinator~n"),
    {ok, #state{}}.

handle_call({orchestrate, ResourceConfig}, _From, State) ->
    io:format("[Coordinator] Starting orchestration with ~w resources~n", [length(ResourceConfig)]),
    
    % In production, this would:
    % 1. Parse resource config
    % 2. Build dependency graph
    % 3. Topologically sort resources
    % 4. Invoke Aspire capabilities to register resources
    % 5. Monitor resource state changes
    
    {reply, {ok, starting}, State#state{orchestration_state = running}};

handle_call(status, _From, State) ->
    {reply, {
        status, 
        State#state.orchestration_state,
        maps:size(State#state.active_resources)
    }, State};

handle_call(_Request, _From, State) ->
    {reply, ok, State}.

handle_cast(_Msg, State) ->
    {noreply, State}.

handle_info(_Info, State) ->
    {noreply, State}.

terminate(_Reason, _State) ->
    ok.

code_change(_OldVsn, State, _Extra) ->
    {ok, State}.
