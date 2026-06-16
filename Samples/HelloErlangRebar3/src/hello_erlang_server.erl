-module(hello_erlang_server).
-behaviour(gen_server).

-export([start_link/0, ping/0]).
-export([init/1, handle_call/3, handle_cast/2, handle_info/2, terminate/2, code_change/3]).

-define(SERVER, ?MODULE).

start_link() ->
    gen_server:start_link({local, ?SERVER}, ?MODULE, [], []).

ping() ->
    gen_server:call(?SERVER, ping).

init([]) ->
    io:format("hello_erlang_server started.~n", []),
    erlang:send_after(timer:seconds(5), self(), heartbeat),
    {ok, #{heartbeat_count => 0}}.

handle_call(ping, _From, State) ->
    {reply, {pong, maps:get(heartbeat_count, State, 0)}, State};
handle_call(_Request, _From, State) ->
    {reply, ok, State}.

handle_cast(_Message, State) ->
    {noreply, State}.

handle_info(heartbeat, State) ->
    Count = maps:get(heartbeat_count, State, 0) + 1,
    io:format("hello_erlang_server heartbeat ~p.~n", [Count]),
    erlang:send_after(timer:seconds(5), self(), heartbeat),
    {noreply, State#{heartbeat_count => Count}};
handle_info(_Info, State) ->
    {noreply, State}.

terminate(_Reason, _State) ->
    ok.

code_change(_OldVsn, State, _Extra) ->
    {ok, State}.
