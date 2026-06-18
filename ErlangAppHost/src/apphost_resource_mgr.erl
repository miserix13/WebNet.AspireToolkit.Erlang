-module(apphost_resource_mgr).
-behaviour(gen_server).

-export([start_link/0, register_resource/3, get_resource/1, list_resources/0]).
-export([init/1, handle_call/3, handle_cast/2, handle_info/2, terminate/2, code_change/3]).

-record(state, {
    resources = #{}  % Map of resource_name -> {type, handle, config}
}).

start_link() ->
    gen_server:start_link({local, ?MODULE}, ?MODULE, [], []).

%% @doc Register a resource with Aspire
register_resource(Name, Type, Config) ->
    gen_server:call(?MODULE, {register_resource, Name, Type, Config}).

%% @doc Get a registered resource
get_resource(Name) ->
    gen_server:call(?MODULE, {get_resource, Name}).

%% @doc List all registered resources
list_resources() ->
    gen_server:call(?MODULE, list_resources).

init([]) ->
    io:format("[ResourceMgr] Starting resource manager~n"),
    {ok, #state{}}.

handle_call({register_resource, Name, Type, Config}, _From, State) ->
    io:format("[ResourceMgr] Registering resource: ~w (type: ~w)~n", [Name, Type]),
    
    % In production, this would:
    % 1. Invoke Aspire capability to register the resource
    % 2. Get back a handle
    % 3. Store the handle and config
    
    NewResources = maps:put(Name, {Type, undefined, Config}, State#state.resources),
    {reply, {ok, Name}, State#state{resources = NewResources}};

handle_call({get_resource, Name}, _From, State) ->
    case maps:get(Name, State#state.resources, undefined) of
        undefined ->
            {reply, {error, not_found}, State};
        Resource ->
            {reply, {ok, Resource}, State}
    end;

handle_call(list_resources, _From, State) ->
    Resources = maps:to_list(State#state.resources),
    {reply, Resources, State};

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
