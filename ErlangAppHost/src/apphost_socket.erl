-module(apphost_socket).
-behaviour(gen_server).

-export([start_link/2, send_json/1, recv_json_response/2, is_connected/0]).
-export([init/1, handle_call/3, handle_cast/2, handle_info/2, terminate/2, code_change/3]).

-record(state, {
    socket = undefined,
    connected = false,
    socket_path = undefined,
    auth_token = undefined,
    pending_responses = #{}  % Map of request_id -> response
}).

start_link(SocketPath, AuthToken) ->
    gen_server:start_link({local, ?MODULE}, ?MODULE, {SocketPath, AuthToken}, []).

%% @doc Send JSON data to socket
send_json(JsonData) ->
    gen_server:call(?MODULE, {send_json, JsonData}, 5000).

%% @doc Receive a JSON-RPC response for a specific request ID
recv_json_response(RequestId, Timeout) ->
    gen_server:call(?MODULE, {recv_response, RequestId}, Timeout).

%% @doc Check if connected to Aspire
is_connected() ->
    gen_server:call(?MODULE, is_connected).

init({SocketPath, AuthToken}) ->
    io:format("[Socket] Initializing socket client~n"),
    io:format("[Socket] Socket path: ~p~n", [SocketPath]),
    
    State = #state{
        socket_path = SocketPath,
        auth_token = AuthToken
    },
    
    % Try to connect asynchronously
    self() ! connect,
    {ok, State}.

handle_call({send_json, JsonData}, _From, State) ->
    case State#state.connected of
        false ->
            {reply, {error, not_connected}, State};
        true ->
            case send_to_socket(State#state.socket, JsonData) of
                ok ->
                    {reply, ok, State};
                {error, Reason} ->
                    io:format("[Socket] Send error: ~p~n", [Reason]),
                    {reply, {error, Reason}, State#state{connected = false}}
            end
    end;

handle_call({recv_response, RequestId}, From, State) ->
    % Check if response already received
    case maps:get(RequestId, State#state.pending_responses, undefined) of
        undefined ->
            % Response not ready yet - in real impl, wait for it
            % For now, return error
            {reply, {error, no_response_yet}, State};
        Response ->
            NewResponses = maps:remove(RequestId, State#state.pending_responses),
            {reply, Response, State#state{pending_responses = NewResponses}}
    end;

handle_call(is_connected, _From, State) ->
    {reply, State#state.connected, State};

handle_call(_Request, _From, State) ->
    {reply, ok, State}.

handle_cast(_Msg, State) ->
    {noreply, State}.

handle_info(connect, State) ->
    io:format("[Socket] Attempting to connect to Aspire~n"),
    case connect_to_aspire(State#state.socket_path, State#state.auth_token) of
        {ok, Socket} ->
            io:format("[Socket] Connected to Aspire~n"),
            {noreply, State#state{socket = Socket, connected = true}};
        {error, Reason} ->
            io:format("[Socket] Connection failed: ~p. Retrying in 5s~n", [Reason]),
            erlang:send_after(5000, self(), connect),
            {noreply, State#state{connected = false}}
    end;

handle_info(_Info, State) ->
    {noreply, State}.

terminate(_Reason, State) ->
    case State#state.socket of
        undefined -> ok;
        Socket -> catch gen_tcp:close(Socket)
    end,
    ok.

code_change(_OldVsn, State, _Extra) ->
    {ok, State}.

%% Private functions

connect_to_aspire(SocketPath, AuthToken) when SocketPath /= undefined, AuthToken /= undefined ->
    % For now, this is a placeholder
    % In production, we would:
    % 1. Connect to the socket (named pipe on Windows, Unix socket on Linux)
    % 2. Send authentication token
    % 3. Establish JSON-RPC connection
    io:format("[Socket] Would connect to: ~p with token: [REDACTED]~n", [SocketPath]),
    {error, not_implemented};

connect_to_aspire(_, _) ->
    {error, missing_config}.

send_to_socket(_Socket, _JsonData) ->
    % Placeholder for actual socket send
    {error, not_implemented}.
