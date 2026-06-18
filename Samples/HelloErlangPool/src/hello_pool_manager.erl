-module(hello_pool_manager).
-behaviour(gen_server).

-export([start_link/0, spawn_task/1, pool_status/0]).
-export([init/1, handle_call/3, handle_cast/2, handle_info/2, terminate/2, code_change/3]).

-define(POOL_SIZE, 3).

-record(state, {
    workers = [],
    tasks = 0,
    completed = 0
}).

start_link() ->
    gen_server:start_link({local, ?MODULE}, ?MODULE, [], []).

spawn_task(Task) ->
    gen_server:call(?MODULE, {spawn_task, Task}).

pool_status() ->
    gen_server:call(?MODULE, status).

init([]) ->
    io:format("[Pool Manager] Starting with pool size ~w~n", [?POOL_SIZE]),
    Workers = spawn_workers(?POOL_SIZE),
    {ok, #state{workers = Workers}}.

handle_call({spawn_task, Task}, _From, State) ->
    case State#state.workers of
        [Worker | Rest] ->
            Worker ! {task, Task, self()},
            NewState = State#state{
                workers = Rest,
                tasks = State#state.tasks + 1
            },
            {reply, ok, NewState};
        [] ->
            {reply, {error, pool_exhausted}, State}
    end;

handle_call(status, _From, State) ->
    Status = {
        available_workers, length(State#state.workers),
        total_tasks_dispatched, State#state.tasks,
        completed_tasks, State#state.completed
    },
    {reply, Status, State};

handle_call(_Request, _From, State) ->
    {reply, ok, State}.

handle_cast(_Msg, State) ->
    {noreply, State}.

handle_info({worker_done, _WorkerId, _Result}, State) ->
    NewState = State#state{
        completed = State#state.completed + 1,
        workers = [spawn_worker() | State#state.workers]
    },
    {noreply, NewState};

handle_info(_Info, State) ->
    {noreply, State}.

terminate(_Reason, _State) ->
    ok.

code_change(_OldVsn, State, _Extra) ->
    {ok, State}.

spawn_workers(0) -> [];
spawn_workers(N) ->
    [spawn_worker() | spawn_workers(N - 1)].

spawn_worker() ->
    spawn(fun worker_loop/0).

worker_loop() ->
    receive
        {task, Task, Manager} ->
            io:format("[Worker ~w] Processing task: ~w~n", [self(), Task]),
            timer:sleep(500),
            io:format("[Worker ~w] Task complete~n", [self()]),
            Manager ! {worker_done, self(), ok},
            worker_loop();
        stop ->
            ok
    end.
