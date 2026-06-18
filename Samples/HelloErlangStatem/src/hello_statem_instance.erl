-module(hello_statem_instance).
-behaviour(gen_statem).

-export([start_link/1, create/1, send_event/2, status/1]).
-export([callback_mode/0, init/1]).
-export([idle/3, processing/3, complete/3, error/3]).

callback_mode() ->
    [state_functions, state_enter].

start_link(Id) ->
    gen_statem:start_link({local, Id}, ?MODULE, Id, []).

create(Id) ->
    gen_statem:call(Id, status).

send_event(Id, Event) ->
    gen_statem:cast(Id, Event).

status(Id) ->
    gen_statem:call(Id, status).

init(Id) ->
    io:format("[FSM ~w] Starting in idle state~n", [Id]),
    hello_statem_registry:register(Id, idle),
    {ok, idle, #{id => Id, start_time => erlang:monotonic_time(millisecond)}}.

idle(enter, _PrevState, Data) ->
    io:format("[FSM ~w] Entered idle state~n", [maps:get(id, Data)]),
    {keep_state, Data};

idle(cast, {start_work, Task}, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Received work task: ~w, transitioning to processing~n", [Id, Task]),
    hello_statem_registry:update_state(Id, processing),
    {next_state, processing, Data#{task => Task, work_start => erlang:monotonic_time(millisecond)}};

idle({call, From}, status, Data) ->
    {keep_state_and_data, [{reply, From, {idle, Data}}]};

idle(EventType, Event, Data) ->
    handle_common(EventType, Event, Data, idle).

processing(enter, _PrevState, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Entered processing state~n", [Id]),
    {keep_state, Data};

processing(cast, work_complete, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Work complete, transitioning to complete~n", [Id]),
    hello_statem_registry:update_state(Id, complete),
    {next_state, complete, Data#{work_end => erlang:monotonic_time(millisecond)}};

processing(cast, work_error, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Work failed, transitioning to error~n", [Id]),
    hello_statem_registry:update_state(Id, error),
    {next_state, error, Data};

processing({call, From}, status, Data) ->
    {keep_state_and_data, [{reply, From, {processing, Data}}]};

processing(EventType, Event, Data) ->
    handle_common(EventType, Event, Data, processing).

complete(enter, _PrevState, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Entered complete state~n", [Id]),
    {keep_state, Data};

complete(cast, reset, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Resetting to idle~n", [Id]),
    hello_statem_registry:update_state(Id, idle),
    {next_state, idle, Data#{task => undefined}};

complete({call, From}, status, Data) ->
    {keep_state_and_data, [{reply, From, {complete, Data}}]};

complete(EventType, Event, Data) ->
    handle_common(EventType, Event, Data, complete).

error(enter, _PrevState, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Entered error state~n", [Id]),
    {keep_state, Data};

error(cast, retry, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Retrying, transitioning to processing~n", [Id]),
    hello_statem_registry:update_state(Id, processing),
    {next_state, processing, Data};

error(cast, reset, Data) ->
    Id = maps:get(id, Data),
    io:format("[FSM ~w] Resetting from error to idle~n", [Id]),
    hello_statem_registry:update_state(Id, idle),
    {next_state, idle, Data#{task => undefined}};

error({call, From}, status, Data) ->
    {keep_state_and_data, [{reply, From, {error, Data}}]};

error(EventType, Event, Data) ->
    handle_common(EventType, Event, Data, error).

handle_common(EventType, _Event, Data, State) ->
    io:format("[FSM ~w] Unexpected event in ~w state~n", [maps:get(id, Data), State]),
    {keep_state, Data}.
