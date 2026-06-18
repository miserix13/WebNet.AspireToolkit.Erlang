-module(hello_statem_registry).
-behaviour(gen_server).

-export([start_link/0, register/2, update_state/2, list_instances/0]).
-export([init/1, handle_call/3, handle_cast/2, handle_info/2, terminate/2, code_change/3]).

-record(state, {
    instances = #{}
}).

start_link() ->
    gen_server:start_link({local, ?MODULE}, ?MODULE, [], []).

register(Id, InitialState) ->
    gen_server:cast(?MODULE, {register, Id, InitialState}).

update_state(Id, NewState) ->
    gen_server:cast(?MODULE, {update_state, Id, NewState}).

list_instances() ->
    gen_server:call(?MODULE, list_instances).

init([]) ->
    io:format("[Registry] Starting state machine registry~n"),
    {ok, #state{}}.

handle_call(list_instances, _From, State) ->
    Instances = maps:to_list(State#state.instances),
    {reply, Instances, State};

handle_call(_Request, _From, State) ->
    {reply, ok, State}.

handle_cast({register, Id, InitialState}, State) ->
    io:format("[Registry] Registered FSM instance ~w in state ~w~n", [Id, InitialState]),
    NewInstances = maps:put(Id, #{state => InitialState, registered_at => erlang:monotonic_time(millisecond)}, State#state.instances),
    {noreply, State#state{instances = NewInstances}};

handle_cast({update_state, Id, NewState}, State) ->
    Instances = State#state.instances,
    case maps:get(Id, Instances, undefined) of
        undefined ->
            {noreply, State};
        Instance ->
            io:format("[Registry] FSM instance ~w transitioned to ~w~n", [Id, NewState]),
            UpdatedInstance = Instance#{state => NewState, last_transition => erlang:monotonic_time(millisecond)},
            NewInstances = maps:put(Id, UpdatedInstance, Instances),
            {noreply, State#state{instances = NewInstances}}
    end;

handle_cast(_Msg, State) ->
    {noreply, State}.

handle_info(_Info, State) ->
    {noreply, State}.

terminate(_Reason, _State) ->
    ok.

code_change(_OldVsn, State, _Extra) ->
    {ok, State}.
